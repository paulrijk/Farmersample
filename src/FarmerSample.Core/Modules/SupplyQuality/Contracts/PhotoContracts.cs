namespace FarmerSample.Core.Modules.SupplyQuality.Contracts;

public sealed record FarmPhotoDto(Guid Id, string ImageUrl, int Width, int Height, string? Caption, bool ShowsPeople, string Status,
    string UploadedBy, DateTimeOffset UploadedAt, string? ReviewedBy, string? ReviewNote, bool VisibleToBuyers, string? NotVisibleBecause);

/// <summary>Buyer-safe review. Marking a photo buyer-safe requires confirming the identifying-details check.</summary>
public sealed record PhotoReviewRequest(bool BuyerSafe, bool CheckedNoIdentifyingDetails, string? Note);

public sealed record PublicPhotoDto(string Url, int Width, int Height, string? Caption);
