using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade22 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "files_converts",
                columns: new[] { "input", "output" },
                values: new object[,]
                {
                    { ".djvu", ".pdf" },
                    { ".oform", ".pdf" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".djvu", ".pdf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".oform", ".pdf" });
        }
    }
}
