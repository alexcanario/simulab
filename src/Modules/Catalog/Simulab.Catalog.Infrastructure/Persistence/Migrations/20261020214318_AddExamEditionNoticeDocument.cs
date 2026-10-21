using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Simulab.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExamEditionNoticeDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "notice_document_blob_name",
                schema: "catalog",
                table: "exam_editions",
                type: "character varying(98)",
                maxLength: 98,
                nullable: true,
                comment: "The name the notice PDF is stored under in the file storage, never the curator's file name. Empty when the edition has no PDF.");

            migrationBuilder.AddColumn<string>(
                name: "notice_document_file_name",
                schema: "catalog",
                table: "exam_editions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                comment: "The notice PDF's file name as the curator gave it, without any folder. Empty when the edition has no PDF.");

            migrationBuilder.AddColumn<long>(
                name: "notice_document_size",
                schema: "catalog",
                table: "exam_editions",
                type: "bigint",
                nullable: true,
                comment: "The notice PDF's size in bytes, from 1 to 20 MB. Empty when the edition has no PDF.");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "notice_document_uploaded_at",
                schema: "catalog",
                table: "exam_editions",
                type: "timestamp with time zone",
                nullable: true,
                comment: "When the notice PDF was uploaded. Empty when the edition has no PDF.");

            migrationBuilder.AddCheckConstraint(
                name: "ck_exam_editions_notice_document",
                schema: "catalog",
                table: "exam_editions",
                sql: "(notice_document_blob_name IS NULL AND notice_document_file_name IS NULL AND notice_document_size IS NULL AND notice_document_uploaded_at IS NULL) OR (notice_document_blob_name IS NOT NULL AND notice_document_file_name IS NOT NULL AND notice_document_uploaded_at IS NOT NULL AND notice_document_size IS NOT NULL AND notice_document_size BETWEEN 1 AND 20971520)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_exam_editions_notice_document",
                schema: "catalog",
                table: "exam_editions");

            migrationBuilder.DropColumn(
                name: "notice_document_blob_name",
                schema: "catalog",
                table: "exam_editions");

            migrationBuilder.DropColumn(
                name: "notice_document_file_name",
                schema: "catalog",
                table: "exam_editions");

            migrationBuilder.DropColumn(
                name: "notice_document_size",
                schema: "catalog",
                table: "exam_editions");

            migrationBuilder.DropColumn(
                name: "notice_document_uploaded_at",
                schema: "catalog",
                table: "exam_editions");
        }
    }
}
