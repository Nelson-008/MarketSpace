using MarketPulse.Application.Abstractions.Security;
using MarketPulse.Infrastructure.Common.Persistence;
using MarketPulse.Infrastructure.Common.Security;
using MarketPulse.Modules.Workspace.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MarketPulse.Modules.Workspace.Infrastructure;

/// <summary>Owns the <c>workspace</c> schema. Every table is tenant-owned and filtered (see <see cref="ModuleDbContext"/>).</summary>
public sealed class WorkspaceDbContext(DbContextOptions<WorkspaceDbContext> options, IUserContext userContext)
    : ModuleDbContext(options, userContext)
{
    public const string SchemaName = "workspace";

    public override string Schema => SchemaName;

    public DbSet<Watchlist> Watchlists => Set<Watchlist>();

    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Watchlist>(b =>
        {
            b.ToTable("Watchlists");
            b.Property(w => w.Name).HasMaxLength(Watchlist.MaxNameLength);
            b.Property(w => w.RowVersion).IsRowVersion();
            b.HasIndex(w => new { w.TenantId, w.UserId, w.Name }).IsUnique();
            b.HasIndex(w => new { w.UserId, w.CreatedAtUtc });
        });

        modelBuilder.Entity<UserPreferences>(b =>
        {
            b.ToTable("UserPreferences");
            b.HasKey(p => p.UserId);
            b.HasIndex(p => p.TenantId);
        });
    }
}

internal sealed class DesignTimeFactory : IDesignTimeDbContextFactory<WorkspaceDbContext>
{
    public WorkspaceDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<WorkspaceDbContext>()
                .UseSqlServer(
                    "Server=.;Database=MarketPulse_DesignTime;Trusted_Connection=True;TrustServerCertificate=True",
                    sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", WorkspaceDbContext.SchemaName))
                .Options,
            new SystemUserContext());
}
