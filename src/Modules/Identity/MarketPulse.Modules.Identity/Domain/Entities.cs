using Microsoft.AspNetCore.Identity;

namespace MarketPulse.Modules.Identity.Domain;

public enum AccountStatus : byte
{
    Active = 1,
    Suspended = 2,
    Deleted = 3,
}

/// <summary>An account. MVP tenancy: one user = one tenant (decision log 2026-11-07).</summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public AccountStatus Status { get; set; } = AccountStatus.Active;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }
}

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(Guid id, string name)
    {
        Id = id;
        Name = name;
        NormalizedName = name.ToUpperInvariant();
        ConcurrencyStamp = id.ToString();
    }
}

public sealed class Tenant
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// A refresh token (ADR-0004). Only a SHA-256 hash is stored. Tokens rotate on every use; all tokens issued from
/// one login share a <see cref="FamilyId"/> so that reuse of a rotated token revokes the whole family.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid FamilyId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? RevokedReason { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public string? CreatedByIp { get; set; }

    public string? UserAgent { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public bool IsActive(DateTime nowUtc) => RevokedAtUtc is null && ExpiresAtUtc > nowUtc;
}

/// <summary>A subscription plan. Prices/billing are out of MVP scope (decision log).</summary>
public sealed class Plan
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public List<PlanFeature> Features { get; set; } = [];
}

public sealed class PlanFeature
{
    public Guid PlanId { get; set; }

    public string FeatureKey { get; set; } = string.Empty;

    /// <summary>Optional usage limit (e.g. max watchlists). Null = no numeric limit.</summary>
    public int? Limit { get; set; }
}

public sealed class Subscription
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid PlanId { get; set; }

    public DateTime ValidFromUtc { get; set; }

    public DateTime? ValidToUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>Append-only audit record. UPDATE/DELETE are blocked by a database trigger.</summary>
public sealed class AuditLog
{
    public long Id { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public Guid? ActorUserId { get; set; }

    public Guid? ActorTenantId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? TargetType { get; set; }

    public string? TargetId { get; set; }

    public string? CorrelationId { get; set; }

    public string? IpAddress { get; set; }

    public string? DetailsJson { get; set; }
}
