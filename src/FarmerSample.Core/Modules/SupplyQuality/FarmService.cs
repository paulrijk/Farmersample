using FarmerSample.Core.Modules.SupplyQuality.Contracts;
using FarmerSample.Core.Persistence;
using FarmerSample.Core.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FarmerSample.Core.Modules.SupplyQuality;

public interface IFarmService
{
    Task<IReadOnlyList<FarmSummaryDto>> ListAsync(CancellationToken ct);
    Task<FarmSummaryDto> CreateAsync(CreateFarmRequest request, CancellationToken ct);
    Task<bool> RecordConsentAsync(Guid farmId, RecordConsentRequest request, CancellationToken ct);
    /// <summary>Publishes a farm. Fails if there is no active public-profile consent.</summary>
    Task<(bool Ok, string? Error)> PublishAsync(Guid farmId, CancellationToken ct);
    Task<bool> UnpublishAsync(Guid farmId, CancellationToken ct);
}

internal sealed class FarmService(AppDbContext db) : IFarmService
{
    public async Task<IReadOnlyList<FarmSummaryDto>> ListAsync(CancellationToken ct)
    {
        var rows = await db.Farms.AsNoTracking()
            .Include(f => f.Farmer!).ThenInclude(f => f.Consents)
            .OrderBy(f => f.Name).ToListAsync(ct);
        var regions = await db.Regions.AsNoTracking().ToDictionaryAsync(r => r.Id, r => r.Name, ct);
        return rows.Select(f => ToSummary(f, regions[f.RegionId])).ToList();
    }

    public async Task<FarmSummaryDto> CreateAsync(CreateFarmRequest r, CancellationToken ct)
    {
        var region = await db.Regions.FirstOrDefaultAsync(x => x.Id == r.RegionId, ct)
                     ?? throw new ArgumentException("Unknown region.");
        var farmer = new Farmer { PublicName = r.FarmerPublicName.Trim(), LegalName = r.FarmerLegalName, PhoneInternal = r.FarmerPhone };
        var farm = new Farm
        {
            PublicCode = PublicCode.New(PublicCode.FarmPrefix),
            Farmer = farmer,
            Name = r.FarmName.Trim(),
            RegionId = region.Id,
            Hectares = r.Hectares,
            Story = r.Story,
            Status = FarmStatus.NeedsReview,
            Products = r.Products.Select(p => new FarmProduct { Product = p.Product, Grade = p.Grade, HarvestMonths = p.HarvestMonths }).ToList(),
        };
        db.Farms.Add(farm);
        await db.SaveChangesAsync(ct);
        return ToSummary(farm, region.Name);
    }

    public async Task<bool> RecordConsentAsync(Guid farmId, RecordConsentRequest r, CancellationToken ct)
    {
        var farm = await db.Farms.FirstOrDefaultAsync(f => f.Id == farmId, ct);
        if (farm is null) return false;
        var purposes = r.Purposes.Aggregate(ConsentPurpose.None, (acc, p) => acc | Enum.Parse<ConsentPurpose>(p, ignoreCase: true));
        db.FarmerConsents.Add(new FarmerConsent
        {
            FarmerId = farm.FarmerId,
            Purposes = purposes,
            Language = r.Language,
            ConsentTextVersion = r.ConsentTextVersion,
            Evidence = Enum.Parse<ConsentEvidence>(r.Evidence, ignoreCase: true),
            ObtainedBy = r.ObtainedBy,
            ObtainedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<(bool Ok, string? Error)> PublishAsync(Guid farmId, CancellationToken ct)
    {
        var farm = await db.Farms.Include(f => f.Farmer!).ThenInclude(f => f.Consents).FirstOrDefaultAsync(f => f.Id == farmId, ct);
        if (farm is null) return (false, "not_found");
        if (!farm.Farmer!.Consents.Any(c => c.IsActiveFor(ConsentPurpose.PublicProfile)))
            return (false, "Record the farmer's consent for a public profile before publishing.");
        farm.Status = FarmStatus.Published;
        farm.PublishedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<bool> UnpublishAsync(Guid farmId, CancellationToken ct)
    {
        var farm = await db.Farms.FirstOrDefaultAsync(f => f.Id == farmId, ct);
        if (farm is null) return false;
        farm.Status = FarmStatus.Unpublished;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static FarmSummaryDto ToSummary(Farm f, string region) => new(
        f.Id, f.PublicCode, f.Name, f.Farmer!.PublicName, region, f.Status.ToString(),
        f.Farmer.Consents.Any(c => c.IsActiveFor(ConsentPurpose.PublicProfile)));
}
