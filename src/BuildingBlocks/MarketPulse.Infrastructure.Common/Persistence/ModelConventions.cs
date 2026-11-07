using MarketPulse.SharedKernel.Identifiers;
using Microsoft.EntityFrameworkCore;

namespace MarketPulse.Infrastructure.Common.Persistence;

/// <summary>Model-wide conventions from ADR-0003, shared by every module DbContext (including Identity's).</summary>
public static class ModelConventions
{
    public static void Apply(ModelConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>().HaveColumnType("datetime2(7)");
        builder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>().HaveColumnType("datetime2(7)");

        // Default precision for any decimal not configured explicitly is a price: decimal(19,4).
        builder.Properties<decimal>().HavePrecision(19, 4);

        // No accidental nvarchar(max); long text must opt in explicitly.
        builder.Properties<string>().HaveMaxLength(256);

        builder.Properties<TenantId>().HaveConversion<TenantIdConverter>();
        builder.Properties<UserId>().HaveConversion<UserIdConverter>();
        builder.Properties<InstrumentId>().HaveConversion<InstrumentIdConverter>();
        builder.Properties<ProviderId>().HaveConversion<ProviderIdConverter>();
    }
}
