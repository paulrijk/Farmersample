namespace FarmerSample.Core.Modules.Catalogue.Contracts;

public sealed record SampleRequestInput(
    string? FarmCode,
    string CompanyName,
    string ContactName,
    string Email,
    string CountryCode,
    string? Message,
    /// <summary>Honeypot: real people leave this empty.</summary>
    string? Website);

public sealed record SampleRequestDto(Guid Id, string? FarmCode, string CompanyName, string ContactName, string Email, string CountryCode, string? Message, DateTimeOffset CreatedAt, bool Handled);

public sealed record SampleRequestReceived(Guid SampleRequestId, string CompanyName, string ContactName, string Email, string CountryCode, string? FarmCode, string? Message);
