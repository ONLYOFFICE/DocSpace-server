using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade39 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "trusteddomainsenabled",
                table: "tenants_tenants",
                type: "int",
                nullable: false,
                defaultValueSql: "'0'",
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValueSql: "'1'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "trusteddomainsenabled",
                table: "tenants_tenants",
                type: "int",
                nullable: false,
                defaultValueSql: "'1'",
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValueSql: "'0'");
        }
    }
}
