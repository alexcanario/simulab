using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "organizers",
                schema: "catalog",
                comment: "Who runs an assessment: an exam board, a certifying body or a university. An exam belongs to one of them.");

            migrationBuilder.AlterTable(
                name: "issuing_authorities",
                schema: "catalog",
                comment: "The body that publishes a notice and contracts an organizer to run the exam: a ministry, a court, a city hall.");

            migrationBuilder.AlterTable(
                name: "exams",
                schema: "catalog",
                comment: "A recurring assessment an issuing authority runs, such as a competitive exam for a job or a certification. Its editions are the papers actually applied.");

            migrationBuilder.AlterColumn<string>(
                name: "website",
                schema: "catalog",
                table: "organizers",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                comment: "The organizer's own address, where its notices are published.",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "catalog",
                table: "organizers",
                type: "uuid",
                nullable: true,
                comment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "catalog",
                table: "organizers",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was last changed, in UTC. Null while it has never been changed.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "catalog",
                table: "organizers",
                type: "uuid",
                nullable: true,
                comment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_name",
                schema: "catalog",
                table: "organizers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                comment: "The name without case or accents, which is what the unique index compares.",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_acronym",
                schema: "catalog",
                table: "organizers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                comment: "The acronym without case or accents, which is what the unique index compares.",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "catalog",
                table: "organizers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                comment: "Full name, as the organizer writes it.",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "kind",
                schema: "catalog",
                table: "organizers",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "What kind of organizer it is: ExamBoard, CertifyingBody or University.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "catalog",
                table: "organizers",
                type: "boolean",
                nullable: false,
                comment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "catalog",
                table: "organizers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Free notes about the organizer, shown to whoever curates the catalog.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "catalog",
                table: "organizers",
                type: "uuid",
                nullable: true,
                comment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "catalog",
                table: "organizers",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was deleted, in UTC. Null while it is not deleted.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "catalog",
                table: "organizers",
                type: "uuid",
                nullable: true,
                comment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "catalog",
                table: "organizers",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "acronym",
                schema: "catalog",
                table: "organizers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                comment: "Short name people search by, such as CEBRASPE or FGV.",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "catalog",
                table: "organizers",
                type: "uuid",
                nullable: false,
                comment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "website",
                schema: "catalog",
                table: "issuing_authorities",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                comment: "The body's own address, where its notices are published.",
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "catalog",
                table: "issuing_authorities",
                type: "uuid",
                nullable: true,
                comment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "catalog",
                table: "issuing_authorities",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was last changed, in UTC. Null while it has never been changed.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "catalog",
                table: "issuing_authorities",
                type: "uuid",
                nullable: true,
                comment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_name",
                schema: "catalog",
                table: "issuing_authorities",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                comment: "The name without case or accents, which is what the unique index compares.",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_acronym",
                schema: "catalog",
                table: "issuing_authorities",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                comment: "The acronym without case or accents, which is what the unique index compares.",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "catalog",
                table: "issuing_authorities",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                comment: "Full name, as the notice writes it.",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "catalog",
                table: "issuing_authorities",
                type: "boolean",
                nullable: false,
                comment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "catalog",
                table: "issuing_authorities",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Free notes about the body, shown to whoever curates the catalog.",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "catalog",
                table: "issuing_authorities",
                type: "uuid",
                nullable: true,
                comment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "catalog",
                table: "issuing_authorities",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was deleted, in UTC. Null while it is not deleted.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "catalog",
                table: "issuing_authorities",
                type: "uuid",
                nullable: true,
                comment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "catalog",
                table: "issuing_authorities",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "acronym",
                schema: "catalog",
                table: "issuing_authorities",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                comment: "Short name people search by, such as TRF1 or INSS.",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "catalog",
                table: "issuing_authorities",
                type: "uuid",
                nullable: false,
                comment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "catalog",
                table: "exams",
                type: "uuid",
                nullable: true,
                comment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "catalog",
                table: "exams",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was last changed, in UTC. Null while it has never been changed.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "catalog",
                table: "exams",
                type: "uuid",
                nullable: true,
                comment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "scope_detail",
                schema: "catalog",
                table: "exams",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                comment: "Which state or city the scope means, when it is not national.",
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "scope",
                schema: "catalog",
                table: "exams",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "How far the exam reaches: Federal, State, Municipal, National or International.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<string>(
                name: "normalized_name",
                schema: "catalog",
                table: "exams",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "The name without case or accents, which is what the unique index compares.",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "catalog",
                table: "exams",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                comment: "The exam's name, unique inside its issuing authority.",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<Guid>(
                name: "issuing_authority_id",
                schema: "catalog",
                table: "exams",
                type: "uuid",
                nullable: false,
                comment: "The body that publishes this exam's notices.",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "catalog",
                table: "exams",
                type: "boolean",
                nullable: false,
                comment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.",
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "catalog",
                table: "exams",
                type: "uuid",
                nullable: true,
                comment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "catalog",
                table: "exams",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the row was deleted, in UTC. Null while it is not deleted.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "catalog",
                table: "exams",
                type: "uuid",
                nullable: true,
                comment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "catalog",
                table: "exams",
                type: "timestamp with time zone",
                nullable: false,
                comment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "content_language",
                schema: "catalog",
                table: "exams",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                comment: "The language the questions are written in, such as pt-BR. Exam content is never translated.",
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AlterColumn<string>(
                name: "assessment_type",
                schema: "catalog",
                table: "exams",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "What kind of assessment it is: PublicServiceExam, Certification, UniversityEntranceExam or Enem.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "catalog",
                table: "exams",
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
                name: "organizers",
                schema: "catalog",
                oldComment: "Who runs an assessment: an exam board, a certifying body or a university. An exam belongs to one of them.");

            migrationBuilder.AlterTable(
                name: "issuing_authorities",
                schema: "catalog",
                oldComment: "The body that publishes a notice and contracts an organizer to run the exam: a ministry, a court, a city hall.");

            migrationBuilder.AlterTable(
                name: "exams",
                schema: "catalog",
                oldComment: "A recurring assessment an issuing authority runs, such as a competitive exam for a job or a certification. Its editions are the papers actually applied.");

            migrationBuilder.AlterColumn<string>(
                name: "website",
                schema: "catalog",
                table: "organizers",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true,
                oldComment: "The organizer's own address, where its notices are published.");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "catalog",
                table: "organizers",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "catalog",
                table: "organizers",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was last changed, in UTC. Null while it has never been changed.");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "catalog",
                table: "organizers",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.");

            migrationBuilder.AlterColumn<string>(
                name: "normalized_name",
                schema: "catalog",
                table: "organizers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldComment: "The name without case or accents, which is what the unique index compares.");

            migrationBuilder.AlterColumn<string>(
                name: "normalized_acronym",
                schema: "catalog",
                table: "organizers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "The acronym without case or accents, which is what the unique index compares.");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "catalog",
                table: "organizers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldComment: "Full name, as the organizer writes it.");

            migrationBuilder.AlterColumn<string>(
                name: "kind",
                schema: "catalog",
                table: "organizers",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "What kind of organizer it is: ExamBoard, CertifyingBody or University.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "catalog",
                table: "organizers",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "catalog",
                table: "organizers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "Free notes about the organizer, shown to whoever curates the catalog.");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "catalog",
                table: "organizers",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "catalog",
                table: "organizers",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was deleted, in UTC. Null while it is not deleted.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "catalog",
                table: "organizers",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "catalog",
                table: "organizers",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.");

            migrationBuilder.AlterColumn<string>(
                name: "acronym",
                schema: "catalog",
                table: "organizers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "Short name people search by, such as CEBRASPE or FGV.");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "catalog",
                table: "organizers",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.");

            migrationBuilder.AlterColumn<string>(
                name: "website",
                schema: "catalog",
                table: "issuing_authorities",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300,
                oldNullable: true,
                oldComment: "The body's own address, where its notices are published.");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "catalog",
                table: "issuing_authorities",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "catalog",
                table: "issuing_authorities",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was last changed, in UTC. Null while it has never been changed.");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "catalog",
                table: "issuing_authorities",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.");

            migrationBuilder.AlterColumn<string>(
                name: "normalized_name",
                schema: "catalog",
                table: "issuing_authorities",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldComment: "The name without case or accents, which is what the unique index compares.");

            migrationBuilder.AlterColumn<string>(
                name: "normalized_acronym",
                schema: "catalog",
                table: "issuing_authorities",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "The acronym without case or accents, which is what the unique index compares.");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "catalog",
                table: "issuing_authorities",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldComment: "Full name, as the notice writes it.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "catalog",
                table: "issuing_authorities",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "catalog",
                table: "issuing_authorities",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true,
                oldComment: "Free notes about the body, shown to whoever curates the catalog.");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "catalog",
                table: "issuing_authorities",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "catalog",
                table: "issuing_authorities",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was deleted, in UTC. Null while it is not deleted.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "catalog",
                table: "issuing_authorities",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "catalog",
                table: "issuing_authorities",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.");

            migrationBuilder.AlterColumn<string>(
                name: "acronym",
                schema: "catalog",
                table: "issuing_authorities",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "Short name people search by, such as TRF1 or INSS.");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "catalog",
                table: "issuing_authorities",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.");

            migrationBuilder.AlterColumn<Guid>(
                name: "updated_by",
                schema: "catalog",
                table: "exams",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who last changed the row. Null while it has never been changed, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "catalog",
                table: "exams",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was last changed, in UTC. Null while it has never been changed.");

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "catalog",
                table: "exams",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "The institution the row belongs to. Null means global data or an individual account, which is every row while multi-tenancy is dormant.");

            migrationBuilder.AlterColumn<string>(
                name: "scope_detail",
                schema: "catalog",
                table: "exams",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true,
                oldComment: "Which state or city the scope means, when it is not national.");

            migrationBuilder.AlterColumn<string>(
                name: "scope",
                schema: "catalog",
                table: "exams",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "How far the exam reaches: Federal, State, Municipal, National or International.");

            migrationBuilder.AlterColumn<string>(
                name: "normalized_name",
                schema: "catalog",
                table: "exams",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "The name without case or accents, which is what the unique index compares.");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "catalog",
                table: "exams",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldComment: "The exam's name, unique inside its issuing authority.");

            migrationBuilder.AlterColumn<Guid>(
                name: "issuing_authority_id",
                schema: "catalog",
                table: "exams",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "The body that publishes this exam's notices.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_deleted",
                schema: "catalog",
                table: "exams",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldComment: "True once the row was deleted. Rows are never removed; a global query filter hides the deleted ones.");

            migrationBuilder.AlterColumn<Guid>(
                name: "deleted_by",
                schema: "catalog",
                table: "exams",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who deleted the row. Null while it is not deleted, or when no one was signed in.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "catalog",
                table: "exams",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "When the row was deleted, in UTC. Null while it is not deleted.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "catalog",
                table: "exams",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Who created the row. Null when no one was signed in, as in a sign-up or a background job.");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "catalog",
                table: "exams",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldComment: "When the row was created, in UTC. Filled by the audit interceptor; never set by hand.");

            migrationBuilder.AlterColumn<string>(
                name: "content_language",
                schema: "catalog",
                table: "exams",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldComment: "The language the questions are written in, such as pt-BR. Exam content is never translated.");

            migrationBuilder.AlterColumn<string>(
                name: "assessment_type",
                schema: "catalog",
                table: "exams",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "What kind of assessment it is: PublicServiceExam, Certification, UniversityEntranceExam or Enem.");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                schema: "catalog",
                table: "exams",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Identifier of the row, a UUID v7: generated by the app, ordered by creation time.");
        }
    }
}
