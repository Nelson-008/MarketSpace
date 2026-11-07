using MarketPulse.SharedKernel.Market;
using Shouldly;

namespace MarketPulse.UnitTests.SharedKernel;

public class PriceTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("0.05")]
    [InlineData("2450.1234")]
    [InlineData("100.12340")] // trailing zero beyond scale 4 is harmless
    public void Valid_prices_are_accepted(string raw)
    {
        Price.Create(decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Negative_price_is_rejected()
    {
        Price.Create(-0.05m).Error.Code.ShouldBe("price.negative");
    }

    [Fact]
    public void More_than_four_decimals_is_rejected_not_rounded()
    {
        Price.Create(10.12345m).Error.Code.ShouldBe("price.precision");
    }

    [Theory]
    [InlineData("100.05", "0.05", true)]
    [InlineData("100.03", "0.05", false)]
    [InlineData("100.01", "0.01", true)]
    public void Tick_alignment_uses_supplied_tick_size(string price, string tick, bool expected)
    {
        var p = Price.Create(decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture)).Value;

        p.IsAlignedTo(decimal.Parse(tick, System.Globalization.CultureInfo.InvariantCulture)).ShouldBe(expected);
    }

    [Fact]
    public void Zero_tick_size_throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Price.Create(1m).Value.IsAlignedTo(0m));
    }
}

public class QuantityTests
{
    [Fact]
    public void Negative_quantity_is_rejected()
    {
        Quantity.Create(-1).IsFailure.ShouldBeTrue();
    }

    [Theory]
    [InlineData(150, 75, true)]
    [InlineData(100, 75, false)]
    [InlineData(0, 75, true)]
    public void Lot_multiple_check(long qty, int lot, bool expected)
    {
        Quantity.Create(qty).Value.IsMultipleOfLot(lot).ShouldBe(expected);
    }
}

public class MoneyTests
{
    [Fact]
    public void Same_currency_arithmetic_works_and_allows_negative()
    {
        var result = Money.Inr(100m) - Money.Inr(150.25m);

        result.ShouldBe(Money.Inr(-50.25m));
    }

    [Fact]
    public void Mixed_currency_arithmetic_throws()
    {
        var usd = Money.Create(1m, "USD").Value;

        Should.Throw<InvalidOperationException>(() => Money.Inr(1m) + usd);
    }

    [Theory]
    [InlineData("")]
    [InlineData("inr")]
    [InlineData("RUPEE")]
    public void Invalid_currency_codes_are_rejected(string code)
    {
        Money.Create(1m, code).Error.Code.ShouldBe("money.currency");
    }
}
