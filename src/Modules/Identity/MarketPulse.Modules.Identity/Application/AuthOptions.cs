using System.ComponentModel.DataAnnotations;

namespace MarketPulse.Modules.Identity.Application;

/// <summary>JWT and session settings (ADR-0004). The signing key must come from user-secrets / a secrets vault.</summary>
public sealed class JwtOptions : IValidatableObject
{
    public const string SectionName = "Auth:Jwt";

    [Required]
    public string Issuer { get; set; } = "marketpulse";

    [Required]
    public string Audience { get; set; } = "marketpulse-web";

    /// <summary>Base64-encoded HMAC-SHA256 key of at least 32 bytes. Never commit it.</summary>
    [Required]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 60)]
    public int AccessTokenMinutes { get; set; } = 10;

    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 14;

    public byte[] GetSigningKeyBytes() => Convert.FromBase64String(SigningKey);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        byte[]? bytes = null;
        try
        {
            bytes = GetSigningKeyBytes();
        }
        catch (FormatException)
        {
            // reported below
        }

        if (bytes is null || bytes.Length < 32)
        {
            yield return new ValidationResult(
                "Auth:Jwt:SigningKey must be a base64 string of at least 32 bytes (set it with dotnet user-secrets).",
                [nameof(SigningKey)]);
        }
    }
}

public sealed class AppLinkOptions
{
    public const string SectionName = "App";

    /// <summary>Public base URL of the web app, used in verification and reset links.</summary>
    [Required]
    [Url]
    public string WebBaseUrl { get; set; } = "http://localhost:5173";
}
