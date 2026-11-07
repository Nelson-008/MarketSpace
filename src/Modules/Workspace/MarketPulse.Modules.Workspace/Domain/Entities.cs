using MarketPulse.SharedKernel.Tenancy;
using MarketPulse.SharedKernel.Identifiers;

namespace MarketPulse.Modules.Workspace.Domain;

/// <summary>
/// A user's named watchlist. Phase 1 stores the list itself; instrument items arrive in Phase 2 once the
/// instrument master is populated.
/// </summary>
public sealed class Watchlist : ITenantOwned
{
    public const int MaxNameLength = 100;

    public Guid Id { get; set; }

    public TenantId TenantId { get; set; }

    public UserId UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}

/// <summary>Explore (guided) or Pro (dense) experience over the same services (spec §2).</summary>
public enum ExperienceMode : byte
{
    Explore = 1,
    Pro = 2,
}

public sealed class UserPreferences : ITenantOwned
{
    public UserId UserId { get; set; }

    public TenantId TenantId { get; set; }

    public ExperienceMode ExperienceMode { get; set; } = ExperienceMode.Explore;

    public DateTime UpdatedAtUtc { get; set; }
}
