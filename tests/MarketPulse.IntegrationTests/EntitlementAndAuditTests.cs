using System.Net;
using System.Net.Http.Json;
using MarketPulse.Application.Abstractions.Auditing;
using MarketPulse.Application.Abstractions.Security;
using MarketPulse.IntegrationTests.Infrastructure;
using MarketPulse.Modules.Identity.Domain;
using MarketPulse.Modules.Identity.Endpoints;
using MarketPulse.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static MarketPulse.IntegrationTests.Infrastructure.TestUsers;

namespace MarketPulse.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class EntitlementAndAuditTests(ApiFactory factory)
{
    [Fact]
    public async Task Expired_subscription_removes_access_to_entitled_features()
    {
        var user = await CreateSignedInAsync(factory);
        (await user.Client.GetAsync("/api/v1/watchlists")).StatusCode.ShouldBe(HttpStatusCode.OK);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
            var now = DateTime.UtcNow;
            await db.Subscriptions.Where(s => s.TenantId == user.Auth.Profile.TenantId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ValidFromUtc, now.AddDays(-2)).SetProperty(x => x.ValidToUtc, now.AddDays(-1)));
        }

        var response = await user.Client.GetAsync("/api/v1/watchlists");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ReadErrorAsync(response)).Code.ShouldBe("entitlement.missing");
    }

    [Fact]
    public async Task Registration_and_login_are_audited()
    {
        var user = await CreateSignedInAsync(factory);
        var userId = user.Auth.Profile.UserId.ToString();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        var actions = await db.AuditLogs.Where(a => a.TargetId == userId).Select(a => a.Action).ToListAsync();

        actions.ShouldContain(AuditActions.UserRegistered);
        actions.ShouldContain(AuditActions.EmailVerified);
        actions.ShouldContain(AuditActions.LoginSucceeded);
    }

    [Fact]
    public async Task Audit_log_rows_cannot_be_updated_or_deleted()
    {
        await CreateSignedInAsync(factory);
        await using var connection = new SqlConnection(factory.ConnectionString);
        await connection.OpenAsync();

        await using var update = connection.CreateCommand();
        update.CommandText = "UPDATE TOP (1) [auth].[AuditLogs] SET [Action] = 'tampered'";
        (await Should.ThrowAsync<SqlException>(() => update.ExecuteNonQueryAsync())).Message.ShouldContain("append-only");

        await using var delete = connection.CreateCommand();
        delete.CommandText = "DELETE TOP (1) FROM [auth].[AuditLogs]";
        (await Should.ThrowAsync<SqlException>(() => delete.ExecuteNonQueryAsync())).Message.ShouldContain("append-only");
    }

    [Fact]
    public async Task Audit_endpoint_requires_admin_or_support_role()
    {
        var user = await CreateSignedInAsync(factory);

        var response = await user.Client.GetAsync("/api/v1/admin/audit");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ReadErrorAsync(response)).Code.ShouldBe("auth.forbidden");
    }

    [Fact]
    public async Task Admin_can_read_audit_log_and_the_read_is_itself_audited()
    {
        var admin = await CreateSignedInAsync(factory, "admin", beforeLogin: GrantAdminAsync);

        var page = await admin.Client.GetFromJsonAsync<PagedResponse<AuditLogItem>>("/api/v1/admin/audit?pageSize=500", Json);

        page!.PageSize.ShouldBe(AdminAuditEndpoints.MaxPageSize);
        page.Items.ShouldNotBeEmpty();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        (await db.AuditLogs.AnyAsync(a => a.Action == AuditActions.AuditLogViewed && a.ActorUserId == admin.Auth.Profile.UserId)).ShouldBeTrue();
    }

    [Fact]
    public async Task Admin_providers_endpoint_is_role_protected()
    {
        var user = await CreateSignedInAsync(factory);
        (await user.Client.GetAsync("/api/v1/admin/providers")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var admin = await CreateSignedInAsync(factory, "admin", beforeLogin: GrantAdminAsync);
        (await admin.Client.GetAsync("/api/v1/admin/providers")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task GrantAdminAsync(string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        (await users.AddToRoleAsync(user!, Roles.Admin)).Succeeded.ShouldBeTrue();
    }
}
