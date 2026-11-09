using FarmerSample.Core.Modules.Catalogue;
using FarmerSample.Core.Modules.Catalogue.Contracts;
using FarmerSample.Core.Modules.SupplyQuality;
using FarmerSample.Core.Modules.SupplyQuality.Contracts;
using FarmerSample.Core.SharedKernel;

namespace FarmerSample.Api.Endpoints;

/// <summary>
/// Anonymous endpoints behind QR codes and the public site. Only allow-listed DTOs (PublicFarmDto) leave here.
/// Responses are HTTP-cached for 60 s so publish/unpublish shows within a minute without any rebuild.
/// </summary>
public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this WebApplication app)
    {
        var pub = app.MapGroup("/api/public").AllowAnonymous().RequireRateLimiting(RateLimits.Public).WithTags("Public");

        pub.MapGet("/farms", async (IPublicFarmReader reader, HttpContext http, CancellationToken ct) =>
        {
            Cache(http);
            return Results.Ok(await reader.ListPublishedAsync(ct));
        });

        pub.MapGet("/farms/{code}", async (string code, IPublicFarmReader reader, HttpContext http, CancellationToken ct) =>
        {
            var farm = await reader.FindPublishedAsync(code, ct);
            if (farm is null) return Results.NotFound();   // same answer for invalid, unknown and unpublished codes
            Cache(http);
            return Results.Ok(farm);
        });

        pub.MapGet("/lots", async (IPublicLotReader reader, HttpContext http, CancellationToken ct) =>
        {
            Cache(http);
            return Results.Ok(await reader.ListPublishedAsync(ct));
        });

        pub.MapGet("/lots/{code}", async (string code, IPublicLotReader reader, HttpContext http, CancellationToken ct) =>
        {
            var lot = await reader.FindPublishedAsync(code, ct);
            if (lot is null) return Results.NotFound();
            Cache(http);
            return Results.Ok(lot);
        });

        // QR codes carry one code for a farm (FM-…) or a lot (LT-…); the site asks what kind it is.
        pub.MapGet("/q/{code}", (string code) =>
        {
            if (!PublicCode.TryNormalize(code, out var normalized)) return Results.NotFound();
            var kind = normalized[..2] switch { PublicCode.FarmPrefix => "farm", PublicCode.LotPrefix => "lot", _ => null };
            return kind is null ? Results.NotFound() : Results.Ok(new { kind, code = normalized });
        });

        // Buyer-safe renditions only (re-encoded, no metadata); 404 for anything else, including withdrawn consent.
        pub.MapGet("/photos/{publicId:guid}", async (Guid publicId, IPhotoService photos, HttpContext http, CancellationToken ct) =>
        {
            if (await photos.GetPublicImageAsync(publicId, ct) is not var (stream, type)) return Results.NotFound();
            http.Response.Headers.CacheControl = "public, max-age=3600";
            return Results.Stream(stream, type);
        });

        pub.MapPost("/sample-requests", async (SampleRequestInput input, ISampleRequestService service, CancellationToken ct) =>
        {
            var id = await service.SubmitAsync(input, ct);
            // Bots that fill the honeypot get the same 202 so they learn nothing.
            return id is null && string.IsNullOrEmpty(input.Website)
                ? Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = ["Please fill in company, name, a valid e-mail and a country."] })
                : Results.Accepted();
        }).RequireRateLimiting(RateLimits.Sensitive);
    }

    private static void Cache(HttpContext http) =>
        http.Response.Headers.CacheControl = "public, max-age=60";
}
