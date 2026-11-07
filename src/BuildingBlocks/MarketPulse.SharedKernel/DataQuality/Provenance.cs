using MarketPulse.SharedKernel.Identifiers;
using MarketPulse.SharedKernel.Results;

namespace MarketPulse.SharedKernel.DataQuality;

/// <summary>
/// Where a data record came from and when (spec §4.3, §7). Carried by every market, disclosure and fundamental
/// record and persisted as a standard column block. All instants are UTC; exchange time and received time are
/// kept separate and are never substituted for each other.
/// </summary>
public sealed record Provenance
{
    private Provenance(
        ProviderId providerId,
        string? sourceRecordKey,
        DateTime? exchangeTimeUtc,
        DateTime? providerTimeUtc,
        DateTime receivedAtUtc,
        Guid? ingestionRunId,
        DataQualityStatus quality,
        DataDelivery delivery)
    {
        ProviderId = providerId;
        SourceRecordKey = sourceRecordKey;
        ExchangeTimeUtc = exchangeTimeUtc;
        ProviderTimeUtc = providerTimeUtc;
        ReceivedAtUtc = receivedAtUtc;
        IngestionRunId = ingestionRunId;
        Quality = quality;
        Delivery = delivery;
    }

    public ProviderId ProviderId { get; }

    /// <summary>The provider's own identifier for the record (sequence number, document id, ...), when available.</summary>
    public string? SourceRecordKey { get; }

    /// <summary>Exchange event time, when the provider supplies it.</summary>
    public DateTime? ExchangeTimeUtc { get; }

    /// <summary>Provider timestamp, when supplied.</summary>
    public DateTime? ProviderTimeUtc { get; }

    /// <summary>When MarketPulse received the record. Always present.</summary>
    public DateTime ReceivedAtUtc { get; }

    public Guid? IngestionRunId { get; }

    public DataQualityStatus Quality { get; }

    public DataDelivery Delivery { get; }

    public bool IsSimulated => Delivery.Mode == DeliveryMode.Simulated;

    public static Result<Provenance> Create(
        ProviderId providerId,
        DateTime receivedAtUtc,
        DataQualityStatus quality,
        DataDelivery delivery,
        DateTime? exchangeTimeUtc = null,
        DateTime? providerTimeUtc = null,
        string? sourceRecordKey = null,
        Guid? ingestionRunId = null)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        if (providerId.Value == Guid.Empty)
        {
            return Error.Validation("provenance.provider", "A provider must be identified.");
        }

        if (!IsUtc(receivedAtUtc) || (exchangeTimeUtc is { } e && !IsUtc(e)) || (providerTimeUtc is { } p && !IsUtc(p)))
        {
            return Error.Validation("provenance.utc", "All provenance timestamps must be UTC.");
        }

        if (!Enum.IsDefined(quality))
        {
            return Error.Validation("provenance.quality", "Unknown data-quality status.");
        }

        return new Provenance(providerId, sourceRecordKey, exchangeTimeUtc, providerTimeUtc, receivedAtUtc, ingestionRunId, quality, delivery);
    }

    private static bool IsUtc(DateTime value) => value.Kind == DateTimeKind.Utc;
}
