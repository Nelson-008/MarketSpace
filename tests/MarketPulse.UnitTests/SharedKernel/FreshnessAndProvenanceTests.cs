using MarketPulse.SharedKernel.DataQuality;
using MarketPulse.SharedKernel.Identifiers;
using Shouldly;

namespace MarketPulse.UnitTests.SharedKernel;

public class FreshnessAssessorTests
{
    private static readonly DateTime Now = new(2026, 11, 6, 5, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan MaxAge = TimeSpan.FromSeconds(10);

    [Fact]
    public void Live_contract_with_fresh_heartbeat_and_healthy_quality_is_Live()
    {
        Assess(DataDelivery.Live, DataQualityStatus.Healthy, Now.AddSeconds(-2)).Label.ShouldBe(FreshnessLabel.Live);
    }

    [Fact]
    public void Spec_16_2_provider_disconnect_marks_live_data_Stale_not_Live()
    {
        // Heartbeat stopped 30s ago while the last quote was "live".
        Assess(DataDelivery.Live, DataQualityStatus.Healthy, Now.AddSeconds(-30)).Label.ShouldBe(FreshnessLabel.Stale);
    }

    [Theory]
    [InlineData(DataQualityStatus.Healthy)]
    [InlineData(DataQualityStatus.Reconciled)]
    public void Simulated_data_is_never_Live(DataQualityStatus quality)
    {
        Assess(DataDelivery.Simulated, quality, Now).Label.ShouldBe(FreshnessLabel.Simulated);
    }

    [Fact]
    public void Simulated_feed_that_stops_is_Stale()
    {
        Assess(DataDelivery.Simulated, DataQualityStatus.Healthy, Now.AddMinutes(-5)).Label.ShouldBe(FreshnessLabel.Stale);
    }

    [Theory]
    [InlineData(DataQualityStatus.Partial)]
    [InlineData(DataQualityStatus.Unverified)]
    [InlineData(DataQualityStatus.Delayed)]
    public void Live_contract_with_degraded_quality_is_not_Live(DataQualityStatus quality)
    {
        Assess(DataDelivery.Live, quality, Now).Label.ShouldBe(FreshnessLabel.Delayed);
    }

    [Theory]
    [InlineData(DataQualityStatus.Missing)]
    [InlineData(DataQualityStatus.ProviderUnavailable)]
    public void Missing_or_unavailable_is_Unavailable(DataQualityStatus quality)
    {
        Assess(DataDelivery.Live, quality, Now).Label.ShouldBe(FreshnessLabel.Unavailable);
    }

    [Fact]
    public void Explicit_stale_flag_wins_over_fresh_heartbeat()
    {
        Assess(DataDelivery.Live, DataQualityStatus.Stale, Now).Label.ShouldBe(FreshnessLabel.Stale);
    }

    [Fact]
    public void Delayed_feed_reports_its_delay()
    {
        var delivery = DataDelivery.Delayed(TimeSpan.FromMinutes(15)).Value;

        var result = Assess(delivery, DataQualityStatus.Healthy, Now);

        result.Label.ShouldBe(FreshnessLabel.Delayed);
        result.Reason.ShouldContain("15 min");
    }

    [Fact]
    public void Delayed_requires_positive_delay()
    {
        DataDelivery.Delayed(TimeSpan.Zero).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Non_utc_now_is_rejected()
    {
        Should.Throw<ArgumentException>(() => FreshnessAssessor.Assess(
            DataDelivery.Live, DataQualityStatus.Healthy, Now, DateTime.Now, MaxAge));
    }

    private static FreshnessAssessment Assess(DataDelivery delivery, DataQualityStatus quality, DateTime heartbeat) =>
        FreshnessAssessor.Assess(delivery, quality, heartbeat, Now, MaxAge);
}

public class ProvenanceTests
{
    private static readonly ProviderId Provider = ProviderId.New();
    private static readonly DateTime Received = new(2026, 11, 6, 5, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Valid_provenance_keeps_exchange_and_received_time_separate()
    {
        var exchangeTime = Received.AddMilliseconds(-250);

        var p = Provenance.Create(Provider, Received, DataQualityStatus.Healthy, DataDelivery.Live, exchangeTimeUtc: exchangeTime).Value;

        p.ExchangeTimeUtc.ShouldBe(exchangeTime);
        p.ReceivedAtUtc.ShouldBe(Received);
        p.IsSimulated.ShouldBeFalse();
    }

    [Fact]
    public void Simulated_delivery_marks_record_simulated()
    {
        Provenance.Create(Provider, Received, DataQualityStatus.Healthy, DataDelivery.Simulated).Value.IsSimulated.ShouldBeTrue();
    }

    [Fact]
    public void Non_utc_exchange_time_is_rejected()
    {
        var local = new DateTime(2026, 11, 6, 10, 30, 0, DateTimeKind.Local);

        Provenance.Create(Provider, Received, DataQualityStatus.Healthy, DataDelivery.Live, exchangeTimeUtc: local)
            .Error.Code.ShouldBe("provenance.utc");
    }

    [Fact]
    public void Empty_provider_is_rejected()
    {
        Provenance.Create(new ProviderId(Guid.Empty), Received, DataQualityStatus.Healthy, DataDelivery.Live)
            .Error.Code.ShouldBe("provenance.provider");
    }
}
