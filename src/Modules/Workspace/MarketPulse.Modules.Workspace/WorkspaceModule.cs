using FluentValidation;
using MarketPulse.Application.Abstractions.Security;
using MarketPulse.Infrastructure.Common.Persistence;
using MarketPulse.Infrastructure.Common.Web;
using MarketPulse.Modules.Workspace.Application;
using MarketPulse.Modules.Workspace.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MarketPulse.Modules.Workspace;

public static class WorkspaceModule
{
    public const string Name = "Workspace";

    public static IServiceCollection AddWorkspaceModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<WorkspaceDbContext>(configuration, Name, WorkspaceDbContext.SchemaName);
        services.AddScoped<WatchlistService>();
        services.AddValidatorsFromAssemblyContaining<WatchlistRequestValidator>(includeInternalTypes: true);
        return services;
    }

    public static RouteGroupBuilder MapWorkspaceEndpoints(this RouteGroupBuilder api)
    {
        var watchlists = api.MapGroup("/watchlists")
            .WithTags("Workspace")
            .RequireAuthorization()
            .RequireEntitlement(FeatureKeys.Watchlists);

        watchlists.MapGet("/", async (WatchlistService s, CancellationToken ct) => Results.Ok(await s.ListAsync(ct).ConfigureAwait(false)))
            .Produces<IReadOnlyList<WatchlistResponse>>();

        watchlists.MapPost("/", async (WatchlistRequest request, WatchlistService s, CancellationToken ct) =>
                ApiResults.From(await s.CreateAsync(request, ct).ConfigureAwait(false), w => Results.Created($"/api/v1/watchlists/{w.Id}", w)))
            .WithValidation<WatchlistRequest>()
            .Produces<WatchlistResponse>(StatusCodes.Status201Created)
            .ProducesError(StatusCodes.Status403Forbidden)
            .ProducesError(StatusCodes.Status409Conflict);

        watchlists.MapGet("/{id:guid}", async (Guid id, WatchlistService s, CancellationToken ct) =>
                ApiResults.From(await s.GetAsync(id, ct).ConfigureAwait(false)))
            .Produces<WatchlistResponse>()
            .ProducesError(StatusCodes.Status404NotFound);

        watchlists.MapPut("/{id:guid}", async (Guid id, WatchlistRequest request, WatchlistService s, CancellationToken ct) =>
                ApiResults.From(await s.RenameAsync(id, request, ct).ConfigureAwait(false)))
            .WithValidation<WatchlistRequest>()
            .Produces<WatchlistResponse>()
            .ProducesError(StatusCodes.Status404NotFound)
            .ProducesError(StatusCodes.Status409Conflict);

        watchlists.MapDelete("/{id:guid}", async (Guid id, WatchlistService s, CancellationToken ct) =>
                ApiResults.From(await s.DeleteAsync(id, ct).ConfigureAwait(false)))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesError(StatusCodes.Status404NotFound);

        var prefs = api.MapGroup("/preferences").WithTags("Workspace").RequireAuthorization();

        prefs.MapGet("/", async (WatchlistService s, CancellationToken ct) => Results.Ok(await s.GetPreferencesAsync(ct).ConfigureAwait(false)))
            .Produces<PreferencesResponse>();

        prefs.MapPut("/", async (PreferencesRequest request, WatchlistService s, CancellationToken ct) =>
                Results.Ok(await s.SetPreferencesAsync(request, ct).ConfigureAwait(false)))
            .WithValidation<PreferencesRequest>()
            .Produces<PreferencesResponse>();

        return api;
    }
}
