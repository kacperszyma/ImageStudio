using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Generation.Migrations
{
    /// <inheritdoc />
    public partial class InitialGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "generation");

            migrationBuilder.CreateTable(
                name: "generations",
                schema: "generation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserSub = table.Column<string>(type: "text", nullable: false),
                    ImageModel = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "generations",
                schema: "generation");
        }
    }
}
