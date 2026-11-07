namespace MarketPulse.Infrastructure.Common.Web;

/// <summary>Named rate-limit policies (spec §10.1). Limits are configured by the API host.</summary>
public static class RateLimitPolicies
{
    /// <summary>Credential endpoints (login, register, password reset): strict, per client IP.</summary>
    public const string Auth = "auth";

    /// <summary>Default for authenticated API calls: per user (or per IP when anonymous).</summary>
    public const string PerUser = "per-user";
}
