using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Generation.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCostToGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CreditCost",
                schema: "generation",
                table: "generations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreditCost",
                schema: "generation",
                table: "generations");
        }
    }
}
