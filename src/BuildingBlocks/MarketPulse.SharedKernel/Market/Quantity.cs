using System.Globalization;
using MarketPulse.SharedKernel.Results;

namespace MarketPulse.SharedKernel.Market;

/// <summary>A non-negative count of shares/units (SQL <c>bigint</c>). Lot sizes come from master data, never hardcoded.</summary>
public readonly record struct Quantity : IComparable<Quantity>
{
    private Quantity(long value) => Value = value;

    public long Value { get; }

    public static Quantity Zero => new(0);

    public static Result<Quantity> Create(long value) =>
        value < 0
            ? Error.Validation("quantity.negative", "Quantity cannot be negative.")
            : new Quantity(value);

    /// <summary>True when the quantity is a whole number of lots of the given (positive) lot size.</summary>
    public bool IsMultipleOfLot(int lotSize)
    {
        if (lotSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lotSize), "Lot size must be positive.");
        }

        return Value % lotSize == 0;
    }

    public int CompareTo(Quantity other) => Value.CompareTo(other.Value);

    public static bool operator <(Quantity left, Quantity right) => left.CompareTo(right) < 0;

    public static bool operator >(Quantity left, Quantity right) => left.CompareTo(right) > 0;

    public static bool operator <=(Quantity left, Quantity right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Quantity left, Quantity right) => left.CompareTo(right) >= 0;

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
