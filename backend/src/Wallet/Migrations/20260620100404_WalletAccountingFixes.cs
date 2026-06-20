using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Migrations
{
    /// <inheritdoc />
    public partial class WalletAccountingFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BalanceAfter",
                schema: "wallet",
                table: "wallet_holds");

            migrationBuilder.DropColumn(
                name: "RefernceId",
                schema: "wallet",
                table: "ledger_entries");

            // text -> uuid needs an explicit USING cast in Postgres; the table is empty
            // in dev so the cast is trivially safe. The dependent index is rebuilt automatically.
            migrationBuilder.Sql(
                "ALTER TABLE wallet.wallet_holds " +
                "ALTER COLUMN \"PurchaseId\" TYPE uuid USING \"PurchaseId\"::uuid;");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "wallet",
                table: "wallet_holds",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleasedAt",
                schema: "wallet",
                table: "wallet_holds",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "wallet",
                table: "wallet_holds");

            migrationBuilder.DropColumn(
                name: "ReleasedAt",
                schema: "wallet",
                table: "wallet_holds");

            migrationBuilder.AlterColumn<string>(
                name: "PurchaseId",
                schema: "wallet",
                table: "wallet_holds",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<long>(
                name: "BalanceAfter",
                schema: "wallet",
                table: "wallet_holds",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "RefernceId",
                schema: "wallet",
                table: "ledger_entries",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
