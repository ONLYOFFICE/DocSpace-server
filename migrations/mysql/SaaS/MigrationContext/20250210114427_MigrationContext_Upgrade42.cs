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
            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "price", "product_id" },
                values: new object[] { -10, "since 10.02.2025", "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:268435456000,file_size:1024,manager:1,statistic,year", "adminyear", 200m, "1009" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -10);
        }
    }
}
