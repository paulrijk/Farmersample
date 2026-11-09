using FarmerSample.Core.Modules.SupplyQuality.Contracts;
using FarmerSample.Core.Persistence;
using FarmerSample.Core.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FarmerSample.Core.Modules.SupplyQuality;

internal sealed class PublicFarmReader(AppDbContext db) : IPublicFarmReader
{
    public async Task<IReadOnlyList<PublicFarmDto>> ListPublishedAsync(CancellationToken ct)
    {
        var farms = await Query().OrderBy(f => f.Name).ToListAsync(ct);
        return await ProjectAsync(farms, ct);
    }

    public async Task<PublicFarmDto?> FindPublishedAsync(string code, CancellationToken ct)
    {
        if (!PublicCode.TryNormalize(code, out var normalized)) return null;
        var farm = await Query().FirstOrDefaultAsync(f => f.PublicCode == normalized, ct);
        return farm is null ? null : (await ProjectAsync([farm], ct))[0];
    }

    private IQueryable<Farm> Query() => db.Farms.AsNoTracking()
        .Include(f => f.Farmer!).ThenInclude(f => f.Consents)
        .Include(f => f.Products)
        .Include(f => f.Certifications)
        .Include(f => f.Photos.Where(p => p.Status == PhotoStatus.BuyerSafe))
        .Where(f => f.Status == FarmStatus.Published)
        // No public page at all without an active public-profile consent.
        .Where(f => f.Farmer!.Consents.Any(c => c.WithdrawnAt == null && (c.Purposes & ConsentPurpose.PublicProfile) != 0));

    private async Task<IReadOnlyList<PublicFarmDto>> ProjectAsync(List<Farm> farms, CancellationToken ct)
    {
        var regionIds = farms.Select(f => f.RegionId).Distinct().ToList();
        var regions = await db.Regions.AsNoTracking().Where(r => regionIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, ct);

        return farms.Select(f =>
        {
            var consents = f.Farmer!.Consents;
            bool Has(ConsentPurpose p) => consents.Any(c => c.IsActiveFor(p));
            var region = regions[f.RegionId];
            return new PublicFarmDto(
                Code: f.PublicCode,
                Name: f.Name,
                FarmerName: Has(ConsentPurpose.LegalName) ? f.Farmer.LegalName ?? f.Farmer.PublicName : f.Farmer.PublicName,
                Region: region.Name,
                Country: region.CountryCode,
                Hectares: f.Hectares,
                Story: Has(ConsentPurpose.FamilyStory) ? f.Story : null,
                Products: f.Products.OrderBy(p => p.Product).Select(p => new PublicProductDto(p.Product, p.Grade, p.HarvestMonths)).ToList(),
                Certifications: f.Certifications.OrderBy(c => c.Scheme).Select(c => c.Scheme).ToList(),
                Photos: f.Photos.Where(p => PhotoService.PublicBlocker(p, f) is null).OrderBy(p => p.UploadedAt)
                    .Select(p => new PublicPhotoDto($"/api/public/photos/{p.PublicId}", p.Width, p.Height, p.Caption)).ToList());
        }).ToList();
    }
}
