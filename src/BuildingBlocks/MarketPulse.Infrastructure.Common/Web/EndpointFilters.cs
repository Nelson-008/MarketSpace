using FluentValidation;
using MarketPulse.Application.Abstractions.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MarketPulse.Infrastructure.Common.Web;

/// <summary>Validates the first argument of type <typeparamref name="T"/> with its FluentValidation validator.</summary>
internal sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            return ApiResults.Validation(new Dictionary<string, string[]> { ["body"] = ["A request body is required."] });
        }

        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted).ConfigureAwait(false);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => JsonName(e.PropertyName))
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
            return ApiResults.Validation(errors);
        }

        return await next(context).ConfigureAwait(false);
    }

    private static string JsonName(string propertyName) =>
        string.IsNullOrEmpty(propertyName) ? "body" : char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
}

/// <summary>Denies the request unless the caller's tenant plan grants <c>featureKey</c> (spec §4.1, server-side).</summary>
internal sealed class EntitlementFilter(string featureKey) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var user = services.GetRequiredService<IUserContext>();
        if (!user.IsAuthenticated)
        {
            return new ErrorResult(StatusCodes.Status401Unauthorized, "auth.unauthenticated", "Authentication is required.");
        }

        var entitlements = services.GetRequiredService<IEntitlementService>();
        var entitlement = await entitlements.FindAsync(user.TenantId, featureKey, context.HttpContext.RequestAborted).ConfigureAwait(false);
        if (entitlement is null)
        {
            return new ErrorResult(
                StatusCodes.Status403Forbidden,
                "entitlement.missing",
                $"Your plan does not include the feature '{featureKey}'.");
        }

        return await next(context).ConfigureAwait(false);
    }
}

public static class EndpointFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter<ValidationFilter<T>>().ProducesValidationError();

    public static TBuilder RequireEntitlement<TBuilder>(this TBuilder builder, string featureKey)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(new EntitlementFilter(featureKey));

    public static RouteHandlerBuilder ProducesValidationError(this RouteHandlerBuilder builder) =>
        builder.Produces<ApiError>(StatusCodes.Status400BadRequest);

    public static RouteHandlerBuilder ProducesError(this RouteHandlerBuilder builder, int statusCode) =>
        builder.Produces<ApiError>(statusCode);
}
