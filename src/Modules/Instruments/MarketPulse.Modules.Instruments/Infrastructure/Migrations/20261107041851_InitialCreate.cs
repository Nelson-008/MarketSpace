using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketPulse.Modules.Instruments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "master");

            migrationBuilder.CreateTable(
                name: "Exchanges",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TimeZoneId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CountryCode = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    IsSimulated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exchanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstrumentMasterVersions",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImportedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    RecordCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ValidationSummaryJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsSimulated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstrumentMasterVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sectors",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentSectorId = table.Column<int>(type: "int", nullable: true),
                    ClassificationSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sectors_Sectors_ParentSectorId",
                        column: x => x.ParentSectorId,
                        principalSchema: "master",
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Segments",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExchangeId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Segments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Segments_Exchanges_ExchangeId",
                        column: x => x.ExchangeId,
                        principalSchema: "master",
                        principalTable: "Exchanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SectorId = table.Column<int>(type: "int", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Cin = table.Column<string>(type: "varchar(21)", unicode: false, maxLength: 21, nullable: true),
                    IsSimulated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Companies_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalSchema: "master",
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TradingCalendarDays",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExchangeId = table.Column<int>(type: "int", nullable: false),
                    SegmentId = table.Column<int>(type: "int", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    DayType = table.Column<byte>(type: "tinyint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MasterVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingCalendarDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradingCalendarDays_Exchanges_ExchangeId",
                        column: x => x.ExchangeId,
                        principalSchema: "master",
                        principalTable: "Exchanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradingCalendarDays_InstrumentMasterVersions_MasterVersionId",
                        column: x => x.MasterVersionId,
                        principalSchema: "master",
                        principalTable: "InstrumentMasterVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradingCalendarDays_Segments_SegmentId",
                        column: x => x.SegmentId,
                        principalSchema: "master",
                        principalTable: "Segments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TradingSessions",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExchangeId = table.Column<int>(type: "int", nullable: false),
                    SegmentId = table.Column<int>(type: "int", nullable: false),
                    SessionType = table.Column<byte>(type: "tinyint", nullable: false),
                    StartLocal = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndLocal = table.Column<TimeOnly>(type: "time", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    SpecificDate = table.Column<DateOnly>(type: "date", nullable: true),
                    MasterVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradingSessions", x => x.Id);
                    table.CheckConstraint("CK_TradingSessions_Range", "[ValidTo] IS NULL OR [ValidTo] > [ValidFrom]");
                    table.CheckConstraint("CK_TradingSessions_Times", "[EndLocal] > [StartLocal]");
                    table.ForeignKey(
                        name: "FK_TradingSessions_Exchanges_ExchangeId",
                        column: x => x.ExchangeId,
                        principalSchema: "master",
                        principalTable: "Exchanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradingSessions_InstrumentMasterVersions_MasterVersionId",
                        column: x => x.MasterVersionId,
                        principalSchema: "master",
                        principalTable: "InstrumentMasterVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradingSessions_Segments_SegmentId",
                        column: x => x.SegmentId,
                        principalSchema: "master",
                        principalTable: "Segments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Instruments",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExchangeId = table.Column<int>(type: "int", nullable: false),
                    SegmentId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    Isin = table.Column<string>(type: "char(12)", unicode: false, fixedLength: true, maxLength: 12, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnderlyingInstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StrikePrice = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    OptionType = table.Column<byte>(type: "tinyint", nullable: true),
                    Currency = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    IsSimulated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instruments", x => x.Id);
                    table.CheckConstraint("CK_Instruments_FutureTerms", "[Type] <> 4 OR [ExpiryDate] IS NOT NULL");
                    table.CheckConstraint("CK_Instruments_OptionTerms", "([Type] = 5 AND [OptionType] IS NOT NULL AND [StrikePrice] IS NOT NULL AND [ExpiryDate] IS NOT NULL) OR ([Type] <> 5 AND [OptionType] IS NULL AND [StrikePrice] IS NULL)");
                    table.CheckConstraint("CK_Instruments_Strike", "[StrikePrice] IS NULL OR [StrikePrice] > 0");
                    table.ForeignKey(
                        name: "FK_Instruments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "master",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Instruments_Exchanges_ExchangeId",
                        column: x => x.ExchangeId,
                        principalSchema: "master",
                        principalTable: "Exchanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Instruments_Instruments_UnderlyingInstrumentId",
                        column: x => x.UnderlyingInstrumentId,
                        principalSchema: "master",
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Instruments_Segments_SegmentId",
                        column: x => x.SegmentId,
                        principalSchema: "master",
                        principalTable: "Segments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstrumentAliases",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AliasType = table.Column<byte>(type: "tinyint", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstrumentAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstrumentAliases_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalSchema: "master",
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstrumentAttributeVersions",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Symbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TradingSymbol = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Series = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    LotSize = table.Column<int>(type: "int", nullable: true),
                    TickSize = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    ListingStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    MasterVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstrumentAttributeVersions", x => x.Id);
                    table.CheckConstraint("CK_InstrumentAttributeVersions_Lot", "[LotSize] IS NULL OR [LotSize] > 0");
                    table.CheckConstraint("CK_InstrumentAttributeVersions_Range", "[ValidTo] IS NULL OR [ValidTo] > [ValidFrom]");
                    table.CheckConstraint("CK_InstrumentAttributeVersions_Tick", "[TickSize] IS NULL OR [TickSize] > 0");
                    table.ForeignKey(
                        name: "FK_InstrumentAttributeVersions_InstrumentMasterVersions_MasterVersionId",
                        column: x => x.MasterVersionId,
                        principalSchema: "master",
                        principalTable: "InstrumentMasterVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstrumentAttributeVersions_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalSchema: "master",
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstrumentProviderMappings",
                schema: "master",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderInstrumentKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstrumentProviderMappings", x => x.Id);
                    table.CheckConstraint("CK_InstrumentProviderMappings_Range", "[ValidTo] IS NULL OR [ValidTo] > [ValidFrom]");
                    table.ForeignKey(
                        name: "FK_InstrumentProviderMappings_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalSchema: "master",
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Cin",
                schema: "master",
                table: "Companies",
                column: "Cin",
                unique: true,
                filter: "[Cin] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_LegalName",
                schema: "master",
                table: "Companies",
                column: "LegalName");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_SectorId",
                schema: "master",
                table: "Companies",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Exchanges_Code",
                schema: "master",
                table: "Exchanges",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentAliases_Alias",
                schema: "master",
                table: "InstrumentAliases",
                column: "Alias");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentAliases_InstrumentId_AliasType_Alias_ValidFrom",
                schema: "master",
                table: "InstrumentAliases",
                columns: new[] { "InstrumentId", "AliasType", "Alias", "ValidFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentAttributeVersions_InstrumentId_ValidFrom",
                schema: "master",
                table: "InstrumentAttributeVersions",
                columns: new[] { "InstrumentId", "ValidFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentAttributeVersions_MasterVersionId",
                schema: "master",
                table: "InstrumentAttributeVersions",
                column: "MasterVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentAttributeVersions_Symbol",
                schema: "master",
                table: "InstrumentAttributeVersions",
                column: "Symbol");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentAttributeVersions_TradingSymbol",
                schema: "master",
                table: "InstrumentAttributeVersions",
                column: "TradingSymbol");

            migrationBuilder.CreateIndex(
                name: "UX_InstrumentAttributeVersions_Current",
                schema: "master",
                table: "InstrumentAttributeVersions",
                column: "InstrumentId",
                unique: true,
                filter: "[ValidTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentMasterVersions_ProviderId_ImportedAtUtc",
                schema: "master",
                table: "InstrumentMasterVersions",
                columns: new[] { "ProviderId", "ImportedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentProviderMappings_InstrumentId",
                schema: "master",
                table: "InstrumentProviderMappings",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentProviderMappings_ProviderId_ProviderInstrumentKey_ValidFrom",
                schema: "master",
                table: "InstrumentProviderMappings",
                columns: new[] { "ProviderId", "ProviderInstrumentKey", "ValidFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_CompanyId",
                schema: "master",
                table: "Instruments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_ExchangeId_SegmentId_Isin",
                schema: "master",
                table: "Instruments",
                columns: new[] { "ExchangeId", "SegmentId", "Isin" },
                unique: true,
                filter: "[Isin] IS NOT NULL AND [Type] IN (1,3)");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_SegmentId",
                schema: "master",
                table: "Instruments",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_UnderlyingInstrumentId_ExpiryDate",
                schema: "master",
                table: "Instruments",
                columns: new[] { "UnderlyingInstrumentId", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_ClassificationSource_Code",
                schema: "master",
                table: "Sectors",
                columns: new[] { "ClassificationSource", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_ParentSectorId",
                schema: "master",
                table: "Sectors",
                column: "ParentSectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Segments_ExchangeId_Code",
                schema: "master",
                table: "Segments",
                columns: new[] { "ExchangeId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradingCalendarDays_ExchangeId_SegmentId_Date",
                schema: "master",
                table: "TradingCalendarDays",
                columns: new[] { "ExchangeId", "SegmentId", "Date" },
                unique: true,
                filter: "[SegmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TradingCalendarDays_MasterVersionId",
                schema: "master",
                table: "TradingCalendarDays",
                column: "MasterVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingCalendarDays_SegmentId",
                schema: "master",
                table: "TradingCalendarDays",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingSessions_ExchangeId_SegmentId_SessionType_ValidFrom",
                schema: "master",
                table: "TradingSessions",
                columns: new[] { "ExchangeId", "SegmentId", "SessionType", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_TradingSessions_MasterVersionId",
                schema: "master",
                table: "TradingSessions",
                column: "MasterVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TradingSessions_SegmentId",
                schema: "master",
                table: "TradingSessions",
                column: "SegmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstrumentAliases",
                schema: "master");

            migrationBuilder.DropTable(
                name: "InstrumentAttributeVersions",
                schema: "master");

            migrationBuilder.DropTable(
                name: "InstrumentProviderMappings",
                schema: "master");

            migrationBuilder.DropTable(
                name: "TradingCalendarDays",
                schema: "master");

            migrationBuilder.DropTable(
                name: "TradingSessions",
                schema: "master");

            migrationBuilder.DropTable(
                name: "Instruments",
                schema: "master");

            migrationBuilder.DropTable(
                name: "InstrumentMasterVersions",
                schema: "master");

            migrationBuilder.DropTable(
                name: "Companies",
                schema: "master");

            migrationBuilder.DropTable(
                name: "Segments",
                schema: "master");

            migrationBuilder.DropTable(
                name: "Sectors",
                schema: "master");

            migrationBuilder.DropTable(
                name: "Exchanges",
                schema: "master");
        }
    }
}
