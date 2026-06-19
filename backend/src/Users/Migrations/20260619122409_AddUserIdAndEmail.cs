using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Users.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdAndEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                schema: "users",
                table: "users");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                schema: "users",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "users",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                schema: "users",
                table: "users",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_users_Sub",
                schema: "users",
                table: "users",
                column: "Sub",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                schema: "users",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_Sub",
                schema: "users",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: "users",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "users",
                table: "users");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                schema: "users",
                table: "users",
                column: "Sub");
        }
    }
}
