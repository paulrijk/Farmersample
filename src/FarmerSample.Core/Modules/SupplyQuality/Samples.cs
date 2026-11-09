namespace FarmerSample.Core.Modules.SupplyQuality;

public enum SampleBatchStatus { Received = 0, Graded = 1 }

/// <summary>1–2 kg taken from ONE farm at ONE visit for ONE product. Graded per farm before any lot exists.</summary>
public class SampleBatch
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    /// <summary>Internal label code printed on the batch bag (prefix SB). Not resolvable publicly.</summary>
    public required string Code { get; set; }
    public Guid FarmId { get; set; }
    public Farm? Farm { get; set; }
    public required string Product { get; set; }
    public int WeightGrams { get; set; }
    public DateOnly CollectedOn { get; set; }
    public required string CollectedBy { get; set; }
    public string? Notes { get; set; }
    public SampleBatchStatus Status { get; set; } = SampleBatchStatus.Received;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<Grading> Gradings { get; set; } = [];

    /// <summary>The grading in force: the newest version that nothing supersedes.</summary>
    public Grading? CurrentGrading => Gradings.Where(g => !Gradings.Any(o => o.SupersedesId == g.Id)).MaxBy(g => g.Version);
}

/// <summary>
/// A grading result. Immutable: a correction is a NEW version that supersedes the old one, with a reason.
/// Belongs to a farm's sample batch, or to a lot's composite sample.
/// </summary>
public class Grading
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid? SampleBatchId { get; set; }
    public Guid? LotSampleId { get; set; }
    public decimal? MoisturePercent { get; set; }
    public int? DensityGramsPerLitre { get; set; }
    public string? Cleanliness { get; set; }          // e.g. "Clean", "Minor foreign matter"
    public required string Grade { get; set; }        // e.g. "550 g/L", "Alba", "C5"
    public required string Operator { get; set; }
    public DateTimeOffset GradedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Version { get; set; } = 1;
    public Guid? SupersedesId { get; set; }
    public string? CorrectionReason { get; set; }
}
