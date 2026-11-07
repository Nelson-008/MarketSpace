using MarketPulse.SharedKernel.DataQuality;
using MarketPulse.SharedKernel.Identifiers;

namespace MarketPulse.Modules.Administration.Domain;

public enum ProviderKind : byte
{
    Mock = 1,
    Broker = 2,
    Exchange = 3,
    Vendor = 4,
    OfficialDisclosure = 5,
}

/// <summary>A registered data provider (spec §6). Mock providers are always <see cref="IsSimulated"/>.</summary>
public sealed class Provider
{
    public ProviderId Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ProviderKind Kind { get; set; }

    public bool IsSimulated { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<ProviderCapability> Capabilities { get; set; } = [];

    public List<ProviderLicense> Licenses { get; set; } = [];
}

/// <summary>Capabilities from spec §6.1 interfaces.</summary>
public enum CapabilityType : byte
{
    Quotes = 1,
    QuoteStreaming = 2,
    HistoricalCandles = 3,
    MarketDepth = 4,
    OptionsData = 5,
    Fundamentals = 6,
    CorporateDisclosures = 7,
    News = 8,
    InstrumentMaster = 9,
}

/// <summary>
/// What a provider can supply. Recorded only from verified documentation (spec §18: never invent capabilities);
/// <see cref="VerificationSource"/> points at that documentation.
/// </summary>
public sealed class ProviderCapability
{
    public long Id { get; set; }

    public ProviderId ProviderId { get; set; }

    public CapabilityType Capability { get; set; }

    public DeliveryMode DeliveryMode { get; set; }

    public int? DelaySeconds { get; set; }

    public int? HistoricalDepthDays { get; set; }

    public string? Notes { get; set; }

    public string? VerificationSource { get; set; }

    public DateTime? VerifiedAtUtc { get; set; }
}

/// <summary>
/// Licence terms for a provider (spec §6.3, §14). Every permission defaults to false: an unknown permission is a
/// denied permission. Features must check these flags before serving provider data.
/// </summary>
public sealed class ProviderLicense
{
    public Guid Id { get; set; }

    public ProviderId ProviderId { get; set; }

    public string LicenseReference { get; set; } = string.Empty;

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public bool PermitsPublicDisplay { get; set; }

    public bool PermitsDerivedAnalytics { get; set; }

    public bool PermitsStorage { get; set; }

    public int? RetentionDays { get; set; }

    public bool PermitsExport { get; set; }

    public bool PermitsAlerts { get; set; }

    public bool PermitsAiReports { get; set; }

    public int? MaxUsers { get; set; }

    /// <summary>Where the signed agreement / written confirmation is stored.</summary>
    public string? DocumentReference { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }
}
