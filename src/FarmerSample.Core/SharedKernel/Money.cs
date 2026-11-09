namespace FarmerSample.Core.SharedKernel;

/// <summary>Money in minor units (e.g. cents) with an ISO-4217 currency. Never use floating point for money.</summary>
public readonly record struct Money(long Minor, string Currency)
{
    public static Money Zero(string currency) => new(0, Normalize(currency));

    public static Money Of(long minor, string currency) => new(minor, Normalize(currency));

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Minor = checked(Minor + other.Minor) };
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Minor = checked(Minor - other.Minor) };
    }

    /// <summary>Applies a percentage given in basis points (500 = 5%), rounding half away from zero.</summary>
    public Money Percentage(int basisPoints) =>
        this with { Minor = (long)Math.Round(Minor * (decimal)basisPoints / 10_000m, MidpointRounding.AwayFromZero) };

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
            throw new InvalidOperationException($"Currency mismatch: {Currency} vs {other.Currency}");
    }

    private static string Normalize(string currency)
    {
        if (currency is null || currency.Length != 3 || !currency.All(char.IsAsciiLetter))
            throw new ArgumentException("Currency must be a 3-letter ISO-4217 code.", nameof(currency));
        return currency.ToUpperInvariant();
    }

    public override string ToString() => $"{Minor / 100m:0.00} {Currency}";
}
