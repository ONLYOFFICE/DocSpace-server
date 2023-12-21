using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -7,
                column: "features",
                value: "non-profit,audit,ldap,sso,thirdparty,restore,oauth,contentsearch,total_size:2147483648,file_size:1024,manager:20,statistic");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -6,
                column: "features",
                value: "audit,ldap,sso,whitelabel,thirdparty,restore,oauth,contentsearch,file_size:1024,statistic");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -2,
                column: "features",
                value: "audit,ldap,sso,whitelabel,thirdparty,restore,oauth,contentsearch,total_size:107374182400,file_size:1024,manager:1,statistic");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -1,
                column: "features",
                value: "trial,audit,ldap,sso,whitelabel,thirdparty,restore,oauth,total_size:107374182400,file_size:100,manager:1,statistic");

            migrationBuilder.AddColumn<long>(
                name: "counter",
                table: "files_folder",
                type: "bigint",
                nullable: false,
                defaultValueSql: "'0'");

            migrationBuilder.AddColumn<long>(
                name: "quota",
                table: "files_folder",
                type: "bigint",
                nullable: false,
                defaultValueSql: "'-2'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -7,
                column: "features",
                value: "non-profit,audit,ldap,sso,thirdparty,restore,oauth,contentsearch,total_size:2147483648,file_size:1024,manager:20");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -6,
                column: "features",
                value: "audit,ldap,sso,whitelabel,thirdparty,restore,oauth,contentsearch,file_size:1024");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -2,
                column: "features",
                value: "audit,ldap,sso,whitelabel,thirdparty,restore,oauth,contentsearch,total_size:107374182400,file_size:1024,manager:1");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -1,
                column: "features",
                value: "trial,audit,ldap,sso,whitelabel,thirdparty,restore,oauth,total_size:107374182400,file_size:100,manager:1");

            migrationBuilder.DropColumn(
                name: "counter",
                table: "files_folder");

            migrationBuilder.DropColumn(
                name: "quota",
                table: "files_folder");
        }
    }
}
