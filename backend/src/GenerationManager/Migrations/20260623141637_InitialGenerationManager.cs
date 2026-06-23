using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenerationManager.Migrations
{
    /// <inheritdoc />
    public partial class InitialGenerationManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "generation_manager");

            migrationBuilder.CreateTable(
                name: "generation_jobs",
                schema: "generation_manager",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FalRequestId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_generation_jobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "uq_jobs_fal_request",
                schema: "generation_manager",
                table: "generation_jobs",
                column: "FalRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "generation_jobs",
                schema: "generation_manager");
        }
    }
}
