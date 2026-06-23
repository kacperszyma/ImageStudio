using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Generation.Migrations
{
    /// <inheritdoc />
    public partial class NullableResultAndFalRequestId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ResultUrl",
                schema: "generation",
                table: "generations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "FalRequestId",
                schema: "generation",
                table: "generations",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_generations_fal_request",
                schema: "generation",
                table: "generations",
                column: "FalRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_generations_fal_request",
                schema: "generation",
                table: "generations");

            migrationBuilder.DropColumn(
                name: "FalRequestId",
                schema: "generation",
                table: "generations");

            migrationBuilder.AlterColumn<string>(
                name: "ResultUrl",
                schema: "generation",
                table: "generations",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
