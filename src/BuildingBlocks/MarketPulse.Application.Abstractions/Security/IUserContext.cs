using MarketPulse.SharedKernel.Identifiers;

namespace MarketPulse.Application.Abstractions.Security;

/// <summary>
/// The authenticated caller. All user-owned data access is scoped by <see cref="TenantId"/> and <see cref="UserId"/>
/// (spec §4.1). Background workers run under <see cref="IsSystem"/> and must never read user-owned data implicitly.
/// </summary>
public interface IUserContext
{
    bool IsAuthenticated { get; }

    bool IsSystem { get; }

    /// <summary>The caller's user id. Throws when the caller is not an authenticated user.</summary>
    UserId UserId { get; }

    /// <summary>The caller's tenant id. Throws when the caller is not an authenticated user.</summary>
    TenantId TenantId { get; }

    bool IsInRole(string role);
}

/// <summary>Role names from spec §4.1. Least privilege: users get only <see cref="User"/> by default.</summary>
public static class Roles
{
    public const string User = "User";
    public const string Admin = "Admin";
    public const string Support = "Support";
    public const string DataOperator = "DataOperator";
    public const string ResearchEditor = "ResearchEditor";

    public static readonly IReadOnlyList<string> All = [User, Admin, Support, DataOperator, ResearchEditor];
}
