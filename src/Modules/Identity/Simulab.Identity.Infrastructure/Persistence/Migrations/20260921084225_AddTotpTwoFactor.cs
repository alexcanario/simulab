using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTotpTwoFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "totp_enabled_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "totp_last_accepted_step",
                schema: "identity",
                table: "users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "totp_secret_encrypted",
                schema: "identity",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "totp_enabled_at",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "totp_last_accepted_step",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "totp_secret_encrypted",
                schema: "identity",
                table: "users");
        }
    }
}
