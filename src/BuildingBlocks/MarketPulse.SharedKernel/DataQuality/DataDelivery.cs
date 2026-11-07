using MarketPulse.SharedKernel.Results;

namespace MarketPulse.SharedKernel.DataQuality;

/// <summary>The delivery mode of a dataset plus, for delayed data, the exact contractual delay.</summary>
public sealed record DataDelivery
{
    private DataDelivery(DeliveryMode mode, TimeSpan? delay)
    {
        Mode = mode;
        Delay = delay;
    }

    public DeliveryMode Mode { get; }

    /// <summary>Contractual delay; set only when <see cref="Mode"/> is <see cref="DeliveryMode.Delayed"/>.</summary>
    public TimeSpan? Delay { get; }

    public static DataDelivery Live { get; } = new(DeliveryMode.Live, null);

    public static DataDelivery EndOfDay { get; } = new(DeliveryMode.EndOfDay, null);

    public static DataDelivery Simulated { get; } = new(DeliveryMode.Simulated, null);

    public static Result<DataDelivery> Delayed(TimeSpan delay) =>
        delay <= TimeSpan.Zero
            ? Error.Validation("delivery.delay", "A delayed feed must declare a positive delay.")
            : new DataDelivery(DeliveryMode.Delayed, delay);
}
