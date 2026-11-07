using FluentValidation;
using MarketPulse.Application.Abstractions.Security;
using MarketPulse.Modules.Workspace.Domain;
using MarketPulse.Modules.Workspace.Infrastructure;
using MarketPulse.SharedKernel.Results;
using MarketPulse.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace MarketPulse.Modules.Workspace.Application;

public sealed record WatchlistRequest(string Name);

public sealed record WatchlistResponse(Guid Id, string Name, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);

public sealed record PreferencesRequest(ExperienceMode ExperienceMode);

public sealed record PreferencesResponse(ExperienceMode ExperienceMode, DateTime? UpdatedAtUtc);

internal sealed class WatchlistRequestValidator : AbstractValidator<WatchlistRequest>
{
    public WatchlistRequestValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(Watchlist.MaxNameLength);
}

internal sealed class PreferencesRequestValidator : AbstractValidator<PreferencesRequest>
{
    public PreferencesRequestValidator() => RuleFor(x => x.ExperienceMode).IsInEnum();
}

/// <summary>
/// Watchlist CRUD. Ownership is enforced by the DbContext tenant filter: another tenant's watchlist is simply
/// not found (404), never "forbidden", so existence is not leaked.
/// </summary>
internal sealed class WatchlistService(WorkspaceDbContext db, IUserContext user, IEntitlementService entitlements, IClock clock)
{
    private static readonly Error NotFound = Error.NotFound("watchlist.not_found", "Watchlist not found.");
    private static readonly Error DuplicateName = Error.Conflict("watchlist.duplicate_name", "A watchlist with this name already exists.");

    public async Task<IReadOnlyList<WatchlistResponse>> ListAsync(CancellationToken ct) =>
        await db.Watchlists.AsNoTracking()
            .OrderBy(w => w.CreatedAtUtc)
            .Select(w => new WatchlistResponse(w.Id, w.Name, w.CreatedAtUtc, w.UpdatedAtUtc))
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<Result<WatchlistResponse>> GetAsync(Guid id, CancellationToken ct)
    {
        var w = await db.Watchlists.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
        return w is null ? NotFound : ToResponse(w);
    }

    public async Task<Result<WatchlistResponse>> CreateAsync(WatchlistRequest request, CancellationToken ct)
    {
        var entitlement = await entitlements.FindAsync(user.TenantId, FeatureKeys.Watchlists, ct).ConfigureAwait(false);
        if (entitlement?.Limit is { } limit && await db.Watchlists.CountAsync(ct).ConfigureAwait(false) >= limit)
        {
            return Error.NotEntitled("entitlement.limit_reached", $"Your plan allows at most {limit} watchlists.");
        }

        var name = request.Name.Trim();
        if (await db.Watchlists.AnyAsync(w => w.Name == name, ct).ConfigureAwait(false))
        {
            return DuplicateName;
        }

        var now = clock.UtcNow;
        var watchlist = new Watchlist
        {
            Id = Guid.CreateVersion7(),
            TenantId = user.TenantId,
            UserId = user.UserId,
            Name = name,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.Watchlists.Add(watchlist);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return ToResponse(watchlist);
    }

    public async Task<Result<WatchlistResponse>> RenameAsync(Guid id, WatchlistRequest request, CancellationToken ct)
    {
        var w = await db.Watchlists.SingleOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
        if (w is null)
        {
            return NotFound;
        }

        var name = request.Name.Trim();
        if (await db.Watchlists.AnyAsync(x => x.Id != id && x.Name == name, ct).ConfigureAwait(false))
        {
            return DuplicateName;
        }

        w.Name = name;
        w.UpdatedAtUtc = clock.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return ToResponse(w);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var w = await db.Watchlists.SingleOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);
        if (w is null)
        {
            return Result.Failure(NotFound);
        }

        db.Watchlists.Remove(w);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<PreferencesResponse> GetPreferencesAsync(CancellationToken ct)
    {
        var p = await db.UserPreferences.AsNoTracking().SingleOrDefaultAsync(ct).ConfigureAwait(false);
        return p is null ? new PreferencesResponse(ExperienceMode.Explore, null) : new PreferencesResponse(p.ExperienceMode, p.UpdatedAtUtc);
    }

    public async Task<PreferencesResponse> SetPreferencesAsync(PreferencesRequest request, CancellationToken ct)
    {
        var p = await db.UserPreferences.SingleOrDefaultAsync(ct).ConfigureAwait(false);
        if (p is null)
        {
            p = new UserPreferences { UserId = user.UserId, TenantId = user.TenantId };
            db.UserPreferences.Add(p);
        }

        p.ExperienceMode = request.ExperienceMode;
        p.UpdatedAtUtc = clock.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new PreferencesResponse(p.ExperienceMode, p.UpdatedAtUtc);
    }

    private static WatchlistResponse ToResponse(Watchlist w) => new(w.Id, w.Name, w.CreatedAtUtc, w.UpdatedAtUtc);
}
