using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade42 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "owner_id",
                table: "tenants_tenants",
                type: "varchar(36)",
                nullable: true,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(38)",
                oldNullable: true,
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "portal_user_id",
                table: "telegram_users",
                type: "varchar(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "files_thirdparty_app",
                type: "varchar(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "files_thirdparty_account",
                type: "varchar(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "owner",
                table: "files_tag",
                type: "varchar(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "userid",
                table: "core_usersecurity",
                type: "varchar(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "userid",
                table: "core_userphoto",
                type: "varchar(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "groupid",
                table: "core_usergroup",
                type: "varchar(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "userid",
                table: "core_usergroup",
                type: "varchar(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "core_userdav",
                type: "varchar(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "core_user",
                type: "varchar(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "parentid",
                table: "core_group",
                type: "varchar(36)",
                nullable: true,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(38)",
                oldNullable: true,
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "categoryid",
                table: "core_group",
                type: "varchar(36)",
                nullable: true,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(38)",
                oldNullable: true,
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "core_group",
                type: "varchar(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "action",
                table: "core_acl",
                type: "varchar(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "subject",
                table: "core_acl",
                type: "varchar(36)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(38)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "owner_id",
                table: "tenants_tenants",
                type: "varchar(38)",
                nullable: true,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldNullable: true,
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "portal_user_id",
                table: "telegram_users",
                type: "varchar(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "files_thirdparty_app",
                type: "varchar(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "files_thirdparty_account",
                type: "varchar(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "owner",
                table: "files_tag",
                type: "varchar(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "userid",
                table: "core_usersecurity",
                type: "varchar(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "userid",
                table: "core_userphoto",
                type: "varchar(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "groupid",
                table: "core_usergroup",
                type: "varchar(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "userid",
                table: "core_usergroup",
                type: "varchar(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "core_userdav",
                type: "varchar(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "core_user",
                type: "varchar(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "parentid",
                table: "core_group",
                type: "varchar(38)",
                nullable: true,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldNullable: true,
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "categoryid",
                table: "core_group",
                type: "varchar(38)",
                nullable: true,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldNullable: true,
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "core_group",
                type: "varchar(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "action",
                table: "core_acl",
                type: "varchar(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<string>(
                name: "subject",
                table: "core_acl",
                type: "varchar(38)",
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");
        }
    }
}
