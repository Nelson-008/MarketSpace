using MarketPulse.SharedKernel.Identifiers;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MarketPulse.Infrastructure.Common.Persistence;

/// <summary>
/// Enforces ADR-0003: every DateTime written to SQL must be UTC; every DateTime read back is marked UTC.
/// Writing a Local/Unspecified value is a bug and throws instead of silently shifting time.
/// </summary>
public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(v => UtcGuard.Ensure(v), v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

public static class UtcGuard
{
    public static DateTime Ensure(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new InvalidOperationException($"Refusing to persist a non-UTC DateTime (Kind={value.Kind}).");
}

public sealed class TenantIdConverter : ValueConverter<TenantId, Guid>
{
    public TenantIdConverter()
        : base(v => v.Value, v => new TenantId(v))
    {
    }
}

public sealed class UserIdConverter : ValueConverter<UserId, Guid>
{
    public UserIdConverter()
        : base(v => v.Value, v => new UserId(v))
    {
    }
}

public sealed class InstrumentIdConverter : ValueConverter<InstrumentId, Guid>
{
    public InstrumentIdConverter()
        : base(v => v.Value, v => new InstrumentId(v))
    {
    }
}

public sealed class ProviderIdConverter : ValueConverter<ProviderId, Guid>
{
    public ProviderIdConverter()
        : base(v => v.Value, v => new ProviderId(v))
    {
    }
}
