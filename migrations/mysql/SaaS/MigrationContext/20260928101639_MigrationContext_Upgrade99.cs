using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade99 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -3,
                column: "features",
                value: "free,oauth,total_size:2147483648,manager:10000,room:10000,automationapi");

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "additional", "description", "features", "name", "price", "product_id", "service_group", "service_name", "visible", "wallet" },
                values: new object[] { -19, true, null, "businesstools,sms2fa,audit,ldap,sso,customization,thirdparty,restore,contentsearch,file_size:1024,statistic,free_backup:2:fixed", "businesstools", 99m, "1020", null, "business-tools", true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -19);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -3,
                column: "features",
                value: "free,oauth,total_size:2147483648,manager:3,room:12,automationapi");
        }
    }
}
