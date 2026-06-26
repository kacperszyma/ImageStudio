using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseId",
                schema: "wallet",
                table: "ledger_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "purchases",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageNameId = table.Column<string>(type: "text", nullable: false),
                    DollarAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PebbleAmount = table.Column<long>(type: "bigint", nullable: false),
                    ExternalPaymentId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchases_accounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "wallet",
                        principalTable: "accounts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ledger_entries_PurchaseId",
                schema: "wallet",
                table: "ledger_entries",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "idx_purchases_user",
                schema: "wallet",
                table: "purchases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "uq_purchases_external_payment_id",
                schema: "wallet",
                table: "purchases",
                column: "ExternalPaymentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ledger_entries_purchases_PurchaseId",
                schema: "wallet",
                table: "ledger_entries",
                column: "PurchaseId",
                principalSchema: "wallet",
                principalTable: "purchases",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ledger_entries_purchases_PurchaseId",
                schema: "wallet",
                table: "ledger_entries");

            migrationBuilder.DropTable(
                name: "purchases",
                schema: "wallet");

            migrationBuilder.DropIndex(
                name: "IX_ledger_entries_PurchaseId",
                schema: "wallet",
                table: "ledger_entries");

            migrationBuilder.DropColumn(
                name: "PurchaseId",
                schema: "wallet",
                table: "ledger_entries");
        }
    }
}
