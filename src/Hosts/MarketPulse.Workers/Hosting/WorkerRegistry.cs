using MarketPulse.SharedKernel.Time;

namespace MarketPulse.Workers.Hosting;

/// <summary>Registers only the workers listed in <c>Workers:Enabled</c>.</summary>
internal sealed partial class WorkerRegistry(IConfiguration configuration)
{
    private readonly HashSet<string> _enabled = new(
        configuration.GetSection("Workers:Enabled").Get<string[]>() ?? [],
        StringComparer.OrdinalIgnoreCase);

    private readonly List<string> _registered = [];

    public void Add<TWorker>(IServiceCollection services, string name)
        where TWorker : class, IHostedService
    {
        if (_enabled.Contains(name))
        {
            services.AddHostedService<TWorker>();
            _registered.Add(name);
        }
    }

    public void LogSummaryOnStart(IServiceCollection services)
    {
        var registered = _registered.ToArray();
        services.AddHostedService(sp => new StartupSummary(sp.GetRequiredService<ILogger<WorkerRegistry>>(), registered));
    }

    private sealed partial class StartupSummary(ILogger logger, string[] workers) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (workers.Length == 0)
            {
                LogNone(logger);
            }
            else
            {
                LogEnabled(logger, workers);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        [LoggerMessage(Level = LogLevel.Information, Message = "Enabled workers: {Workers}")]
        private static partial void LogEnabled(ILogger logger, string[] workers);

        [LoggerMessage(Level = LogLevel.Warning, Message = "No workers enabled (configure Workers:Enabled)")]
        private static partial void LogNone(ILogger logger);
    }
}

/// <summary>Liveness signal for the worker process. Provider feed heartbeats are separate (Phase 2).</summary>
internal sealed partial class HeartbeatWorker(ILogger<HeartbeatWorker> logger, IClock clock, IConfiguration configuration) : BackgroundService
{
    public const string Name = "Heartbeat";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, configuration.GetValue("Workers:Heartbeat:IntervalSeconds", 30)));
        using var timer = new PeriodicTimer(interval);
        do
        {
            LogHeartbeat(logger, clock.UtcNow);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Worker host heartbeat at {UtcNow:O}")]
    private static partial void LogHeartbeat(ILogger logger, DateTime utcNow);
}
