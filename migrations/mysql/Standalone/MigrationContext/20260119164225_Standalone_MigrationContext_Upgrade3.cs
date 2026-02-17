using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.Migrations.CoreDb;
/// <inheritdoc />
public partial class MigrationContext_Upgrade3 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // remove storage, backup and AI tools
        migrationBuilder.DeleteData(
            table: "tenants_quota",
            keyColumn: "tenant",
            keyValues:
            [
                -11, -12, -13
            ]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}