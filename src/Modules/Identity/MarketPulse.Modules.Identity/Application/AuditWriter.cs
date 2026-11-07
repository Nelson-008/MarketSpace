using System.Text.Json;
using MarketPulse.Application.Abstractions.Auditing;
using MarketPulse.Application.Abstractions.Security;
using MarketPulse.Infrastructure.Common.Web;
using MarketPulse.Modules.Identity.Domain;
using MarketPulse.Modules.Identity.Infrastructure;
using MarketPulse.SharedKernel.Time;
using Microsoft.AspNetCore.Http;

namespace MarketPulse.Modules.Identity.Application;

/// <summary>
/// Writes to <c>auth.AuditLogs</c> immediately in its own SaveChanges, so an audit record survives even if the
/// surrounding operation later fails. Rows can never be updated or deleted (trigger).
/// </summary>
internal sealed class AuditWriter(
    IdentityModuleDbContext db,
    IUserContext user,
    IHttpContextAccessor httpContextAccessor,
    IClock clock) : IAuditWriter
{
    private const int MaxDetailsLength = 4000;

    public async Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var http = httpContextAccessor.HttpContext;
        var details = entry.Details is { Count: > 0 } ? JsonSerializer.Serialize(entry.Details) : null;
        if (details is { Length: > MaxDetailsLength })
        {
            details = details[..MaxDetailsLength];
        }

        db.AuditLogs.Add(new AuditLog
        {
            OccurredAtUtc = clock.UtcNow,
            ActorUserId = user.IsAuthenticated ? user.UserId.Value : null,
            ActorTenantId = user.IsAuthenticated ? user.TenantId.Value : null,
            Action = entry.Action,
            TargetType = entry.TargetType,
            TargetId = entry.TargetId,
            CorrelationId = http?.GetCorrelationId(),
            IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
            DetailsJson = details,
        });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
