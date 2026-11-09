using System.Text.Json;

namespace FarmerSample.Core.Outbox;

/// <summary>Transactional outbox: written in the same transaction as the business change, processed by the Worker.</summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Type { get; set; }
    public required string Payload { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }

    public static OutboxMessage Create<T>(T message) where T : notnull => new()
    {
        Type = typeof(T).Name,
        Payload = JsonSerializer.Serialize(message),
    };

    public T Read<T>() => JsonSerializer.Deserialize<T>(Payload)
                          ?? throw new InvalidOperationException($"Empty payload for {Type}");
}
