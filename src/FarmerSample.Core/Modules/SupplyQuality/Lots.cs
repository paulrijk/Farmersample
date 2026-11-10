namespace FarmerSample.Core.Modules.SupplyQuality;

public enum LotStatus { Draft = 0, Published = 1, Closed = 2 }
public enum LotClosedReason { SoldOut = 1, Expired = 2, Withdrawn = 3 }
public enum LotSampleKind { SingleFarm = 1, Composite = 2 }

/// <summary>Sellable volume of ONE product and ONE grade from one or more farms (bundling).</summary>
public class Lot
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string PublicCode { get; set; }
    public required string Product { get; set; }
    public required string Grade { get; set; }
    public required string CountryCode { get; set; }
    public LotStatus Status { get; set; } = LotStatus.Draft;
    public LotClosedReason? ClosedReason { get; set; }

    /// <summary>Indicative price range per kg in minor units (cents).</summary>
    public long PriceMinPerKgMinor { get; set; }
    public long PriceMaxPerKgMinor { get; set; }
    public required string Currency { get; set; }
    public required string Incoterm { get; set; }     // e.g. "FOB Colombo"
    public DateOnly ValidUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAt { get; set; }

    public List<LotContribution> Contributions { get; set; } = [];
    public LotSample? Sample { get; set; }
    public List<LotPhoto> Photos { get; set; } = [];

    public int CommittedVolumeKg => Contributions.Where(c => c.Active).Sum(c => c.VolumeKg);
    public bool IsBundled => Contributions.Count(c => c.Active) > 1;
}

/// <summary>A farm's graded batch backing a lot, with the volume that farm commits.</summary>
public class LotContribution
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid LotId { get; set; }
    public Guid SampleBatchId { get; set; }
    public SampleBatch? SampleBatch { get; set; }
    public int VolumeKg { get; set; }
    /// <summary>Share of this farm in the composite sample, in percent (volume-weighted by default).</summary>
    public decimal CompositeSharePercent { get; set; }
    /// <summary>False once the lot is closed, so the batch can back a successor lot.</summary>
    public bool Active { get; set; } = true;
}

/// <summary>The physical sample buyers receive for a lot: the farm's batch itself, or a composite made by QC.</summary>
public class LotSample
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid LotId { get; set; }
    public LotSampleKind Kind { get; set; }
    /// <summary>Set when QC deviates from volume-weighted shares.</summary>
    public string? ShareOverrideReason { get; set; }
    public List<Grading> Gradings { get; set; } = [];
    public Grading? CurrentGrading => Gradings.Where(g => !Gradings.Any(o => o.SupersedesId == g.Id)).MaxBy(g => g.Version);
}

public static class CompositeShares
{
    /// <summary>
    /// Volume-weighted shares in percent with 1 decimal, adjusted so they add up to exactly 100.0
    /// (a farm committing 80% of the volume supplies 80% of the composite sample).
    /// </summary>
    public static decimal[] VolumeWeighted(IReadOnlyList<int> volumesKg)
    {
        if (volumesKg.Count == 0) return [];
        if (volumesKg.Any(v => v <= 0)) throw new ArgumentException("Volumes must be positive.");
        var total = (decimal)volumesKg.Sum();
        var shares = volumesKg.Select(v => Math.Round(v / total * 100m, 1, MidpointRounding.AwayFromZero)).ToArray();
        var drift = 100m - shares.Sum();
        var largest = Array.IndexOf(shares, shares.Max());
        shares[largest] += drift;
        return shares;
    }
}
