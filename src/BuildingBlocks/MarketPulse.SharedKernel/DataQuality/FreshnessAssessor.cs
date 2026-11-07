namespace MarketPulse.SharedKernel.DataQuality;

/// <summary>The label a UI must show next to a data value (spec §7 "UI treatment").</summary>
public enum FreshnessLabel : byte
{
    Live = 1,
    Delayed = 2,
    EndOfDay = 3,
    Simulated = 4,
    Stale = 5,
    Unavailable = 6,
}

public sealed record FreshnessAssessment(FreshnessLabel Label, string Reason);

/// <summary>
/// Single source of truth for the LIVE/DELAYED/STALE/SIMULATED label. Rules, in precedence order:
/// <list type="number">
/// <item>Missing data or an unavailable provider → <see cref="FreshnessLabel.Unavailable"/>.</item>
/// <item>A Stale quality flag, or a feed heartbeat older than the allowed age → <see cref="FreshnessLabel.Stale"/>
///   (spec §16.2: a provider disconnect must never keep showing LIVE).</item>
/// <item>Simulated delivery → <see cref="FreshnessLabel.Simulated"/>; mock data is never LIVE.</item>
/// <item>LIVE only when the contract says live AND quality is Healthy/Reconciled.</item>
/// </list>
/// </summary>
public static class FreshnessAssessor
{
    public static FreshnessAssessment Assess(
        DataDelivery delivery,
        DataQualityStatus quality,
        DateTime lastFeedHeartbeatUtc,
        DateTime nowUtc,
        TimeSpan maxHeartbeatAge)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        Time.MarketTime.EnsureUtc(lastFeedHeartbeatUtc, nameof(lastFeedHeartbeatUtc));
        Time.MarketTime.EnsureUtc(nowUtc, nameof(nowUtc));

        if (maxHeartbeatAge <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHeartbeatAge), "Heartbeat age threshold must be positive.");
        }

        if (quality is DataQualityStatus.Missing or DataQualityStatus.ProviderUnavailable)
        {
            return new(FreshnessLabel.Unavailable, $"Data quality is {quality}.");
        }

        var heartbeatAge = nowUtc - lastFeedHeartbeatUtc;
        if (quality == DataQualityStatus.Stale || heartbeatAge > maxHeartbeatAge)
        {
            return new(FreshnessLabel.Stale, quality == DataQualityStatus.Stale
                ? "Data marked stale."
                : $"No feed heartbeat for {heartbeatAge.TotalSeconds:0}s (limit {maxHeartbeatAge.TotalSeconds:0}s).");
        }

        return delivery.Mode switch
        {
            DeliveryMode.Simulated => new(FreshnessLabel.Simulated, "Simulated data from the mock provider."),
            DeliveryMode.Live when quality is DataQualityStatus.Healthy or DataQualityStatus.Reconciled
                => new(FreshnessLabel.Live, "Live feed with healthy heartbeat."),
            DeliveryMode.Live => new(FreshnessLabel.Delayed, $"Live contract but quality is {quality}; not shown as live."),
            DeliveryMode.Delayed => new(FreshnessLabel.Delayed, $"Delayed by {delivery.Delay!.Value.TotalMinutes:0} min."),
            DeliveryMode.EndOfDay => new(FreshnessLabel.EndOfDay, "End-of-day / periodic data."),
            _ => throw new InvalidOperationException($"Unhandled delivery mode {delivery.Mode}."),
        };
    }
}
