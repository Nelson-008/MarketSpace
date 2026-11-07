IF OBJECT_ID(N'[ops].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'ops') IS NULL EXEC(N'CREATE SCHEMA [ops];');
    CREATE TABLE [ops].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041920_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'ops') IS NULL EXEC(N'CREATE SCHEMA [ops];');
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041920_InitialCreate'
)
BEGIN
    CREATE TABLE [ops].[Providers] (
        [Id] uniqueidentifier NOT NULL,
        [Code] varchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Kind] tinyint NOT NULL,
        [IsSimulated] bit NOT NULL,
        [IsEnabled] bit NOT NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL,
        CONSTRAINT [PK_Providers] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Providers_MockIsSimulated] CHECK ([Kind] <> 1 OR [IsSimulated] = 1)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041920_InitialCreate'
)
BEGIN
    CREATE TABLE [ops].[ProviderCapabilities] (
        [Id] bigint NOT NULL IDENTITY,
        [ProviderId] uniqueidentifier NOT NULL,
        [Capability] tinyint NOT NULL,
        [DeliveryMode] tinyint NOT NULL,
        [DelaySeconds] int NULL,
        [HistoricalDepthDays] int NULL,
        [Notes] nvarchar(1000) NULL,
        [VerificationSource] nvarchar(500) NULL,
        [VerifiedAtUtc] datetime2(7) NULL,
        CONSTRAINT [PK_ProviderCapabilities] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProviderCapabilities_Delay] CHECK (([DeliveryMode] = 2 AND [DelaySeconds] > 0) OR ([DeliveryMode] <> 2 AND [DelaySeconds] IS NULL)),
        CONSTRAINT [FK_ProviderCapabilities_Providers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [ops].[Providers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041920_InitialCreate'
)
BEGIN
    CREATE TABLE [ops].[ProviderLicenses] (
        [Id] uniqueidentifier NOT NULL,
        [ProviderId] uniqueidentifier NOT NULL,
        [LicenseReference] nvarchar(200) NOT NULL,
        [ValidFrom] date NOT NULL,
        [ValidTo] date NULL,
        [PermitsPublicDisplay] bit NOT NULL,
        [PermitsDerivedAnalytics] bit NOT NULL,
        [PermitsStorage] bit NOT NULL,
        [RetentionDays] int NULL,
        [PermitsExport] bit NOT NULL,
        [PermitsAlerts] bit NOT NULL,
        [PermitsAiReports] bit NOT NULL,
        [MaxUsers] int NULL,
        [DocumentReference] nvarchar(500) NULL,
        [ApprovedByUserId] uniqueidentifier NULL,
        [ApprovedAtUtc] datetime2(7) NULL,
        CONSTRAINT [PK_ProviderLicenses] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_ProviderLicenses_Range] CHECK ([ValidTo] IS NULL OR [ValidTo] > [ValidFrom]),
        CONSTRAINT [FK_ProviderLicenses_Providers_ProviderId] FOREIGN KEY ([ProviderId]) REFERENCES [ops].[Providers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041920_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProviderCapabilities_ProviderId_Capability] ON [ops].[ProviderCapabilities] ([ProviderId], [Capability]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041920_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProviderLicenses_ProviderId_ValidFrom] ON [ops].[ProviderLicenses] ([ProviderId], [ValidFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041920_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Providers_Code] ON [ops].[Providers] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [ops].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041920_InitialCreate'
)
BEGIN
    INSERT INTO [ops].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261107041920_InitialCreate', N'10.0.14');
END;

COMMIT;
GO

