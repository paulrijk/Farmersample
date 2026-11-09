using FarmerSample.Core.Modules.SupplyQuality.Contracts;
using FarmerSample.Core.Persistence;
using FarmerSample.Core.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FarmerSample.Core.Modules.SupplyQuality;

public interface ILotService
{
    Task<IReadOnlyList<LotDto>> ListAsync(CancellationToken ct);
    Task<LotDto> GetAsync(Guid id, CancellationToken ct);
    Task<LotDto> CreateAsync(CreateLotRequest request, CancellationToken ct);
    /// <summary>Grades the composite sample of a bundled lot.</summary>
    Task<LotDto> GradeCompositeAsync(Guid lotId, GradingInput input, CancellationToken ct);
    Task<LotDto> PublishAsync(Guid lotId, CancellationToken ct);
    Task<LotDto> WithdrawAsync(Guid lotId, CancellationToken ct);
}

internal sealed class LotService(AppDbContext db, TimeProvider clock) : ILotService
{
    private static readonly string[] Currencies = ["EUR", "USD"];

    public async Task<IReadOnlyList<LotDto>> ListAsync(CancellationToken ct)
    {
        var lots = await Query().OrderByDescending(l => l.CreatedAt).ToListAsync(ct);
        var result = new List<LotDto>();
        foreach (var lot in lots) result.Add(await ToDtoAsync(lot, ct));
        return result;
    }

    public async Task<LotDto> GetAsync(Guid id, CancellationToken ct) =>
        await ToDtoAsync(await Query().FirstOrDefaultAsync(l => l.Id == id, ct) ?? throw new DomainException("Unknown lot."), ct);

    public async Task<LotDto> CreateAsync(CreateLotRequest r, CancellationToken ct)
    {
        if (r.Contributions is not { Count: > 0 }) throw new DomainException("Choose at least one graded sample batch.");
        if (r.Contributions.Select(c => c.SampleBatchId).Distinct().Count() != r.Contributions.Count) throw new DomainException("Each batch can be used once.");
        if (r.Contributions.Any(c => c.VolumeKg <= 0)) throw new DomainException("Every farm must commit a volume above 0 kg.");
        var currency = r.Currency?.ToUpperInvariant() ?? "";
        if (!Currencies.Contains(currency)) throw new DomainException("Currency must be EUR or USD.");
        if (r.PriceMinPerKg <= 0 || r.PriceMaxPerKg < r.PriceMinPerKg) throw new DomainException("Enter a price range with minimum above 0 and maximum ≥ minimum.");
        if (string.IsNullOrWhiteSpace(r.Incoterm)) throw new DomainException("Incoterm is required.");
        if (r.ValidUntil < Today()) throw new DomainException("Valid-until date is in the past.");

        var ids = r.Contributions.Select(c => c.SampleBatchId).ToList();
        var batches = await db.SampleBatches.Include(b => b.Gradings).Include(b => b.Farm)
            .Where(b => ids.Contains(b.Id)).ToDictionaryAsync(b => b.Id, ct);
        if (batches.Count != ids.Count) throw new DomainException("One of the batches does not exist.");

        foreach (var b in batches.Values)
        {
            if (!string.Equals(b.Product, r.Product, StringComparison.OrdinalIgnoreCase))
                throw new DomainException($"Batch {b.Code} is {b.Product}, not {r.Product}.");
            var g = b.CurrentGrading ?? throw new DomainException($"Batch {b.Code} has not been graded yet.");
            if (!string.Equals(g.Grade, r.Grade, StringComparison.OrdinalIgnoreCase))
                throw new DomainException($"Batch {b.Code} is graded {g.Grade}, not {r.Grade}. A lot holds one grade only.");
        }
        if (await db.LotContributions.AnyAsync(c => c.Active && ids.Contains(c.SampleBatchId), ct))
            throw new DomainException("One of the batches already backs an active lot.");

        // Composite shares: volume-weighted by default; QC may override with a reason.
        var bundled = r.Contributions.Count > 1;
        var weighted = CompositeShares.VolumeWeighted(r.Contributions.Select(c => c.VolumeKg).ToList());
        var shares = weighted;
        string? overrideReason = null;
        if (bundled && r.Contributions.Any(c => c.CompositeSharePercent is not null))
        {
            if (r.Contributions.Any(c => c.CompositeSharePercent is null or <= 0))
                throw new DomainException("When overriding composite shares, give a share for every farm.");
            var given = r.Contributions.Select(c => c.CompositeSharePercent!.Value).ToArray();
            if (Math.Abs(given.Sum() - 100m) > 0.1m) throw new DomainException("Composite shares must add up to 100%.");
            if (!given.SequenceEqual(weighted))
            {
                if (string.IsNullOrWhiteSpace(r.ShareOverrideReason))
                    throw new DomainException("Composite shares differ from the volume-weighted shares. Give a reason.");
                shares = given;
                overrideReason = r.ShareOverrideReason.Trim();
            }
        }

        var lot = new Lot
        {
            PublicCode = PublicCode.New(PublicCode.LotPrefix), Product = batches[ids[0]].Product, Grade = batches[ids[0]].CurrentGrading!.Grade,
            CountryCode = "LK", PriceMinPerKgMinor = ToMinor(r.PriceMinPerKg), PriceMaxPerKgMinor = ToMinor(r.PriceMaxPerKg),
            Currency = currency, Incoterm = r.Incoterm.Trim(), ValidUntil = r.ValidUntil,
            Contributions = r.Contributions.Select((c, i) => new LotContribution
            {
                SampleBatchId = c.SampleBatchId, VolumeKg = c.VolumeKg, CompositeSharePercent = bundled ? shares[i] : 100m,
            }).ToList(),
            Sample = new LotSample { Kind = bundled ? LotSampleKind.Composite : LotSampleKind.SingleFarm, ShareOverrideReason = overrideReason },
        };
        db.Lots.Add(lot);
        await db.SaveChangesAsync(ct);
        return await GetAsync(lot.Id, ct);
    }

