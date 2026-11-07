using MarketPulse.SharedKernel.Identifiers;

namespace MarketPulse.Modules.Instruments.Domain;

// All master data is IMPORTED with source/version tracking (spec §4.2). Nothing here is hardcoded:
// no exchanges, lot sizes, tick sizes, expiries or calendars are seeded by migrations.

public sealed class Exchange
{
    public int Id { get; set; }

    /// <summary>Exchange code as published by the source, e.g. the code used by the provider's instrument master.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>IANA time zone of the exchange (Asia/Kolkata for Indian exchanges).</summary>
    public string TimeZoneId { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    /// <summary>True for the mock provider's fictional exchange. Simulated rows are never shown as real.</summary>
    public bool IsSimulated { get; set; }
}

public sealed class Segment
{
    public int Id { get; set; }

    public int ExchangeId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

public sealed class Sector
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int? ParentSectorId { get; set; }

    /// <summary>Which classification scheme this sector belongs to (sources differ).</summary>
    public string ClassificationSource { get; set; } = string.Empty;
}

public sealed class Company
{
    public Guid Id { get; set; }

    public string LegalName { get; set; } = string.Empty;

    public string? ShortName { get; set; }

    public int? SectorId { get; set; }

    public string? Industry { get; set; }

    /// <summary>Corporate Identification Number, when the source provides it.</summary>
    public string? Cin { get; set; }

    public bool IsSimulated { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

public enum InstrumentType : byte
{
    Equity = 1,
    Index = 2,
    Etf = 3,
    Future = 4,
    Option = 5,
    Other = 99,
}

public enum OptionType : byte
{
    Call = 1,
    Put = 2,
}

public enum ListingStatus : byte
{
    Active = 1,
    Suspended = 2,
    Delisted = 3,
    Expired = 4,
}

/// <summary>
/// Canonical instrument identity (spec §4.2). Attributes that change over time (symbol, lot size, tick size,
/// status) live in <see cref="InstrumentAttributeVersion"/> with effective dates; contract terms that never change
/// for a given contract (expiry, strike, option type) live here.
/// </summary>
public sealed class Instrument
{
    public InstrumentId Id { get; set; }

    public int ExchangeId { get; set; }

    public int SegmentId { get; set; }

    public InstrumentType Type { get; set; }

    public string? Isin { get; set; }

    public Guid? CompanyId { get; set; }

    /// <summary>For derivatives: the underlying instrument.</summary>
    public InstrumentId? UnderlyingInstrumentId { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public decimal? StrikePrice { get; set; }

    public OptionType? OptionType { get; set; }

    public string Currency { get; set; } = "INR";

    public bool IsSimulated { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<InstrumentAttributeVersion> Versions { get; set; } = [];
}

/// <summary>Effective-dated instrument attributes. Exactly one version is open (ValidTo = null) per instrument.</summary>
public sealed class InstrumentAttributeVersion
{
    public long Id { get; set; }

    public InstrumentId InstrumentId { get; set; }

    public DateOnly ValidFrom { get; set; }

    /// <summary>Exclusive end date; null = current.</summary>
    public DateOnly? ValidTo { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public string TradingSymbol { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Series { get; set; }

    public int? LotSize { get; set; }

    public decimal? TickSize { get; set; }

    public ListingStatus ListingStatus { get; set; }

    /// <summary>The master import that produced this version (provenance).</summary>
    public Guid MasterVersionId { get; set; }
}

/// <summary>How a provider identifies the instrument (spec §11.1: unique provider source ids).</summary>
public sealed class InstrumentProviderMapping
{
    public long Id { get; set; }

    public InstrumentId InstrumentId { get; set; }

    public ProviderId ProviderId { get; set; }

    public string ProviderInstrumentKey { get; set; } = string.Empty;

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }
}

public enum AliasType : byte
{
    FormerSymbol = 1,
    ProviderSymbol = 2,
    CommonName = 3,
}

public sealed class InstrumentAlias
{
    public long Id { get; set; }

    public InstrumentId InstrumentId { get; set; }

    public AliasType AliasType { get; set; }

    public string Alias { get; set; } = string.Empty;

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public string Source { get; set; } = string.Empty;
}

public enum MasterImportStatus : byte
{
    Pending = 1,
    Validated = 2,
    Applied = 3,
    Rejected = 4,
}

/// <summary>One import of an instrument master / calendar file (spec §4.2: source, timestamp, version, validation).</summary>
public sealed class InstrumentMasterVersion
{
    public Guid Id { get; set; }

    public ProviderId ProviderId { get; set; }

    public string SourceName { get; set; } = string.Empty;

    public string? SourceVersion { get; set; }

    public DateTime ImportedAtUtc { get; set; }

    public int RecordCount { get; set; }

    public MasterImportStatus Status { get; set; }

    public string? ValidationSummaryJson { get; set; }

    public bool IsSimulated { get; set; }
}

public enum CalendarDayType : byte
{
    Trading = 1,
    Holiday = 2,
    SpecialSession = 3,
}

public sealed class TradingCalendarDay
{
    public long Id { get; set; }

    public int ExchangeId { get; set; }

    public int? SegmentId { get; set; }

    public DateOnly Date { get; set; }

    public CalendarDayType DayType { get; set; }

    public string? Description { get; set; }

    public Guid MasterVersionId { get; set; }
}

public enum SessionType : byte
{
    PreOpen = 1,
    Normal = 2,
    PostClose = 3,
    Special = 4,
}

/// <summary>Session times in exchange-local wall-clock time, effective-dated, optionally for one specific date.</summary>
public sealed class TradingSession
{
    public long Id { get; set; }

    public int ExchangeId { get; set; }

    public int SegmentId { get; set; }

    public SessionType SessionType { get; set; }

    public TimeOnly StartLocal { get; set; }

    public TimeOnly EndLocal { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    /// <summary>Set when the session applies to a single date only (e.g. a special session).</summary>
    public DateOnly? SpecificDate { get; set; }

    public Guid MasterVersionId { get; set; }
}
