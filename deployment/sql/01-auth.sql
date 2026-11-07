IF OBJECT_ID(N'[auth].[__EFMigrationsHistory]') IS NULL
BEGIN
    IF SCHEMA_ID(N'auth') IS NULL EXEC(N'CREATE SCHEMA [auth];');
    CREATE TABLE [auth].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'auth') IS NULL EXEC(N'CREATE SCHEMA [auth];');
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE TABLE [auth].[AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [OccurredAtUtc] datetime2(7) NOT NULL,
        [ActorUserId] uniqueidentifier NULL,
        [ActorTenantId] uniqueidentifier NULL,
        [Action] nvarchar(100) NOT NULL,
        [TargetType] nvarchar(100) NULL,
        [TargetId] nvarchar(100) NULL,
        [CorrelationId] nvarchar(64) NULL,
        [IpAddress] nvarchar(45) NULL,
        [DetailsJson] nvarchar(4000) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE TABLE [auth].[Plans] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Plans] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE TABLE [auth].[Roles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(256) NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE TABLE [auth].[Tenants] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL,
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE TABLE [auth].[PlanFeatures] (
        [PlanId] uniqueidentifier NOT NULL,
        [FeatureKey] nvarchar(100) NOT NULL,
        [Limit] int NULL,
        CONSTRAINT [PK_PlanFeatures] PRIMARY KEY ([PlanId], [FeatureKey]),
        CONSTRAINT [FK_PlanFeatures_Plans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [auth].[Plans] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE TABLE [auth].[RoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(256) NULL,
        [ClaimValue] nvarchar(256) NULL,
        CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [auth].[Roles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE TABLE [auth].[Subscriptions] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [PlanId] uniqueidentifier NOT NULL,
        [ValidFromUtc] datetime2(7) NOT NULL,
        [ValidToUtc] datetime2(7) NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL,
        CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Subscriptions_ValidRange] CHECK ([ValidToUtc] IS NULL OR [ValidToUtc] > [ValidFromUtc]),
        CONSTRAINT [FK_Subscriptions_Plans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [auth].[Plans] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Subscriptions_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [auth].[Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE TABLE [auth].[Users] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [DisplayName] nvarchar(100) NOT NULL,
        [Status] tinyint NOT NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL,
        [LastLoginAtUtc] datetime2(7) NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(512) NULL,
        [SecurityStamp] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(256) NULL,
        [PhoneNumber] nvarchar(256) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [auth].[Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE TABLE [auth].[RefreshTokens] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [FamilyId] uniqueidentifier NOT NULL,
        [TokenHash] char(64) NOT NULL,
        [CreatedAtUtc] datetime2(7) NOT NULL,
        [ExpiresAtUtc] datetime2(7) NOT NULL,
        [RevokedAtUtc] datetime2(7) NULL,
        [RevokedReason] nvarchar(64) NULL,
        [ReplacedByTokenId] uniqueidentifier NULL,
        [CreatedByIp] nvarchar(45) NULL,
        [UserAgent] nvarchar(256) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [auth].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE TABLE [auth].[UserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [ClaimType] nvarchar(256) NULL,
        [ClaimValue] nvarchar(256) NULL,
        CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [auth].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE TABLE [auth].[UserLogins] (
        [LoginProvider] nvarchar(256) NOT NULL,
        [ProviderKey] nvarchar(256) NOT NULL,
        [ProviderDisplayName] nvarchar(256) NULL,
        [UserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [auth].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE TABLE [auth].[UserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [auth].[Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [auth].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE TABLE [auth].[UserTokens] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginProvider] nvarchar(256) NOT NULL,
        [Name] nvarchar(256) NOT NULL,
        [Value] nvarchar(256) NULL,
        CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [auth].[Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[auth].[Plans]'))
        SET IDENTITY_INSERT [auth].[Plans] ON;
    EXEC(N'INSERT INTO [auth].[Plans] ([Id], [Code], [IsActive], [Name])
    VALUES (''0193a000-0000-7000-8000-000000000001'', N''beta'', CAST(1 AS bit), N''Public Beta'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'IsActive', N'Name') AND [object_id] = OBJECT_ID(N'[auth].[Plans]'))
        SET IDENTITY_INSERT [auth].[Plans] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConcurrencyStamp', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[auth].[Roles]'))
        SET IDENTITY_INSERT [auth].[Roles] ON;
    EXEC(N'INSERT INTO [auth].[Roles] ([Id], [ConcurrencyStamp], [Name], [NormalizedName])
    VALUES (''0193a000-0000-7000-8000-0000000000a1'', N''0193a000-0000-7000-8000-0000000000a1'', N''User'', N''USER''),
    (''0193a000-0000-7000-8000-0000000000a2'', N''0193a000-0000-7000-8000-0000000000a2'', N''Admin'', N''ADMIN''),
    (''0193a000-0000-7000-8000-0000000000a3'', N''0193a000-0000-7000-8000-0000000000a3'', N''Support'', N''SUPPORT''),
    (''0193a000-0000-7000-8000-0000000000a4'', N''0193a000-0000-7000-8000-0000000000a4'', N''DataOperator'', N''DATAOPERATOR''),
    (''0193a000-0000-7000-8000-0000000000a5'', N''0193a000-0000-7000-8000-0000000000a5'', N''ResearchEditor'', N''RESEARCHEDITOR'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConcurrencyStamp', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[auth].[Roles]'))
        SET IDENTITY_INSERT [auth].[Roles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'FeatureKey', N'PlanId', N'Limit') AND [object_id] = OBJECT_ID(N'[auth].[PlanFeatures]'))
        SET IDENTITY_INSERT [auth].[PlanFeatures] ON;
    EXEC(N'INSERT INTO [auth].[PlanFeatures] ([FeatureKey], [PlanId], [Limit])
    VALUES (N''instruments.search'', ''0193a000-0000-7000-8000-000000000001'', NULL),
    (N''workspace.watchlists'', ''0193a000-0000-7000-8000-000000000001'', 10)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'FeatureKey', N'PlanId', N'Limit') AND [object_id] = OBJECT_ID(N'[auth].[PlanFeatures]'))
        SET IDENTITY_INSERT [auth].[PlanFeatures] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_Action] ON [auth].[AuditLogs] ([Action]);
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_ActorUserId_OccurredAtUtc] ON [auth].[AuditLogs] ([ActorUserId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_OccurredAtUtc] ON [auth].[AuditLogs] ([OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Plans_Code] ON [auth].[Plans] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_FamilyId] ON [auth].[RefreshTokens] ([FamilyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [auth].[RefreshTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId_RevokedAtUtc] ON [auth].[RefreshTokens] ([UserId], [RevokedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RoleClaims_RoleId] ON [auth].[RoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [auth].[Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Subscriptions_PlanId] ON [auth].[Subscriptions] ([PlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Subscriptions_TenantId_ValidFromUtc] ON [auth].[Subscriptions] ([TenantId], [ValidFromUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserClaims_UserId] ON [auth].[UserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserLogins_UserId] ON [auth].[UserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [auth].[UserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [auth].[Users] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_TenantId] ON [auth].[Users] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [auth].[Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    CREATE TRIGGER [auth].[TR_AuditLogs_AppendOnly]
    ON [auth].[AuditLogs]
    INSTEAD OF UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, 'auth.AuditLogs is append-only: UPDATE and DELETE are not permitted.', 1;
    END
END;

IF NOT EXISTS (
    SELECT * FROM [auth].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107041743_InitialCreate'
)
BEGIN
    INSERT INTO [auth].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261107041743_InitialCreate', N'10.0.14');
END;

COMMIT;
GO

