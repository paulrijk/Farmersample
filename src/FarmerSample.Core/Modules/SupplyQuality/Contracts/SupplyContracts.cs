namespace FarmerSample.Core.Modules.SupplyQuality.Contracts;

/// <summary>
/// The ONLY shape of farm data that may leave the platform to visitors and buyers.
/// Allow-list: add a field here only after checking it cannot identify or contact the farmer.
/// </summary>
public sealed record PublicFarmDto(
    string Code,
    string Name,
    string? FarmerName,
    string Region,
    string Country,
    decimal? Hectares,
    string? Story,
    IReadOnlyList<PublicProductDto> Products,
    IReadOnlyList<string> Certifications,
    IReadOnlyList<PublicPhotoDto> Photos);

public sealed record PublicProductDto(string Product, string? Grade, int[] HarvestMonths);

public interface IPublicFarmReader
{
    Task<IReadOnlyList<PublicFarmDto>> ListPublishedAsync(CancellationToken ct);
    Task<PublicFarmDto?> FindPublishedAsync(string code, CancellationToken ct);
}

// ---- staff-side contracts ----
public sealed record FarmSummaryDto(Guid Id, string Code, string Name, string FarmerName, string Region, string Status, bool HasPublicProfileConsent);

public sealed record CreateFarmRequest(
    string FarmerPublicName,
    string? FarmerLegalName,
    string? FarmerPhone,
    string FarmName,
    Guid RegionId,
    decimal? Hectares,
    string? Story,
    IReadOnlyList<PublicProductDto> Products);

public sealed record RecordConsentRequest(string[] Purposes, string Language, string ConsentTextVersion, string Evidence, string ObtainedBy);
