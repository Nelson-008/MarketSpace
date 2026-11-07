using System.Net;
using System.Net.Http.Json;
using MarketPulse.IntegrationTests.Infrastructure;
using MarketPulse.Modules.Administration;
using MarketPulse.Modules.Instruments;
using Shouldly;
using static MarketPulse.IntegrationTests.Infrastructure.TestUsers;

namespace MarketPulse.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class SystemAndContractTests(ApiFactory factory)
{
    [Fact]
    public async Task System_status_reports_demo_mode_when_no_licensed_provider_is_configured()
    {
        var status = await factory.CreateApiClient().GetFromJsonAsync<SystemStatusResponse>("/api/v1/system/status", Json);

        status!.DataMode.ShouldBe(PlatformDataMode.NoProvider);
        status.ShowDemoBanner.ShouldBeTrue();
        status.DataNotice.ShouldStartWith("DEMO");
        status.Disclaimer.ShouldContain("not guarantees of future performance");
    }

    [Fact]
    public async Task Health_endpoints_respond()
    {
        var client = factory.CreateApiClient();

        (await client.GetAsync("/health/live")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/health/ready")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Api_responses_carry_security_headers()
    {
        var response = await factory.CreateApiClient().GetAsync("/api/v1/system/status");

        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        response.Headers.GetValues("X-Frame-Options").ShouldContain("DENY");
        response.Headers.GetValues("Content-Security-Policy").Single().ShouldContain("default-src 'none'");
    }

    [Fact]
    public async Task Unknown_route_returns_404_envelope()
    {
        var response = await factory.CreateApiClient().GetAsync("/api/v1/does-not-exist");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ReadErrorAsync(response)).Code.ShouldBe("resource.not_found");
    }

    [Fact]
    public async Task Instrument_search_is_empty_until_a_master_is_imported_and_never_invents_data()
    {
        var user = await CreateSignedInAsync(factory);

        var page = await user.Client.GetFromJsonAsync<InstrumentPage>("/api/v1/instruments?q=REL", Json);

        page!.TotalCount.ShouldBe(0);
        page.Items.ShouldBeEmpty();
        (await user.Client.GetAsync($"/api/v1/instruments/{Guid.NewGuid()}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// API contract snapshot (spec §10.1: publish OpenAPI; version breaking changes). The committed document lives at
    /// docs/api/openapi.json. Set UPDATE_OPENAPI_SNAPSHOT=1 to accept an intentional change.
    /// </summary>
    [Fact]
    public async Task OpenApi_document_matches_committed_snapshot()
    {
        var actual = Normalize(await factory.CreateApiClient().GetStringAsync("/openapi/v1.json"));
        var path = Path.Combine(RepoRoot(), "docs", "api", "openapi.json");

        if (!File.Exists(path) || Environment.GetEnvironmentVariable("UPDATE_OPENAPI_SNAPSHOT") == "1")
        {
            await File.WriteAllTextAsync(path, actual);
            return;
        }

        var expected = Normalize(await File.ReadAllTextAsync(path));
        actual.ShouldBe(expected, "The public API changed. Review it, then re-run with UPDATE_OPENAPI_SNAPSHOT=1 to accept.");
    }

    private static string Normalize(string json) => json.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd() + "\n";

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MarketPulse.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
