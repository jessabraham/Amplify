using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amplify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatternLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentPrice",
                table: "DetectedPatterns",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "DetectedPatterns",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "HighWaterMark",
                table: "DetectedPatterns",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LowWaterMark",
                table: "DetectedPatterns",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PortfolioAdviceId",
                table: "DetectedPatterns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ResolutionPrice",
                table: "DetectedPatterns",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "DetectedPatterns",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPrice",
                table: "DetectedPatterns");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "DetectedPatterns");

            migrationBuilder.DropColumn(
                name: "HighWaterMark",
                table: "DetectedPatterns");

            migrationBuilder.DropColumn(
                name: "LowWaterMark",
                table: "DetectedPatterns");

            migrationBuilder.DropColumn(
                name: "PortfolioAdviceId",
                table: "DetectedPatterns");

            migrationBuilder.DropColumn(
                name: "ResolutionPrice",
                table: "DetectedPatterns");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DetectedPatterns");
        }
    }
}
