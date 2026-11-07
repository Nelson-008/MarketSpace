namespace MarketPulse.SharedKernel.Time;

/// <summary>Abstraction over the current time so that market-session logic is testable. Always UTC.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
