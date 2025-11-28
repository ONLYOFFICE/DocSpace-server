using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade67 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "internal_entry_id",
                table: "files_security",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "tenant_id_internal_entry_id",
                table: "files_security",
                columns: new[] { "tenant_id", "internal_entry_id" });

            migrationBuilder.Sql("update ignore files_security set internal_entry_id = CAST(entry_id AS signed)");
            // migrationBuilder.UpdateData(
            //     table: "files_security",
            //     keyColumn: "internal_entry_id",
            //     keyValue: 0,
            //     column: "internal_entry_id",
            //     value: "cast(entry_id AS SIGNED)"
            // );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "tenant_id_internal_entry_id",
                table: "files_security");

            migrationBuilder.DropColumn(
                name: "internal_entry_id",
                table: "files_security");
        }
    }
}
