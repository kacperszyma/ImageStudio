using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Generation.Migrations
{
    /// <inheritdoc />
    public partial class AddPromptAndResultUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserSub",
                schema: "generation",
                table: "generations");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                schema: "generation",
                table: "generations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Prompt",
                schema: "generation",
                table: "generations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResultUrl",
                schema: "generation",
                table: "generations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "generation",
                table: "generations");

            migrationBuilder.DropColumn(
                name: "Prompt",
                schema: "generation",
                table: "generations");

            migrationBuilder.DropColumn(
                name: "ResultUrl",
                schema: "generation",
                table: "generations");

            migrationBuilder.AddColumn<string>(
                name: "UserSub",
                schema: "generation",
                table: "generations",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
