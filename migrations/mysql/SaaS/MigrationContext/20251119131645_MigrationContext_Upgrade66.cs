using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade66 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "tenants_forbiden",
                column: "address",
                values: new object[]
                {
                    "api-system-eu-central-1",
                    "api-system-us-east-2",
                    "identity-eu-central-1",
                    "identity-us-east-2",
                    "oauth",
                    "settings"
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tenants_forbiden",
                keyColumn: "address",
                keyValue: "api-system-eu-central-1");

            migrationBuilder.DeleteData(
                table: "tenants_forbiden",
                keyColumn: "address",
                keyValue: "api-system-us-east-2");

            migrationBuilder.DeleteData(
                table: "tenants_forbiden",
                keyColumn: "address",
                keyValue: "identity-eu-central-1");

            migrationBuilder.DeleteData(
                table: "tenants_forbiden",
                keyColumn: "address",
                keyValue: "identity-us-east-2");

            migrationBuilder.DeleteData(
                table: "tenants_forbiden",
                keyColumn: "address",
                keyValue: "oauth");

            migrationBuilder.DeleteData(
                table: "tenants_forbiden",
                keyColumn: "address",
                keyValue: "settings");
        }
    }
}
