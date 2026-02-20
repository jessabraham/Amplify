using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amplify.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AssetClass = table.Column<int>(type: "int", nullable: false),
                    SignalType = table.Column<int>(type: "int", nullable: false),
                    EntryPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    EntryDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ExitDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StopLoss = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Target1 = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Target2 = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnrealizedPnL = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RealizedPnL = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReturnPercent = table.Column<decimal>(type: "decimal(8,4)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TradeSignalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Positions_TradeSignals_TradeSignalId",
                        column: x => x.TradeSignalId,
                        principalTable: "TradeSignals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Positions_Symbol",
                table: "Positions",
                column: "Symbol");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_TradeSignalId",
                table: "Positions",
                column: "TradeSignalId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_UserId_Status",
                table: "Positions",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Positions");
        }
    }
}
