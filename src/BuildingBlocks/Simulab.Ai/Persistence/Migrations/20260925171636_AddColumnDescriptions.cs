using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Ai.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "ai_calls",
                schema: "ai",
                comment: "One call that reached the model, answered or failed. A call the gateway refused before it left the app is not here: nothing was spent.");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                schema: "ai",
                table: "ai_calls",
                type: "uuid",
                nullable: false,
                comment: "Who the call is charged to. Every call belongs to a signed-in user.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "ai",
                table: "ai_calls",
                type: "uuid",
                nullable: true,
                comment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "ai",
                table: "ai_calls",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was last changed, in UTC. Null while it has never been changed.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "ai",
                table: "ai_calls",
                type: "uuid",
                nullable: true,
                comment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "succeeded",
                schema: "ai",
                table: "ai_calls",
                type: "boolean",
                nullable: false,
                comment: "False when the model answered with an error or could not be reached. Only successful calls count against the monthly quota.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "started_at",
                schema: "ai",
                table: "ai_calls",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When the call left the app, in UTC. The monthly quota counts from the first day of the calendar month.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "purpose",
                schema: "ai",
                table: "ai_calls",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                comment: "What the call was for, such as diagnostics. Cost can be read per purpose, and a purpose can be sent to another model from configuration.",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "output_tokens",
                schema: "ai",
                table: "ai_calls",
                type: "integer",
                nullable: false,
                comment: "Tokens the answer spent, as the answer reports them. Zero on a failed call, where none are known.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "output_price_per_million",
                schema: "ai",
                table: "ai_calls",
                type: "numeric(18,6)",
                nullable: false,
                comment: "US dollars per million output tokens, as configured when the call was made.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");

            migrationBuilder.AlterColumn<string>(
                name: "model",
                schema: "ai",
                table: "ai_calls",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                comment: "The model that was called, as the provider names it.",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "ai",
                table: "ai_calls",
                type: "boolean",
                nullable: false,
                comment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<int>(
                name: "input_tokens",
                schema: "ai",
                table: "ai_calls",
                type: "integer",
                nullable: false,
                comment: "Tokens the request spent, as the answer reports them. Zero on a failed call, where none are known.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "input_price_per_million",
                schema: "ai",
                table: "ai_calls",
                type: "numeric(18,6)",
                nullable: false,
                comment: "US dollars per million input tokens, as configured when the call was made. Kept on the row so a later price change never rewrites the past.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");

            migrationBuilder.AlterColumn<string>(
                name: "error_code",
                schema: "ai",
                table: "ai_calls",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                comment: "Why the call failed, as a stable code such as ai.call_failed. Null when it succeeded.",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "duration_ms",
                schema: "ai",
                table: "ai_calls",
                type: "integer",
                nullable: false,
                comment: "How long the call took, in milliseconds, measured around the request.",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "ai",
                table: "ai_calls",
                type: "uuid",
                nullable: true,
                comment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "ai",
                table: "ai_calls",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was deleted, in UTC. Null while it is not deleted.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "ai",
                table: "ai_calls",
                type: "uuid",
                nullable: true,
                comment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "ai",
                table: "ai_calls",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<decimal>(
                name: "cost_usd",
                schema: "ai",
                table: "ai_calls",
                type: "numeric(18,6)",
                nullable: false,
                comment: "What the call cost in US dollars: the tokens multiplied by the two prices on this row.",
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "ai",
                table: "ai_calls",
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
                name: "ai_calls",
                schema: "ai",
                oldComment: "One call that reached the model, answered or failed. A call the gateway refused before it left the app is not here: nothing was spent.");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                schema: "ai",
                table: "ai_calls",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Who the call is charged to. Every call belongs to a signed-in user.");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "ai",
                table: "ai_calls",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "ai",
                table: "ai_calls",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was last changed, in UTC. Null while it has never been changed.");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "ai",
                table: "ai_calls",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.");

            migrationBuilder.AlterColumn<bool>(
                name: "succeeded",
                schema: "ai",
                table: "ai_calls",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "False when the model answered with an error or could not be reached. Only successful calls count against the monthly quota.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "started_at",
                schema: "ai",
                table: "ai_calls",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "When the call left the app, in UTC. The monthly quota counts from the first day of the calendar month.");

            migrationBuilder.AlterColumn<string>(
                name: "purpose",
                schema: "ai",
                table: "ai_calls",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldComment: "What the call was for, such as diagnostics. Cost can be read per purpose, and a purpose can be sent to another model from configuration.");

            migrationBuilder.AlterColumn<int>(
                name: "output_tokens",
                schema: "ai",
                table: "ai_calls",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Tokens the answer spent, as the answer reports them. Zero on a failed call, where none are known.");

            migrationBuilder.AlterColumn<decimal>(
                name: "output_price_per_million",
                schema: "ai",
                table: "ai_calls",
                type: "numeric(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldComment: "US dollars per million output tokens, as configured when the call was made.");

            migrationBuilder.AlterColumn<string>(
                name: "model",
                schema: "ai",
                table: "ai_calls",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldComment: "The model that was called, as the provider names it.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "ai",
                table: "ai_calls",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.");

            migrationBuilder.AlterColumn<int>(
                name: "input_tokens",
                schema: "ai",
                table: "ai_calls",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Tokens the request spent, as the answer reports them. Zero on a failed call, where none are known.");

            migrationBuilder.AlterColumn<decimal>(
                name: "input_price_per_million",
                schema: "ai",
                table: "ai_calls",
                type: "numeric(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldComment: "US dollars per million input tokens, as configured when the call was made. Kept on the row so a later price change never rewrites the past.");

            migrationBuilder.AlterColumn<string>(
                name: "error_code",
                schema: "ai",
                table: "ai_calls",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldComment: "Why the call failed, as a stable code such as ai.call_failed. Null when it succeeded.");

            migrationBuilder.AlterColumn<int>(
                name: "duration_ms",
                schema: "ai",
                table: "ai_calls",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "How long the call took, in milliseconds, measured around the request.");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "ai",
                table: "ai_calls",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "ai",
                table: "ai_calls",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was deleted, in UTC. Null while it is not deleted.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "ai",
                table: "ai_calls",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "ai",
                table: "ai_calls",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.");

            migrationBuilder.AlterColumn<decimal>(
                name: "cost_usd",
                schema: "ai",
                table: "ai_calls",
                type: "numeric(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldComment: "What the call cost in US dollars: the tokens multiplied by the two prices on this row.");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "ai",
                table: "ai_calls",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.");
        }
    }
}
