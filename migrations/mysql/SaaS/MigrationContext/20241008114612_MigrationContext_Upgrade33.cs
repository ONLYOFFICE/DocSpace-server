using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade33 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "spam",
                table: "core_user",
                type: "tinyint(1)",
                nullable: true,
                defaultValueSql: "'1'");

            /*
            migrationBuilder.UpdateData(
                table: "core_user",
                keyColumn: "id",
                keyValue: "66faa6e4-f133-11ea-b126-00ffeec8b4ef",
                columns: new string[0],
                values: new object[0]);
            */

            migrationBuilder.Sql("UPDATE core_user JOIN tenants_tenants ON core_user.id = tenants_tenants.owner_id SET core_user.spam = tenants_tenants.spam");

            migrationBuilder.DropColumn(
                name: "spam",
                table: "tenants_tenants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "spam",
                table: "tenants_tenants",
                type: "tinyint(1)",
                nullable: false,
                defaultValueSql: "'1'");

            /*
            migrationBuilder.UpdateData(
                table: "tenants_tenants",
                keyColumn: "id",
                keyValue: -1,
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "tenants_tenants",
                keyColumn: "id",
                keyValue: 1,
                columns: new string[0],
                values: new object[0]);
            */

            migrationBuilder.Sql("UPDATE tenants_tenants JOIN core_user ON tenants_tenants.owner_id = core_user.id SET tenants_tenants.spam = core_user.spam");

            migrationBuilder.DropColumn(
                name: "spam",
                table: "core_user");
        }
    }
}
