using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.Migrations.CoreDb;
/// <inheritdoc />
public partial class MigrationContext_Upgrade1 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.UpdateData(
            table: "tenants_quota",
            keyColumn: "tenant",
            keyValue: -1,
            column: "features",
            value: "audit,ldap,sso,whitelabel,thirdparty,restore,oauth,contentsearch,file_size:102400,statistic");

        migrationBuilder.UpdateData(
            table: "tenants_quota",
            keyColumn: "tenant",
            keyValue: -1000,
            column: "features",
            value: "audit,ldap,sso,whitelabel,thirdparty,restore,oauth,contentsearch,file_size:102400,docs,customization,statistic");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.UpdateData(
            table: "tenants_quota",
            keyColumn: "tenant",
            keyValue: -1,
            column: "features",
            value: "audit,ldap,sso,whitelabel,thirdparty,restore,oauth,contentsearch,file_size:102400");

        migrationBuilder.UpdateData(
            table: "tenants_quota",
            keyColumn: "tenant",
            keyValue: -1000,
            column: "features",
            value: "audit,ldap,sso,whitelabel,thirdparty,restore,oauth,contentsearch,file_size:102400,docs,customization");
    }
}