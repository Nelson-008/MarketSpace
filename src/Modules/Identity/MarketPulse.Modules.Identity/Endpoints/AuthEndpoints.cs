using MarketPulse.Application.Abstractions.Security;
using MarketPulse.Infrastructure.Common.Web;
using MarketPulse.Modules.Identity.Application;
using MarketPulse.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MarketPulse.Modules.Identity.Endpoints;

internal static class AuthEndpoints
{
    public const string RefreshCookieName = "mp_refresh";
    public const string RefreshCookiePath = "/api/v1/auth";

    /// <summary>
    /// CSRF defence for cookie-authenticated endpoints (refresh, logout): browsers cannot send this custom header
    /// cross-site without a CORS preflight, which our CORS policy only allows for the configured web origin.
    /// </summary>
    public const string CsrfHeaderName = "X-Requested-With";
    public const string CsrfHeaderValue = "MarketPulse";

    public static void Map(RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Auth");

        auth.MapPost("/register", async (RegisterRequest request, AuthService service, CancellationToken ct) =>
                Results.Accepted(value: await service.RegisterAsync(request, ct).ConfigureAwait(false)))
            .WithValidation<RegisterRequest>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<AcceptedResponse>(StatusCodes.Status202Accepted)
            .WithSummary("Register a new account. Always returns 202 to prevent account enumeration.");

        auth.MapPost("/verify-email", async (VerifyEmailRequest request, AuthService service, CancellationToken ct) =>
                ApiResults.From(await service.VerifyEmailAsync(request, ct).ConfigureAwait(false)))
            .WithValidation<VerifyEmailRequest>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces(StatusCodes.Status204NoContent);

        auth.MapPost("/resend-verification", async (EmailRequest request, AuthService service, CancellationToken ct) =>
                Results.Accepted(value: await service.ResendVerificationAsync(request, ct).ConfigureAwait(false)))
            .WithValidation<EmailRequest>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<AcceptedResponse>(StatusCodes.Status202Accepted);

        auth.MapPost("/login", async (LoginRequest request, AuthService service, HttpContext http, CancellationToken ct) =>
            {
                var result = await service.LoginAsync(request, ClientIp(http), UserAgent(http), ct).ConfigureAwait(false);
                return ToAuthResult(result, http);
            })
            .WithValidation<LoginRequest>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<AuthResponse>()
            .ProducesError(StatusCodes.Status401Unauthorized)
            .ProducesError(StatusCodes.Status403Forbidden);

        auth.MapPost("/refresh", async (AuthService service, HttpContext http, CancellationToken ct) =>
            {
                if (!HasCsrfHeader(http))
                {
                    return new ErrorResult(StatusCodes.Status403Forbidden, "auth.csrf", "Missing anti-forgery header.");
                }

                var raw = http.Request.Cookies[RefreshCookieName];
                if (string.IsNullOrEmpty(raw))
                {
                    return ApiResults.Error(RefreshTokenService.InvalidRefreshToken);
                }

                var result = await service.RefreshAsync(raw, ClientIp(http), UserAgent(http), ct).ConfigureAwait(false);
                if (result.IsFailure)
                {
                    ClearRefreshCookie(http);
                }

                return ToAuthResult(result, http);
            })
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<AuthResponse>()
            .ProducesError(StatusCodes.Status401Unauthorized)
            .WithSummary("Rotate the refresh-token cookie and issue a new access token.");

        auth.MapPost("/logout", async (AuthService service, HttpContext http, CancellationToken ct) =>
            {
                if (!HasCsrfHeader(http))
                {
                    return new ErrorResult(StatusCodes.Status403Forbidden, "auth.csrf", "Missing anti-forgery header.");
                }

                var raw = http.Request.Cookies[RefreshCookieName];
                if (!string.IsNullOrEmpty(raw))
                {
                    await service.LogoutAsync(raw, ct).ConfigureAwait(false);
                }

                ClearRefreshCookie(http);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        auth.MapPost("/forgot-password", async (EmailRequest request, AuthService service, CancellationToken ct) =>
                Results.Accepted(value: await service.ForgotPasswordAsync(request, ct).ConfigureAwait(false)))
            .WithValidation<EmailRequest>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<AcceptedResponse>(StatusCodes.Status202Accepted);

        auth.MapPost("/reset-password", async (ResetPasswordRequest request, AuthService service, CancellationToken ct) =>
                ApiResults.From(await service.ResetPasswordAsync(request, ct).ConfigureAwait(false)))
            .WithValidation<ResetPasswordRequest>()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces(StatusCodes.Status204NoContent);

        var authenticated = auth.MapGroup(string.Empty).RequireAuthorization();

        authenticated.MapGet("/profile", async (AuthService service, IUserContext user, CancellationToken ct) =>
                ApiResults.From(await service.GetProfileAsync(user.UserId, ct).ConfigureAwait(false)))
            .Produces<ProfileResponse>();

        authenticated.MapPut("/profile", async (UpdateProfileRequest request, AuthService service, IUserContext user, CancellationToken ct) =>
                ApiResults.From(await service.UpdateProfileAsync(user.UserId, request, ct).ConfigureAwait(false)))
            .WithValidation<UpdateProfileRequest>()
            .Produces<ProfileResponse>();

        authenticated.MapPost("/sessions/revoke-all", async (AuthService service, IUserContext user, HttpContext http, CancellationToken ct) =>
            {
                await service.RevokeAllSessionsAsync(user.UserId, ct).ConfigureAwait(false);
                ClearRefreshCookie(http);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .WithSummary("Sign out of all sessions. Existing access tokens remain valid until they expire (max 10 minutes).");
    }

    private static IResult ToAuthResult(Result<LoginResult> result, HttpContext http)
    {
        if (result.IsFailure)
        {
            return ApiResults.Error(result.Error);
        }

        SetRefreshCookie(http, result.Value.RefreshToken);
        return Results.Ok(result.Value.Response);
    }

    private static void SetRefreshCookie(HttpContext http, IssuedRefreshToken token) =>
        http.Response.Cookies.Append(RefreshCookieName, token.RawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = new DateTimeOffset(token.ExpiresAtUtc),
            IsEssential = true,
        });

    private static void ClearRefreshCookie(HttpContext http) =>
        http.Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
        });

    private static bool HasCsrfHeader(HttpContext http) =>
        string.Equals(http.Request.Headers[CsrfHeaderName], CsrfHeaderValue, StringComparison.Ordinal);

    private static string? ClientIp(HttpContext http) => http.Connection.RemoteIpAddress?.ToString();

    private static string? UserAgent(HttpContext http) => http.Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null;
}
