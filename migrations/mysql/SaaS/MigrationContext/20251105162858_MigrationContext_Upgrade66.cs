using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade66 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                table: "tenants_quota",
                type: "decimal(15,9)",
                nullable: false,
                defaultValueSql: "'0.00'",
                oldClrType: typeof(decimal),
                oldType: "decimal(10,4)",
                oldDefaultValueSql: "'0.00'");

            migrationBuilder.AddColumn<string>(
                name: "service_group",
                table: "tenants_quota",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -12,
                columns: new[] { "product_id", "service_group", "visible" },
                values: new object[] { null, null, true });

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -11,
                column: "service_group",
                value: null);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -10,
                column: "service_group",
                value: null);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -9,
                column: "service_group",
                value: null);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -8,
                column: "service_group",
                value: null);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -7,
                column: "service_group",
                value: null);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -6,
                column: "service_group",
                value: null);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -5,
                column: "service_group",
                value: null);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -4,
                column: "service_group",
                value: null);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -3,
                column: "service_group",
                value: null);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -2,
                column: "service_group",
                value: null);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -1,
                column: "service_group",
                value: null);

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "price", "product_id", "service_group", "service_name", "wallet" },
                values: new object[,]
                {
                    { -22, null, "gpt5output", "gpt5output", 0.000012m, null, "aitools", "gpt5output", true },
                    { -21, null, "gpt5input", "gpt5input", 0.0000015m, null, "aitools", "gpt5input", true },
                    { -20, null, "claude45output", "claude45output", 0.000018m, null, "aitools", "claude4.5output", true },
                    { -19, null, "claude45input", "claude45input", 0.0000036m, null, "aitools", "claude4.5input", true },
                    { -18, null, "deepseek31output", "deepseek31output", 0.00000108m, null, "aitools", "deepseek3.1output", true },
                    { -17, null, "deepseek31input", "deepseek31input", 0.000000276m, null, "aitools", "deepseek3.1input", true },
                    { -16, null, "embedding", "embedding", 0.000000024m, null, "aitools", "embedding", true },
                    { -15, null, "webfetch", "webfetch", 0.0012m, null, "aitools", "webfetch", true },
                    { -14, null, "websearch", "websearch", 0.006m, null, "aitools", "websearch", true }
                });

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "product_id", "service_group", "service_name", "visible", "wallet" },
                values: new object[] { -13, null, "aitools", "aitools", null, null, null, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -22);

            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -21);

            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -20);

            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -19);

            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -18);

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

            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -14);

            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -13);

            migrationBuilder.DropColumn(
                name: "service_group",
                table: "tenants_quota");

            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                table: "tenants_quota",
                type: "decimal(10,4)",
                nullable: false,
                defaultValueSql: "'0.00'",
                oldClrType: typeof(decimal),
                oldType: "decimal(15,9)",
                oldDefaultValueSql: "'0.00'");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -12,
                columns: new[] { "product_id", "visible" },
                values: new object[] { "10006", false });
        }
    }
}
