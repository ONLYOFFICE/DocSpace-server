using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade42 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "dump",
                table: "backup_schedule",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValueSql: "'0'");

            migrationBuilder.Sql("ALTER TABLE `backup_schedule`\r\n\tDROP PRIMARY KEY,\r\n\tADD PRIMARY KEY (`tenant_id`, `dump`) USING BTREE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE `backup_schedule`\r\n\tDROP PRIMARY KEY,\r\n\tADD PRIMARY KEY (`tenant_id`) USING BTREE;");

            migrationBuilder.AlterColumn<bool>(
                name: "dump",
                table: "backup_schedule",
                type: "tinyint(1)",
                nullable: false,
                defaultValueSql: "'0'",
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");
        }
    }
}
