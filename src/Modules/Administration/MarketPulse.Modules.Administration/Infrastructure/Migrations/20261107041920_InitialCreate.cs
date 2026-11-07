using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketPulse.Modules.Administration.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ops");

            migrationBuilder.CreateTable(
                name: "Providers",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    IsSimulated = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                    table.CheckConstraint("CK_Providers_MockIsSimulated", "[Kind] <> 1 OR [IsSimulated] = 1");
                });

            migrationBuilder.CreateTable(
                name: "ProviderCapabilities",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Capability = table.Column<byte>(type: "tinyint", nullable: false),
                    DeliveryMode = table.Column<byte>(type: "tinyint", nullable: false),
                    DelaySeconds = table.Column<int>(type: "int", nullable: true),
                    HistoricalDepthDays = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VerificationSource = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderCapabilities", x => x.Id);
                    table.CheckConstraint("CK_ProviderCapabilities_Delay", "([DeliveryMode] = 2 AND [DelaySeconds] > 0) OR ([DeliveryMode] <> 2 AND [DelaySeconds] IS NULL)");
                    table.ForeignKey(
                        name: "FK_ProviderCapabilities_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "ops",
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderLicenses",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LicenseReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    PermitsPublicDisplay = table.Column<bool>(type: "bit", nullable: false),
                    PermitsDerivedAnalytics = table.Column<bool>(type: "bit", nullable: false),
                    PermitsStorage = table.Column<bool>(type: "bit", nullable: false),
                    RetentionDays = table.Column<int>(type: "int", nullable: true),
                    PermitsExport = table.Column<bool>(type: "bit", nullable: false),
                    PermitsAlerts = table.Column<bool>(type: "bit", nullable: false),
                    PermitsAiReports = table.Column<bool>(type: "bit", nullable: false),
                    MaxUsers = table.Column<int>(type: "int", nullable: true),
                    DocumentReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderLicenses", x => x.Id);
                    table.CheckConstraint("CK_ProviderLicenses_Range", "[ValidTo] IS NULL OR [ValidTo] > [ValidFrom]");
                    table.ForeignKey(
                        name: "FK_ProviderLicenses_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalSchema: "ops",
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCapabilities_ProviderId_Capability",
                schema: "ops",
                table: "ProviderCapabilities",
                columns: new[] { "ProviderId", "Capability" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderLicenses_ProviderId_ValidFrom",
                schema: "ops",
                table: "ProviderLicenses",
                columns: new[] { "ProviderId", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Code",
                schema: "ops",
                table: "Providers",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderCapabilities",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "ProviderLicenses",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "Providers",
                schema: "ops");
        }
    }
}
