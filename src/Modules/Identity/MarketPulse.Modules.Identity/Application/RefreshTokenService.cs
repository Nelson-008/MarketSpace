using MarketPulse.Application.Abstractions.Auditing;
using MarketPulse.Modules.Identity.Domain;
using MarketPulse.Modules.Identity.Infrastructure;
using MarketPulse.SharedKernel.Results;
using MarketPulse.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarketPulse.Modules.Identity.Application;

internal sealed record IssuedRefreshToken(string RawToken, DateTime ExpiresAtUtc);

internal sealed record RotatedRefreshToken(Guid UserId, IssuedRefreshToken Token);

/// <summary>Refresh-token issue, rotation, reuse detection and revocation (ADR-0004).</summary>
internal sealed class RefreshTokenService(
    IdentityModuleDbContext db,
    IOptions<JwtOptions> options,
    IClock clock,
    IAuditWriter audit)
{
    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("auth.invalid_refresh_token", "The session has expired. Please sign in again.");

    public async Task<IssuedRefreshToken> IssueAsync(Guid userId, Guid? familyId, string? ip, string? userAgent, CancellationToken ct)
    {
        var token = NewToken(userId, familyId ?? Guid.CreateVersion7(), ip, userAgent, out var raw);
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new IssuedRefreshToken(raw, token.ExpiresAtUtc);
    }

    public async Task<Result<RotatedRefreshToken>> RotateAsync(string rawToken, string? ip, string? userAgent, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var hash = TokenService.Hash(rawToken);
        var existing = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct).ConfigureAwait(false);
        if (existing is null)
        {
            return InvalidRefreshToken;
        }

        if (existing.RevokedAtUtc is not null)
        {
            if (existing.ReplacedByTokenId is not null)
            {
                // A token that was already rotated has been presented again: assume theft, kill the family.
                await RevokeFamilyAsync(existing.FamilyId, "reuse_detected", ct).ConfigureAwait(false);
                await audit.WriteAsync(
                    new AuditEntry(AuditActions.RefreshTokenReuseDetected, "User", existing.UserId.ToString(),
                        new Dictionary<string, string?> { ["familyId"] = existing.FamilyId.ToString() }),
                    ct).ConfigureAwait(false);
            }

            return InvalidRefreshToken;
        }

        if (existing.ExpiresAtUtc <= now)
        {
            return InvalidRefreshToken;
        }

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == existing.UserId, ct).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active || !user.EmailConfirmed)
        {
            return InvalidRefreshToken;
        }

        var replacement = NewToken(existing.UserId, existing.FamilyId, ip, userAgent, out var raw);
        existing.RevokedAtUtc = now;
        existing.RevokedReason = "rotated";
        existing.ReplacedByTokenId = replacement.Id;
        db.RefreshTokens.Add(replacement);

        try
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request rotated this token at the same moment; only one may win.
            return InvalidRefreshToken;
        }

        return new RotatedRefreshToken(existing.UserId, new IssuedRefreshToken(raw, replacement.ExpiresAtUtc));
    }

    /// <summary>Revokes the family of the presented token (logout of one session). Unknown tokens are ignored.</summary>
    public async Task RevokeByRawTokenAsync(string rawToken, CancellationToken ct)
    {
        var hash = TokenService.Hash(rawToken);
        var familyId = await db.RefreshTokens.Where(t => t.TokenHash == hash).Select(t => (Guid?)t.FamilyId)
            .SingleOrDefaultAsync(ct).ConfigureAwait(false);
        if (familyId is not null)
        {
            await RevokeFamilyAsync(familyId.Value, "logout", ct).ConfigureAwait(false);
        }
    }

    public Task<int> RevokeAllForUserAsync(Guid userId, string reason, CancellationToken ct)
    {
        var now = clock.UtcNow;
        return db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, now).SetProperty(t => t.RevokedReason, reason), ct);
    }

    private Task<int> RevokeFamilyAsync(Guid familyId, string reason, CancellationToken ct)
    {
        var now = clock.UtcNow;
        return db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, now).SetProperty(t => t.RevokedReason, reason), ct);
    }

    private RefreshToken NewToken(Guid userId, Guid familyId, string? ip, string? userAgent, out string raw)
    {
        raw = TokenService.NewRefreshTokenValue();
        var now = clock.UtcNow;
        return new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            FamilyId = familyId,
            TokenHash = TokenService.Hash(raw),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(options.Value.RefreshTokenDays),
            CreatedByIp = ip,
            UserAgent = userAgent is { Length: > 256 } ? userAgent[..256] : userAgent,
        };
    }
}
