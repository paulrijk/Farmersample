using System.ComponentModel.DataAnnotations;
using FarmerSample.Core.Modules.Catalogue.Contracts;
using FarmerSample.Core.Outbox;
using FarmerSample.Core.Persistence;
using FarmerSample.Core.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FarmerSample.Core.Modules.Catalogue;

public interface ISampleRequestService
{
    /// <summary>Stores the request and queues the staff e-mail in the same transaction. Returns null when rejected.</summary>
    Task<Guid?> SubmitAsync(SampleRequestInput input, CancellationToken ct);
    Task<IReadOnlyList<SampleRequestDto>> ListAsync(CancellationToken ct);
}

internal sealed class SampleRequestService(AppDbContext db) : ISampleRequestService
{
    public async Task<Guid?> SubmitAsync(SampleRequestInput i, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(i.Website)) return null;   // honeypot filled → bot
        if (string.IsNullOrWhiteSpace(i.CompanyName) || string.IsNullOrWhiteSpace(i.ContactName)
            || !new EmailAddressAttribute().IsValid(i.Email) || i.CountryCode is not { Length: 2 })
            return null;

        string? farmCode = null;
        if (!string.IsNullOrWhiteSpace(i.FarmCode) && PublicCode.TryNormalize(i.FarmCode, out var normalized))
            farmCode = normalized;

        var request = new SampleRequest
        {
            FarmCode = farmCode,
            CompanyName = Trim(i.CompanyName, 200),
            ContactName = Trim(i.ContactName, 200),
            Email = Trim(i.Email, 320),
            CountryCode = i.CountryCode.ToUpperInvariant(),
            Message = i.Message is null ? null : Trim(i.Message, 4000),
        };
        db.SampleRequests.Add(request);
        db.OutboxMessages.Add(OutboxMessage.Create(new SampleRequestReceived(
            request.Id, request.CompanyName, request.ContactName, request.Email, request.CountryCode, request.FarmCode, request.Message)));
        await db.SaveChangesAsync(ct);
        return request.Id;
    }

    public async Task<IReadOnlyList<SampleRequestDto>> ListAsync(CancellationToken ct) =>
        await db.SampleRequests.AsNoTracking().OrderByDescending(r => r.CreatedAt)
            .Select(r => new SampleRequestDto(r.Id, r.FarmCode, r.CompanyName, r.ContactName, r.Email, r.CountryCode, r.Message, r.CreatedAt, r.HandledAt != null))
            .ToListAsync(ct);

    private static string Trim(string s, int max) => s.Trim() is var t && t.Length > max ? t[..max] : s.Trim();
}
