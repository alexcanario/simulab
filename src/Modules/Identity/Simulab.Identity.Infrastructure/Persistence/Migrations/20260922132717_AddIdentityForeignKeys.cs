using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_user_logins_user_id",
                schema: "identity",
                table: "user_logins",
                column: "user_id");

            // B-13 D3: rows that point to no user or role would stop the foreign keys below; they serve nothing,
            // so they go. A soft deleted user or role is still a row, and what points to it stays.
            migrationBuilder.Sql(
                """
                DELETE FROM identity.consent_records child WHERE NOT EXISTS (SELECT 1 FROM identity.users parent WHERE parent.id = child.user_id);
                DELETE FROM identity.email_verification_tokens child WHERE NOT EXISTS (SELECT 1 FROM identity.users parent WHERE parent.id = child.user_id);
                DELETE FROM identity.password_reset_tokens child WHERE NOT EXISTS (SELECT 1 FROM identity.users parent WHERE parent.id = child.user_id);
                DELETE FROM identity.user_claims child WHERE NOT EXISTS (SELECT 1 FROM identity.users parent WHERE parent.id = child.user_id);
                DELETE FROM identity.user_logins child WHERE NOT EXISTS (SELECT 1 FROM identity.users parent WHERE parent.id = child.user_id);
                DELETE FROM identity.user_tokens child WHERE NOT EXISTS (SELECT 1 FROM identity.users parent WHERE parent.id = child.user_id);
                DELETE FROM identity.user_roles child WHERE NOT EXISTS (SELECT 1 FROM identity.users parent WHERE parent.id = child.user_id)
                    OR NOT EXISTS (SELECT 1 FROM identity.roles parent WHERE parent.id = child.role_id);
                DELETE FROM identity.role_claims child WHERE NOT EXISTS (SELECT 1 FROM identity.roles parent WHERE parent.id = child.role_id);
                """);

            migrationBuilder.AddForeignKey(
                name: "fk_consent_records_users_user_id",
                schema: "identity",
                table: "consent_records",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_email_verification_tokens_users_user_id",
                schema: "identity",
                table: "email_verification_tokens",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_password_reset_tokens_users_user_id",
                schema: "identity",
                table: "password_reset_tokens",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_role_claims_roles_role_id",
                schema: "identity",
                table: "role_claims",
                column: "role_id",
                principalSchema: "identity",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_claims_users_user_id",
                schema: "identity",
                table: "user_claims",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_logins_users_user_id",
                schema: "identity",
                table: "user_logins",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_roles_roles_role_id",
                schema: "identity",
                table: "user_roles",
                column: "role_id",
                principalSchema: "identity",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_roles_users_user_id",
                schema: "identity",
                table: "user_roles",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_tokens_users_user_id",
                schema: "identity",
                table: "user_tokens",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_consent_records_users_user_id",
                schema: "identity",
                table: "consent_records");

            migrationBuilder.DropForeignKey(
                name: "fk_email_verification_tokens_users_user_id",
                schema: "identity",
                table: "email_verification_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_password_reset_tokens_users_user_id",
                schema: "identity",
                table: "password_reset_tokens");

            migrationBuilder.DropForeignKey(
                name: "fk_role_claims_roles_role_id",
                schema: "identity",
                table: "role_claims");

            migrationBuilder.DropForeignKey(
                name: "fk_user_claims_users_user_id",
                schema: "identity",
                table: "user_claims");

            migrationBuilder.DropForeignKey(
                name: "fk_user_logins_users_user_id",
                schema: "identity",
                table: "user_logins");

            migrationBuilder.DropForeignKey(
                name: "fk_user_roles_roles_role_id",
                schema: "identity",
                table: "user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_user_roles_users_user_id",
                schema: "identity",
                table: "user_roles");

            migrationBuilder.DropForeignKey(
                name: "fk_user_tokens_users_user_id",
                schema: "identity",
                table: "user_tokens");

            migrationBuilder.DropIndex(
                name: "ix_user_logins_user_id",
                schema: "identity",
                table: "user_logins");
        }
    }
}
