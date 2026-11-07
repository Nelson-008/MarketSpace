using MarketPulse.SharedKernel.Time;
using Shouldly;

namespace MarketPulse.UnitTests.SharedKernel;

public class MarketTimeTests
{
    [Fact]
    public void ToIndiaTime_adds_five_and_a_half_hours()
    {
        var utc = new DateTime(2026, 11, 6, 3, 45, 0, DateTimeKind.Utc);

        MarketTime.ToIndiaTime(utc).ShouldBe(new DateTime(2026, 11, 6, 9, 15, 0));
    }

    [Fact]
    public void IndiaDateOf_rolls_to_next_day_after_1830_utc()
    {
        var utc = new DateTime(2026, 11, 6, 20, 0, 0, DateTimeKind.Utc);

        MarketTime.IndiaDateOf(utc).ShouldBe(new DateOnly(2026, 11, 7));
    }

    [Fact]
    public void FromIndiaTime_round_trips()
    {
        var utc = MarketTime.FromIndiaTime(new DateOnly(2026, 11, 6), new TimeOnly(15, 30));

        utc.ShouldBe(new DateTime(2026, 11, 6, 10, 0, 0, DateTimeKind.Utc));
        utc.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Non_utc_input_is_rejected(DateTimeKind kind)
    {
        var value = new DateTime(2026, 11, 6, 3, 45, 0, kind);

        Should.Throw<ArgumentException>(() => MarketTime.ToIndiaTime(value));
    }
}
