using MarketPulse.Application.Abstractions.Security;
using MarketPulse.Infrastructure.Common.Persistence;
using MarketPulse.Modules.Administration.Domain;
using MarketPulse.Modules.Administration.Infrastructure;
using MarketPulse.SharedKernel.DataQuality;
using MarketPulse.SharedKernel.Time;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarketPulse.Modules.Administration;

/// <summary>What kind of data the platform is currently serving. Drives the persistent DEMO banner in the UI.</summary>
public enum PlatformDataMode : byte
{
    /// <summary>No data provider is registered at all.</summary>
    NoProvider = 1,

    /// <summary>Only simulated (mock) providers are enabled. All data is fake.</summary>
    Simulated = 2,

    /// <summary>At least one enabled real provider has a current licence permitting public display.</summary>
    Licensed = 3,
}

public sealed record SystemStatusResponse(
    PlatformDataMode DataMode,
    bool ShowDemoBanner,
    string DataNotice,
    string Disclaimer,
    DateTime ServerTimeUtc);

public sealed record ProviderCapabilityResponse(CapabilityType Capability, DeliveryMode DeliveryMode, int? DelaySeconds, int? HistoricalDepthDays, string? VerificationSource, DateTime? VerifiedAtUtc);

public sealed record ProviderLicenseResponse(
    Guid Id, string LicenseReference, DateOnly ValidFrom, DateOnly? ValidTo,
    bool PermitsPublicDisplay, bool PermitsDerivedAnalytics, bool PermitsStorage, int? RetentionDays,
    bool PermitsExport, bool PermitsAlerts, bool PermitsAiReports, int? MaxUsers, string? DocumentReference);

public sealed record ProviderResponse(
    Guid Id, string Code, string Name, ProviderKind Kind, bool IsSimulated, bool IsEnabled,
    IReadOnlyList<ProviderCapabilityResponse> Capabilities, IReadOnlyList<ProviderLicenseResponse> Licenses);

public static class AdministrationModule
{
    public const string Name = "Administration";

    /// <summary>Appendix B baseline. Final legal wording must be reviewed by qualified counsel.</summary>
    public const string DisclaimerBaseline =
        "MarketPulse provides market data, analytical tools and educational/research information. Market data may be delayed, " +
        "incomplete or unavailable. Analytical scores, indicators, AI-generated explanations and historical simulations are not " +
        "guarantees of future performance. Paper trading results may differ materially from real execution due to liquidity, " +
        "slippage, market impact, fees and other factors. Users remain responsible for their own decisions.";

    public static IServiceCollection AddAdministrationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<AdministrationDbContext>(configuration, Name, AdministrationDbContext.SchemaName);
        return services;
    }

    public static RouteGroupBuilder MapAdministrationEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/system/status", async (AdministrationDbContext db, IClock clock, CancellationToken ct) =>
            {
                var now = clock.UtcNow;
                var mode = await GetDataModeAsync(db, now, ct).ConfigureAwait(false);
                var notice = mode switch
                {
                    PlatformDataMode.Licensed => "Market data is provided under licence. Check each view for its live/delayed status.",
                    PlatformDataMode.Simulated => "DEMO: all market data is SIMULATED by a mock provider. It is not real and must not be used for decisions.",
                    _ => "DEMO: no market-data provider is configured. No real market data is shown.",
                };
                return Results.Ok(new SystemStatusResponse(mode, mode != PlatformDataMode.Licensed, notice, DisclaimerBaseline, now));
            })
            .WithTags("System")
            .AllowAnonymous()
            .Produces<SystemStatusResponse>();

        api.MapGet("/admin/providers", async (AdministrationDbContext db, CancellationToken ct) =>
            {
                var providers = await db.Providers.AsNoTracking()
                    .Include(p => p.Capabilities)
                    .Include(p => p.Licenses)
                    .OrderBy(p => p.Code)
                    .ToListAsync(ct).ConfigureAwait(false);

                return Results.Ok(providers.Select(p => new ProviderResponse(
                    p.Id.Value, p.Code, p.Name, p.Kind, p.IsSimulated, p.IsEnabled,
                    p.Capabilities.OrderBy(c => c.Capability)
                        .Select(c => new ProviderCapabilityResponse(c.Capability, c.DeliveryMode, c.DelaySeconds, c.HistoricalDepthDays, c.VerificationSource, c.VerifiedAtUtc))
                        .ToList(),
                    p.Licenses.OrderByDescending(l => l.ValidFrom)
                        .Select(l => new ProviderLicenseResponse(l.Id, l.LicenseReference, l.ValidFrom, l.ValidTo, l.PermitsPublicDisplay,
                            l.PermitsDerivedAnalytics, l.PermitsStorage, l.RetentionDays, l.PermitsExport, l.PermitsAlerts, l.PermitsAiReports,
                            l.MaxUsers, l.DocumentReference))
                        .ToList())).ToList());
            })
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin, Roles.DataOperator))
            .Produces<IReadOnlyList<ProviderResponse>>();

        return api;
    }

    internal static async Task<PlatformDataMode> GetDataModeAsync(AdministrationDbContext db, DateTime nowUtc, CancellationToken ct)
    {
        var today = MarketTime.IndiaDateOf(nowUtc);
        var licensed = await db.Providers.AsNoTracking()
            .Where(p => p.IsEnabled && !p.IsSimulated)
            .AnyAsync(p => p.Licenses.Any(l => l.PermitsPublicDisplay && l.ValidFrom <= today && (l.ValidTo == null || l.ValidTo > today)), ct)
            .ConfigureAwait(false);
        if (licensed)
        {
            return PlatformDataMode.Licensed;
        }

        var anyEnabled = await db.Providers.AsNoTracking().AnyAsync(p => p.IsEnabled, ct).ConfigureAwait(false);
        return anyEnabled ? PlatformDataMode.Simulated : PlatformDataMode.NoProvider;
    }
}
