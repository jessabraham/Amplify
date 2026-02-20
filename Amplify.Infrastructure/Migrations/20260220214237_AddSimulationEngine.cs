using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amplify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSimulationEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RiskWarnings",
                table: "TradeSignals",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RiskRewardRatio",
                table: "TradeSignals",
                type: "decimal(10,4)",
                precision: 10,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RiskKellyPercent",
                table: "TradeSignals",
                type: "decimal(10,4)",
                precision: 10,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AISummary",
                table: "TradeSignals",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AIRecommendedAction",
                table: "TradeSignals",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AIConfidence",
                table: "TradeSignals",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AIBias",
                table: "TradeSignals",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "PatternPerformances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PatternType = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Timeframe = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Regime = table.Column<int>(type: "int", nullable: false),
                    TotalTrades = table.Column<int>(type: "int", nullable: false),
                    Wins = table.Column<int>(type: "int", nullable: false),
                    Losses = table.Column<int>(type: "int", nullable: false),
                    Expired = table.Column<int>(type: "int", nullable: false),
                    WinRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AvgWinPercent = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    AvgLossPercent = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    AvgRMultiple = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    BestTradePercent = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    WorstTradePercent = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    TotalPnLPercent = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                    ProfitFactor = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    WinRateWhenAligned = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TradesWhenAligned = table.Column<int>(type: "int", nullable: false),
                    WinRateWhenConflicting = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TradesWhenConflicting = table.Column<int>(type: "int", nullable: false),
                    WinRateWithBreakoutVol = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TradesWithBreakoutVol = table.Column<int>(type: "int", nullable: false),
                    AvgDaysHeld = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    LastTradeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatternPerformances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SimulatedTrades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TradeSignalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetectedPatternId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Asset = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    EntryPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StopLoss = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Target1 = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Target2 = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    RegimeAtEntry = table.Column<int>(type: "int", nullable: false),
                    PatternType = table.Column<int>(type: "int", nullable: true),
                    PatternDirection = table.Column<int>(type: "int", nullable: true),
                    PatternTimeframe = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PatternConfidence = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    AIConfidence = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    AIRecommendedAction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TimeframeAlignment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RegimeAlignment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MAAlignment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VolumeProfile = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RSIAtEntry = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ShareCount = table.Column<int>(type: "int", nullable: true),
                    PositionValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxRisk = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DaysHeld = table.Column<int>(type: "int", nullable: false),
                    MaxExpirationDays = table.Column<int>(type: "int", nullable: false),
                    HighestPriceSeen = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    LowestPriceSeen = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    ExitPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PnLDollars = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PnLPercent = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    RMultiple = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    MaxDrawdownPercent = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulatedTrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulatedTrades_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SimulatedTrades_TradeSignals_TradeSignalId",
                        column: x => x.TradeSignalId,
                        principalTable: "TradeSignals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatternPerformances_UserId_PatternType_Direction_Timeframe_Regime",
                table: "PatternPerformances",
                columns: new[] { "UserId", "PatternType", "Direction", "Timeframe", "Regime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimulatedTrades_TradeSignalId",
                table: "SimulatedTrades",
                column: "TradeSignalId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulatedTrades_UserId_Status",
                table: "SimulatedTrades",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatternPerformances");

            migrationBuilder.DropTable(
                name: "SimulatedTrades");

            migrationBuilder.AlterColumn<string>(
                name: "RiskWarnings",
                table: "TradeSignals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RiskRewardRatio",
                table: "TradeSignals",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,4)",
                oldPrecision: 10,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RiskKellyPercent",
                table: "TradeSignals",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,4)",
                oldPrecision: 10,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AISummary",
                table: "TradeSignals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AIRecommendedAction",
                table: "TradeSignals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AIConfidence",
                table: "TradeSignals",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AIBias",
                table: "TradeSignals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
