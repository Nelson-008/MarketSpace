namespace MarketPulse.SharedKernel.Identifiers;

/// <summary>
/// Strongly-typed identifiers shared across modules. Module-specific IDs live in their own module.
/// Per ADR-003, user-owned and API-exposed entities use time-ordered GUID v7 keys.
/// </summary>
public readonly record struct TenantId(Guid Value)
{
    public static TenantId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

/// <summary>Canonical instrument identity, independent of ticker symbol or exchange (spec §4.2).</summary>
public readonly record struct InstrumentId(Guid Value)
{
    public static InstrumentId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

/// <summary>Identifies a registered data provider (mock or licensed vendor).</summary>
public readonly record struct ProviderId(Guid Value)
{
    public static ProviderId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
