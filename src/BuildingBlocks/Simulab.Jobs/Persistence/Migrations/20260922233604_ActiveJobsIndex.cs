using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Jobs.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ActiveJobsIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_jobs_status_run_after_created_at",
                schema: "jobs",
                table: "jobs");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_active_created_at",
                schema: "jobs",
                table: "jobs",
                column: "created_at",
                filter: "status IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_jobs_active_created_at",
                schema: "jobs",
                table: "jobs");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_status_run_after_created_at",
                schema: "jobs",
                table: "jobs",
                columns: new[] { "status", "run_after", "created_at" });
        }
    }
}
