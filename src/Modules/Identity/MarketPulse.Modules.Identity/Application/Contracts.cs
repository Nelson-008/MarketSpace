using FluentValidation;
using MarketPulse.Application.Abstractions.Security;

namespace MarketPulse.Modules.Identity.Application;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed record VerifyEmailRequest(Guid UserId, string Token);

public sealed record EmailRequest(string Email);

public sealed record LoginRequest(string Email, string Password);

public sealed record ResetPasswordRequest(Guid UserId, string Token, string NewPassword);

public sealed record UpdateProfileRequest(string DisplayName);

public sealed record AcceptedResponse(string Message);

public sealed record ProfileResponse(
    Guid UserId,
    Guid TenantId,
    string Email,
    string DisplayName,
    bool EmailVerified,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Entitlement> Entitlements,
    DateTime CreatedAtUtc);

/// <summary>Returned on login/refresh. The refresh token is NOT in the body; it is set as an HttpOnly cookie.</summary>
public sealed record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAtUtc, ProfileResponse Profile);

internal static class PasswordRules
{
    public const int MinLength = 12;
    public const int MaxLength = 128;

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MinimumLength(MinLength).WithMessage($"Password must be at least {MinLength} characters.")
            .MaximumLength(MaxLength)
            .Matches("[a-z]").WithMessage("Password must contain a lower-case letter.")
            .Matches("[A-Z]").WithMessage("Password must contain an upper-case letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
}

internal sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).StrongPassword();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
    }
}

internal sealed class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Token).NotEmpty().MaximumLength(2048);
    }
}

internal sealed class EmailRequestValidator : AbstractValidator<EmailRequest>
{
    public EmailRequestValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
}

internal sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(PasswordRules.MaxLength);
    }
}

internal sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Token).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.NewPassword).StrongPassword();
    }
}

internal sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator() => RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
}
