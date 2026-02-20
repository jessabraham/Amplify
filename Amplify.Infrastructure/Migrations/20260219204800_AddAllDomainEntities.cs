using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amplify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAllDomainEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIAnalytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TradeSignalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnalysisType = table.Column<int>(type: "int", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PromptSent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseReceived = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PromptTokens = table.Column<int>(type: "int", nullable: false),
                    CompletionTokens = table.Column<int>(type: "int", nullable: false),
                    ResponseTimeMs = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIAnalytics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIAnalytics_TradeSignals_TradeSignalId",
                        column: x => x.TradeSignalId,
                        principalTable: "TradeSignals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BacktestResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Asset = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AssetClass = table.Column<int>(type: "int", nullable: false),
                    SignalType = table.Column<int>(type: "int", nullable: false),
                    Regime = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InitialCapital = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalTrades = table.Column<int>(type: "int", nullable: false),
                    WinningTrades = table.Column<int>(type: "int", nullable: false),
                    LosingTrades = table.Column<int>(type: "int", nullable: false),
                    WinRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ProfitFactor = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    MaxDrawdown = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    NetPnL = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SharpeRatio = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    ResultsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BacktestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BacktestResults_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeatureVectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RSI = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MACD = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MACDSignal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BollingerUpper = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BollingerLower = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ATR = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SMA20 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SMA50 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EMA12 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EMA26 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VWAP = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VolumeAvg20 = table.Column<long>(type: "bigint", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureVectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketTicks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssetClass = table.Column<int>(type: "int", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketTicks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TotalValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CashBalance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    InvestedAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DailyPnL = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnrealizedPnL = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RealizedPnL = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OpenPositions = table.Column<int>(type: "int", nullable: false),
                    RiskExposurePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PositionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioSnapshots_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegimeHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Regime = table.Column<int>(type: "int", nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FeatureVectorJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegimeHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TradeSignalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OverrideType = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ModifiedEntryPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ModifiedStopLoss = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ModifiedTarget1 = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ModifiedTarget2 = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ActualPnL = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    WasCorrect = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOverrides_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserOverrides_TradeSignals_TradeSignalId",
                        column: x => x.TradeSignalId,
                        principalTable: "TradeSignals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIAnalytics_TradeSignalId",
                table: "AIAnalytics",
                column: "TradeSignalId");

            migrationBuilder.CreateIndex(
                name: "IX_BacktestResults_UserId",
                table: "BacktestResults",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioSnapshots_UserId",
                table: "PortfolioSnapshots",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOverrides_TradeSignalId",
                table: "UserOverrides",
                column: "TradeSignalId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOverrides_UserId",
                table: "UserOverrides",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIAnalytics");

            migrationBuilder.DropTable(
                name: "BacktestResults");

            migrationBuilder.DropTable(
                name: "FeatureVectors");

            migrationBuilder.DropTable(
                name: "MarketTicks");

            migrationBuilder.DropTable(
                name: "PortfolioSnapshots");

            migrationBuilder.DropTable(
                name: "RegimeHistory");

            migrationBuilder.DropTable(
                name: "UserOverrides");
        }
    }
}
