namespace FarmerSample.Core.Modules.SupplyQuality.Contracts;

// ---- staff ----
public sealed record CreateSampleBatchRequest(Guid FarmId, string Product, int WeightGrams, DateOnly CollectedOn, string CollectedBy, string? Notes);
public sealed record GradingInput(decimal? MoisturePercent, int? DensityGramsPerLitre, string? Cleanliness, string Grade, string Operator, string? CorrectionReason);
public sealed record GradingDto(Guid Id, decimal? MoisturePercent, int? DensityGramsPerLitre, string? Cleanliness, string Grade, string Operator, DateTimeOffset GradedAt, int Version, string? CorrectionReason);
public sealed record SampleBatchDto(Guid Id, string Code, Guid FarmId, string FarmName, string FarmCode, string Product, int WeightGrams, DateOnly CollectedOn,
    string CollectedBy, string? Notes, string Status, GradingDto? Grading, string? ActiveLotCode);

public sealed record LotContributionInput(Guid SampleBatchId, int VolumeKg, decimal? CompositeSharePercent);
public sealed record CreateLotRequest(
    string Product, string Grade, IReadOnlyList<LotContributionInput> Contributions, string? ShareOverrideReason,
    decimal PriceMinPerKg, decimal PriceMaxPerKg, string Currency, string Incoterm, DateOnly ValidUntil);
public sealed record LotContributionDto(Guid SampleBatchId, string BatchCode, string FarmName, string FarmCode, int VolumeKg, decimal CompositeSharePercent, string? BatchGrade);
public sealed record LotDto(Guid Id, string Code, string Product, string Grade, string Status, string? ClosedReason, int CommittedVolumeKg,
    decimal PriceMinPerKg, decimal PriceMaxPerKg, string Currency, string Incoterm, DateOnly ValidUntil, string SampleKind, string? ShareOverrideReason,
    GradingDto? LotGrading, IReadOnlyList<LotContributionDto> Contributions, IReadOnlyList<string> PublishBlockers);

// ---- public (allow-list: no internal ids, no farmer contact data) ----
public sealed record PublicLotFarmDto(string Code, string Name, string Region, decimal SharePercent);
public sealed record PublicGradingDto(string Grade, decimal? MoisturePercent, int? DensityGramsPerLitre, string? Cleanliness, DateOnly GradedOn);
public sealed record PublicLotDto(
    string Code, string Product, string Grade, string Country, int VolumeKg, decimal PriceMinPerKg, decimal PriceMaxPerKg,
    string Currency, string Incoterm, DateOnly ValidUntil, int[] HarvestMonths, bool Bundled,
    /// <summary>Verified by FarmerSample's hub (as opposed to producer-supplied information).</summary>
    PublicGradingDto? HubGrading,
    IReadOnlyList<PublicLotFarmDto> Farms);

public interface IPublicLotReader
{
    Task<IReadOnlyList<PublicLotDto>> ListPublishedAsync(CancellationToken ct);
    Task<PublicLotDto?> FindPublishedAsync(string code, CancellationToken ct);
}
