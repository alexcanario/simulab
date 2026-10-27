using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.ExamEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttemptsStudentIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_attempts_student",
                schema: "exam_engine",
                table: "attempts",
                column: "student_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_attempts_student",
                schema: "exam_engine",
                table: "attempts");
        }
    }
}
