using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "role_changes",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name_before = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    name_after = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    role_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    added = table.Column<string>(type: "jsonb", nullable: true),
                    removed = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_changes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_role_changes_created_at",
                schema: "identity",
                table: "role_changes",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_role_changes_created_by",
                schema: "identity",
                table: "role_changes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_role_changes_role_ids",
                schema: "identity",
                table: "role_changes",
                column: "role_ids")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_role_changes_target_user_id",
                schema: "identity",
                table: "role_changes",
                column: "target_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_changes",
                schema: "identity");
        }
    }
}
