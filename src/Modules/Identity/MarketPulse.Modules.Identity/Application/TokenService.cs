using System.Security.Claims;
using System.Security.Cryptography;
using MarketPulse.Infrastructure.Common.Security;
using MarketPulse.Modules.Identity.Domain;
using MarketPulse.SharedKernel.Time;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

namespace MarketPulse.Modules.Identity.Application;

internal sealed record AccessToken(string Token, DateTime ExpiresAtUtc);

internal sealed class TokenService(IOptions<JwtOptions> options, IClock clock)
{
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken CreateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var o = options.Value;
        var now = clock.UtcNow;
        var expires = now.AddMinutes(o.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(MarketPulseClaims.Subject, user.Id.ToString()),
            new(MarketPulseClaims.Tenant, user.TenantId.ToString()),
            new(MarketPulseClaims.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        claims.AddRange(roles.Select(r => new Claim(MarketPulseClaims.Role, r)));

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = o.Issuer,
            Audience = o.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(o.GetSigningKeyBytes()), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expires);
    }

    /// <summary>A 512-bit random opaque token, URL-safe.</summary>
    public static string NewRefreshTokenValue() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

    /// <summary>Only this hash is stored. Hex, upper-case, 64 chars.</summary>
    public static string Hash(string rawToken) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
}
