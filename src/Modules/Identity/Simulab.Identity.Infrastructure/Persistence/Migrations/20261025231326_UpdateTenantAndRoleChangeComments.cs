using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTenantAndRoleChangeComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "role_changes",
                schema: "identity",
                comment: "The trail of what was done to roles and to who holds them: created, renamed, deleted, granted, revoked, and a person's tenant changed. Read by an admin auditing access.",
                oldComment: "The trail of what was done to roles and to who holds them: created, renamed, deleted, granted, revoked. Read by an admin auditing access.");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: true,
                comment: "The partner organization the account belongs to, set by an administrator. Null for none. Carried in the token; nothing is filtered by it yet.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.");

            migrationBuilder.AlterColumn<string>(
                name: "action",
                schema: "identity",
                table: "role_changes",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "What was done: RoleCreated, RoleUpdated, RoleDeleted, UserRolesChanged or UserTenantChanged.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "What was done: RoleCreated, RoleUpdated, RoleDeleted or UserRolesChanged.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "role_changes",
                schema: "identity",
                comment: "The trail of what was done to roles and to who holds them: created, renamed, deleted, granted, revoked. Read by an admin auditing access.",
                oldComment: "The trail of what was done to roles and to who holds them: created, renamed, deleted, granted, revoked, and a person's tenant changed. Read by an admin auditing access.");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: true,
                comment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "The partner organization the account belongs to, set by an administrator. Null for none. Carried in the token; nothing is filtered by it yet.");

            migrationBuilder.AlterColumn<string>(
                name: "action",
                schema: "identity",
                table: "role_changes",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "What was done: RoleCreated, RoleUpdated, RoleDeleted or UserRolesChanged.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "What was done: RoleCreated, RoleUpdated, RoleDeleted, UserRolesChanged or UserTenantChanged.");
        }
    }
}
