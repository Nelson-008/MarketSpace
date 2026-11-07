using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MarketPulse.Modules.Identity.Infrastructure;

/// <summary>Used only by <c>dotnet ef</c> to create migrations. Never used at runtime.</summary>
internal sealed class DesignTimeFactory : IDesignTimeDbContextFactory<IdentityModuleDbContext>
{
    public IdentityModuleDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityModuleDbContext>()
            .UseSqlServer(
                "Server=.;Database=MarketPulse_DesignTime;Trusted_Connection=True;TrustServerCertificate=True",
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityModuleDbContext.SchemaName))
            .Options;
        return new IdentityModuleDbContext(options);
    }
}
