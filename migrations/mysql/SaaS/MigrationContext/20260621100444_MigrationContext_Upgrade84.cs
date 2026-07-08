using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade84 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "additional",
                table: "tenants_quota",
                type: "tinyint(1)",
                nullable: false,
                defaultValueSql: "'0'");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -13,
                column: "additional",
                value: true);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -12,
                column: "additional",
                value: true);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -11,
                column: "additional",
                value: true);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -10,
                column: "additional",
                value: false);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -9,
                column: "additional",
                value: false);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -8,
                column: "additional",
                value: false);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -7,
                column: "additional",
                value: false);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -6,
                column: "additional",
                value: false);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -5,
                column: "additional",
                value: true);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -4,
                column: "additional",
                value: true);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -3,
                column: "additional",
                value: false);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -2,
                column: "additional",
                value: false);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -1,
                column: "additional",
                value: false);

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "price", "product_id", "service_group", "service_name", "visible", "wallet", "additional" },
                values: new object[] { -14, null, "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:268435456000,file_size:1024,manager:1,statistic,free_backup:2:fixed,automationapi", "adminwallet", 20m, "1013", null, "admin", true, true, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -14);

            migrationBuilder.DropColumn(
                name: "additional",
                table: "tenants_quota");
        }
    }
}
