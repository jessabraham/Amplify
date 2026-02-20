using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amplify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskAndAIFieldsToTradeSignal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AIBias",
                table: "TradeSignals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AIConfidence",
                table: "TradeSignals",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AIRecommendedAction",
                table: "TradeSignals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AISummary",
                table: "TradeSignals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RiskKellyPercent",
                table: "TradeSignals",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RiskMaxLoss",
                table: "TradeSignals",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RiskPassesCheck",
                table: "TradeSignals",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RiskPortfolioSize",
                table: "TradeSignals",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RiskPositionValue",
                table: "TradeSignals",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RiskRewardRatio",
                table: "TradeSignals",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskShareCount",
                table: "TradeSignals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiskWarnings",
                table: "TradeSignals",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AIBias",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "AIConfidence",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "AIRecommendedAction",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "AISummary",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "RiskKellyPercent",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "RiskMaxLoss",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "RiskPassesCheck",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "RiskPortfolioSize",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "RiskPositionValue",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "RiskRewardRatio",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "RiskShareCount",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "RiskWarnings",
                table: "TradeSignals");
        }
    }
}
