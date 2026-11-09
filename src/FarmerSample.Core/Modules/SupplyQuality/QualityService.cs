using FarmerSample.Core.Modules.SupplyQuality.Contracts;
using FarmerSample.Core.Persistence;
using FarmerSample.Core.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FarmerSample.Core.Modules.SupplyQuality;

public interface IQualityService
{
    Task<IReadOnlyList<SampleBatchDto>> ListBatchesAsync(CancellationToken ct);
    Task<SampleBatchDto> CreateBatchAsync(CreateSampleBatchRequest request, CancellationToken ct);
    /// <summary>Records a grading; if the batch already has one, the new grading supersedes it and needs a reason.</summary>
    Task<SampleBatchDto> GradeBatchAsync(Guid batchId, GradingInput input, CancellationToken ct);
}

internal sealed class QualityService(AppDbContext db) : IQualityService
{
    public async Task<IReadOnlyList<SampleBatchDto>> ListBatchesAsync(CancellationToken ct)
    {
        var batches = await Query().OrderByDescending(b => b.CreatedAt).ToListAsync(ct);
        var active = await ActiveLotCodesAsync(batches.Select(b => b.Id).ToList(), ct);
        return batches.Select(b => ToDto(b, active.GetValueOrDefault(b.Id))).ToList();
    }

    public async Task<SampleBatchDto> CreateBatchAsync(CreateSampleBatchRequest r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Product)) throw new DomainException("Product is required.");
        if (r.WeightGrams is <= 0 or > 50_000) throw new DomainException("Weight must be between 1 g and 50 kg.");
        if (string.IsNullOrWhiteSpace(r.CollectedBy)) throw new DomainException("Who collected the sample is required.");
        if (!await db.Farms.AnyAsync(f => f.Id == r.FarmId, ct)) throw new DomainException("Unknown farm.");

        var batch = new SampleBatch
        {
            Code = PublicCode.New("SB"), FarmId = r.FarmId, Product = r.Product.Trim(), WeightGrams = r.WeightGrams,
            CollectedOn = r.CollectedOn, CollectedBy = r.CollectedBy.Trim(), Notes = r.Notes?.Trim(),
        };
        db.SampleBatches.Add(batch);
        await db.SaveChangesAsync(ct);
        return ToDto(await Query().FirstAsync(b => b.Id == batch.Id, ct), null);
    }

    public async Task<SampleBatchDto> GradeBatchAsync(Guid batchId, GradingInput input, CancellationToken ct)
    {
        var batch = await Query().FirstOrDefaultAsync(b => b.Id == batchId, ct) ?? throw new DomainException("Unknown sample batch.");
        var active = await ActiveLotCodesAsync([batchId], ct);
        if (active.ContainsKey(batchId))
            throw new DomainException($"This batch backs lot {active[batchId]}. Withdraw the lot before correcting its grading.");

        var grading = GradingFactory.Create(input, batch.CurrentGrading);
        grading.SampleBatchId = batch.Id;
        db.Gradings.Add(grading);
        batch.Status = SampleBatchStatus.Graded;
        await db.SaveChangesAsync(ct);
        return ToDto(await Query().FirstAsync(b => b.Id == batchId, ct), null);
    }

    private IQueryable<SampleBatch> Query() => db.SampleBatches.Include(b => b.Farm).Include(b => b.Gradings);

    private async Task<Dictionary<Guid, string>> ActiveLotCodesAsync(List<Guid> batchIds, CancellationToken ct) =>
        await db.LotContributions.Where(c => c.Active && batchIds.Contains(c.SampleBatchId))
            .Join(db.Lots, c => c.LotId, l => l.Id, (c, l) => new { c.SampleBatchId, l.PublicCode })
            .ToDictionaryAsync(x => x.SampleBatchId, x => x.PublicCode, ct);

    internal static SampleBatchDto ToDto(SampleBatch b, string? activeLot) => new(
        b.Id, b.Code, b.FarmId, b.Farm!.Name, b.Farm.PublicCode, b.Product, b.WeightGrams, b.CollectedOn, b.CollectedBy, b.Notes,
        b.Status.ToString(), GradingFactory.ToDto(b.CurrentGrading), activeLot);
}

internal static class GradingFactory
{
    public static Grading Create(GradingInput input, Grading? current)
    {
        if (string.IsNullOrWhiteSpace(input.Grade)) throw new DomainException("Grade is required.");
        if (string.IsNullOrWhiteSpace(input.Operator)) throw new DomainException("Who graded the sample is required.");
        if (input.MoisturePercent is < 0 or > 100) throw new DomainException("Moisture must be between 0 and 100%.");
        if (input.DensityGramsPerLitre is < 100 or > 1500) throw new DomainException("Density must be between 100 and 1500 g/L.");
        if (current is not null && string.IsNullOrWhiteSpace(input.CorrectionReason))
            throw new DomainException("This sample is already graded. Give a reason for the correction.");

        return new Grading
        {
            MoisturePercent = input.MoisturePercent, DensityGramsPerLitre = input.DensityGramsPerLitre,
            Cleanliness = string.IsNullOrWhiteSpace(input.Cleanliness) ? null : input.Cleanliness.Trim(),
            Grade = input.Grade.Trim(), Operator = input.Operator.Trim(),
            Version = (current?.Version ?? 0) + 1, SupersedesId = current?.Id,
            CorrectionReason = current is null ? null : input.CorrectionReason!.Trim(),
        };
    }

    public static GradingDto? ToDto(Grading? g) => g is null ? null :
        new(g.Id, g.MoisturePercent, g.DensityGramsPerLitre, g.Cleanliness, g.Grade, g.Operator, g.GradedAt, g.Version, g.CorrectionReason);
}
