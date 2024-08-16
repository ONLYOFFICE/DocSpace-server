using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade26 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -9,
                column: "features",
                value: "audit,ldap,sso,whitelabel,customization,thirdparty,restore,oauth,contentsearch,total_size:268435456000,file_size:1024,manager:1,statistic");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -6,
                column: "features",
                value: "audit,ldap,sso,whitelabel,customization,thirdparty,restore,oauth,contentsearch,file_size:1024,statistic");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -2,
                column: "features",
                value: "audit,ldap,sso,whitelabel,customization,thirdparty,restore,oauth,contentsearch,total_size:107374182400,file_size:1024,manager:1,statistic");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -1,
                column: "features",
                value: "trial,audit,ldap,sso,whitelabel,customization,thirdparty,restore,oauth,total_size:107374182400,file_size:100,manager:1,statistic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -9,
                column: "features",
                value: "audit,ldap,sso,whitelabel,thirdparty,restore,oauth,contentsearch,total_size:268435456000,file_size:1024,manager:1,statistic");

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
        }
    }
}
