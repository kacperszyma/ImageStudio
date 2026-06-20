using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Migrations
{
    /// <inheritdoc />
    public partial class AddHoldsAndLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                schema: "wallet",
                table: "accounts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ledger_entries",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    RefernceId = table.Column<string>(type: "text", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ledger_entries", x => x.Id);
                    table.CheckConstraint("ck_ledger_amount_non_negative", "\"Amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_ledger_entries_accounts_WalletId",
                        column: x => x.WalletId,
                        principalSchema: "wallet",
                        principalTable: "accounts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wallet_holds",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PurchaseId = table.Column<string>(type: "text", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "text", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_holds", x => x.Id);
                    table.CheckConstraint("ck_holds_amount_non_negative", "\"Amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_wallet_holds_accounts_WalletId",
                        column: x => x.WalletId,
                        principalSchema: "wallet",
                        principalTable: "accounts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_balance_non_negative",
                schema: "wallet",
                table: "accounts",
                sql: "\"Balance\" >= 0");

            migrationBuilder.CreateIndex(
                name: "idx_ledger_wallet",
                schema: "wallet",
                table: "ledger_entries",
                columns: new[] { "WalletId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "uq_ledger_idempotency_key",
                schema: "wallet",
                table: "ledger_entries",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_holds_purchase",
                schema: "wallet",
                table: "wallet_holds",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "idx_holds_wallet",
                schema: "wallet",
                table: "wallet_holds",
                columns: new[] { "WalletId", "Status" });

            migrationBuilder.CreateIndex(
                name: "uq_holds_idempotency_key",
                schema: "wallet",
                table: "wallet_holds",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ledger_entries",
                schema: "wallet");

            migrationBuilder.DropTable(
                name: "wallet_holds",
                schema: "wallet");

            migrationBuilder.DropCheckConstraint(
                name: "ck_balance_non_negative",
                schema: "wallet",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: "wallet",
                table: "accounts");
        }
    }
}
