namespace MarketPulse.Application.Abstractions.Auditing;

/// <summary>
/// An append-only audit record for privileged actions, security events and entitlement changes (spec §4.13, §13).
/// Never put passwords, tokens, secrets or unnecessary personal data in <see cref="Details"/>.
/// </summary>
public sealed record AuditEntry(
    string Action,
    string? TargetType = null,
    string? TargetId = null,
    IReadOnlyDictionary<string, string?>? Details = null);

public interface IAuditWriter
{
    /// <summary>Persists the entry immediately, attributing it to the current caller, correlation id and IP.</summary>
    Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken);
}

/// <summary>Stable audit action names.</summary>
public static class AuditActions
{
    public const string UserRegistered = "auth.user_registered";
    public const string EmailVerified = "auth.email_verified";
    public const string LoginSucceeded = "auth.login_succeeded";
    public const string LoginFailed = "auth.login_failed";
    public const string LockedOut = "auth.locked_out";
    public const string RefreshTokenReuseDetected = "auth.refresh_token_reuse_detected";
    public const string SessionsRevoked = "auth.sessions_revoked";
    public const string PasswordResetRequested = "auth.password_reset_requested";
    public const string PasswordReset = "auth.password_reset";
    public const string SubscriptionCreated = "entitlements.subscription_created";
    public const string AuditLogViewed = "admin.audit_viewed";
}
