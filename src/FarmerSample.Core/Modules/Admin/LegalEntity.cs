namespace FarmerSample.Core.Modules.Admin;

public enum LegalEntityRole
{
    /// <summary>European seller: sample stock, sample sales, buyer contracts, commission.</summary>
    EuropeanSeller = 1,
    /// <summary>Sri Lankan branch: sourcing, grading hub, export.</summary>
    SourcingAndExport = 2,
}

public class LegalEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string CountryCode { get; set; }
    public LegalEntityRole Role { get; set; }
    public string? VatNumber { get; set; }
}
