using MarketPulse.Application.Abstractions.Security;
using RoleNames = MarketPulse.Application.Abstractions.Security.Roles;
using MarketPulse.Infrastructure.Common.Persistence;
using MarketPulse.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MarketPulse.Modules.Identity.Infrastructure;

/// <summary>Owns the <c>auth</c> schema: users, roles, tenants, refresh tokens, plans, subscriptions, audit log.</summary>
public sealed class IdentityModuleDbContext(DbContextOptions<IdentityModuleDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public const string SchemaName = "auth";

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<PlanFeature> PlanFeatures => Set<PlanFeature>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        ModelConventions.Apply(configurationBuilder);

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(SchemaName);

        builder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("Users");
            b.Property(u => u.DisplayName).HasMaxLength(100);
            b.Property(u => u.PasswordHash).HasMaxLength(512);
            b.HasIndex(u => u.TenantId);
            b.HasOne<Tenant>().WithMany().HasForeignKey(u => u.TenantId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<ApplicationRole>(b =>
        {
            b.ToTable("Roles");
            b.HasData(SeedData.Roles);
        });
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        builder.Entity<Tenant>(b =>
        {
            b.ToTable("Tenants");
            b.Property(t => t.Name).HasMaxLength(200);
        });

        builder.Entity<RefreshToken>(b =>
        {
            b.ToTable("RefreshTokens");
            b.Property(t => t.TokenHash).HasMaxLength(64).IsFixedLength().IsUnicode(false);
            b.HasIndex(t => t.TokenHash).IsUnique();
            b.HasIndex(t => new { t.UserId, t.RevokedAtUtc });
            b.HasIndex(t => t.FamilyId);
            b.Property(t => t.RevokedReason).HasMaxLength(64);
            b.Property(t => t.CreatedByIp).HasMaxLength(45);
            b.Property(t => t.RowVersion).IsRowVersion();
            b.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Plan>(b =>
        {
            b.ToTable("Plans");
            b.Property(p => p.Code).HasMaxLength(50);
            b.HasIndex(p => p.Code).IsUnique();
            b.Property(p => p.Name).HasMaxLength(100);
            b.HasMany(p => p.Features).WithOne().HasForeignKey(f => f.PlanId);
            b.HasData(SeedData.BetaPlan);
        });

        builder.Entity<PlanFeature>(b =>
        {
            b.ToTable("PlanFeatures");
            b.HasKey(f => new { f.PlanId, f.FeatureKey });
            b.Property(f => f.FeatureKey).HasMaxLength(100);
            b.HasData(SeedData.BetaPlanFeatures);
        });

        builder.Entity<Subscription>(b =>
        {
            b.ToTable("Subscriptions");
            b.HasIndex(s => new { s.TenantId, s.ValidFromUtc });
            b.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Plan>().WithMany().HasForeignKey(s => s.PlanId).OnDelete(DeleteBehavior.Restrict);
            b.ToTable(t => t.HasCheckConstraint("CK_Subscriptions_ValidRange", "[ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]"));
        });

        builder.Entity<AuditLog>(b =>
        {
            b.ToTable("AuditLogs");
            b.Property(a => a.Id).UseIdentityColumn();
            b.Property(a => a.Action).HasMaxLength(100);
            b.Property(a => a.TargetType).HasMaxLength(100);
            b.Property(a => a.TargetId).HasMaxLength(100);
            b.Property(a => a.CorrelationId).HasMaxLength(64);
            b.Property(a => a.IpAddress).HasMaxLength(45);
            b.Property(a => a.DetailsJson).HasMaxLength(4000);
            b.HasIndex(a => a.OccurredAtUtc);
            b.HasIndex(a => new { a.ActorUserId, a.OccurredAtUtc });
            b.HasIndex(a => a.Action);
        });
    }
}

/// <summary>Reference data seeded by migration. Fixed ids keep migrations deterministic.</summary>
internal static class SeedData
{
    public static readonly Guid BetaPlanId = new("0193a000-0000-7000-8000-000000000001");

    public static readonly ApplicationRole[] Roles =
    [
        new(new Guid("0193a000-0000-7000-8000-0000000000a1"), RoleNames.User),
        new(new Guid("0193a000-0000-7000-8000-0000000000a2"), RoleNames.Admin),
        new(new Guid("0193a000-0000-7000-8000-0000000000a3"), RoleNames.Support),
        new(new Guid("0193a000-0000-7000-8000-0000000000a4"), RoleNames.DataOperator),
        new(new Guid("0193a000-0000-7000-8000-0000000000a5"), RoleNames.ResearchEditor),
    ];

    /// <summary>The only plan in MVP. Its limits are product configuration, not market facts.</summary>
    public static readonly object BetaPlan = new { Id = BetaPlanId, Code = "beta", Name = "Public Beta", IsActive = true };

    public static readonly PlanFeature[] BetaPlanFeatures =
    [
        new() { PlanId = BetaPlanId, FeatureKey = FeatureKeys.Watchlists, Limit = 10 },
        new() { PlanId = BetaPlanId, FeatureKey = FeatureKeys.InstrumentSearch, Limit = null },
    ];
}
