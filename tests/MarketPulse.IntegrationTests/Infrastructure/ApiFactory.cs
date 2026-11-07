using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using MarketPulse.Application.Abstractions.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MarketPulse.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API against a throwaway SQL Server database that is created (via migrations) for the test run and
/// dropped afterwards. The SQL Server instance comes from <c>MARKETPULSE_TEST_SQL</c> (a connection string without a
/// database name); the default is the local SQL Express instance.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DefaultServer = @"Server=.\SQLEXPRESS;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True";

    private readonly string _databaseName = $"MarketPulse_IT_{Guid.NewGuid():N}";
    private readonly string _serverConnectionString =
        Environment.GetEnvironmentVariable("MARKETPULSE_TEST_SQL") is { Length: > 0 } fromEnv ? fromEnv : DefaultServer;

    private readonly ConditionalWeakTable<HttpClient, CookieContainer> _cookieJars = new();

    public CapturingEmailSender Emails { get; } = new();

    public string ConnectionString => new SqlConnectionStringBuilder(_serverConnectionString) { InitialCatalog = _databaseName }.ConnectionString;

    /// <summary>A client with its own cookie jar, on HTTPS so the Secure refresh cookie is stored and sent back.</summary>
    public HttpClient CreateApiClient()
    {
        var cookies = new CookieContainerHandler();
        var client = CreateDefaultClient(new Uri("https://localhost"), cookies);
        _cookieJars.AddOrUpdate(client, cookies.Container);
        return client;
    }

    /// <summary>The raw value of the refresh cookie currently held by <paramref name="client"/>.</summary>
    public string RefreshCookieOf(HttpClient client) =>
        _cookieJars.TryGetValue(client, out var jar)
            ? jar.GetCookies(new Uri("https://localhost/api/v1/auth/refresh"))["mp_refresh"]?.Value
              ?? throw new InvalidOperationException("The client holds no refresh cookie.")
            : throw new InvalidOperationException("Client was not created by CreateApiClient.");

    public Task InitializeAsync()
    {
        // Starting the server applies all module migrations (Database:ApplyMigrationsOnStartup = true).
        _ = Server;
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync().ConfigureAwait(false);
        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(new SqlConnectionStringBuilder(_serverConnectionString) { InitialCatalog = "master" }.ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(N'{_databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{_databaseName}];
            END
            """;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:MarketPulse", ConnectionString);
        builder.UseSetting("Auth:Jwt:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("RateLimiting:AuthPermitPerMinute", "10000");
        builder.UseSetting("RateLimiting:UserTokensPerMinute", "10000");
        builder.UseSetting("Email:Mode", "Smtp");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
        });
    }
}

/// <summary>Captures outgoing email so tests can follow verification and reset links.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public IReadOnlyList<EmailMessage> Messages => [.. _messages];

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<EmailMessage> For(string email) =>
        Messages.Where(m => string.Equals(m.To, email, StringComparison.OrdinalIgnoreCase)).ToList();
}

[CollectionDefinition(Name)]
public sealed class ApiTestGroup : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
