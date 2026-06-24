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
                columns: new[] { "tenant", "Additional", "description", "features", "name", "product_id", "service_group", "service_name" },
                values: new object[] { -17, true, null, "docscloud:1000,docsclouddevpack,docscloudtrial", "docscloudtrial", "1016", null, null });

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "Additional", "description", "features", "name", "price", "product_id", "service_group", "service_name", "visible", "wallet" },
                values: new object[] { -16, true, null, "docscloud:1,docsclouddevpack", "docsclouddevpack", 12m, "1015", null, "docscloud-devpack", true, true });

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "Additional", "description", "features", "name", "price", "product_id", "service_group", "service_name", "visible", "wallet" },
                values: new object[] { -15, true, null, "docscloud:1", "docscloud", 8m, "1014", null, "docscloud", true, true });
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
