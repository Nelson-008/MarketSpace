using MarketPulse.Application.Abstractions.Auditing;
using MarketPulse.Application.Abstractions.Security;
using MarketPulse.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace MarketPulse.Modules.Identity.Endpoints;

public sealed record AuditLogItem(
    long Id,
    DateTime OccurredAtUtc,
    Guid? ActorUserId,
    string Action,
    string? TargetType,
    string? TargetId,
    string? CorrelationId,
    string? IpAddress,
    string? DetailsJson);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

internal static class AdminAuditEndpoints
{
    public const int MaxPageSize = 100;

    public static void Map(RouteGroupBuilder api)
    {
        api.MapGet("/admin/audit", async (
                IdentityModuleDbContext db,
                IAuditWriter audit,
                string? action,
                DateTime? fromUtc,
                DateTime? toUtc,
                int? page,
                int? pageSize,
                CancellationToken ct) =>
            {
                var p = Math.Max(1, page ?? 1);
                var size = Math.Clamp(pageSize ?? 50, 1, MaxPageSize);

                var query = db.AuditLogs.AsNoTracking();
                if (!string.IsNullOrWhiteSpace(action))
                {
                    query = query.Where(a => a.Action == action);
                }

                if (fromUtc is { } from)
                {
                    var f = DateTime.SpecifyKind(from.ToUniversalTime(), DateTimeKind.Utc);
                    query = query.Where(a => a.OccurredAtUtc >= f);
                }

                if (toUtc is { } to)
                {
                    var t = DateTime.SpecifyKind(to.ToUniversalTime(), DateTimeKind.Utc);
                    query = query.Where(a => a.OccurredAtUtc < t);
                }

                var total = await query.CountAsync(ct).ConfigureAwait(false);
                var items = await query
                    .OrderByDescending(a => a.Id)
                    .Skip((p - 1) * size)
                    .Take(size)
                    .Select(a => new AuditLogItem(a.Id, a.OccurredAtUtc, a.ActorUserId, a.Action, a.TargetType, a.TargetId, a.CorrelationId, a.IpAddress, a.DetailsJson))
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                // Viewing the audit trail is itself a sensitive action (spec §4.13).
                await audit.WriteAsync(new AuditEntry(AuditActions.AuditLogViewed), ct).ConfigureAwait(false);
                return Results.Ok(new PagedResponse<AuditLogItem>(items, p, size, total));
            })
            .WithTags("Admin")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Admin, Roles.Support))
            .Produces<PagedResponse<AuditLogItem>>();
    }
}
