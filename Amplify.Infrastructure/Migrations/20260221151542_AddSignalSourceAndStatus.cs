using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amplify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalSourceAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcceptedAt",
                table: "TradeSignals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PatternConfidence",
                table: "TradeSignals",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatternName",
                table: "TradeSignals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatternTimeframe",
                table: "TradeSignals",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "TradeSignals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "TradeSignals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TradeSignals",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedAt",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "PatternConfidence",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "PatternName",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "PatternTimeframe",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "TradeSignals");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TradeSignals");
        }
    }
}
