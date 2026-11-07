using MarketPulse.SharedKernel.Identifiers;

namespace MarketPulse.SharedKernel.Tenancy;

/// <summary>
/// Marks a user-owned record. Every such record carries both ids (spec §4.1), and persistence enforces that a
/// caller can only read or write records of its own tenant.
/// </summary>
public interface ITenantOwned
{
    TenantId TenantId { get; }

    UserId UserId { get; }
}
