using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MarketPulse.Modules.Identity.Application;
using Shouldly;

namespace MarketPulse.IntegrationTests.Infrastructure;

public sealed record SignedInUser(HttpClient Client, string Email, string Password, AuthResponse Auth);

/// <summary>Shared helpers that drive the real HTTP API.</summary>
public static partial class TestUsers
{
    public const string DefaultPassword = "Correct-Horse-42";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static string NewEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@example.test";

    public static async Task RegisterAsync(HttpClient client, string email, string password = DefaultPassword)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, password, "Test User"));
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    public static (Guid UserId, string Token) ExtractLink(ApiFactory factory, string email, string path)
    {
        var message = factory.Emails.For(email).LastOrDefault(m => m.TextBody.Contains(path, StringComparison.Ordinal));
        message.ShouldNotBeNull($"Expected an email containing '{path}' for {email}.");
        var match = LinkRegex().Match(message.TextBody);
        match.Success.ShouldBeTrue();
        return (Guid.Parse(match.Groups["user"].Value), match.Groups["token"].Value);
    }

    public static async Task VerifyAsync(ApiFactory factory, HttpClient client, string email)
    {
        var (userId, token) = ExtractLink(factory, email, "/verify-email");
        var response = await client.PostAsJsonAsync("/api/v1/auth/verify-email", new VerifyEmailRequest(userId, token));
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    public static async Task<AuthResponse> LoginAsync(HttpClient client, string email, string password = DefaultPassword)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return auth;
    }

    /// <summary>Registers, verifies and signs in a new user on its own client (own cookie jar).</summary>
    public static async Task<SignedInUser> CreateSignedInAsync(ApiFactory factory, string prefix = "user", Func<string, Task>? beforeLogin = null)
    {
        var client = factory.CreateApiClient();
        var email = NewEmail(prefix);
        await RegisterAsync(client, email);
        await VerifyAsync(factory, client, email);
        if (beforeLogin is not null)
        {
            await beforeLogin(email);
        }

        var auth = await LoginAsync(client, email);
        return new SignedInUser(client, email, DefaultPassword, auth);
    }

    public static async Task<ApiErrorBody> ReadErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApiErrorBody>(Json);
        body.ShouldNotBeNull();
        body.CorrelationId.ShouldNotBeNullOrWhiteSpace();
        return body;
    }

    public static HttpRequestMessage CsrfPost(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("X-Requested-With", "MarketPulse");
        return request;
    }

    [GeneratedRegex(@"userId=(?<user>[0-9a-fA-F-]{36})&token=(?<token>[A-Za-z0-9_\-]+)")]
    private static partial Regex LinkRegex();
}

public sealed record ApiErrorBody(string Code, string Message, string CorrelationId, Dictionary<string, string[]>? Errors);
