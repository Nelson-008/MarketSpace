using System.Security.Claims;
using MarketPulse.Application.Abstractions.Security;
using MarketPulse.SharedKernel.Identifiers;
using Microsoft.AspNetCore.Http;

namespace MarketPulse.Infrastructure.Common.Security;

/// <summary>JWT claim names issued by the Identity module.</summary>
public static class MarketPulseClaims
{
    public const string Subject = "sub";
    public const string Tenant = "tid";
    public const string Role = "role";
    public const string Email = "email";
}

/// <summary>Reads the caller from the validated JWT on the current HTTP request.</summary>
public sealed class HttpUserContext(IHttpContextAccessor accessor) : IUserContext
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true
        && TryGetGuid(MarketPulseClaims.Subject, out _)
        && TryGetGuid(MarketPulseClaims.Tenant, out _);

    public bool IsSystem => false;

    public UserId UserId => TryGetGuid(MarketPulseClaims.Subject, out var id)
        ? new UserId(id)
        : throw new InvalidOperationException("No authenticated user.");

    public TenantId TenantId => TryGetGuid(MarketPulseClaims.Tenant, out var id)
        ? new TenantId(id)
        : throw new InvalidOperationException("No authenticated tenant.");

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;

    private bool TryGetGuid(string claimType, out Guid value)
    {
        value = Guid.Empty;
        var raw = Principal?.FindFirst(claimType)?.Value;
        return raw is not null && Guid.TryParse(raw, out value) && value != Guid.Empty;
    }
}

/// <summary>Context for background workers. Has no user or tenant; tenant-owned data is invisible to queries.</summary>
public sealed class SystemUserContext : IUserContext
{
    public bool IsAuthenticated => false;

    public bool IsSystem => true;

    public UserId UserId => throw new InvalidOperationException("The system context has no user.");

    public TenantId TenantId => throw new InvalidOperationException("The system context has no tenant.");

    public bool IsInRole(string role) => false;
}
