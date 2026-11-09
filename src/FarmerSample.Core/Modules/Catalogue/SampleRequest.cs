namespace FarmerSample.Core.Modules.Catalogue;

/// <summary>A "request a sample" submission from the public site (P0). First funnel data for the phase gates.</summary>
public class SampleRequest
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string? FarmCode { get; set; }
    public string? LotCode { get; set; }
    public required string CompanyName { get; set; }
    public required string ContactName { get; set; }
    public required string Email { get; set; }
    public required string CountryCode { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? HandledAt { get; set; }
}
