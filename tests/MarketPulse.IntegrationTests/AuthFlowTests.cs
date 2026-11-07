using System.Net;
using System.Net.Http.Json;
using MarketPulse.Application.Abstractions.Auditing;
using MarketPulse.Application.Abstractions.Security;
using MarketPulse.IntegrationTests.Infrastructure;
using MarketPulse.Modules.Identity.Application;
using MarketPulse.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static MarketPulse.IntegrationTests.Infrastructure.TestUsers;

namespace MarketPulse.IntegrationTests;

[Collection(ApiTestGroup.Name)]
public sealed class AuthFlowTests(ApiFactory factory)
{
    [Fact]
    public async Task Register_verify_login_and_read_profile()
    {
        var user = await CreateSignedInAsync(factory);

        user.Auth.AccessToken.ShouldNotBeNullOrWhiteSpace();
        user.Auth.Profile.Email.ShouldBe(user.Email);
        user.Auth.Profile.EmailVerified.ShouldBeTrue();

        var profile = await user.Client.GetFromJsonAsync<ProfileResponse>("/api/v1/auth/profile", Json);
        profile!.Roles.ShouldBe([Roles.User]);
        profile.Entitlements.ShouldContain(e => e.FeatureKey == FeatureKeys.Watchlists && e.Limit == 10);
        profile.TenantId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Login_sets_HttpOnly_Secure_SameSite_strict_refresh_cookie_scoped_to_auth_path()
    {
        var client = factory.CreateApiClient();
        var email = NewEmail();
        await RegisterAsync(client, email);
        await VerifyAsync(factory, client, email);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, DefaultPassword));

        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("mp_refresh=", StringComparison.Ordinal));
        cookie.ShouldContain("httponly", Case.Insensitive);
        cookie.ShouldContain("secure", Case.Insensitive);
        cookie.ShouldContain("samesite=strict", Case.Insensitive);
        cookie.ShouldContain("path=/api/v1/auth", Case.Insensitive);
        (await response.Content.ReadAsStringAsync()).ShouldNotContain("refresh", Case.Insensitive);
    }

    [Fact]
    public async Task Login_before_email_verification_is_refused()
    {
        var client = factory.CreateApiClient();
        var email = NewEmail();
        await RegisterAsync(client, email);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, DefaultPassword));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ReadErrorAsync(response)).Code.ShouldBe("auth.email_not_verified");
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_return_the_same_generic_error()
    {
        var user = await CreateSignedInAsync(factory);
        var client = factory.CreateApiClient();

        var wrongPassword = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(user.Email, "Wrong-Password-99"));
        var unknown = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(NewEmail(), "Wrong-Password-99"));

        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unknown.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await ReadErrorAsync(wrongPassword)).Code.ShouldBe("auth.invalid_credentials");
        (await ReadErrorAsync(unknown)).Code.ShouldBe("auth.invalid_credentials");
    }

    [Fact]
    public async Task Duplicate_registration_returns_accepted_without_sending_a_second_email()
    {
        var client = factory.CreateApiClient();
        var email = NewEmail();
        await RegisterAsync(client, email);
        await RegisterAsync(client, email);

        factory.Emails.For(email).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Weak_password_is_rejected_with_validation_envelope()
    {
        var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(NewEmail(), "short", "X"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.Code.ShouldBe("validation.failed");
        error.Errors.ShouldNotBeNull();
        error.Errors.ShouldContainKey("password");
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_issues_a_new_access_token()
    {
        var user = await CreateSignedInAsync(factory);

        var response = await user.Client.SendAsync(CsrfPost("/api/v1/auth/refresh"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
        auth!.AccessToken.ShouldNotBe(user.Auth.AccessToken);
        response.Headers.GetValues("Set-Cookie").ShouldContain(c => c.StartsWith("mp_refresh=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Reusing_a_rotated_refresh_token_revokes_the_whole_session_family()
    {
        var user = await CreateSignedInAsync(factory);
        var stolenCookie = CookieValue(user.Client);

        (await user.Client.SendAsync(CsrfPost("/api/v1/auth/refresh"))).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Attacker replays the old (already rotated) token from a different client.
        var attacker = factory.CreateApiClient();
        var replay = CsrfPost("/api/v1/auth/refresh");
        replay.Headers.Add("Cookie", $"mp_refresh={stolenCookie}");
        var replayResponse = await attacker.SendAsync(replay);
        replayResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // The legitimate holder's newer token is now revoked too.
        (await user.Client.SendAsync(CsrfPost("/api/v1/auth/refresh"))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        (await db.AuditLogs.AnyAsync(a => a.Action == AuditActions.RefreshTokenReuseDetected && a.TargetId == user.Auth.Profile.UserId.ToString()))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Refresh_without_anti_forgery_header_is_refused()
    {
        var user = await CreateSignedInAsync(factory);

        var response = await user.Client.PostAsync("/api/v1/auth/refresh", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ReadErrorAsync(response)).Code.ShouldBe("auth.csrf");
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        var user = await CreateSignedInAsync(factory);
        var cookie = CookieValue(user.Client);

        (await user.Client.SendAsync(CsrfPost("/api/v1/auth/logout"))).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var replay = CsrfPost("/api/v1/auth/refresh");
        replay.Headers.Add("Cookie", $"mp_refresh={cookie}");
        (await factory.CreateApiClient().SendAsync(replay)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Password_reset_flow_changes_password_and_revokes_sessions()
    {
        var user = await CreateSignedInAsync(factory);
        var oldCookie = CookieValue(user.Client);
        var anonymous = factory.CreateApiClient();

        (await anonymous.PostAsJsonAsync("/api/v1/auth/forgot-password", new EmailRequest(user.Email))).StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var (userId, token) = ExtractLink(factory, user.Email, "/reset-password");
        const string newPassword = "Another-Strong-77";
        (await anonymous.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(userId, token, newPassword)))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await LoginAsync(factory.CreateApiClient(), user.Email, newPassword);
        var replay = CsrfPost("/api/v1/auth/refresh");
        replay.Headers.Add("Cookie", $"mp_refresh={oldCookie}");
        (await factory.CreateApiClient().SendAsync(replay)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Forgot_password_for_unknown_email_is_accepted_and_sends_nothing()
    {
        var email = NewEmail();

        var response = await factory.CreateApiClient().PostAsJsonAsync("/api/v1/auth/forgot-password", new EmailRequest(email));

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        factory.Emails.For(email).ShouldBeEmpty();
    }

    [Fact]
    public async Task Profile_without_token_returns_401_envelope_and_echoes_correlation_id()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/profile");
        request.Headers.Add("X-Correlation-Id", "test-corr-123");

        var response = await factory.CreateApiClient().SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.GetValues("X-Correlation-Id").Single().ShouldBe("test-corr-123");
        var error = await ReadErrorAsync(response);
        error.Code.ShouldBe("auth.unauthenticated");
        error.CorrelationId.ShouldBe("test-corr-123");
    }

    [Fact]
    public async Task Tampered_access_token_is_rejected()
    {
        var user = await CreateSignedInAsync(factory);
        var parts = user.Auth.AccessToken.Split('.');
        var tampered = $"{parts[0]}.{parts[1]}.{new string('A', parts[2].Length)}";
        var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", tampered);

        (await client.GetAsync("/api/v1/auth/profile")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private string CookieValue(HttpClient client) => factory.RefreshCookieOf(client);
}
