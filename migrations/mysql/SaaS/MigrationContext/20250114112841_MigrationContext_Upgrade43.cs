using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade43 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "login_events",
                type: "char(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "char(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "files_tag_link",
                type: "char(36)",
                nullable: true,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "char(38)",
                oldNullable: true,
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<Guid>(
                name: "owner",
                table: "files_security",
                type: "char(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "char(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<Guid>(
                name: "subject",
                table: "files_security",
                type: "char(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "char(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<Guid>(
                name: "linked_for",
                table: "files_link",
                type: "char(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "char(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<Guid>(
                name: "modified_by",
                table: "files_folder",
                type: "char(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "char(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "files_folder",
                type: "char(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "char(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<Guid>(
                name: "modified_by",
                table: "files_file",
                type: "char(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "char(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "files_file",
                type: "char(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "char(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<Guid>(
                name: "create_by",
                table: "event_bus_integration_event_log",
                type: "char(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "char(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<Guid>(
                name: "event_id",
                table: "event_bus_integration_event_log",
                type: "char(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "char(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "backup_backup",
                type: "char(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "char(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "audit_events",
                type: "char(36)",
                nullable: true,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "char(38)",
                oldNullable: true,
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "login_events",
                type: "char(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "create_by",
                table: "files_tag_link",
                type: "char(38)",
                nullable: true,
                collation: "utf8_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true,
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "owner",
                table: "files_security",
                type: "char(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "subject",
                table: "files_security",
                type: "char(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "linked_for",
                table: "files_link",
                type: "char(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "modified_by",
                table: "files_folder",
                type: "char(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "create_by",
                table: "files_folder",
                type: "char(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "modified_by",
                table: "files_file",
                type: "char(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "create_by",
                table: "files_file",
                type: "char(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "create_by",
                table: "event_bus_integration_event_log",
                type: "char(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "event_id",
                table: "event_bus_integration_event_log",
                type: "char(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "backup_backup",
                type: "char(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "audit_events",
                type: "char(38)",
                nullable: true,
                collation: "utf8_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true,
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");
        }
    }
}
