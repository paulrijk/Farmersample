using System.Net;
using System.Text;
using FarmerSample.Core.Modules.SupplyQuality;
using FarmerSample.Core.Modules.SupplyQuality.Contracts;
using FarmerSample.Core.Persistence;
using FarmerSample.Core.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace FarmerSample.IntegrationTests;

public class LotAndPhotoTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private async Task<T> InScope<T>(Func<IServiceProvider, Task<T>> work)
    {
        using var scope = api.Services.CreateScope();
        return await work(scope.ServiceProvider);
    }

    private Task<Farm> FarmAsync(string name) => InScope(sp => sp.GetRequiredService<AppDbContext>().Farms.FirstAsync(f => f.Name == name));

    private Task<SampleBatchDto> GradedBatchAsync(Guid farmId, string grade = "550 g/L") => InScope(async sp =>
    {
        var q = sp.GetRequiredService<IQualityService>();
        var b = await q.CreateBatchAsync(new CreateSampleBatchRequest(farmId, "Black pepper", 1500, new DateOnly(2026, 11, 1), "agent", null), default);
        return await q.GradeBatchAsync(b.Id, new GradingInput(11.5m, 560, "Clean", grade, "qc", null), default);
    });

    private static CreateLotRequest Lot(params (Guid batch, int kg)[] parts) => new(
        "Black pepper", "550 g/L", parts.Select(p => new LotContributionInput(p.batch, p.kg, null)).ToList(), null,
        6.2m, 7.1m, "EUR", "FOB Colombo", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30));

    [Fact]
    public async Task Published_lot_is_public_and_leaks_no_farmer_data()
    {
        var farm = await FarmAsync("Canary Farm");
        var batch = await GradedBatchAsync(farm.Id);
        var lot = await InScope(sp => sp.GetRequiredService<ILotService>().CreateAsync(Lot((batch.Id, 900)), default));
        await InScope(sp => sp.GetRequiredService<ILotService>().PublishAsync(lot.Id, default));

        var client = api.CreateClient();
        var list = await client.GetStringAsync("/api/public/lots");
        var detail = await client.GetStringAsync($"/api/public/lots/{lot.Code}");
        Assert.Contains(lot.Code, list);
        foreach (var body in new[] { list, detail })
        {
            Assert.DoesNotContain("CANARY 0001", body);
            Assert.DoesNotContain(ApiFixture.CanaryLegalName, body);
            Assert.DoesNotContain("7.1234", body);
            Assert.DoesNotContain(batch.Code, body);          // internal batch codes stay internal
            Assert.DoesNotContain(farm.Id.ToString(), body);  // no internal ids
        }

        var resolved = await client.GetStringAsync($"/api/public/q/{lot.Code.ToLowerInvariant()}");
        Assert.Contains("\"lot\"", resolved);
    }

    [Fact]
    public async Task Lot_with_a_farm_without_consent_cannot_be_published()
    {
        var hidden = await FarmAsync("Hidden Farm");
        var batch = await GradedBatchAsync(hidden.Id);
        var lot = await InScope(sp => sp.GetRequiredService<ILotService>().CreateAsync(Lot((batch.Id, 500)), default));
        Assert.Contains(lot.PublishBlockers, b => b.Contains("consent"));
        await Assert.ThrowsAsync<DomainException>(() => InScope(sp => sp.GetRequiredService<ILotService>().PublishAsync(lot.Id, default)));
    }

    [Fact]
    public async Task Bundled_lot_needs_a_composite_grading_and_uses_volume_weighted_shares()
    {
        var farm = await FarmAsync("Canary Farm");
        var a = await GradedBatchAsync(farm.Id);
        var b = await GradedBatchAsync(farm.Id);
        var lots = (Func<IServiceProvider, ILotService>)(sp => sp.GetRequiredService<ILotService>());
        var lot = await InScope(sp => lots(sp).CreateAsync(Lot((a.Id, 1800), (b.Id, 4200)), default));

        Assert.Equal("Composite", lot.SampleKind);
        Assert.Equal([30.0m, 70.0m], lot.Contributions.Select(c => c.CompositeSharePercent));
        Assert.Contains(lot.PublishBlockers, x => x.Contains("composite"));

        await InScope(sp => lots(sp).GradeCompositeAsync(lot.Id, new GradingInput(11.8m, 558, "Clean", "550 g/L", "qc", null), default));
        var published = await InScope(sp => lots(sp).PublishAsync(lot.Id, default));
        Assert.Equal("Published", published.Status);
    }

    [Fact]
    public async Task A_batch_cannot_back_two_active_lots_but_can_after_withdrawal()
    {
        var farm = await FarmAsync("Canary Farm");
        var batch = await GradedBatchAsync(farm.Id);
        var first = await InScope(sp => sp.GetRequiredService<ILotService>().CreateAsync(Lot((batch.Id, 100)), default));
        await Assert.ThrowsAsync<DomainException>(() => InScope(sp => sp.GetRequiredService<ILotService>().CreateAsync(Lot((batch.Id, 100)), default)));

        await InScope(sp => sp.GetRequiredService<ILotService>().WithdrawAsync(first.Id, default));
        var second = await InScope(sp => sp.GetRequiredService<ILotService>().CreateAsync(Lot((batch.Id, 100)), default));
        Assert.Equal("Draft", second.Status);
    }

    [Fact]
    public async Task Mixed_grades_cannot_form_a_lot()
    {
        var farm = await FarmAsync("Canary Farm");
        var a = await GradedBatchAsync(farm.Id, "550 g/L");
        var b = await GradedBatchAsync(farm.Id, "500 g/L");
        var e = await Assert.ThrowsAsync<DomainException>(() => InScope(sp => sp.GetRequiredService<ILotService>().CreateAsync(Lot((a.Id, 100), (b.Id, 100)), default)));
        Assert.Contains("one grade", e.Message);
    }

    [Fact]
    public async Task Photos_reach_buyers_only_when_buyer_safe_with_photo_consent_and_without_metadata()
    {
        var farm = await FarmAsync("Canary Farm");
        var original = JpegWithExif("GPS 7.2906N 80.6337E CANARY-PHONE-SERIAL");
        var photo = await InScope(sp => sp.GetRequiredService<IPhotoService>().UploadAsync(farm.Id, original, "IMG_0001.jpg", "Drying pepper", false, "agent", default));
        var publicId = await InScope(sp => sp.GetRequiredService<AppDbContext>().FarmPhotos.Where(p => p.Id == photo.Id).Select(p => p.PublicId).SingleAsync());
        var client = api.CreateClient();

        // 1. Not reviewed yet → not public.
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/public/photos/{publicId}")).StatusCode);

        // 2. Buyer-safe needs the identifying-details check.
        await Assert.ThrowsAsync<DomainException>(() => InScope(sp => sp.GetRequiredService<IPhotoService>().ReviewAsync(photo.Id, new PhotoReviewRequest(true, false, null), "qc", default)));
        var reviewed = await InScope(sp => sp.GetRequiredService<IPhotoService>().ReviewAsync(photo.Id, new PhotoReviewRequest(true, true, null), "qc", default));

        // 3. Reviewed, but the farmer gave no photo consent → still not public.
        Assert.False(reviewed.VisibleToBuyers);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/public/photos/{publicId}")).StatusCode);

        // 4. With photo consent (signed form) it is public — and the bytes carry no metadata.
        await InScope(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.FarmerConsents.Add(new FarmerConsent { FarmerId = farm.FarmerId, Purposes = ConsentPurpose.Photos, Language = "en", ConsentTextVersion = "t",
                Evidence = ConsentEvidence.SignedFormPhoto, ObtainedBy = "agent", ObtainedAt = DateTimeOffset.UtcNow });
            return await db.SaveChangesAsync();
        });
        var response = await client.GetAsync($"/api/public/photos/{publicId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/webp", response.Content.Headers.ContentType?.MediaType);
        var bytes = Encoding.ASCII.GetString(await response.Content.ReadAsByteArrayAsync());
        Assert.DoesNotContain("CANARY", bytes);
        Assert.DoesNotContain("Exif", bytes);

        var farmJson = await client.GetStringAsync($"/api/public/farms/{farm.PublicCode}");
        Assert.Contains(publicId.ToString(), farmJson);
        Assert.DoesNotContain(photo.Id.ToString(), farmJson);
    }

    private static byte[] JpegWithExif(string canary)
    {
        using var bmp = new SKBitmap(640, 480);
        using (var canvas = new SKCanvas(bmp)) canvas.Clear(new SKColor(200, 146, 47));
        using var img = SKImage.FromBitmap(bmp);
        var jpeg = img.Encode(SKEncodedImageFormat.Jpeg, 90).ToArray();
        var payload = Encoding.ASCII.GetBytes("Exif\0\0MM\0*\0\0\0\b\0\0" + canary);
        var len = payload.Length + 2;
        var app1 = new byte[] { 0xFF, 0xE1, (byte)(len >> 8), (byte)len }.Concat(payload).ToArray();
        return jpeg.Take(2).Concat(app1).Concat(jpeg.Skip(2)).ToArray();
    }
}
