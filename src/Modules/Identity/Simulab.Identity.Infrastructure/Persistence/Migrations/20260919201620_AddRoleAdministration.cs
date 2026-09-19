using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                schema: "identity",
                table: "roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "identity",
                table: "roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by",
                schema: "identity",
                table: "roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_system",
                schema: "identity",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "identity",
                table: "roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "roles",
                type: "uuid",
                nullable: true);

            // F-9, BR1: the three seed roles that already exist become system roles. The startup seed
            // creates them marked on a new installation (EnsureRolesAndPermissionsAsync).
            migrationBuilder.Sql(
                "UPDATE identity.roles SET is_system = TRUE WHERE normalized_name IN ('STUDENT', 'CURATOR', 'ADMIN');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "identity",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "identity",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                schema: "identity",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "deleted_by",
                schema: "identity",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                schema: "identity",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "is_system",
                schema: "identity",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "identity",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "identity",
                table: "roles");
        }
    }
}
