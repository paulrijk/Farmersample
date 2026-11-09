namespace FarmerSample.Core.ReferenceData;

/// <summary>Shared reference data (not owned by a module): growing regions per country.</summary>
public class Region
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string CountryCode { get; set; }
    public required string Name { get; set; }
}
