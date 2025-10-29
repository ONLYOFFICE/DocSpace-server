using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade65 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "price", "product_id", "wallet" },
                values: new object[] { -13, null, "aitools", "aitools", 0.0002m, "10009", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -13);
        }
    }
}