    public async Task<LotDto> GradeCompositeAsync(Guid lotId, GradingInput input, CancellationToken ct)
    {
        var lot = await Query().FirstOrDefaultAsync(l => l.Id == lotId, ct) ?? throw new DomainException("Unknown lot.");
        if (lot.Sample!.Kind != LotSampleKind.Composite) throw new DomainException("Single-farm lots use the farm batch's grading.");
        if (lot.Status != LotStatus.Draft) throw new DomainException("Grade the composite sample before publishing.");
        var grading = GradingFactory.Create(input, lot.Sample.CurrentGrading);
        if (!string.Equals(grading.Grade, lot.Grade, StringComparison.OrdinalIgnoreCase))
            throw new DomainException($"The composite graded {grading.Grade}, but the lot is {lot.Grade}. Rebuild the lot with matching batches.");
        grading.LotSampleId = lot.Sample.Id;
        db.Gradings.Add(grading);
        await db.SaveChangesAsync(ct);
        return await GetAsync(lotId, ct);
    }

    public async Task<LotDto> PublishAsync(Guid lotId, CancellationToken ct)
    {
        var lot = await Query().FirstOrDefaultAsync(l => l.Id == lotId, ct) ?? throw new DomainException("Unknown lot.");
        var blockers = await BlockersAsync(lot, ct);
        if (blockers.Count > 0) throw new DomainException(string.Join(" ", blockers));
        lot.Status = LotStatus.Published;
        lot.PublishedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return await GetAsync(lotId, ct);
    }

    public async Task<LotDto> WithdrawAsync(Guid lotId, CancellationToken ct)
    {
        var lot = await Query().FirstOrDefaultAsync(l => l.Id == lotId, ct) ?? throw new DomainException("Unknown lot.");
        if (lot.Status == LotStatus.Closed) throw new DomainException("The lot is already closed.");
        lot.Status = LotStatus.Closed;
        lot.ClosedReason = LotClosedReason.Withdrawn;
        foreach (var c in lot.Contributions) c.Active = false;   // batches may back a successor lot
        await db.SaveChangesAsync(ct);
        return await GetAsync(lotId, ct);
    }

    private IQueryable<Lot> Query() => db.Lots
        .Include(l => l.Contributions).ThenInclude(c => c.SampleBatch!).ThenInclude(b => b.Farm!).ThenInclude(f => f.Farmer!).ThenInclude(f => f.Consents)
        .Include(l => l.Contributions).ThenInclude(c => c.SampleBatch!).ThenInclude(b => b.Gradings)
        .Include(l => l.Sample!).ThenInclude(s => s.Gradings)
        .AsSplitQuery();

    private Task<List<string>> BlockersAsync(Lot lot, CancellationToken _)
    {
        var blockers = new List<string>();
        if (lot.Status != LotStatus.Draft) blockers.Add("Only draft lots can be published.");
        if (lot.ValidUntil < Today()) blockers.Add("The valid-until date has passed.");
        if (lot.Sample!.Kind == LotSampleKind.Composite && lot.Sample.CurrentGrading is null)
            blockers.Add("Grade the composite sample first.");
        foreach (var c in lot.Contributions.Where(c => c.Active))
        {
            var farm = c.SampleBatch!.Farm!;
            if (farm.Status != FarmStatus.Published) blockers.Add($"Farm {farm.Name} is not published.");
            else if (!farm.Farmer!.Consents.Any(x => x.IsActiveFor(ConsentPurpose.PublicProfile)))
                blockers.Add($"Farm {farm.Name} has no public-profile consent.");
        }
        return Task.FromResult(blockers);
    }

    private async Task<LotDto> ToDtoAsync(Lot l, CancellationToken ct)
    {
        var lotGrading = l.Sample!.Kind == LotSampleKind.Composite
            ? l.Sample.CurrentGrading
            : l.Contributions.FirstOrDefault()?.SampleBatch?.CurrentGrading;
        return new LotDto(
            l.Id, l.PublicCode, l.Product, l.Grade, l.Status.ToString(), l.ClosedReason?.ToString(), l.CommittedVolumeKg,
            FromMinor(l.PriceMinPerKgMinor), FromMinor(l.PriceMaxPerKgMinor), l.Currency, l.Incoterm, l.ValidUntil,
            l.Sample.Kind.ToString(), l.Sample.ShareOverrideReason, GradingFactory.ToDto(lotGrading),
            l.Contributions.Select(c => new LotContributionDto(c.SampleBatchId, c.SampleBatch!.Code, c.SampleBatch.Farm!.Name, c.SampleBatch.Farm.PublicCode,
                c.VolumeKg, c.CompositeSharePercent, c.SampleBatch.CurrentGrading?.Grade)).ToList(),
            l.Status == LotStatus.Draft ? await BlockersAsync(l, ct) : []);
    }

    private DateOnly Today() => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
    internal static long ToMinor(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
    internal static decimal FromMinor(long minor) => minor / 100m;
}
