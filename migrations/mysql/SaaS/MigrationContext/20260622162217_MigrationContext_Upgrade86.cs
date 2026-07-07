using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade86 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "product_id", "service_group", "service_name", "additional" },
                values: new object[] { -17, null, "docscloud:1000,docsclouddevpack,docscloudtrial", "docscloudtrial", "1016", null, null, true });

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "price", "product_id", "service_group", "service_name", "visible", "wallet", "additional" },
                values: new object[] { -16, null, "docscloud:1,docsclouddevpack", "docsclouddevpack", 12m, "1015", null, "docscloud-devpack", true, true, true });

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "price", "product_id", "service_group", "service_name", "visible", "wallet", "additional" },
                values: new object[] { -15, null, "docscloud:1", "docscloud", 8m, "1014", null, "docscloud", true, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -17);

            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -16);

            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -15);
        }
    }
}
