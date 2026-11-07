using MarketPulse.Application.Abstractions.Security;
using MarketPulse.Infrastructure.Common.Persistence;
using MarketPulse.Infrastructure.Common.Security;
using MarketPulse.Modules.Instruments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MarketPulse.Modules.Instruments.Infrastructure;

/// <summary>Owns the <c>master</c> schema. Reference data is not tenant-owned.</summary>
public sealed class InstrumentsDbContext(DbContextOptions<InstrumentsDbContext> options, IUserContext userContext)
    : ModuleDbContext(options, userContext)
{
    public const string SchemaName = "master";

    public override string Schema => SchemaName;

    public DbSet<Exchange> Exchanges => Set<Exchange>();

    public DbSet<Segment> Segments => Set<Segment>();

    public DbSet<Sector> Sectors => Set<Sector>();

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Instrument> Instruments => Set<Instrument>();

    public DbSet<InstrumentAttributeVersion> InstrumentAttributeVersions => Set<InstrumentAttributeVersion>();

    public DbSet<InstrumentProviderMapping> InstrumentProviderMappings => Set<InstrumentProviderMapping>();

    public DbSet<InstrumentAlias> InstrumentAliases => Set<InstrumentAlias>();

    public DbSet<InstrumentMasterVersion> InstrumentMasterVersions => Set<InstrumentMasterVersion>();

    public DbSet<TradingCalendarDay> TradingCalendarDays => Set<TradingCalendarDay>();

    public DbSet<TradingSession> TradingSessions => Set<TradingSession>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        var b = modelBuilder;
        b.Entity<Exchange>(e =>
        {
            e.ToTable("Exchanges");
            e.Property(x => x.Code).HasMaxLength(20).IsUnicode(false);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.TimeZoneId).HasMaxLength(64).IsUnicode(false);
            e.Property(x => x.CountryCode).HasMaxLength(2).IsFixedLength().IsUnicode(false);
        });

        b.Entity<Segment>(e =>
        {
            e.ToTable("Segments");
            e.Property(x => x.Code).HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasIndex(x => new { x.ExchangeId, x.Code }).IsUnique();
            e.HasOne<Exchange>().WithMany().HasForeignKey(x => x.ExchangeId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Sector>(e =>
        {
            e.ToTable("Sectors");
            e.Property(x => x.Code).HasMaxLength(50).IsUnicode(false);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.ClassificationSource).HasMaxLength(100);
            e.HasIndex(x => new { x.ClassificationSource, x.Code }).IsUnique();
            e.HasOne<Sector>().WithMany().HasForeignKey(x => x.ParentSectorId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Company>(e =>
        {
            e.ToTable("Companies");
            e.Property(x => x.LegalName).HasMaxLength(300);
            e.Property(x => x.ShortName).HasMaxLength(100);
            e.Property(x => x.Industry).HasMaxLength(200);
            e.Property(x => x.Cin).HasMaxLength(21).IsUnicode(false);
            e.HasIndex(x => x.Cin).IsUnique().HasFilter("[Cin] IS NOT NULL");
            e.HasIndex(x => x.LegalName);
            e.HasOne<Sector>().WithMany().HasForeignKey(x => x.SectorId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Instrument>(e =>
        {
            e.ToTable("Instruments", t =>
            {
                t.HasCheckConstraint("CK_Instruments_OptionTerms",
                    "([Type] = 5 AND [OptionType] IS NOT NULL AND [StrikePrice] IS NOT NULL AND [ExpiryDate] IS NOT NULL) OR ([Type] <> 5 AND [OptionType] IS NULL AND [StrikePrice] IS NULL)");
                t.HasCheckConstraint("CK_Instruments_FutureTerms", "[Type] <> 4 OR [ExpiryDate] IS NOT NULL");
                t.HasCheckConstraint("CK_Instruments_Strike", "[StrikePrice] IS NULL OR [StrikePrice] > 0");
            });
            e.Property(x => x.Isin).HasMaxLength(12).IsFixedLength().IsUnicode(false);
            e.Property(x => x.Currency).HasMaxLength(3).IsFixedLength().IsUnicode(false);
            e.Property(x => x.StrikePrice).HasPrecision(19, 4);
            e.HasIndex(x => new { x.ExchangeId, x.SegmentId, x.Isin }).HasFilter("[Isin] IS NOT NULL AND [Type] IN (1,3)").IsUnique();
            e.HasIndex(x => x.CompanyId);
            e.HasIndex(x => new { x.UnderlyingInstrumentId, x.ExpiryDate });
            e.HasOne<Exchange>().WithMany().HasForeignKey(x => x.ExchangeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Segment>().WithMany().HasForeignKey(x => x.SegmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Instrument>().WithMany().HasForeignKey(x => x.UnderlyingInstrumentId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Versions).WithOne().HasForeignKey(v => v.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<InstrumentAttributeVersion>(e =>
        {
            e.ToTable("InstrumentAttributeVersions", t =>
            {
                t.HasCheckConstraint("CK_InstrumentAttributeVersions_Range", "[ValidTo] IS NULL OR [ValidTo] > [ValidFrom]");
                t.HasCheckConstraint("CK_InstrumentAttributeVersions_Lot", "[LotSize] IS NULL OR [LotSize] > 0");
                t.HasCheckConstraint("CK_InstrumentAttributeVersions_Tick", "[TickSize] IS NULL OR [TickSize] > 0");
            });
            e.Property(x => x.Symbol).HasMaxLength(50);
            e.Property(x => x.TradingSymbol).HasMaxLength(100);
            e.Property(x => x.Name).HasMaxLength(300);
            e.Property(x => x.Series).HasMaxLength(10);
            e.Property(x => x.TickSize).HasPrecision(19, 4);
            e.HasIndex(x => new { x.InstrumentId, x.ValidFrom }).IsUnique();
            // At most one open (current) version per instrument.
            e.HasIndex(x => x.InstrumentId).IsUnique().HasFilter("[ValidTo] IS NULL").HasDatabaseName("UX_InstrumentAttributeVersions_Current");
            e.HasIndex(x => x.TradingSymbol);
            e.HasIndex(x => x.Symbol);
            e.HasOne<InstrumentMasterVersion>().WithMany().HasForeignKey(x => x.MasterVersionId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<InstrumentProviderMapping>(e =>
        {
            e.ToTable("InstrumentProviderMappings", t => t.HasCheckConstraint("CK_InstrumentProviderMappings_Range", "[ValidTo] IS NULL OR [ValidTo] > [ValidFrom]"));
            e.Property(x => x.ProviderInstrumentKey).HasMaxLength(100);
            e.HasIndex(x => new { x.ProviderId, x.ProviderInstrumentKey, x.ValidFrom }).IsUnique();
            e.HasIndex(x => x.InstrumentId);
            e.HasOne<Instrument>().WithMany().HasForeignKey(x => x.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<InstrumentAlias>(e =>
        {
            e.ToTable("InstrumentAliases");
            e.Property(x => x.Alias).HasMaxLength(100);
            e.Property(x => x.Source).HasMaxLength(100);
            e.HasIndex(x => x.Alias);
            e.HasIndex(x => new { x.InstrumentId, x.AliasType, x.Alias, x.ValidFrom }).IsUnique();
            e.HasOne<Instrument>().WithMany().HasForeignKey(x => x.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<InstrumentMasterVersion>(e =>
        {
            e.ToTable("InstrumentMasterVersions");
            e.Property(x => x.SourceName).HasMaxLength(200);
            e.Property(x => x.SourceVersion).HasMaxLength(100);
            e.Property(x => x.ValidationSummaryJson).HasMaxLength(4000);
            e.HasIndex(x => new { x.ProviderId, x.ImportedAtUtc });
        });

        b.Entity<TradingCalendarDay>(e =>
        {
            e.ToTable("TradingCalendarDays");
            e.Property(x => x.Description).HasMaxLength(200);
            e.HasIndex(x => new { x.ExchangeId, x.SegmentId, x.Date }).IsUnique();
            e.HasOne<Exchange>().WithMany().HasForeignKey(x => x.ExchangeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Segment>().WithMany().HasForeignKey(x => x.SegmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<InstrumentMasterVersion>().WithMany().HasForeignKey(x => x.MasterVersionId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<TradingSession>(e =>
        {
            e.ToTable("TradingSessions", t =>
            {
                t.HasCheckConstraint("CK_TradingSessions_Times", "[EndLocal] > [StartLocal]");
                t.HasCheckConstraint("CK_TradingSessions_Range", "[ValidTo] IS NULL OR [ValidTo] > [ValidFrom]");
            });
            e.HasIndex(x => new { x.ExchangeId, x.SegmentId, x.SessionType, x.ValidFrom });
            e.HasOne<Exchange>().WithMany().HasForeignKey(x => x.ExchangeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Segment>().WithMany().HasForeignKey(x => x.SegmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<InstrumentMasterVersion>().WithMany().HasForeignKey(x => x.MasterVersionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

internal sealed class DesignTimeFactory : IDesignTimeDbContextFactory<InstrumentsDbContext>
{
    public InstrumentsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<InstrumentsDbContext>()
                .UseSqlServer(
                    "Server=.;Database=MarketPulse_DesignTime;Trusted_Connection=True;TrustServerCertificate=True",
                    sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", InstrumentsDbContext.SchemaName))
                .Options,
            new SystemUserContext());
}
