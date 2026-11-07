using System.Globalization;
using MarketPulse.SharedKernel.Results;

namespace MarketPulse.SharedKernel.Market;

/// <summary>
/// A monetary amount with an explicit ISO-4217 currency. May be negative (P&amp;L, ledger debits).
/// Never use float/double for money (spec §11.1).
/// </summary>
public readonly record struct Money
{
    public const int MaxScale = 4;

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3 || !currency.All(char.IsAsciiLetterUpper))
        {
            return Error.Validation("money.currency", "Currency must be a 3-letter upper-case ISO-4217 code.");
        }

        return new Money(decimal.Round(amount, MaxScale, MidpointRounding.ToEven), currency);
    }

    public static Money Inr(decimal amount) => Create(amount, "INR").Value;

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Negate() => new(-Amount, Currency);

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static Money operator -(Money value) => value.Negate();

    public override string ToString() => $"{Amount.ToString(CultureInfo.InvariantCulture)} {Currency}";

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Cannot combine {Currency} with {other.Currency}.");
        }
    }
}
