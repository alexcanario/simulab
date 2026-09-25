using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "users",
                schema: "identity",
                comment: "A person with an account. Erasing an account anonymizes this row rather than removing it, so their attempts stay countable without naming anyone.");

            migrationBuilder.AlterTable(
                name: "roles",
                schema: "identity",
                comment: "A named set of permissions given to people, such as Student, Curator or Admin. A deleted role keeps its name taken.");

            migrationBuilder.AlterTable(
                name: "role_permissions",
                schema: "identity",
                comment: "Which permissions a role has. A row is the grant itself; removing it takes the permission away.");

            migrationBuilder.AlterTable(
                name: "role_changes",
                schema: "identity",
                comment: "The trail of what was done to roles and to who holds them: created, renamed, deleted, granted, revoked. Read by an admin auditing access.");

            migrationBuilder.AlterTable(
                name: "permissions",
                schema: "identity",
                comment: "Everything a role can be allowed to do. Each module declares its own names and Identity seeds the union of them.");

            migrationBuilder.AlterTable(
                name: "password_reset_tokens",
                schema: "identity",
                comment: "The single-use link that lets someone set a new password without knowing the old one. It is spent on the first success and never replayed.");

            migrationBuilder.AlterTable(
                name: "email_verification_tokens",
                schema: "identity",
                comment: "The single-use link that proves an address belongs to the person who signed up. It is spent on the first success and never replayed.");

            migrationBuilder.AlterTable(
                name: "consent_records",
                schema: "identity",
                comment: "Evidence that a person accepted the terms and the privacy policy, with the versions they saw. Legal evidence: erasing an account never takes it along.");

            migrationBuilder.AlterTable(
                name: "account_events",
                schema: "identity",
                comment: "What happened to an account: sign-ins, password changes, two-factor enrolments. A person reads their own recent activity from it, and it is kept when the account is erased, without naming anyone.");

            migrationBuilder.AlterColumn<string>(
                name: "user_name",
                schema: "identity",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true,
                comment: "The sign-in name. Simulab signs in by email, so it holds the same address.",
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: true,
                comment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was last changed, in UTC. Null while it has never been changed.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "two_factor_enabled",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                comment: "True when the person finished enrolling in two-factor sign-in.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "totp_secret_encrypted",
                schema: "identity",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                comment: "The two-factor secret, encrypted with the app's key. Losing that key makes every enrolment unusable.",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "totp_last_accepted_step",
                schema: "identity",
                table: "users",
                type: "bigint",
                nullable: true,
                comment: "The last time step accepted by two-factor. A code from that step or earlier is refused, so one code cannot be used twice.",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "totp_enabled_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When two-factor enrolment finished, in UTC. Null while it is off.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: true,
                comment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

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
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "security_stamp",
                schema: "identity",
                table: "users",
                type: "text",
                nullable: true,
                comment: "Changes whenever the credentials change. Every open session that does not carry the current value is rejected, which is how a password change signs other devices out.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "preferred_language",
                schema: "identity",
                table: "users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                comment: "The language the app and its emails use for this person, such as pt-BR. It is the first source of the culture, before the cookie and the browser.",
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<bool>(
                name: "phone_number_confirmed",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                comment: "Not used by Simulab, like the phone number itself.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "phone_number",
                schema: "identity",
                table: "users",
                type: "text",
                nullable: true,
                comment: "Not used by Simulab: no feature collects a phone number. The column belongs to ASP.NET Identity.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                schema: "identity",
                table: "users",
                type: "text",
                nullable: true,
                comment: "The password, hashed by ASP.NET Identity. The password itself is never stored and cannot be recovered from this.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_user_name",
                schema: "identity",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true,
                comment: "The sign-in name upper-cased, which is what the unique index compares.",
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_email",
                schema: "identity",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                comment: "The address upper-cased, which is what the unique index compares: one account per address.",
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "lockout_end",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                comment: "The account refuses sign-in until this instant, after too many failed attempts. Null when it is not locked.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "lockout_enabled",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                comment: "True when this account can be locked out after failed sign-ins. True for everyone.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                comment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "is_adult_declared",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                comment: "The person declared at sign-up that they are 18 or older. Simulab takes the declaration; it does not check an age.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                schema: "identity",
                table: "users",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                comment: "The name the person gave, shown in the app. Cleared when the account is erased.",
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "email_verified_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the person followed the verification link, in UTC. Null while the address is unverified.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "email_confirmed",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                comment: "True once the person followed the verification link. Sign-in is refused until then.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "identity",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                comment: "The address the person signs in with and receives mail at.",
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254);

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: true,
                comment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was deleted, in UTC. Null while it is not deleted.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: true,
                comment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "concurrency_stamp",
                schema: "identity",
                table: "users",
                type: "text",
                nullable: true,
                comment: "Changes on every save, so two people editing the same account at once cannot overwrite each other silently.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "access_failed_count",
                schema: "identity",
                table: "users",
                type: "integer",
                nullable: false,
                comment: "Failed sign-in attempts since the last success. It reaches the limit and locks the account, and a success clears it.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: false,
                comment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "roles",
                type: "uuid",
                nullable: true,
                comment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "identity",
                table: "roles",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was last changed, in UTC. Null while it has never been changed.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_name",
                schema: "identity",
                table: "roles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                comment: "The name upper-cased, which is what the unique index compares.",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "identity",
                table: "roles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                comment: "The role's name, as it is shown and as permissions are granted to it.",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_system",
                schema: "identity",
                table: "roles",
                type: "boolean",
                nullable: false,
                comment: "True for a role Simulab seeds and depends on, such as Admin: it cannot be renamed or deleted.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "roles",
                type: "boolean",
                nullable: false,
                comment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "identity",
                table: "roles",
                type: "uuid",
                nullable: true,
                comment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "identity",
                table: "roles",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was deleted, in UTC. Null while it is not deleted.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "roles",
                type: "uuid",
                nullable: true,
                comment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "identity",
                table: "roles",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "concurrency_stamp",
                schema: "identity",
                table: "roles",
                type: "text",
                nullable: true,
                comment: "Changes on every save, so two admins editing the same role at once cannot overwrite each other silently.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "identity",
                table: "roles",
                type: "uuid",
                nullable: false,
                comment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "permission_name",
                schema: "identity",
                table: "role_permissions",
                type: "character varying(100)",
                nullable: false,
                comment: "The permission that is granted.",
                oldClrType: typeof(string),
                oldType: "character varying(100)");

            migrationBuilder.AlterColumn<Guid>(
                name: "role_id",
                schema: "identity",
                table: "role_permissions",
                type: "uuid",
                nullable: false,
                comment: "The role the permission is granted to.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "role_changes",
                type: "uuid",
                nullable: true,
                comment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "identity",
                table: "role_changes",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was last changed, in UTC. Null while it has never been changed.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "role_changes",
                type: "uuid",
                nullable: true,
                comment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "target_user_id",
                schema: "identity",
                table: "role_changes",
                type: "uuid",
                nullable: true,
                comment: "Whose roles changed. Null when the change was about the role itself.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "role_name",
                schema: "identity",
                table: "role_changes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                comment: "The role's name when the change happened, kept so the trail still reads after the role is deleted.",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<List<Guid>>(
                name: "role_ids",
                schema: "identity",
                table: "role_changes",
                type: "uuid[]",
                nullable: false,
                comment: "Every role the change touched, so a search by role finds it whichever action it was.",
                oldClrType: typeof(List<Guid>),
                oldType: "uuid[]");

            migrationBuilder.AlterColumn<Guid>(
                name: "role_id",
                schema: "identity",
                table: "role_changes",
                type: "uuid",
                nullable: true,
                comment: "The role that changed. Null when the change was about a person's roles rather than one role.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name_before",
                schema: "identity",
                table: "role_changes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                comment: "The name before a rename. Null for any other action.",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name_after",
                schema: "identity",
                table: "role_changes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                comment: "The name after a rename. Null for any other action.",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "role_changes",
                type: "boolean",
                nullable: false,
                comment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "identity",
                table: "role_changes",
                type: "uuid",
                nullable: true,
                comment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "identity",
                table: "role_changes",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was deleted, in UTC. Null while it is not deleted.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "role_changes",
                type: "uuid",
                nullable: true,
                comment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "identity",
                table: "role_changes",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

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
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "identity",
                table: "role_changes",
                type: "uuid",
                nullable: false,
                comment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "identity",
                table: "permissions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "What the permission allows, shown to an admin choosing permissions for a role.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "identity",
                table: "permissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "The permission name and the key of the row, such as catalog.manage. The part before the first dot is the module that declares it.",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                schema: "identity",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: false,
                comment: "Whose account the link belongs to.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: true,
                comment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "identity",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was last changed, in UTC. Null while it has never been changed.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "token_hash",
                schema: "identity",
                table: "password_reset_tokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                comment: "The hash of the token that travelled in the link. The token itself is never stored, so a copy of this table does not let anyone in.",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: true,
                comment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "password_reset_tokens",
                type: "boolean",
                nullable: false,
                comment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "expires_at",
                schema: "identity",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When the link stops working, in UTC.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "identity",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: true,
                comment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "identity",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was deleted, in UTC. Null while it is not deleted.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: true,
                comment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "identity",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "consumed_at",
                schema: "identity",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the link was spent, in UTC. Null while it is still usable; set once, so it cannot be used twice.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "identity",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: false,
                comment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                schema: "identity",
                table: "email_verification_tokens",
                type: "uuid",
                nullable: false,
                comment: "Whose account the link belongs to.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "email_verification_tokens",
                type: "uuid",
                nullable: true,
                comment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "identity",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was last changed, in UTC. Null while it has never been changed.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "token_hash",
                schema: "identity",
                table: "email_verification_tokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                comment: "The hash of the token that travelled in the link. The token itself is never stored, so a copy of this table does not let anyone in.",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "email_verification_tokens",
                type: "uuid",
                nullable: true,
                comment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "email_verification_tokens",
                type: "boolean",
                nullable: false,
                comment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "expires_at",
                schema: "identity",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When the link stops working, in UTC.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "identity",
                table: "email_verification_tokens",
                type: "uuid",
                nullable: true,
                comment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "identity",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was deleted, in UTC. Null while it is not deleted.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "email_verification_tokens",
                type: "uuid",
                nullable: true,
                comment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "identity",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "consumed_at",
                schema: "identity",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the link was spent, in UTC. Null while it is still usable; set once, so it cannot be used twice.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "identity",
                table: "email_verification_tokens",
                type: "uuid",
                nullable: false,
                comment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                schema: "identity",
                table: "consent_records",
                type: "uuid",
                nullable: false,
                comment: "Who accepted.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "consent_records",
                type: "uuid",
                nullable: true,
                comment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "identity",
                table: "consent_records",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was last changed, in UTC. Null while it has never been changed.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "terms_version",
                schema: "identity",
                table: "consent_records",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "The version of the terms that was shown and accepted.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "consent_records",
                type: "uuid",
                nullable: true,
                comment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "privacy_version",
                schema: "identity",
                table: "consent_records",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "The version of the privacy policy that was shown and accepted.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "locale",
                schema: "identity",
                table: "consent_records",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                comment: "The language the documents were shown in, such as pt-BR: it is what they actually read.",
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "consent_records",
                type: "boolean",
                nullable: false,
                comment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                schema: "identity",
                table: "consent_records",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                comment: "The address the acceptance came from, long enough for IPv6. Null when it could not be read.",
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "identity",
                table: "consent_records",
                type: "uuid",
                nullable: true,
                comment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "identity",
                table: "consent_records",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was deleted, in UTC. Null while it is not deleted.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "declares_adult",
                schema: "identity",
                table: "consent_records",
                type: "boolean",
                nullable: false,
                comment: "The person declared they are 18 or older when accepting.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "consent_records",
                type: "uuid",
                nullable: true,
                comment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "identity",
                table: "consent_records",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "accepted_at",
                schema: "identity",
                table: "consent_records",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When they accepted, in UTC.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "identity",
                table: "consent_records",
                type: "uuid",
                nullable: false,
                comment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                schema: "identity",
                table: "account_events",
                type: "uuid",
                nullable: true,
                comment: "Whose account it happened to. Null when the attempt named no existing account.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "account_events",
                type: "uuid",
                nullable: true,
                comment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "identity",
                table: "account_events",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was last changed, in UTC. Null while it has never been changed.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "identity",
                table: "account_events",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "What happened, such as SignInSucceeded, PasswordChanged or TwoFactorEnabled.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "account_events",
                type: "uuid",
                nullable: true,
                comment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

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
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "method",
                schema: "identity",
                table: "account_events",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                comment: "How it was done, such as Password or Google. Null when the event has no method.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "account_events",
                type: "boolean",
                nullable: false,
                comment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                schema: "identity",
                table: "account_events",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                comment: "The address the request came from, long enough for IPv6. Null when it could not be read.",
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "identity",
                table: "account_events",
                type: "uuid",
                nullable: true,
                comment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "identity",
                table: "account_events",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was deleted, in UTC. Null while it is not deleted.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "account_events",
                type: "uuid",
                nullable: true,
                comment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "identity",
                table: "account_events",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "identity",
                table: "account_events",
                type: "uuid",
                nullable: false,
                comment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.",
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "users",
                schema: "identity",
                oldComment: "A person with an account. Erasing an account anonymizes this row rather than removing it, so their attempts stay countable without naming anyone.");

            migrationBuilder.AlterTable(
                name: "roles",
                schema: "identity",
                oldComment: "A named set of permissions given to people, such as Student, Curator or Admin. A deleted role keeps its name taken.");

            migrationBuilder.AlterTable(
                name: "role_permissions",
                schema: "identity",
                oldComment: "Which permissions a role has. A row is the grant itself; removing it takes the permission away.");

            migrationBuilder.AlterTable(
                name: "role_changes",
                schema: "identity",
                oldComment: "The trail of what was done to roles and to who holds them: created, renamed, deleted, granted, revoked. Read by an admin auditing access.");

            migrationBuilder.AlterTable(
                name: "permissions",
                schema: "identity",
                oldComment: "Everything a role can be allowed to do. Each module declares its own names and Identity seeds the union of them.");

            migrationBuilder.AlterTable(
                name: "password_reset_tokens",
                schema: "identity",
                oldComment: "The single-use link that lets someone set a new password without knowing the old one. It is spent on the first success and never replayed.");

            migrationBuilder.AlterTable(
                name: "email_verification_tokens",
                schema: "identity",
                oldComment: "The single-use link that proves an address belongs to the person who signed up. It is spent on the first success and never replayed.");

            migrationBuilder.AlterTable(
                name: "consent_records",
                schema: "identity",
                oldComment: "Evidence that a person accepted the terms and the privacy policy, with the versions they saw. Legal evidence: erasing an account never takes it along.");

            migrationBuilder.AlterTable(
                name: "account_events",
                schema: "identity",
                oldComment: "What happened to an account: sign-ins, password changes, two-factor enrolments. A person reads their own recent activity from it, and it is kept when the account is erased, without naming anyone.");

            migrationBuilder.AlterColumn<string>(
                name: "user_name",
                schema: "identity",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254,
                oldNullable: true,
                oldComment: "The sign-in name. Simulab signs in by email, so it holds the same address.");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was last changed, in UTC. Null while it has never been changed.");

            migrationBuilder.AlterColumn<bool>(
                name: "two_factor_enabled",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "True when the person finished enrolling in two-factor sign-in.");

            migrationBuilder.AlterColumn<string>(
                name: "totp_secret_encrypted",
                schema: "identity",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "The two-factor secret, encrypted with the app's key. Losing that key makes every enrolment unusable.");

            migrationBuilder.AlterColumn<long>(
                name: "totp_last_accepted_step",
                schema: "identity",
                table: "users",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true,
                oldComment: "The last time step accepted by two-factor. A code from that step or earlier is refused, so one code cannot be used twice.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "totp_enabled_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When two-factor enrolment finished, in UTC. Null while it is off.");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "identity",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "Where the account stands: PendingVerification, Active or Erased.");

            migrationBuilder.AlterColumn<string>(
                name: "security_stamp",
                schema: "identity",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Changes whenever the credentials change. Every open session that does not carry the current value is rejected, which is how a password change signs other devices out.");

            migrationBuilder.AlterColumn<string>(
                name: "preferred_language",
                schema: "identity",
                table: "users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldComment: "The language the app and its emails use for this person, such as pt-BR. It is the first source of the culture, before the cookie and the browser.");

            migrationBuilder.AlterColumn<bool>(
                name: "phone_number_confirmed",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "Not used by Simulab, like the phone number itself.");

            migrationBuilder.AlterColumn<string>(
                name: "phone_number",
                schema: "identity",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Not used by Simulab: no feature collects a phone number. The column belongs to ASP.NET Identity.");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                schema: "identity",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "The password, hashed by ASP.NET Identity. The password itself is never stored and cannot be recovered from this.");

            migrationBuilder.AlterColumn<string>(
                name: "normalized_user_name",
                schema: "identity",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254,
                oldNullable: true,
                oldComment: "The sign-in name upper-cased, which is what the unique index compares.");

            migrationBuilder.AlterColumn<string>(
                name: "normalized_email",
                schema: "identity",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254,
                oldComment: "The address upper-cased, which is what the unique index compares: one account per address.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "lockout_end",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "The account refuses sign-in until this instant, after too many failed attempts. Null when it is not locked.");

            migrationBuilder.AlterColumn<bool>(
                name: "lockout_enabled",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "True when this account can be locked out after failed sign-ins. True for everyone.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_adult_declared",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "The person declared at sign-up that they are 18 or older. Simulab takes the declaration; it does not check an age.");

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                schema: "identity",
                table: "users",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true,
                oldComment: "The name the person gave, shown in the app. Cleared when the account is erased.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "email_verified_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the person followed the verification link, in UTC. Null while the address is unverified.");

            migrationBuilder.AlterColumn<bool>(
                name: "email_confirmed",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "True once the person followed the verification link. Sign-in is refused until then.");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "identity",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254,
                oldComment: "The address the person signs in with and receives mail at.");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was deleted, in UTC. Null while it is not deleted.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.");

            migrationBuilder.AlterColumn<string>(
                name: "concurrency_stamp",
                schema: "identity",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Changes on every save, so two people editing the same account at once cannot overwrite each other silently.");

            migrationBuilder.AlterColumn<int>(
                name: "access_failed_count",
                schema: "identity",
                table: "users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Failed sign-in attempts since the last success. It reaches the limit and locks the account, and a success clears it.");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "roles",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "identity",
                table: "roles",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was last changed, in UTC. Null while it has never been changed.");

            migrationBuilder.AlterColumn<string>(
                name: "normalized_name",
                schema: "identity",
                table: "roles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "The name upper-cased, which is what the unique index compares.");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "identity",
                table: "roles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "The role's name, as it is shown and as permissions are granted to it.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_system",
                schema: "identity",
                table: "roles",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "True for a role Simulab seeds and depends on, such as Admin: it cannot be renamed or deleted.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "roles",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "identity",
                table: "roles",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "identity",
                table: "roles",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was deleted, in UTC. Null while it is not deleted.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "roles",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "identity",
                table: "roles",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.");

            migrationBuilder.AlterColumn<string>(
                name: "concurrency_stamp",
                schema: "identity",
                table: "roles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Changes on every save, so two admins editing the same role at once cannot overwrite each other silently.");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "identity",
                table: "roles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.");

            migrationBuilder.AlterColumn<string>(
                name: "permission_name",
                schema: "identity",
                table: "role_permissions",
                type: "character varying(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldComment: "The permission that is granted.");

            migrationBuilder.AlterColumn<Guid>(
                name: "role_id",
                schema: "identity",
                table: "role_permissions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "The role the permission is granted to.");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "role_changes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "identity",
                table: "role_changes",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was last changed, in UTC. Null while it has never been changed.");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "role_changes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.");

            migrationBuilder.AlterColumn<Guid>(
                name: "target_user_id",
                schema: "identity",
                table: "role_changes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Whose roles changed. Null when the change was about the role itself.");

            migrationBuilder.AlterColumn<string>(
                name: "role_name",
                schema: "identity",
                table: "role_changes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "The role's name when the change happened, kept so the trail still reads after the role is deleted.");

            migrationBuilder.AlterColumn<List<Guid>>(
                name: "role_ids",
                schema: "identity",
                table: "role_changes",
                type: "uuid[]",
                nullable: false,
                oldClrType: typeof(List<Guid>),
                oldType: "uuid[]",
                oldComment: "Every role the change touched, so a search by role finds it whichever action it was.");

            migrationBuilder.AlterColumn<Guid>(
                name: "role_id",
                schema: "identity",
                table: "role_changes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "The role that changed. Null when the change was about a person's roles rather than one role.");

            migrationBuilder.AlterColumn<string>(
                name: "name_before",
                schema: "identity",
                table: "role_changes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "The name before a rename. Null for any other action.");

            migrationBuilder.AlterColumn<string>(
                name: "name_after",
                schema: "identity",
                table: "role_changes",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldComment: "The name after a rename. Null for any other action.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "role_changes",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "identity",
                table: "role_changes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "identity",
                table: "role_changes",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was deleted, in UTC. Null while it is not deleted.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "role_changes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "identity",
                table: "role_changes",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.");

            migrationBuilder.AlterColumn<string>(
                name: "action",
                schema: "identity",
                table: "role_changes",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "What was done, such as RoleCreated, RoleRenamed, RoleDeleted or UserRolesChanged.");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "identity",
                table: "role_changes",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "identity",
                table: "permissions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "What the permission allows, shown to an admin choosing permissions for a role.");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "identity",
                table: "permissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "The permission name and the key of the row, such as catalog.manage. The part before the first dot is the module that declares it.");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                schema: "identity",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Whose account the link belongs to.");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "identity",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was last changed, in UTC. Null while it has never been changed.");

            migrationBuilder.AlterColumn<string>(
                name: "token_hash",
                schema: "identity",
                table: "password_reset_tokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "The hash of the token that travelled in the link. The token itself is never stored, so a copy of this table does not let anyone in.");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "password_reset_tokens",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "expires_at",
                schema: "identity",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "When the link stops working, in UTC.");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "identity",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "identity",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was deleted, in UTC. Null while it is not deleted.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "identity",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "consumed_at",
                schema: "identity",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the link was spent, in UTC. Null while it is still usable; set once, so it cannot be used twice.");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "identity",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                schema: "identity",
                table: "email_verification_tokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Whose account the link belongs to.");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "email_verification_tokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "identity",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was last changed, in UTC. Null while it has never been changed.");

            migrationBuilder.AlterColumn<string>(
                name: "token_hash",
                schema: "identity",
                table: "email_verification_tokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldComment: "The hash of the token that travelled in the link. The token itself is never stored, so a copy of this table does not let anyone in.");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "email_verification_tokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "email_verification_tokens",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "expires_at",
                schema: "identity",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "When the link stops working, in UTC.");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "identity",
                table: "email_verification_tokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "identity",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was deleted, in UTC. Null while it is not deleted.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "email_verification_tokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "identity",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "consumed_at",
                schema: "identity",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the link was spent, in UTC. Null while it is still usable; set once, so it cannot be used twice.");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "identity",
                table: "email_verification_tokens",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                schema: "identity",
                table: "consent_records",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Who accepted.");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "consent_records",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "identity",
                table: "consent_records",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was last changed, in UTC. Null while it has never been changed.");

            migrationBuilder.AlterColumn<string>(
                name: "terms_version",
                schema: "identity",
                table: "consent_records",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "The version of the terms that was shown and accepted.");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "consent_records",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.");

            migrationBuilder.AlterColumn<string>(
                name: "privacy_version",
                schema: "identity",
                table: "consent_records",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "The version of the privacy policy that was shown and accepted.");

            migrationBuilder.AlterColumn<string>(
                name: "locale",
                schema: "identity",
                table: "consent_records",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldComment: "The language the documents were shown in, such as pt-BR: it is what they actually read.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "consent_records",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.");

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                schema: "identity",
                table: "consent_records",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true,
                oldComment: "The address the acceptance came from, long enough for IPv6. Null when it could not be read.");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "identity",
                table: "consent_records",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "identity",
                table: "consent_records",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was deleted, in UTC. Null while it is not deleted.");

            migrationBuilder.AlterColumn<bool>(
                name: "declares_adult",
                schema: "identity",
                table: "consent_records",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "The person declared they are 18 or older when accepting.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "consent_records",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "identity",
                table: "consent_records",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "accepted_at",
                schema: "identity",
                table: "consent_records",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "When they accepted, in UTC.");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "identity",
                table: "consent_records",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                schema: "identity",
                table: "account_events",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Whose account it happened to. Null when the attempt named no existing account.");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "account_events",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "identity",
                table: "account_events",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was last changed, in UTC. Null while it has never been changed.");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                schema: "identity",
                table: "account_events",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "What happened, such as SignInSucceeded, PasswordChanged or TwoFactorEnabled.");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "identity",
                table: "account_events",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.");

            migrationBuilder.AlterColumn<string>(
                name: "reason",
                schema: "identity",
                table: "account_events",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true,
                oldComment: "Why it failed, such as InvalidCredentials or LockedOut. Null when nothing failed.");

            migrationBuilder.AlterColumn<string>(
                name: "method",
                schema: "identity",
                table: "account_events",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true,
                oldComment: "How it was done, such as Password or Google. Null when the event has no method.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "identity",
                table: "account_events",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.");

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                schema: "identity",
                table: "account_events",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true,
                oldComment: "The address the request came from, long enough for IPv6. Null when it could not be read.");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "identity",
                table: "account_events",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "identity",
                table: "account_events",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was deleted, in UTC. Null while it is not deleted.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "account_events",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "identity",
                table: "account_events",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "identity",
                table: "account_events",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.");
        }
    }
}
