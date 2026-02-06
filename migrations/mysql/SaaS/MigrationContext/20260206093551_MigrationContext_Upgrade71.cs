using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade71 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DbFilesGroup_tenants_tenants_TenantId",
                table: "DbFilesGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_files_roomgroup_DbFilesGroup_group_id",
                table: "files_roomgroup");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_DbFilesGroup_TempId",
                table: "DbFilesGroup");

            migrationBuilder.RenameTable(
                name: "DbFilesGroup",
                newName: "files_group");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "files_group",
                newName: "tenant_id");

            migrationBuilder.RenameColumn(
                name: "TempId",
                table: "files_group",
                newName: "id");

            migrationBuilder.AlterTable(
                name: "files_group")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "tenant_id",
                table: "files_group",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "files_group",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<string>(
                name: "icon",
                table: "files_group",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "files_group",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "",
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "files_group",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddPrimaryKey(
                name: "PK_files_group",
                table: "files_group",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_files_group_tenant_id",
                table: "files_group",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "FK_files_group_tenants_tenants_tenant_id",
                table: "files_group",
                column: "tenant_id",
                principalTable: "tenants_tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_files_roomgroup_files_group_group_id",
                table: "files_roomgroup",
                column: "group_id",
                principalTable: "files_group",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_files_group_tenants_tenants_tenant_id",
                table: "files_group");

            migrationBuilder.DropForeignKey(
                name: "FK_files_roomgroup_files_group_group_id",
                table: "files_roomgroup");

            migrationBuilder.DropPrimaryKey(
                name: "PK_files_group",
                table: "files_group");

            migrationBuilder.DropIndex(
                name: "IX_files_group_tenant_id",
                table: "files_group");

            migrationBuilder.DropColumn(
                name: "icon",
                table: "files_group");

            migrationBuilder.DropColumn(
                name: "name",
                table: "files_group");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "files_group");

            migrationBuilder.RenameTable(
                name: "files_group",
                newName: "DbFilesGroup");

            migrationBuilder.RenameColumn(
                name: "tenant_id",
                table: "DbFilesGroup",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "DbFilesGroup",
                newName: "TempId");

            migrationBuilder.AlterTable(
                name: "DbFilesGroup")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AlterColumn<int>(
                name: "TenantId",
                table: "DbFilesGroup",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "TempId",
                table: "DbFilesGroup",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_DbFilesGroup_TempId",
                table: "DbFilesGroup",
                column: "TempId");

            migrationBuilder.AddForeignKey(
                name: "FK_DbFilesGroup_tenants_tenants_TenantId",
                table: "DbFilesGroup",
                column: "TenantId",
                principalTable: "tenants_tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_files_roomgroup_DbFilesGroup_group_id",
                table: "files_roomgroup",
                column: "group_id",
                principalTable: "DbFilesGroup",
                principalColumn: "TempId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
