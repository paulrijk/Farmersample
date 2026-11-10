using System.Globalization;
using System.Net;
using System.Net.Http.Json;

namespace FarmerSample.IntegrationTests;

public class PublicApiTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Privacy_canary_never_leaks_through_public_endpoints()
    {
        var client = api.CreateClient();
        var list = await client.GetStringAsync("/api/public/farms");
        Assert.Contains("Canary Farm", list);

        var code = System.Text.Json.JsonDocument.Parse(list).RootElement.EnumerateArray()
            .First(f => f.GetProperty("name").GetString() == "Canary Farm").GetProperty("code").GetString()!;
        var detail = await client.GetStringAsync($"/api/public/farms/{code}");

        foreach (var body in new[] { list, detail })
        {
            Assert.DoesNotContain("CANARY 0001", body);
            Assert.DoesNotContain(ApiFixture.CanaryLegalName, body);
            Assert.DoesNotContain(ApiFixture.CanaryLatitude.ToString(CultureInfo.InvariantCulture)[..6], body);
            Assert.DoesNotContain("phone", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("latitude", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Farms_without_public_profile_consent_are_not_public()
    {
        var list = await api.CreateClient().GetStringAsync("/api/public/farms");
        Assert.DoesNotContain("Hidden Farm", list);
    }

    [Fact]
    public async Task Public_responses_are_cacheable_for_a_minute()
    {
        var response = await api.CreateClient().GetAsync("/api/public/farms");
        Assert.Equal("public, max-age=60", response.Headers.CacheControl?.ToString());
    }

    [Theory]
    [InlineData("nonsense")]
    [InlineData("FM-0000-0000")]
    public async Task Unknown_or_invalid_codes_return_404(string code)
    {
        var response = await api.CreateClient().GetAsync($"/api/public/farms/{code}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Sample_request_is_accepted_and_honeypot_is_silently_dropped()
    {
        var client = api.CreateClient();
        var ok = await client.PostAsJsonAsync("/api/public/sample-requests",
            new { companyName = "Test BV", contactName = "Ann", email = "ann@example.com", countryCode = "NL", message = "100 g please" });
        Assert.Equal(HttpStatusCode.Accepted, ok.StatusCode);

        var bot = await client.PostAsJsonAsync("/api/public/sample-requests",
            new { companyName = "Bot", contactName = "Bot", email = "bot@example.com", countryCode = "NL", website = "http://spam" });
        Assert.Equal(HttpStatusCode.Accepted, bot.StatusCode);

        var bad = await client.PostAsJsonAsync("/api/public/sample-requests",
            new { companyName = "", contactName = "Ann", email = "not-an-email", countryCode = "NL" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Staff_endpoints_require_sign_in()
    {
        var response = await api.CreateClient().GetAsync("/api/v1/staff/farms");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/v1/admin/users")]
    [InlineData("POST", "/api/v1/admin/users")]
    public async Task User_management_requires_sign_in(string method, string url)
    {
        var response = await api.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), url));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
