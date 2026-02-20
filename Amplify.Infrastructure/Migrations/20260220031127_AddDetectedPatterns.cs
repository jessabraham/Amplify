using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amplify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDetectedPatterns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DetectedPatterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Asset = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PatternType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Timeframe = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    HistoricalWinRate = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DetectedAtPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SuggestedEntry = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SuggestedStop = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SuggestedTarget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PatternStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PatternEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AIAnalysis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AIConfidence = table.Column<decimal>(type: "decimal(5,1)", nullable: true),
                    WasCorrect = table.Column<bool>(type: "bit", nullable: true),
                    ActualPnLPercent = table.Column<decimal>(type: "decimal(8,2)", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GeneratedSignalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmailSent = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectedPatterns", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetectedPatterns");
        }
    }
}
