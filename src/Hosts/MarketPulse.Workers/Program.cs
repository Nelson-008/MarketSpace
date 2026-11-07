using MarketPulse.SharedKernel.Time;
using MarketPulse.Workers.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IClock, SystemClock>();

// Each worker is opt-in via configuration ("Workers:Enabled"), so one build can be deployed as several
// instances running different worker sets (ADR-0001). Phase 2 adds ingestion, candle and data-quality workers.
var registry = new WorkerRegistry(builder.Configuration);
registry.Add<HeartbeatWorker>(builder.Services, HeartbeatWorker.Name);
registry.LogSummaryOnStart(builder.Services);

var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
