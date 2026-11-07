using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarketPulse.Infrastructure.Common.Persistence;

/// <summary>Applies pending EF migrations for one module's DbContext. The host runs all registered migrators.</summary>
public interface IModuleMigrator
{
    string ModuleName { get; }

    Task MigrateAsync(IServiceProvider scopedServices, CancellationToken cancellationToken);
}

internal sealed class ModuleMigrator<TContext>(string moduleName) : IModuleMigrator
    where TContext : DbContext
{
    public string ModuleName => moduleName;

    public Task MigrateAsync(IServiceProvider scopedServices, CancellationToken cancellationToken) =>
        scopedServices.GetRequiredService<TContext>().Database.MigrateAsync(cancellationToken);
}

public static class DatabaseRegistration
{
    public const string ConnectionStringName = "MarketPulse";

    /// <summary>Registers a module DbContext on the shared database with its own schema and migrations-history table.</summary>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string moduleName,
        string schema)
        where TContext : DbContext
    {
        // Resolved lazily so that tooling (OpenAPI generation, EF design time) can build the host without a database.
        services.AddDbContext<TContext>(options => options.UseSqlServer(
            configuration.GetConnectionString(ConnectionStringName) is { Length: > 0 } connectionString
                ? connectionString
                : throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured."),
            sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory", schema);
                sql.EnableRetryOnFailure(maxRetryCount: 3);
            }));

        services.AddSingleton<IModuleMigrator>(new ModuleMigrator<TContext>(moduleName));
        return services;
    }
}
