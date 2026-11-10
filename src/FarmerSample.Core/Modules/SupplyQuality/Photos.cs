namespace FarmerSample.Core.Modules.SupplyQuality;

public enum PhotoStatus { PendingReview = 0, BuyerSafe = 1, Rejected = 2 }

/// <summary>
/// A farm photo. The ORIGINAL (with EXIF, GPS, faces, signs) is private and never leaves the platform.
/// Buyers only ever get the RENDITION: re-encoded, resized, all metadata stripped, and only after a
/// staff review marked it buyer-safe (no names, signs, plates or phone numbers visible).
/// </summary>
public class FarmPhoto
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    /// <summary>Random id used in public URLs (internal ids are time-ordered and not shown publicly).</summary>
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid FarmId { get; set; }
    public required string OriginalBlobName { get; set; }
    public required string RenditionBlobName { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? Caption { get; set; }
    /// <summary>Recognisable people in the photo: needs the farmer's photo consent with a signed form.</summary>
    public bool ShowsPeople { get; set; }
    public PhotoStatus Status { get; set; } = PhotoStatus.PendingReview;
    public required string UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}

/// <summary>
/// A photo of a lot: the product itself (the graded sample, the bags, the drying floor). Same handling as farm
/// photos: private original, metadata-free rendition, and a buyer-safe review before buyers see it.
/// People should not be recognisable in lot photos; photos of people belong to the farm, where consent is recorded.
/// </summary>
public class LotPhoto
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public Guid LotId { get; set; }
    public required string OriginalBlobName { get; set; }
    public required string RenditionBlobName { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? Caption { get; set; }
    public PhotoStatus Status { get; set; } = PhotoStatus.PendingReview;
    public required string UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}
