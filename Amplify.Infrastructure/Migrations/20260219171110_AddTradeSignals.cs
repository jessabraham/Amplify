using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amplify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeSignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TradeSignals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Asset = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AssetClass = table.Column<int>(type: "int", nullable: false),
                    SignalType = table.Column<int>(type: "int", nullable: false),
                    Regime = table.Column<int>(type: "int", nullable: false),
                    SetupScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EntryPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    StopLoss = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Target1 = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Target2 = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RiskPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    AIAdvisoryJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeSignals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeSignals_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TradeSignals_UserId",
                table: "TradeSignals",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradeSignals");
        }
    }
}
