using System.Security.Cryptography;

namespace FarmerSample.Core.SharedKernel;

/// <summary>
/// Random, non-sequential public codes such as <c>FM-7KQ2-M9XD</c>: a 2-letter prefix,
/// 7 random Crockford base32 characters and 1 check character (mod 37).
/// Codes cannot be enumerated and reveal nothing about volumes.
/// </summary>
public static class PublicCode
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";   // Crockford base32
    private const string CheckAlphabet = Alphabet + "*~$=U";              // Crockford check symbols (37)

    public const string FarmPrefix = "FM";
    public const string LotPrefix = "LT";
    public const string PouchPrefix = "PO";

    public static string New(string prefix)
    {
        if (prefix is not { Length: 2 } || !prefix.All(char.IsAsciiLetterUpper))
            throw new ArgumentException("Prefix must be two upper-case letters.", nameof(prefix));

        Span<char> body = stackalloc char[7];
        for (var i = 0; i < body.Length; i++)
            body[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

        var payload = new string(body);
        var check = CheckChar(payload);
        return $"{prefix}-{payload[..4]}-{payload[4..]}{check}";
    }

    /// <summary>Normalises user input (case, dashes, I/L→1, O→0) and validates the check character.</summary>
    public static bool TryNormalize(string? input, out string code)
    {
        code = string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var raw = input.Trim().ToUpperInvariant().Replace("-", "").Replace(" ", "");
        if (raw.Length != 10 || !char.IsAsciiLetterUpper(raw[0]) || !char.IsAsciiLetterUpper(raw[1])) return false;

        var payload = new string(raw[2..9].Select(Fix).ToArray());
        if (payload.Any(c => Alphabet.IndexOf(c) < 0)) return false;
        var check = Fix(raw[9]);
        if (CheckChar(payload) != check) return false;

        code = $"{raw[..2]}-{payload[..4]}-{payload[4..]}{check}";
        return true;
    }

    private static char Fix(char c) => c switch { 'I' or 'L' => '1', 'O' => '0', _ => c };

    private static char CheckChar(string payload)
    {
        long value = 0;
        foreach (var c in payload) value = value * 32 + Alphabet.IndexOf(c);
        return CheckAlphabet[(int)(value % 37)];
    }
}
