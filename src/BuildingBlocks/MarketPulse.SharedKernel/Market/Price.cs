using System.Globalization;
using MarketPulse.SharedKernel.Results;

namespace MarketPulse.SharedKernel.Market;

/// <summary>
/// A traded price in the instrument's quote currency. Non-negative, at most <see cref="MaxScale"/> decimal places
/// (matches SQL <c>decimal(19,4)</c>). Derived values that can be negative (basis, spreads, P&amp;L) are not prices.
/// </summary>
public readonly record struct Price : IComparable<Price>
{
    public const int MaxScale = 4;

    private Price(decimal value) => Value = value;

    public decimal Value { get; }

    public static Result<Price> Create(decimal value)
    {
        if (value < 0m)
        {
            return Error.Validation("price.negative", "Price cannot be negative.");
        }

        if (value.Scale > MaxScale && decimal.Round(value, MaxScale) != value)
        {
            return Error.Validation("price.precision", $"Price cannot have more than {MaxScale} decimal places.");
        }

        return new Price(decimal.Round(value, MaxScale));
    }

    /// <summary>True when the price is an exact multiple of the instrument's tick size (tick size comes from master data).</summary>
    public bool IsAlignedTo(decimal tickSize)
    {
        if (tickSize <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(tickSize), "Tick size must be positive.");
        }

        return Value % tickSize == 0m;
    }

    public int CompareTo(Price other) => Value.CompareTo(other.Value);

    public static bool operator <(Price left, Price right) => left.CompareTo(right) < 0;

    public static bool operator >(Price left, Price right) => left.CompareTo(right) > 0;

    public static bool operator <=(Price left, Price right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Price left, Price right) => left.CompareTo(right) >= 0;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
