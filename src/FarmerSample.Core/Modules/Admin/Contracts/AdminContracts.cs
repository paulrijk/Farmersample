namespace FarmerSample.Core.Modules.Admin.Contracts;

public sealed record LegalEntityDto(Guid Id, string Code, string Name, string CountryCode, string Role);
public sealed record RegionDto(Guid Id, string CountryCode, string Name);
