IF OBJECT_ID(N'[master].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'master') IS NULL EXEC(N'CREATE SCHEMA [master];');
    CREATE TABLE [master].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'master') IS NULL EXEC(N'CREATE SCHEMA [master];');
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE TABLE [master].[Exchanges] (
        [Id] int NOT NULL IDENTITY,
        [Code] varchar(20) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [TimeZoneId] varchar(64) NOT NULL,
        [CountryCode] char(2) NOT NULL,
        [IsSimulated] bit NOT NULL,
        CONSTRAINT [PK_Exchanges] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE TABLE [master].[InstrumentMasterVersions] (
        [Id] uniqueidentifier NOT NULL,
        [ProviderId] uniqueidentifier NOT NULL,
        [SourceName] nvarchar(200) NOT NULL,
        [SourceVersion] nvarchar(100) NULL,
        [ImportedAtUtc] datetime2(7) NOT NULL,
        [RecordCount] int NOT NULL,
        [Status] tinyint NOT NULL,
        [ValidationSummaryJson] nvarchar(4000) NULL,
        [IsSimulated] bit NOT NULL,
        CONSTRAINT [PK_InstrumentMasterVersions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE TABLE [master].[Sectors] (
        [Id] int NOT NULL IDENTITY,
        [Code] varchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [ParentSectorId] int NULL,
        [ClassificationSource] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Sectors] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Sectors_Sectors_ParentSectorId] FOREIGN KEY ([ParentSectorId]) REFERENCES [master].[Sectors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE TABLE [master].[Segments] (
        [Id] int NOT NULL IDENTITY,
        [ExchangeId] int NOT NULL,
        [Code] varchar(20) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        CONSTRAINT [PK_Segments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Segments_Exchanges_ExchangeId] FOREIGN KEY ([ExchangeId]) REFERENCES [master].[Exchanges] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE TABLE [master].[Companies] (
        [Id] uniqueidentifier NOT NULL,
        [LegalName] nvarchar(300) NOT NULL,
        [ShortName] nvarchar(100) NULL,
        [SectorId] int NULL,
        [Industry] nvarchar(200) NULL,
        [Cin] varchar(21) NULL,
        [IsSimulated] bit NOT NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL,
        [UpdatedAtUtc] datetime2(7) NOT NULL,
        CONSTRAINT [PK_Companies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Companies_Sectors_SectorId] FOREIGN KEY ([SectorId]) REFERENCES [master].[Sectors] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE TABLE [master].[TradingCalendarDays] (
        [Id] bigint NOT NULL IDENTITY,
        [ExchangeId] int NOT NULL,
        [SegmentId] int NULL,
        [Date] date NOT NULL,
        [DayType] tinyint NOT NULL,
        [Description] nvarchar(200) NULL,
        [MasterVersionId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_TradingCalendarDays] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TradingCalendarDays_Exchanges_ExchangeId] FOREIGN KEY ([ExchangeId]) REFERENCES [master].[Exchanges] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TradingCalendarDays_InstrumentMasterVersions_MasterVersionId] FOREIGN KEY ([MasterVersionId]) REFERENCES [master].[InstrumentMasterVersions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TradingCalendarDays_Segments_SegmentId] FOREIGN KEY ([SegmentId]) REFERENCES [master].[Segments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE TABLE [master].[TradingSessions] (
        [Id] bigint NOT NULL IDENTITY,
        [ExchangeId] int NOT NULL,
        [SegmentId] int NOT NULL,
        [SessionType] tinyint NOT NULL,
        [StartLocal] time NOT NULL,
        [EndLocal] time NOT NULL,
        [ValidFrom] date NOT NULL,
        [ValidTo] date NULL,
        [SpecificDate] date NULL,
        [MasterVersionId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_TradingSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_TradingSessions_Range] CHECK ([ValidTo] IS NULL OR [ValidTo] > [ValidFrom]),
        CONSTRAINT [CK_TradingSessions_Times] CHECK ([EndLocal] > [StartLocal]),
        CONSTRAINT [FK_TradingSessions_Exchanges_ExchangeId] FOREIGN KEY ([ExchangeId]) REFERENCES [master].[Exchanges] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TradingSessions_InstrumentMasterVersions_MasterVersionId] FOREIGN KEY ([MasterVersionId]) REFERENCES [master].[InstrumentMasterVersions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TradingSessions_Segments_SegmentId] FOREIGN KEY ([SegmentId]) REFERENCES [master].[Segments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE TABLE [master].[Instruments] (
        [Id] uniqueidentifier NOT NULL,
        [ExchangeId] int NOT NULL,
        [SegmentId] int NOT NULL,
        [Type] tinyint NOT NULL,
        [Isin] char(12) NULL,
        [CompanyId] uniqueidentifier NULL,
        [UnderlyingInstrumentId] uniqueidentifier NULL,
        [ExpiryDate] date NULL,
        [StrikePrice] decimal(19,4) NULL,
        [OptionType] tinyint NULL,
        [Currency] char(3) NOT NULL,
        [IsSimulated] bit NOT NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL,
        CONSTRAINT [PK_Instruments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Instruments_FutureTerms] CHECK ([Type] <> 4 OR [ExpiryDate] IS NOT NULL),
        CONSTRAINT [CK_Instruments_OptionTerms] CHECK (([Type] = 5 AND [OptionType] IS NOT NULL AND [StrikePrice] IS NOT NULL AND [ExpiryDate] IS NOT NULL) OR ([Type] <> 5 AND [OptionType] IS NULL AND [StrikePrice] IS NULL)),
        CONSTRAINT [CK_Instruments_Strike] CHECK ([StrikePrice] IS NULL OR [StrikePrice] > 0),
        CONSTRAINT [FK_Instruments_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [master].[Companies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Instruments_Exchanges_ExchangeId] FOREIGN KEY ([ExchangeId]) REFERENCES [master].[Exchanges] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Instruments_Instruments_UnderlyingInstrumentId] FOREIGN KEY ([UnderlyingInstrumentId]) REFERENCES [master].[Instruments] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Instruments_Segments_SegmentId] FOREIGN KEY ([SegmentId]) REFERENCES [master].[Segments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE TABLE [master].[InstrumentAliases] (
        [Id] bigint NOT NULL IDENTITY,
        [InstrumentId] uniqueidentifier NOT NULL,
        [AliasType] tinyint NOT NULL,
        [Alias] nvarchar(100) NOT NULL,
        [ValidFrom] date NOT NULL,
        [ValidTo] date NULL,
        [Source] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_InstrumentAliases] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InstrumentAliases_Instruments_InstrumentId] FOREIGN KEY ([InstrumentId]) REFERENCES [master].[Instruments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE TABLE [master].[InstrumentAttributeVersions] (
        [Id] bigint NOT NULL IDENTITY,
        [InstrumentId] uniqueidentifier NOT NULL,
        [ValidFrom] date NOT NULL,
        [ValidTo] date NULL,
        [Symbol] nvarchar(50) NOT NULL,
        [TradingSymbol] nvarchar(100) NOT NULL,
        [Name] nvarchar(300) NOT NULL,
        [Series] nvarchar(10) NULL,
        [LotSize] int NULL,
        [TickSize] decimal(19,4) NULL,
        [ListingStatus] tinyint NOT NULL,
        [MasterVersionId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_InstrumentAttributeVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_InstrumentAttributeVersions_Lot] CHECK ([LotSize] IS NULL OR [LotSize] > 0),
        CONSTRAINT [CK_InstrumentAttributeVersions_Range] CHECK ([ValidTo] IS NULL OR [ValidTo] > [ValidFrom]),
        CONSTRAINT [CK_InstrumentAttributeVersions_Tick] CHECK ([TickSize] IS NULL OR [TickSize] > 0),
        CONSTRAINT [FK_InstrumentAttributeVersions_InstrumentMasterVersions_MasterVersionId] FOREIGN KEY ([MasterVersionId]) REFERENCES [master].[InstrumentMasterVersions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InstrumentAttributeVersions_Instruments_InstrumentId] FOREIGN KEY ([InstrumentId]) REFERENCES [master].[Instruments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE TABLE [master].[InstrumentProviderMappings] (
        [Id] bigint NOT NULL IDENTITY,
        [InstrumentId] uniqueidentifier NOT NULL,
        [ProviderId] uniqueidentifier NOT NULL,
        [ProviderInstrumentKey] nvarchar(100) NOT NULL,
        [ValidFrom] date NOT NULL,
        [ValidTo] date NULL,
        CONSTRAINT [PK_InstrumentProviderMappings] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_InstrumentProviderMappings_Range] CHECK ([ValidTo] IS NULL OR [ValidTo] > [ValidFrom]),
        CONSTRAINT [FK_InstrumentProviderMappings_Instruments_InstrumentId] FOREIGN KEY ([InstrumentId]) REFERENCES [master].[Instruments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Companies_Cin] ON [master].[Companies] ([Cin]) WHERE [Cin] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Companies_LegalName] ON [master].[Companies] ([LegalName]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Companies_SectorId] ON [master].[Companies] ([SectorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Exchanges_Code] ON [master].[Exchanges] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InstrumentAliases_Alias] ON [master].[InstrumentAliases] ([Alias]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InstrumentAliases_InstrumentId_AliasType_Alias_ValidFrom] ON [master].[InstrumentAliases] ([InstrumentId], [AliasType], [Alias], [ValidFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InstrumentAttributeVersions_InstrumentId_ValidFrom] ON [master].[InstrumentAttributeVersions] ([InstrumentId], [ValidFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InstrumentAttributeVersions_MasterVersionId] ON [master].[InstrumentAttributeVersions] ([MasterVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InstrumentAttributeVersions_Symbol] ON [master].[InstrumentAttributeVersions] ([Symbol]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InstrumentAttributeVersions_TradingSymbol] ON [master].[InstrumentAttributeVersions] ([TradingSymbol]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_InstrumentAttributeVersions_Current] ON [master].[InstrumentAttributeVersions] ([InstrumentId]) WHERE [ValidTo] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InstrumentMasterVersions_ProviderId_ImportedAtUtc] ON [master].[InstrumentMasterVersions] ([ProviderId], [ImportedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InstrumentProviderMappings_InstrumentId] ON [master].[InstrumentProviderMappings] ([InstrumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InstrumentProviderMappings_ProviderId_ProviderInstrumentKey_ValidFrom] ON [master].[InstrumentProviderMappings] ([ProviderId], [ProviderInstrumentKey], [ValidFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Instruments_CompanyId] ON [master].[Instruments] ([CompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Instruments_ExchangeId_SegmentId_Isin] ON [master].[Instruments] ([ExchangeId], [SegmentId], [Isin]) WHERE [Isin] IS NOT NULL AND [Type] IN (1,3)');
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Instruments_SegmentId] ON [master].[Instruments] ([SegmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Instruments_UnderlyingInstrumentId_ExpiryDate] ON [master].[Instruments] ([UnderlyingInstrumentId], [ExpiryDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Sectors_ClassificationSource_Code] ON [master].[Sectors] ([ClassificationSource], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Sectors_ParentSectorId] ON [master].[Sectors] ([ParentSectorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Segments_ExchangeId_Code] ON [master].[Segments] ([ExchangeId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TradingCalendarDays_ExchangeId_SegmentId_Date] ON [master].[TradingCalendarDays] ([ExchangeId], [SegmentId], [Date]) WHERE [SegmentId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TradingCalendarDays_MasterVersionId] ON [master].[TradingCalendarDays] ([MasterVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TradingCalendarDays_SegmentId] ON [master].[TradingCalendarDays] ([SegmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TradingSessions_ExchangeId_SegmentId_SessionType_ValidFrom] ON [master].[TradingSessions] ([ExchangeId], [SegmentId], [SessionType], [ValidFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TradingSessions_MasterVersionId] ON [master].[TradingSessions] ([MasterVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TradingSessions_SegmentId] ON [master].[TradingSessions] ([SegmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [master].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041851_InitialCreate'
)
BEGIN
    INSERT INTO [master].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261107041851_InitialCreate', N'10.0.14');
END;

COMMIT;
GO

