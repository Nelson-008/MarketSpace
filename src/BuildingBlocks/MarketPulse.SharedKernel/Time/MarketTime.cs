namespace MarketPulse.SharedKernel.Time;

/// <summary>
/// Conversions between UTC (the storage and transport standard) and Indian exchange-local time
/// (Asia/Kolkata), used for display and for deriving a trading date (spec §7).
/// </summary>
/// <remarks>
/// This class does NOT know which days are trading days or when sessions open. That comes from
/// imported trading calendars and sessions (master data), never from hardcoded rules.
/// </remarks>
public static class MarketTime
{
    public const string IndiaTimeZoneId = "Asia/Kolkata";

    private static readonly Lazy<TimeZoneInfo> India = new(() => TimeZoneInfo.FindSystemTimeZoneById(IndiaTimeZoneId));

    public static TimeZoneInfo IndiaTimeZone => India.Value;

    /// <summary>Converts a UTC instant to Asia/Kolkata local time.</summary>
    /// <exception cref="ArgumentException">If <paramref name="utc"/> is not of kind <see cref="DateTimeKind.Utc"/>.</exception>
    public static DateTime ToIndiaTime(DateTime utc)
    {
        EnsureUtc(utc, nameof(utc));
        return TimeZoneInfo.ConvertTimeFromUtc(utc, IndiaTimeZone);
    }

    /// <summary>The calendar date in Asia/Kolkata on which the UTC instant falls. It is not guaranteed to be a trading day.</summary>
    public static DateOnly IndiaDateOf(DateTime utc) => DateOnly.FromDateTime(ToIndiaTime(utc));

    /// <summary>Converts an exchange-local (Asia/Kolkata) wall-clock time to UTC.</summary>
    public static DateTime FromIndiaTime(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, IndiaTimeZone);
    }

    /// <summary>Guard used across the codebase: every persisted or transported instant must be UTC.</summary>
    public static void EnsureUtc(DateTime value, string paramName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"Expected a UTC DateTime but got Kind={value.Kind}.", paramName);
        }
    }
}
