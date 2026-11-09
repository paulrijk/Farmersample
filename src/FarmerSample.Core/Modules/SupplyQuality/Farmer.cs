namespace FarmerSample.Core.Modules.SupplyQuality;

/// <summary>
/// A farmer. Contact fields are INTERNAL and must never appear in public or buyer projections
/// (see the privacy canary test).
/// </summary>
public class Farmer
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    /// <summary>Name shown on the public profile, only with consent for a public profile.</summary>
    public required string PublicName { get; set; }
    public string? PreferredLanguage { get; set; }        // "si", "ta", "en"

    // ---- internal only ----
    public string? LegalName { get; set; }
    public string? PhoneInternal { get; set; }

    public List<Farm> Farms { get; set; } = [];
    public List<FarmerConsent> Consents { get; set; } = [];
}

public enum FarmStatus { Draft = 0, NeedsReview = 1, Published = 2, Unpublished = 3 }

public class Farm
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string PublicCode { get; set; }
    public Guid FarmerId { get; set; }
    public Farmer? Farmer { get; set; }

    public required string Name { get; set; }
    public Guid RegionId { get; set; }
    public decimal? Hectares { get; set; }
    public string? Story { get; set; }
    public FarmStatus Status { get; set; } = FarmStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAt { get; set; }

    // ---- internal only ----
    public double? LatitudeInternal { get; set; }
    public double? LongitudeInternal { get; set; }

    public List<FarmProduct> Products { get; set; } = [];
    public List<Certification> Certifications { get; set; } = [];
}

public class FarmProduct
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid FarmId { get; set; }
    public required string Product { get; set; }          // e.g. "Black pepper", "Ceylon cinnamon"
    public string? Grade { get; set; }                    // e.g. "550 g/L", "Alba", "C5"
    /// <summary>Harvest months as 1–12.</summary>
    public int[] HarvestMonths { get; set; } = [];
    public int? EstimatedVolumeKg { get; set; }
}

public class Certification
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid FarmId { get; set; }
    public required string Scheme { get; set; }           // e.g. "EU organic", "Rainforest Alliance"
    public DateOnly? ValidUntil { get; set; }
}

[Flags]
public enum ConsentPurpose
{
    None = 0,
    PublicProfile = 1,
    Photos = 2,
    FamilyStory = 4,
    PriceShare = 8,
    LegalName = 16,
}

public enum ConsentEvidence { SignedFormPhoto = 1, AgentReadAloud = 2 }

public class FarmerConsent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid FarmerId { get; set; }
    public ConsentPurpose Purposes { get; set; }
    public required string Language { get; set; }
    public required string ConsentTextVersion { get; set; }
    public ConsentEvidence Evidence { get; set; }
    public string? EvidenceBlobPath { get; set; }
    public required string ObtainedBy { get; set; }
    public DateTimeOffset ObtainedAt { get; set; }
    public DateTimeOffset? WithdrawnAt { get; set; }

    public bool IsActiveFor(ConsentPurpose purpose) => WithdrawnAt is null && Purposes.HasFlag(purpose)
        // Photos of people need written evidence, not only a read-aloud tick.
        && (purpose != ConsentPurpose.Photos || Evidence == ConsentEvidence.SignedFormPhoto);
}
