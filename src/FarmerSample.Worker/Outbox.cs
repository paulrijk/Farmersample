using FarmerSample.Core.Modules.Catalogue.Contracts;
using FarmerSample.Core.Outbox;
using FarmerSample.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FarmerSample.Worker;

/// <summary>Polls the outbox and dispatches messages. At-least-once; handlers must be idempotent or harmless when repeated.</summary>
public sealed class OutboxProcessor(IServiceScopeFactory scopes, ILogger<OutboxProcessor> log) : BackgroundService
{
    private const int MaxAttempts = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken);
                if (processed == 0) await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception e)
            {
                log.LogError(e, "Outbox loop failed; retrying shortly");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // SKIP LOCKED lets several worker replicas run without double-processing.
        var batch = await db.OutboxMessages
            .FromSql($"""
                SELECT * FROM infra.outbox_messages
                WHERE "ProcessedAt" IS NULL AND "Attempts" < {MaxAttempts}
                ORDER BY "CreatedAt" LIMIT 20 FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        foreach (var message in batch)
        {
            try
            {
                await dispatcher.DispatchAsync(message, ct);
                message.ProcessedAt = DateTimeOffset.UtcNow;
                message.LastError = null;
            }
            catch (Exception e)
            {
                message.Attempts++;
                message.LastError = e.Message.Length > 2000 ? e.Message[..2000] : e.Message;
                log.LogWarning(e, "Outbox message {Id} ({Type}) failed, attempt {Attempt}", message.Id, message.Type, message.Attempts);
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return batch.Count;
    }
}

public sealed class OutboxDispatcher(IEmailSender email, IOptions<SmtpOptions> smtp, IConfiguration config, ILogger<OutboxDispatcher> log)
{
    public async Task DispatchAsync(OutboxMessage message, CancellationToken ct)
    {
        switch (message.Type)
        {
            case nameof(SampleRequestReceived):
                var r = message.Read<SampleRequestReceived>();
                var baseUrl = config["PublicBaseUrl"] ?? "http://localhost:8080";
                var farmLine = r.FarmCode is null ? "General request (no farm selected)" : $"Farm: {r.FarmCode} — {baseUrl}/q/{r.FarmCode}";
                await email.SendAsync(
                    smtp.Value.StaffInbox,
                    $"New sample request: {r.CompanyName} ({r.CountryCode})",
                    $"""
                    A buyer requested a sample on FarmerSample.

                    Company: {r.CompanyName}
                    Contact: {r.ContactName} <{r.Email}>
                    Country: {r.CountryCode}
                    {farmLine}

                    Message:
                    {r.Message ?? "(none)"}

                    Handle it in the back office: {baseUrl}/admin/sample-requests
                    """,
                    replyTo: r.Email, ct);
                break;
            default:
                log.LogWarning("No handler for outbox message type {Type}; marking processed", message.Type);
                break;
        }
    }
}
