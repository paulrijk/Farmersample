using FarmerSample.Core.Modules.SupplyQuality.Contracts;
using FarmerSample.Core.Persistence;
using FarmerSample.Core.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FarmerSample.Core.Modules.SupplyQuality;

/// <summary>Published, unexpired lots whose every farm is public with consent. Allow-listed fields only.</summary>
internal sealed class PublicLotReader(AppDbContext db, TimeProvider clock) : IPublicLotReader
{
    public async Task<IReadOnlyList<PublicLotDto>> ListPublishedAsync(CancellationToken ct) =>
        await ProjectAsync(await Query().OrderByDescending(l => l.PublishedAt).ToListAsync(ct), ct);

    public async Task<PublicLotDto?> FindPublishedAsync(string code, CancellationToken ct)
    {
        if (!PublicCode.TryNormalize(code, out var normalized)) return null;
        return (await ProjectAsync(await Query().Where(l => l.PublicCode == normalized).ToListAsync(ct), ct)).FirstOrDefault();
    }

    private IQueryable<Lot> Query()
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        return db.Lots.AsNoTracking()
            .Where(l => l.Status == LotStatus.Published && l.ValidUntil >= today)
            .Include(l => l.Contributions.Where(c => c.Active)).ThenInclude(c => c.SampleBatch!).ThenInclude(b => b.Farm!).ThenInclude(f => f.Farmer!).ThenInclude(f => f.Consents)
            .Include(l => l.Contributions).ThenInclude(c => c.SampleBatch!).ThenInclude(b => b.Farm!).ThenInclude(f => f.Products)
            .Include(l => l.Contributions).ThenInclude(c => c.SampleBatch!).ThenInclude(b => b.Gradings)
            .Include(l => l.Sample!).ThenInclude(s => s.Gradings)
            .AsSplitQuery();
    }

    private async Task<IReadOnlyList<PublicLotDto>> ProjectAsync(List<Lot> lots, CancellationToken ct)
    {
        var regions = await db.Regions.AsNoTracking().ToDictionaryAsync(r => r.Id, r => r.Name, ct);
        var result = new List<PublicLotDto>();
        foreach (var l in lots)
        {
            var farms = l.Contributions.Where(c => c.Active).Select(c => c.SampleBatch!.Farm!).ToList();
            // A lot is only public while EVERY contributing farm is public with consent.
            if (farms.Count == 0 || farms.Any(f => f.Status != FarmStatus.Published
                                                  || !f.Farmer!.Consents.Any(c => c.IsActiveFor(ConsentPurpose.PublicProfile))))
                continue;

            var grading = l.Sample!.Kind == LotSampleKind.Composite ? l.Sample.CurrentGrading : l.Contributions.First(c => c.Active).SampleBatch!.CurrentGrading;
            var months = farms.SelectMany(f => f.Products.Where(p => string.Equals(p.Product, l.Product, StringComparison.OrdinalIgnoreCase)))
                .SelectMany(p => p.HarvestMonths).Distinct().Order().ToArray();

            result.Add(new PublicLotDto(
                l.PublicCode, l.Product, l.Grade, l.CountryCode, l.CommittedVolumeKg,
                LotService.FromMinor(l.PriceMinPerKgMinor), LotService.FromMinor(l.PriceMaxPerKgMinor), l.Currency, l.Incoterm, l.ValidUntil,
                months, l.IsBundled,
                grading is null ? null : new PublicGradingDto(grading.Grade, grading.MoisturePercent, grading.DensityGramsPerLitre, grading.Cleanliness,
                    DateOnly.FromDateTime(grading.GradedAt.UtcDateTime)),
                l.Contributions.Where(c => c.Active).Select(c => new PublicLotFarmDto(
                    c.SampleBatch!.Farm!.PublicCode, c.SampleBatch.Farm.Name, regions[c.SampleBatch.Farm.RegionId], c.CompositeSharePercent)).ToList()));
        }
        return result;
    }
}
