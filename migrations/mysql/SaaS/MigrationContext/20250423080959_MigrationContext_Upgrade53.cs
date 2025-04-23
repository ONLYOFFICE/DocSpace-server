using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade53 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "due_date",
                table: "tenants_tariffrow",
                type: "datetime",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                table: "tenants_quota",
                type: "decimal(10,4)",
                nullable: false,
                defaultValueSql: "'0.00'",
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldDefaultValueSql: "'0.00'");

            migrationBuilder.AddColumn<bool>(
                name: "wallet",
                table: "tenants_quota",
                type: "tinyint(1)",
                nullable: false,
                defaultValueSql: "'0'");

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "price", "product_id", "visible", "wallet" },
                values: new object[] { -11, null, "total_size:1073741824", "storage", 0.0322m, "1011", true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -11);

            migrationBuilder.DropColumn(
                name: "due_date",
                table: "tenants_tariffrow");

            migrationBuilder.DropColumn(
                name: "wallet",
                table: "tenants_quota");

            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                table: "tenants_quota",
                type: "decimal(10,2)",
                nullable: false,
                defaultValueSql: "'0.00'",
                oldClrType: typeof(decimal),
                oldType: "decimal(10,4)",
                oldDefaultValueSql: "'0.00'");
        }
    }
}
