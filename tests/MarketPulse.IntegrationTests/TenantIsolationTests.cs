using System.Net;
using System.Net.Http.Json;
using MarketPulse.Application.Abstractions.Security;
using MarketPulse.Infrastructure.Common.Persistence;
using MarketPulse.IntegrationTests.Infrastructure;
using MarketPulse.Modules.Workspace.Application;
using MarketPulse.Modules.Workspace.Domain;
using MarketPulse.Modules.Workspace.Infrastructure;
using MarketPulse.SharedKernel.Identifiers;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using static MarketPulse.IntegrationTests.Infrastructure.TestUsers;

namespace MarketPulse.IntegrationTests;

/// <summary>Spec §16.2: "A user cannot access another user's watchlist, paper portfolio, alerts or reports."</summary>
[Collection(ApiTestGroup.Name)]
public sealed class TenantIsolationTests(ApiFactory factory)
{
    [Fact]
    public async Task Another_user_cannot_read_rename_or_delete_my_watchlist()
    {
        var alice = await CreateSignedInAsync(factory, "alice");
        var bob = await CreateSignedInAsync(factory, "bob");

        var created = await alice.Client.PostAsJsonAsync("/api/v1/watchlists", new WatchlistRequest("Alice private"));
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var watchlist = (await created.Content.ReadFromJsonAsync<WatchlistResponse>(Json))!;
        var url = $"/api/v1/watchlists/{watchlist.Id}";

        (await bob.Client.GetAsync(url)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await bob.Client.PutAsJsonAsync(url, new WatchlistRequest("hijacked"))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await bob.Client.DeleteAsync(url)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await bob.Client.GetFromJsonAsync<List<WatchlistResponse>>("/api/v1/watchlists", Json))!.ShouldBeEmpty();

        var stillThere = await alice.Client.GetFromJsonAsync<WatchlistResponse>(url, Json);
        stillThere!.Name.ShouldBe("Alice private");
    }

    [Fact]
    public async Task Preferences_are_per_user()
    {
        var alice = await CreateSignedInAsync(factory, "alice");
        var bob = await CreateSignedInAsync(factory, "bob");

        (await alice.Client.PutAsJsonAsync("/api/v1/preferences", new PreferencesRequest(ExperienceMode.Pro), Json)).StatusCode.ShouldBe(HttpStatusCode.OK);

        (await alice.Client.GetFromJsonAsync<PreferencesResponse>("/api/v1/preferences", Json))!.ExperienceMode.ShouldBe(ExperienceMode.Pro);
        (await bob.Client.GetFromJsonAsync<PreferencesResponse>("/api/v1/preferences", Json))!.ExperienceMode.ShouldBe(ExperienceMode.Explore);
    }

    [Fact]
    public async Task Persistence_layer_blocks_a_cross_tenant_write_even_if_application_code_tries()
    {
        var alice = await CreateSignedInAsync(factory, "alice");
        var bob = await CreateSignedInAsync(factory, "bob");
        var created = await alice.Client.PostAsJsonAsync("/api/v1/watchlists", new WatchlistRequest("target"));
        var id = (await created.Content.ReadFromJsonAsync<WatchlistResponse>(Json))!.Id;

        // Simulate buggy code running as Bob that bypasses the query filter and edits Alice's row.
        var options = new DbContextOptionsBuilder<WorkspaceDbContext>().UseSqlServer(factory.ConnectionString).Options;
        await using var db = new WorkspaceDbContext(options, new FixedUser(bob.Auth.Profile.UserId, bob.Auth.Profile.TenantId));

        var alicesRow = await db.Watchlists.IgnoreQueryFilters([ModuleDbContext.TenantFilterName]).SingleAsync(w => w.Id == id);
        alicesRow.Name = "tampered";

        await Should.ThrowAsync<CrossTenantWriteException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Query_filter_hides_other_tenants_rows_at_the_persistence_layer()
    {
        var alice = await CreateSignedInAsync(factory, "alice");
        var bob = await CreateSignedInAsync(factory, "bob");
        await alice.Client.PostAsJsonAsync("/api/v1/watchlists", new WatchlistRequest("only alice"));

        var options = new DbContextOptionsBuilder<WorkspaceDbContext>().UseSqlServer(factory.ConnectionString).Options;
        await using var db = new WorkspaceDbContext(options, new FixedUser(bob.Auth.Profile.UserId, bob.Auth.Profile.TenantId));

        (await db.Watchlists.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Watchlist_names_must_be_unique_per_user_and_plan_limit_is_enforced_server_side()
    {
        var user = await CreateSignedInAsync(factory);

        (await user.Client.PostAsJsonAsync("/api/v1/watchlists", new WatchlistRequest("Main"))).StatusCode.ShouldBe(HttpStatusCode.Created);
        var duplicate = await user.Client.PostAsJsonAsync("/api/v1/watchlists", new WatchlistRequest("Main"));
        duplicate.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ReadErrorAsync(duplicate)).Code.ShouldBe("watchlist.duplicate_name");

        for (var i = 2; i <= 10; i++)
        {
            (await user.Client.PostAsJsonAsync("/api/v1/watchlists", new WatchlistRequest($"List {i}"))).StatusCode.ShouldBe(HttpStatusCode.Created);
        }

        var overLimit = await user.Client.PostAsJsonAsync("/api/v1/watchlists", new WatchlistRequest("List 11"));
        overLimit.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ReadErrorAsync(overLimit)).Code.ShouldBe("entitlement.limit_reached");
    }

    private sealed class FixedUser(Guid userId, Guid tenantId) : IUserContext
    {
        public bool IsAuthenticated => true;

        public bool IsSystem => false;

        public UserId UserId => new(userId);

        public TenantId TenantId => new(tenantId);

        public bool IsInRole(string role) => false;
    }
}
