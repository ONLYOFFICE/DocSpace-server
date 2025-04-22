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

            migrationBuilder.AddColumn<bool>(
                name: "wallet",
                table: "tenants_quota",
                type: "tinyint(1)",
                nullable: false,
                defaultValueSql: "'0'");

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "price", "product_id", "visible", "wallet" },
                values: new object[] { -11, null, "total_size:107374182400", "storage", 30m, "1011", true, true });
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
        }
    }
}
