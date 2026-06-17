using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade83 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Additional",
                table: "tenants_quota",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -13,
                column: "Additional",
                value: true);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -12,
                column: "Additional",
                value: true);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -11,
                column: "Additional",
                value: true);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -10,
                column: "Additional",
                value: false);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -9,
                column: "Additional",
                value: false);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -8,
                column: "Additional",
                value: false);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -7,
                column: "Additional",
                value: false);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -6,
                column: "Additional",
                value: false);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -5,
                column: "Additional",
                value: true);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -4,
                column: "Additional",
                value: true);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -3,
                column: "Additional",
                value: false);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -2,
                column: "Additional",
                value: false);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -1,
                column: "Additional",
                value: false);

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "Additional", "description", "features", "name", "price", "product_id", "service_group", "service_name", "visible", "wallet" },
                values: new object[] { -14, false, null, "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:268435456000,file_size:1024,manager:1,statistic,free_backup:2:fixed,automationapi", "adminwallet", 20m, "1013", null, "admin", true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -14);

            migrationBuilder.DropColumn(
                name: "Additional",
                table: "tenants_quota");
        }
    }
}
