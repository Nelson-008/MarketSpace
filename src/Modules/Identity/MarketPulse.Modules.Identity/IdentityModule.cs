using FluentValidation;
using MarketPulse.Application.Abstractions.Auditing;
using MarketPulse.Application.Abstractions.Security;
using MarketPulse.Infrastructure.Common.Persistence;
using MarketPulse.Infrastructure.Common.Security;
using MarketPulse.Modules.Identity.Application;
using MarketPulse.Modules.Identity.Domain;
using MarketPulse.Modules.Identity.Endpoints;
using MarketPulse.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MarketPulse.Modules.Identity;

public static class IdentityModule
{
    public const string Name = "Identity";

    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<IdentityModuleDbContext>(configuration, Name, IdentityModuleDbContext.SchemaName);

        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<AppLinkOptions>().Bind(configuration.GetSection(AppLinkOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

        services
            .AddIdentityCore<ApplicationUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                // Deliberately false: Identity would reject unconfirmed accounts BEFORE checking the password,
                // revealing that the account exists. AuthService enforces confirmation after the password check,
                // and RefreshTokenService refuses unconfirmed users.
                o.SignIn.RequireConfirmedEmail = false;
                o.Password.RequiredLength = PasswordRules.MinLength;
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = true;
                o.Password.RequireNonAlphanumeric = false;
                o.Lockout.AllowedForNewUsers = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<IdentityModuleDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(24));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(jwt.GetSigningKeyBytes()),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = MarketPulseClaims.Subject,
                    RoleClaimType = MarketPulseClaims.Role,
                };
            });

        services.AddScoped<TokenService>();
        services.AddScoped<RefreshTokenService>();
        services.AddScoped<AuthService>();
        services.AddScoped<IEntitlementService, EntitlementService>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>(includeInternalTypes: true);

        return services;
    }

    public static RouteGroupBuilder MapIdentityEndpoints(this RouteGroupBuilder api)
    {
        AuthEndpoints.Map(api);
        AdminAuditEndpoints.Map(api);
        return api;
    }

    /// <summary>IValidatableObject is not run by ValidateDataAnnotations for the root object; run it explicitly.</summary>
    private sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
    {
        public ValidateOptionsResult Validate(string? name, JwtOptions options)
        {
            var errors = options.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(options))
                .Select(r => r.ErrorMessage ?? "Invalid JWT options.")
                .ToList();
            return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
        }
    }
}
