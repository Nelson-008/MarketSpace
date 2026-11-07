using MarketPulse.Application.Abstractions.Security;
using MarketPulse.Infrastructure.Common.Persistence;
using MarketPulse.Infrastructure.Common.Security;
using MarketPulse.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MarketPulse.Modules.Administration.Infrastructure;

/// <summary>Owns the <c>ops</c> schema (provider registry; health, ingestion and data-quality tables arrive in Phase 2).</summary>
public sealed class AdministrationDbContext(DbContextOptions<AdministrationDbContext> options, IUserContext userContext)
    : ModuleDbContext(options, userContext)
{
    public const string SchemaName = "ops";

    public override string Schema => SchemaName;

    public DbSet<Provider> Providers => Set<Provider>();

    public DbSet<ProviderCapability> ProviderCapabilities => Set<ProviderCapability>();

    public DbSet<ProviderLicense> ProviderLicenses => Set<ProviderLicense>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        var b = modelBuilder;
        b.Entity<Provider>(e =>
        {
            e.ToTable("Providers", t => t.HasCheckConstraint("CK_Providers_MockIsSimulated", "[Kind] <> 1 OR [IsSimulated] = 1"));
            e.Property(x => x.Code).HasMaxLength(50).IsUnicode(false);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasMany(x => x.Capabilities).WithOne().HasForeignKey(c => c.ProviderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Licenses).WithOne().HasForeignKey(l => l.ProviderId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ProviderCapability>(e =>
        {
            e.ToTable("ProviderCapabilities", t =>
                t.HasCheckConstraint("CK_ProviderCapabilities_Delay", "([DeliveryMode] = 2 AND [DelaySeconds] > 0) OR ([DeliveryMode] <> 2 AND [DelaySeconds] IS NULL)"));
            e.HasIndex(x => new { x.ProviderId, x.Capability }).IsUnique();
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.VerificationSource).HasMaxLength(500);
        });

        b.Entity<ProviderLicense>(e =>
        {
            e.ToTable("ProviderLicenses", t => t.HasCheckConstraint("CK_ProviderLicenses_Range", "[ValidTo] IS NULL OR [ValidTo] > [ValidFrom]"));
            e.Property(x => x.LicenseReference).HasMaxLength(200);
            e.Property(x => x.DocumentReference).HasMaxLength(500);
            e.HasIndex(x => new { x.ProviderId, x.ValidFrom });
        });
    }
}

internal sealed class DesignTimeFactory : IDesignTimeDbContextFactory<AdministrationDbContext>
{
    public AdministrationDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AdministrationDbContext>()
                .UseSqlServer(
                    "Server=.;Database=MarketPulse_DesignTime;Trusted_Connection=True;TrustServerCertificate=True",
                    sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", AdministrationDbContext.SchemaName))
                .Options,
            new SystemUserContext());
}
