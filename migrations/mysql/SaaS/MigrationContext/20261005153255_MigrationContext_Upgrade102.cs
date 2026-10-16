using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade102 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_files_file_keys_tenants_tenants_tenant_id",
                table: "files_file_keys");

            migrationBuilder.AddForeignKey(
                name: "FK_files_file_keys_tenants_tenants_tenant_id",
                table: "files_file_keys",
                column: "tenant_id",
                principalTable: "tenants_tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_files_file_keys_tenants_tenants_tenant_id",
                table: "files_file_keys");

            migrationBuilder.AddForeignKey(
                name: "FK_files_file_keys_tenants_tenants_tenant_id",
                table: "files_file_keys",
                column: "tenant_id",
                principalTable: "tenants_tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
