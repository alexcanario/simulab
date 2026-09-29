using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixEnumColumnDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "identity",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                comment: "Where the account stands: Pending, Active or Erased.",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "Where the account stands: PendingVerification, Active or Erased.");

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
                oldComment: "What was done, such as RoleCreated, RoleRenamed, RoleDeleted or UserRolesChanged.");

            migrationBuilder.AlterColumn<string>(
                name: "reason",
                schema: "identity",
                table: "account_events",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                comment: "Why it failed, such as WrongPassword or LockedOut. Null when nothing failed.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true,
                oldComment: "Why it failed, such as InvalidCredentials or LockedOut. Null when nothing failed.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "identity",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                comment: "Where the account stands: PendingVerification, Active or Erased.",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "Where the account stands: Pending, Active or Erased.");

            migrationBuilder.AlterColumn<string>(
                name: "action",
                schema: "identity",
                table: "role_changes",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "What was done, such as RoleCreated, RoleRenamed, RoleDeleted or UserRolesChanged.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "What was done: RoleCreated, RoleUpdated, RoleDeleted or UserRolesChanged.");

            migrationBuilder.AlterColumn<string>(
                name: "reason",
                schema: "identity",
                table: "account_events",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                comment: "Why it failed, such as InvalidCredentials or LockedOut. Null when nothing failed.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true,
                oldComment: "Why it failed, such as WrongPassword or LockedOut. Null when nothing failed.");
        }
    }
}
