IF OBJECT_ID(N'[workspace].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'workspace') IS NULL EXEC(N'CREATE SCHEMA [workspace];');
    CREATE TABLE [workspace].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [workspace].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041905_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'workspace') IS NULL EXEC(N'CREATE SCHEMA [workspace];');
END;

IF NOT EXISTS (
    SELECT * FROM [workspace].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041905_InitialCreate'
)
BEGIN
    CREATE TABLE [workspace].[UserPreferences] (
        [UserId] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [ExperienceMode] tinyint NOT NULL,
        [UpdatedAtUtc] datetime2(7) NOT NULL,
        CONSTRAINT [PK_UserPreferences] PRIMARY KEY ([UserId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [workspace].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041905_InitialCreate'
)
BEGIN
    CREATE TABLE [workspace].[Watchlists] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL,
        [UpdatedAtUtc] datetime2(7) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Watchlists] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [workspace].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041905_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserPreferences_TenantId] ON [workspace].[UserPreferences] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [workspace].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041905_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Watchlists_TenantId_UserId_Name] ON [workspace].[Watchlists] ([TenantId], [UserId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [workspace].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041905_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Watchlists_UserId_CreatedAtUtc] ON [workspace].[Watchlists] ([UserId], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [workspace].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041905_InitialCreate'
)
BEGIN
    INSERT INTO [workspace].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261107041905_InitialCreate', N'10.0.14');
END;

COMMIT;
GO

