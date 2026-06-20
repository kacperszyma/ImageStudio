using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Migrations
{
    /// <inheritdoc />
    public partial class AddFrozenAndBalanceAfter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BalanceAfter",
                schema: "wallet",
                table: "wallet_holds",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "BalanceAfter",
                schema: "wallet",
                table: "ledger_entries",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Frozen",
                schema: "wallet",
                table: "accounts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BalanceAfter",
                schema: "wallet",
                table: "wallet_holds");

            migrationBuilder.DropColumn(
                name: "BalanceAfter",
                schema: "wallet",
                table: "ledger_entries");

            migrationBuilder.DropColumn(
                name: "Frozen",
                schema: "wallet",
                table: "accounts");
        }
    }
}
