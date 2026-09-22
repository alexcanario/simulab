using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Identity.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// F-18: empty on purpose. This context maps the job table excluded from its migrations (F-13), so the
    /// index change is created by the Jobs migration <c>ActiveJobsIndex</c>; this one only moves the snapshot,
    /// or EF would report pending model changes at startup.
    /// </summary>
    public partial class JobsActiveIndexSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
