using System.Reflection;
using MarketPulse.Application.Abstractions.Security;
using MarketPulse.SharedKernel.Identifiers;
using MarketPulse.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MarketPulse.Infrastructure.Common.Persistence;

/// <summary>
/// Base class for module DbContexts. Each module owns exactly one SQL schema (ADR-0002).
/// Tenant isolation is enforced twice:
/// <list type="bullet">
/// <item>reads: a named global query filter restricts every <see cref="ITenantOwned"/> entity to the caller's tenant
///   (an unauthenticated caller matches nothing);</item>
/// <item>writes: <see cref="SaveChangesAsync(bool, CancellationToken)"/> rejects any insert/update/delete of another
///   tenant's record.</item>
/// </list>
/// </summary>
public abstract class ModuleDbContext : DbContext
{
    public const string TenantFilterName = "TenantIsolation";

    private static readonly MethodInfo SetTenantFilterMethod =
        typeof(ModuleDbContext).GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly IUserContext _userContext;

    protected ModuleDbContext(DbContextOptions options, IUserContext userContext)
        : base(options)
    {
        _userContext = userContext;
    }

    public abstract string Schema { get; }

    /// <summary>Referenced by the query filter; EF re-evaluates it per query. Empty tenant when not a user.</summary>
    protected TenantId CurrentTenantId =>
        _userContext is { IsAuthenticated: true, IsSystem: false } ? _userContext.TenantId : default;

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GuardTenantWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        GuardTenantWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        ModelConventions.Apply(configurationBuilder);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        ConfigureModel(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(ITenantOwned).IsAssignableFrom(t.ClrType) && t.BaseType is null))
        {
            SetTenantFilterMethod.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
        }
    }

    protected abstract void ConfigureModel(ModelBuilder modelBuilder);

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantOwned =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(TenantFilterName, e => e.TenantId == CurrentTenantId);

    private void GuardTenantWrites()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            if (_userContext.IsSystem)
            {
                continue;
            }

            if (!_userContext.IsAuthenticated || entry.Entity.TenantId != _userContext.TenantId)
            {
                throw new CrossTenantWriteException(entry.Metadata.ClrType.Name);
            }
        }
    }
}

/// <summary>Thrown when code attempts to write another tenant's data. Always a bug; surfaced as a 500 and logged.</summary>
public sealed class CrossTenantWriteException(string entityName)
    : InvalidOperationException($"Blocked a cross-tenant write to {entityName}.");
