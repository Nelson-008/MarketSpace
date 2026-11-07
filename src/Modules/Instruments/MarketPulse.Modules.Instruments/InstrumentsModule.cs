using MarketPulse.Application.Abstractions.Security;
using MarketPulse.Infrastructure.Common.Persistence;
using MarketPulse.Infrastructure.Common.Web;
using MarketPulse.Modules.Instruments.Domain;
using MarketPulse.Modules.Instruments.Infrastructure;
using MarketPulse.SharedKernel.Identifiers;
using MarketPulse.SharedKernel.Time;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarketPulse.Modules.Instruments;

public sealed record InstrumentSummary(
    Guid Id,
    string Exchange,
    string Segment,
    InstrumentType Type,
    string Symbol,
    string TradingSymbol,
    string Name,
    string? Isin,
    DateOnly? ExpiryDate,
    decimal? StrikePrice,
    OptionType? OptionType,
    int? LotSize,
    decimal? TickSize,
    ListingStatus ListingStatus,
    bool IsSimulated);

public sealed record CompanyResponse(Guid Id, string LegalName, string? ShortName, string? Sector, string? Industry, bool IsSimulated);

public sealed record SectorResponse(int Id, string Code, string Name, int? ParentSectorId, string ClassificationSource);

public sealed record InstrumentPage(IReadOnlyList<InstrumentSummary> Items, int Page, int PageSize, int TotalCount);

public static class InstrumentsModule
{
    public const string Name = "Instruments";
    public const int MaxPageSize = 100;

    public static IServiceCollection AddInstrumentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<InstrumentsDbContext>(configuration, Name, InstrumentsDbContext.SchemaName);
        return services;
    }

    public static RouteGroupBuilder MapInstrumentsEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup(string.Empty)
            .WithTags("Instruments")
            .RequireAuthorization()
            .RequireEntitlement(FeatureKeys.InstrumentSearch);

        group.MapGet("/instruments", async (InstrumentsDbContext db, IClock clock, string? q, InstrumentType? type, int? page, int? pageSize, CancellationToken ct) =>
            {
                var p = Math.Max(1, page ?? 1);
                var size = Math.Clamp(pageSize ?? 25, 1, MaxPageSize);
                var query = CurrentVersions(db, MarketTime.IndiaDateOf(clock.UtcNow));

                if (!string.IsNullOrWhiteSpace(q))
                {
                    var term = q.Trim();
                    query = query.Where(x => x.Version.Symbol.StartsWith(term) || x.Version.TradingSymbol.StartsWith(term) || x.Version.Name.Contains(term));
                }

                if (type is not null)
                {
                    query = query.Where(x => x.Instrument.Type == type);
                }

                var total = await query.CountAsync(ct).ConfigureAwait(false);
                var items = await query.OrderBy(x => x.Version.Symbol).Skip((p - 1) * size).Take(size)
                    .Select(x => ToSummary(x.Instrument, x.Version, x.Exchange, x.Segment))
                    .ToListAsync(ct).ConfigureAwait(false);
                return Results.Ok(new InstrumentPage(items, p, size, total));
            })
            .Produces<InstrumentPage>()
            .WithSummary("Search the instrument master. Empty until an instrument master is imported (Phase 2).");

        group.MapGet("/instruments/{id:guid}", async (Guid id, InstrumentsDbContext db, IClock clock, CancellationToken ct) =>
            {
                var instrumentId = new InstrumentId(id);
                var item = await CurrentVersions(db, MarketTime.IndiaDateOf(clock.UtcNow))
                    .Where(x => x.Instrument.Id == instrumentId)
                    .Select(x => ToSummary(x.Instrument, x.Version, x.Exchange, x.Segment))
                    .SingleOrDefaultAsync(ct).ConfigureAwait(false);
                return item is null
                    ? ApiResults.Error(SharedKernel.Results.Error.NotFound("instrument.not_found", "Instrument not found."))
                    : Results.Ok(item);
            })
            .Produces<InstrumentSummary>()
            .ProducesError(StatusCodes.Status404NotFound);

        group.MapGet("/companies/{id:guid}", async (Guid id, InstrumentsDbContext db, CancellationToken ct) =>
            {
                var company = await (
                        from c in db.Companies.AsNoTracking()
                        join s in db.Sectors.AsNoTracking() on c.SectorId equals s.Id into sectors
                        from s in sectors.DefaultIfEmpty()
                        where c.Id == id
                        select new CompanyResponse(c.Id, c.LegalName, c.ShortName, s != null ? s.Name : null, c.Industry, c.IsSimulated))
                    .SingleOrDefaultAsync(ct).ConfigureAwait(false);
                return company is null
                    ? ApiResults.Error(SharedKernel.Results.Error.NotFound("company.not_found", "Company not found."))
                    : Results.Ok(company);
            })
            .Produces<CompanyResponse>()
            .ProducesError(StatusCodes.Status404NotFound);

        group.MapGet("/sectors", async (InstrumentsDbContext db, CancellationToken ct) =>
                Results.Ok(await db.Sectors.AsNoTracking().OrderBy(s => s.Name)
                    .Select(s => new SectorResponse(s.Id, s.Code, s.Name, s.ParentSectorId, s.ClassificationSource))
                    .ToListAsync(ct).ConfigureAwait(false)))
            .Produces<IReadOnlyList<SectorResponse>>();

        return api;
    }

    /// <summary>Instruments joined to the attribute version effective on <paramref name="asOf"/> (exchange-local date).</summary>
    private static IQueryable<CurrentRow> CurrentVersions(InstrumentsDbContext db, DateOnly asOf) =>
        from i in db.Instruments.AsNoTracking()
        join v in db.InstrumentAttributeVersions.AsNoTracking() on i.Id equals v.InstrumentId
        join e in db.Exchanges.AsNoTracking() on i.ExchangeId equals e.Id
        join s in db.Segments.AsNoTracking() on i.SegmentId equals s.Id
        where v.ValidFrom <= asOf && (v.ValidTo == null || v.ValidTo > asOf)
        select new CurrentRow { Instrument = i, Version = v, Exchange = e, Segment = s };

    private static InstrumentSummary ToSummary(Instrument i, InstrumentAttributeVersion v, Exchange e, Segment s) =>
        new(i.Id.Value, e.Code, s.Code, i.Type, v.Symbol, v.TradingSymbol, v.Name, i.Isin, i.ExpiryDate, i.StrikePrice, i.OptionType,
            v.LotSize, v.TickSize, v.ListingStatus, i.IsSimulated || e.IsSimulated);

    /// <summary>Member-initialised (not positional) so EF can compose further filters on it.</summary>
    private sealed class CurrentRow
    {
        public required Instrument Instrument { get; init; }

        public required InstrumentAttributeVersion Version { get; init; }

        public required Exchange Exchange { get; init; }

        public required Segment Segment { get; init; }
    }
}
