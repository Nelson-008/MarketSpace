using System.Text;
using MarketPulse.Application.Abstractions.Auditing;
using MarketPulse.Application.Abstractions.Messaging;
using MarketPulse.Application.Abstractions.Security;
using MarketPulse.Modules.Identity.Domain;
using MarketPulse.Modules.Identity.Infrastructure;
using MarketPulse.SharedKernel.Identifiers;
using MarketPulse.SharedKernel.Results;
using MarketPulse.SharedKernel.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketPulse.Modules.Identity.Application;

internal sealed record LoginResult(AuthResponse Response, IssuedRefreshToken RefreshToken);

/// <summary>
/// Account workflows. Anti-enumeration: register, resend-verification and forgot-password always return the same
/// "accepted" response, and login failures share one generic error.
/// </summary>
internal sealed partial class AuthService(
    IdentityModuleDbContext db,
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    TokenService tokens,
    RefreshTokenService refreshTokens,
    IEntitlementService entitlements,
    IEmailSender email,
    IAuditWriter audit,
    IClock clock,
    IOptions<AppLinkOptions> links,
    ILogger<AuthService> logger)
{
    public const string AcceptedMessage = "If the address can be used, an email with further instructions has been sent.";

    private static readonly Error InvalidCredentials =
        Error.Unauthorized("auth.invalid_credentials", "Invalid email or password, or the account is temporarily locked.");

    private static readonly Error InvalidToken =
        Error.Validation("auth.invalid_token", "The link is invalid or has expired.");

    public async Task<AcceptedResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var existing = await users.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (existing is not null)
        {
            LogDuplicateRegistration(logger);
            return new AcceptedResponse(AcceptedMessage);
        }

        var now = clock.UtcNow;
        var tenant = new Tenant { Id = TenantId.New().Value, Name = request.DisplayName.Trim(), CreatedAtUtc = now };
        var user = new ApplicationUser
        {
            Id = UserId.New().Value,
            TenantId = tenant.Id,
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName.Trim(),
            CreatedAtUtc = now,
        };

        IdentityResult? createResult = null;
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
            db.Tenants.Add(tenant);
            db.Subscriptions.Add(new Subscription
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenant.Id,
                PlanId = SeedData.BetaPlanId,
                ValidFromUtc = now,
                CreatedAtUtc = now,
            });
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            createResult = await users.CreateAsync(user, request.Password).ConfigureAwait(false);
            if (!createResult.Succeeded)
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return;
            }

            var roleResult = await users.AddToRoleAsync(user, Roles.User).ConfigureAwait(false);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException("Could not assign the default role.");
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }).ConfigureAwait(false);

        if (createResult is not { Succeeded: true })
        {
            // Password policy is validated up-front, so this is a race (duplicate) or an unexpected store error.
            LogRegistrationFailed(logger, string.Join(",", createResult?.Errors.Select(e => e.Code) ?? []));
            return new AcceptedResponse(AcceptedMessage);
        }

        await audit.WriteAsync(new AuditEntry(AuditActions.UserRegistered, "User", user.Id.ToString()), ct).ConfigureAwait(false);
        await audit.WriteAsync(new AuditEntry(AuditActions.SubscriptionCreated, "Tenant", tenant.Id.ToString(),
            new Dictionary<string, string?> { ["plan"] = "beta" }), ct).ConfigureAwait(false);
        await SendVerificationEmailAsync(user, ct).ConfigureAwait(false);
        return new AcceptedResponse(AcceptedMessage);
    }

    public async Task<Result> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);
        if (user is null || !TryDecode(request.Token, out var token))
        {
            return Result.Failure(InvalidToken);
        }

        if (user.EmailConfirmed)
        {
            return Result.Success();
        }

        var result = await users.ConfirmEmailAsync(user, token).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return Result.Failure(InvalidToken);
        }

        await audit.WriteAsync(new AuditEntry(AuditActions.EmailVerified, "User", user.Id.ToString()), ct).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<AcceptedResponse> ResendVerificationAsync(EmailRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is { EmailConfirmed: false, Status: AccountStatus.Active })
        {
            await SendVerificationEmailAsync(user, ct).ConfigureAwait(false);
        }

        return new AcceptedResponse(AcceptedMessage);
    }

    public async Task<Result<LoginResult>> LoginAsync(LoginRequest request, string? ip, string? userAgent, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is null)
        {
            // Spend comparable time to a real check to reduce timing-based enumeration.
            users.PasswordHasher.HashPassword(new ApplicationUser(), request.Password);
            await audit.WriteAsync(new AuditEntry(AuditActions.LoginFailed, "Email", null,
                new Dictionary<string, string?> { ["reason"] = "unknown_user" }), ct).ConfigureAwait(false);
            return InvalidCredentials;
        }

        var check = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true).ConfigureAwait(false);
        if (check.IsLockedOut)
        {
            await audit.WriteAsync(new AuditEntry(AuditActions.LockedOut, "User", user.Id.ToString()), ct).ConfigureAwait(false);
            return InvalidCredentials;
        }

        if (!check.Succeeded || user.Status != AccountStatus.Active)
        {
            await audit.WriteAsync(new AuditEntry(AuditActions.LoginFailed, "User", user.Id.ToString(),
                new Dictionary<string, string?> { ["reason"] = check.Succeeded ? "inactive" : "bad_password" }), ct).ConfigureAwait(false);
            return InvalidCredentials;
        }

        if (!user.EmailConfirmed)
        {
            // Only revealed after a correct password, so it does not leak account existence.
            return Error.Forbidden("auth.email_not_verified", "Please verify your email address before signing in.");
        }

        user.LastLoginAtUtc = clock.UtcNow;
        await users.UpdateAsync(user).ConfigureAwait(false);

        var refresh = await refreshTokens.IssueAsync(user.Id, familyId: null, ip, userAgent, ct).ConfigureAwait(false);
        var response = await BuildAuthResponseAsync(user, ct).ConfigureAwait(false);
        await audit.WriteAsync(new AuditEntry(AuditActions.LoginSucceeded, "User", user.Id.ToString()), ct).ConfigureAwait(false);
        return new LoginResult(response, refresh);
    }

    public async Task<Result<LoginResult>> RefreshAsync(string rawRefreshToken, string? ip, string? userAgent, CancellationToken ct)
    {
        var rotated = await refreshTokens.RotateAsync(rawRefreshToken, ip, userAgent, ct).ConfigureAwait(false);
        if (rotated.IsFailure)
        {
            return rotated.Error;
        }

        var user = await users.FindByIdAsync(rotated.Value.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return RefreshTokenService.InvalidRefreshToken;
        }

        var response = await BuildAuthResponseAsync(user, ct).ConfigureAwait(false);
        return new LoginResult(response, rotated.Value.Token);
    }

    public Task LogoutAsync(string rawRefreshToken, CancellationToken ct) => refreshTokens.RevokeByRawTokenAsync(rawRefreshToken, ct);

    public async Task RevokeAllSessionsAsync(UserId userId, CancellationToken ct)
    {
        var count = await refreshTokens.RevokeAllForUserAsync(userId.Value, "revoke_all", ct).ConfigureAwait(false);
        var user = await users.FindByIdAsync(userId.Value.ToString()).ConfigureAwait(false);
        if (user is not null)
        {
            await users.UpdateSecurityStampAsync(user).ConfigureAwait(false);
        }

        await audit.WriteAsync(new AuditEntry(AuditActions.SessionsRevoked, "User", userId.ToString(),
            new Dictionary<string, string?> { ["count"] = count.ToString(System.Globalization.CultureInfo.InvariantCulture) }), ct).ConfigureAwait(false);
    }

    public async Task<AcceptedResponse> ForgotPasswordAsync(EmailRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is { EmailConfirmed: true, Status: AccountStatus.Active })
        {
            var token = Encode(await users.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false));
            var link = $"{links.Value.WebBaseUrl.TrimEnd('/')}/reset-password?userId={user.Id}&token={token}";
            await email.SendAsync(new EmailMessage(user.Email!, "Reset your MarketPulse password",
                $"Hello {user.DisplayName},\n\nUse this link to reset your password:\n{link}\n\nIf you did not request this, ignore this email."), ct).ConfigureAwait(false);
            await audit.WriteAsync(new AuditEntry(AuditActions.PasswordResetRequested, "User", user.Id.ToString()), ct).ConfigureAwait(false);
        }

        return new AcceptedResponse(AcceptedMessage);
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);
        if (user is null || !TryDecode(request.Token, out var token))
        {
            return Result.Failure(InvalidToken);
        }

        var result = await users.ResetPasswordAsync(user, token, request.NewPassword).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return Result.Failure(InvalidToken);
        }

        await refreshTokens.RevokeAllForUserAsync(user.Id, "password_reset", ct).ConfigureAwait(false);
        await audit.WriteAsync(new AuditEntry(AuditActions.PasswordReset, "User", user.Id.ToString()), ct).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<ProfileResponse>> GetProfileAsync(UserId userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.Value.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return Error.NotFound("user.not_found", "User not found.");
        }

        return await BuildProfileAsync(user, ct).ConfigureAwait(false);
    }

    public async Task<Result<ProfileResponse>> UpdateProfileAsync(UserId userId, UpdateProfileRequest request, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.Value.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return Error.NotFound("user.not_found", "User not found.");
        }

        user.DisplayName = request.DisplayName.Trim();
        await users.UpdateAsync(user).ConfigureAwait(false);
        return await BuildProfileAsync(user, ct).ConfigureAwait(false);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(ApplicationUser user, CancellationToken ct)
    {
        var profile = await BuildProfileAsync(user, ct).ConfigureAwait(false);
        var access = tokens.CreateAccessToken(user, profile.Roles);
        return new AuthResponse(access.Token, access.ExpiresAtUtc, profile);
    }

    private async Task<ProfileResponse> BuildProfileAsync(ApplicationUser user, CancellationToken ct)
    {
        var roles = (await users.GetRolesAsync(user).ConfigureAwait(false)).Order(StringComparer.Ordinal).ToList();
        var granted = await entitlements.GetEntitlementsAsync(new TenantId(user.TenantId), ct).ConfigureAwait(false);
        return new ProfileResponse(user.Id, user.TenantId, user.Email!, user.DisplayName, user.EmailConfirmed, roles, granted, user.CreatedAtUtc);
    }

    private async Task SendVerificationEmailAsync(ApplicationUser user, CancellationToken ct)
    {
        var token = Encode(await users.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false));
        var link = $"{links.Value.WebBaseUrl.TrimEnd('/')}/verify-email?userId={user.Id}&token={token}";
        await email.SendAsync(new EmailMessage(user.Email!, "Verify your MarketPulse email",
            $"Hello {user.DisplayName},\n\nPlease verify your email address:\n{link}\n\nIf you did not create this account, ignore this email."), ct).ConfigureAwait(false);
    }

    private static string Encode(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static bool TryDecode(string encoded, out string token)
    {
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded));
            return true;
        }
        catch (FormatException)
        {
            token = string.Empty;
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Registration attempted for an existing email; accepted response returned")]
    private static partial void LogDuplicateRegistration(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Registration failed: {Codes}")]
    private static partial void LogRegistrationFailed(ILogger logger, string codes);
}
