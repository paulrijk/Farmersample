using FarmerSample.Core.SharedKernel;

namespace FarmerSample.UnitTests;

public class PublicCodeTests
{
    [Fact]
    public void New_codes_validate_and_have_expected_shape()
    {
        for (var i = 0; i < 1000; i++)
        {
            var code = PublicCode.New(PublicCode.LotPrefix);
            Assert.Matches("^LT-[0-9A-HJKMNP-TV-Z]{4}-[0-9A-HJKMNP-TV-Z]{4}$", code);
            Assert.True(PublicCode.TryNormalize(code, out var normalized));
            Assert.Equal(code, normalized);
        }
    }

    [Fact]
    public void Codes_are_not_sequential()
    {
        var codes = Enumerable.Range(0, 200).Select(_ => PublicCode.New("FM")).ToHashSet();
        Assert.Equal(200, codes.Count);
    }

    [Fact]
    public void Normalizes_human_input_lowercase_dashes_and_lookalikes()
    {
        var code = PublicCode.New("PO");
        var messy = " " + code.ToLowerInvariant().Replace("-", "").Replace('1', 'l').Replace('0', 'o') + " ";
        Assert.True(PublicCode.TryNormalize(messy, out var normalized));
        Assert.Equal(code, normalized);
    }

    [Fact]
    public void Every_single_typo_is_rejected_by_the_check_character()
    {
        const string alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        for (var n = 0; n < 50; n++)
        {
            var code = PublicCode.New("FM");
            foreach (var pos in new[] { 3, 4, 5, 6, 8, 9, 10, 11 })
                foreach (var c in alphabet.Where(c => c != code[pos]))
                {
                    var chars = code.ToCharArray(); chars[pos] = c;
                    Assert.False(PublicCode.TryNormalize(new string(chars), out _), $"{new string(chars)} accepted");
                }
        }
    }

    [Fact]
    public void Swapping_two_neighbouring_characters_is_rejected_except_0_and_Z()
    {
        for (var n = 0; n < 200; n++)
        {
            var code = PublicCode.New("LT").Replace("-", "");
            for (var i = 2; i < 8; i++)
            {
                if (code[i] == code[i + 1] || (code[i] is '0' or 'Z' && code[i + 1] is '0' or 'Z')) continue;   // Luhn's one blind spot
                var chars = code.ToCharArray(); (chars[i], chars[i + 1]) = (chars[i + 1], chars[i]);
                Assert.False(PublicCode.TryNormalize(new string(chars), out _), $"{new string(chars)} accepted");
            }
        }
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("FM-1234")] [InlineData("12-ABCD-EFGH")] [InlineData("FM-ABCD-EFGU!")]
    public void Garbage_is_rejected(string? input) => Assert.False(PublicCode.TryNormalize(input, out _));
}

public class MoneyTests
{
    [Fact]
    public void Adds_same_currency() => Assert.Equal(Money.Of(350, "EUR"), Money.Of(100, "eur").Add(Money.Of(250, "EUR")));

    [Fact]
    public void Refuses_mixed_currencies() =>
        Assert.Throws<InvalidOperationException>(() => Money.Of(100, "EUR").Add(Money.Of(100, "USD")));

    [Fact]
    public void Five_percent_commission_on_a_container() =>
        Assert.Equal(Money.Of(375_000, "EUR"), Money.Of(7_500_000, "EUR").Percentage(500));

    [Fact]
    public void Rejects_invalid_currency() => Assert.Throws<ArgumentException>(() => Money.Of(1, "EURO"));
}
