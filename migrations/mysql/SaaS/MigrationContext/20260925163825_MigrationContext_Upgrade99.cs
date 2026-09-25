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
            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "additional", "description", "features", "name", "price", "product_id", "service_group", "service_name", "visible", "wallet" },
                values: new object[] { -20, true, null, "businesstools,sms2fa,audit,ldap,sso,customization,thirdparty,restore,contentsearch,file_size:1024,statistic,free_backup:2:fixed", "businesstools", 99m, "1020", null, "business-tools", true, true });

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "product_id", "service_group", "service_name" },
                values: new object[] { -19, null, "free,oauth,total_size:2147483648,manager:10000,room:10000,automationapi", "free", null, null, null });

            // move startup portals (-3) to the free plan (-19): the current tariff of the tenant (the one with the highest id)
            migrationBuilder.Sql(@"
UPDATE tenants_tariffrow r
JOIN (SELECT tenant, MAX(id) AS id FROM tenants_tariff GROUP BY tenant) t ON r.tenant = t.tenant AND r.tariff_id = t.id
SET r.quota = -19
WHERE r.quota = -3;");

            // and the initial tariff (id = -tenant) that TariffService restores the base plan from when only add-ons are paid
            migrationBuilder.Sql("UPDATE tenants_tariffrow SET quota = -19 WHERE quota = -3 AND tariff_id = -tenant;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE tenants_tariffrow SET quota = -3 WHERE quota = -19;");

            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -20);

            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -19);
        }
    }
}
