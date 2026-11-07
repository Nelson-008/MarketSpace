using MarketPulse.SharedKernel.Identifiers;

namespace MarketPulse.Application.Abstractions.Security;

/// <summary>A feature the tenant's current plan grants, with an optional numeric usage limit.</summary>
public sealed record Entitlement(string FeatureKey, int? Limit);

/// <summary>
/// Server-side subscription/feature checks (spec §4.1: never rely on hiding a UI control).
/// A feature that is absent from the active plan is denied.
/// </summary>
public interface IEntitlementService
{
    Task<IReadOnlyList<Entitlement>> GetEntitlementsAsync(TenantId tenantId, CancellationToken cancellationToken);

    Task<Entitlement?> FindAsync(TenantId tenantId, string featureKey, CancellationToken cancellationToken);
}

/// <summary>Catalogue of feature keys. Each module adds its keys here so plans can be validated against one list.</summary>
public static class FeatureKeys
{
    /// <summary>Create and manage watchlists. Limit = maximum number of watchlists.</summary>
    public const string Watchlists = "workspace.watchlists";

    /// <summary>Browse the instrument and company master.</summary>
    public const string InstrumentSearch = "instruments.search";

    public static readonly IReadOnlyList<string> All = [Watchlists, InstrumentSearch];
}
