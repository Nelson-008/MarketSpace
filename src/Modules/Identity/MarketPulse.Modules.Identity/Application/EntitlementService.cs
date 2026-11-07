using MarketPulse.Application.Abstractions.Security;
using MarketPulse.Modules.Identity.Infrastructure;
using MarketPulse.SharedKernel.Identifiers;
using MarketPulse.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace MarketPulse.Modules.Identity.Application;

/// <summary>
/// Resolves the tenant's active subscription (ValidFrom ≤ now &lt; ValidTo) and its plan features.
/// No active subscription or inactive plan → no entitlements (deny by default). Cached per request (scoped).
/// </summary>
internal sealed class EntitlementService(IdentityModuleDbContext db, IClock clock) : IEntitlementService
{
    private readonly Dictionary<TenantId, IReadOnlyList<Entitlement>> _cache = [];

    public async Task<IReadOnlyList<Entitlement>> GetEntitlementsAsync(TenantId tenantId, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(tenantId, out var cached))
        {
            return cached;
        }

        var now = clock.UtcNow;
        var planId = await db.Subscriptions.AsNoTracking()
            .Where(s => s.TenantId == tenantId.Value && s.ValidFromUtc <= now && (s.ValidToUtc == null || s.ValidToUtc > now))
            .OrderByDescending(s => s.ValidFromUtc)
            .Select(s => (Guid?)s.PlanId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<Entitlement> result = planId is null
            ? []
            : await db.Plans.AsNoTracking()
                .Where(p => p.Id == planId && p.IsActive)
                .SelectMany(p => p.Features)
                .OrderBy(f => f.FeatureKey)
                .Select(f => new Entitlement(f.FeatureKey, f.Limit))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        _cache[tenantId] = result;
        return result;
    }

    public async Task<Entitlement?> FindAsync(TenantId tenantId, string featureKey, CancellationToken cancellationToken)
    {
        var all = await GetEntitlementsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault(e => string.Equals(e.FeatureKey, featureKey, StringComparison.Ordinal));
    }
}
