using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade56 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "files_converts",
                columns: new[] { "input", "output" },
                values: new object[,]
                {
                    { ".hwp", ".docm" },
                    { ".hwp", ".docx" },
                    { ".hwp", ".dotm" },
                    { ".hwp", ".dotx" },
                    { ".hwp", ".epub" },
                    { ".hwp", ".fb2" },
                    { ".hwp", ".html" },
                    { ".hwp", ".odt" },
                    { ".hwp", ".ott" },
                    { ".hwp", ".pdf" },
                    { ".hwp", ".rtf" },
                    { ".hwp", ".txt" },
                    { ".hwpx", ".docm" },
                    { ".hwpx", ".docx" },
                    { ".hwpx", ".dotm" },
                    { ".hwpx", ".dotx" },
                    { ".hwpx", ".epub" },
                    { ".hwpx", ".fb2" },
                    { ".hwpx", ".html" },
                    { ".hwpx", ".odt" },
                    { ".hwpx", ".ott" },
                    { ".hwpx", ".pdf" },
                    { ".hwpx", ".rtf" },
                    { ".hwpx", ".txt" },
                    { ".key", ".odp" },
                    { ".key", ".otp" },
                    { ".key", ".pdf" },
                    { ".key", ".potm" },
                    { ".key", ".potx" },
                    { ".key", ".ppsm" },
                    { ".key", ".ppsx" },
                    { ".key", ".pptm" },
                    { ".key", ".pptx" },
                    { ".md", ".docm" },
                    { ".md", ".docx" },
                    { ".md", ".dotm" },
                    { ".md", ".dotx" },
                    { ".md", ".epub" },
                    { ".md", ".fb2" },
                    { ".md", ".html" },
                    { ".md", ".odt" },
                    { ".md", ".ott" },
                    { ".md", ".pdf" },
                    { ".md", ".rtf" },
                    { ".md", ".txt" },
                    { ".numbers", ".csv" },
                    { ".numbers", ".ods" },
                    { ".numbers", ".ots" },
                    { ".numbers", ".pdf" },
                    { ".numbers", ".xlsm" },
                    { ".numbers", ".xlsx" },
                    { ".numbers", ".xltm" },
                    { ".numbers", ".xltx" },
                    { ".odg", ".odp" },
                    { ".odg", ".otp" },
                    { ".odg", ".pdf" },
                    { ".odg", ".potm" },
                    { ".odg", ".potx" },
                    { ".odg", ".ppsm" },
                    { ".odg", ".ppsx" },
                    { ".odg", ".pptm" },
                    { ".odg", ".pptx" },
                    { ".pages", ".docm" },
                    { ".pages", ".docx" },
                    { ".pages", ".dotm" },
                    { ".pages", ".dotx" },
                    { ".pages", ".epub" },
                    { ".pages", ".fb2" },
                    { ".pages", ".html" },
                    { ".pages", ".odt" },
                    { ".pages", ".ott" },
                    { ".pages", ".pdf" },
                    { ".pages", ".rtf" },
                    { ".pages", ".txt" },
                    { ".vsdm", ".bmp" },
                    { ".vsdm", ".gif" },
                    { ".vsdm", ".jpg" },
                    { ".vsdm", ".pdf" },
                    { ".vsdm", ".pdfa" },
                    { ".vsdm", ".png" },
                    { ".vsdx", ".bmp" },
                    { ".vsdx", ".gif" },
                    { ".vsdx", ".jpg" },
                    { ".vsdx", ".pdf" },
                    { ".vsdx", ".pdfa" },
                    { ".vsdx", ".png" },
                    { ".vssm", ".bmp" },
                    { ".vssm", ".gif" },
                    { ".vssm", ".jpg" },
                    { ".vssm", ".pdf" },
                    { ".vssm", ".pdfa" },
                    { ".vssm", ".png" },
                    { ".vssx", ".bmp" },
                    { ".vssx", ".gif" },
                    { ".vssx", ".jpg" },
                    { ".vssx", ".pdf" },
                    { ".vssx", ".pdfa" },
                    { ".vssx", ".png" },
                    { ".vstm", ".bmp" },
                    { ".vstm", ".gif" },
                    { ".vstm", ".jpg" },
                    { ".vstm", ".pdf" },
                    { ".vstm", ".pdfa" },
                    { ".vstm", ".png" },
                    { ".vstx", ".bmp" },
                    { ".vstx", ".gif" },
                    { ".vstx", ".jpg" },
                    { ".vstx", ".pdf" },
                    { ".vstx", ".pdfa" },
                    { ".vstx", ".png" }
                });

            migrationBuilder.Sql(
                @"UPDATE files_file f
                    SET f.category = 25
                    WHERE title LIKE '%.vsdm'
                       OR title LIKE '%.vsdx'
                       OR title LIKE '%.vssm'
                       OR title LIKE '%.vssx'
                       OR title LIKE '%.vstm'
                       OR title LIKE '%.vstx';"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwp", ".docm" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwp", ".docx" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwp", ".dotm" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwp", ".dotx" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwp", ".epub" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwp", ".fb2" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwp", ".html" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwp", ".odt" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwp", ".ott" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwp", ".pdf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwp", ".rtf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwp", ".txt" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwpx", ".docm" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwpx", ".docx" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwpx", ".dotm" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwpx", ".dotx" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwpx", ".epub" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwpx", ".fb2" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwpx", ".html" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwpx", ".odt" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwpx", ".ott" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwpx", ".pdf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwpx", ".rtf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".hwpx", ".txt" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".key", ".odp" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".key", ".otp" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".key", ".pdf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".key", ".potm" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".key", ".potx" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".key", ".ppsm" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".key", ".ppsx" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".key", ".pptm" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".key", ".pptx" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".md", ".docm" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".md", ".docx" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".md", ".dotm" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".md", ".dotx" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".md", ".epub" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".md", ".fb2" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".md", ".html" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".md", ".odt" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".md", ".ott" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".md", ".pdf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".md", ".rtf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".md", ".txt" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".numbers", ".csv" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".numbers", ".ods" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".numbers", ".ots" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".numbers", ".pdf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".numbers", ".xlsm" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".numbers", ".xlsx" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".numbers", ".xltm" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".numbers", ".xltx" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".odg", ".odp" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".odg", ".otp" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".odg", ".pdf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".odg", ".potm" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".odg", ".potx" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".odg", ".ppsm" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".odg", ".ppsx" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".odg", ".pptm" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".odg", ".pptx" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".pages", ".docm" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".pages", ".docx" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".pages", ".dotm" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".pages", ".dotx" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".pages", ".epub" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".pages", ".fb2" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".pages", ".html" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".pages", ".odt" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".pages", ".ott" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".pages", ".pdf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".pages", ".rtf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".pages", ".txt" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vsdm", ".bmp" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vsdm", ".gif" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vsdm", ".jpg" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vsdm", ".pdf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vsdm", ".pdfa" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vsdm", ".png" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vsdx", ".bmp" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vsdx", ".gif" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vsdx", ".jpg" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vsdx", ".pdf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vsdx", ".pdfa" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vsdx", ".png" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vssm", ".bmp" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vssm", ".gif" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vssm", ".jpg" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vssm", ".pdf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vssm", ".pdfa" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vssm", ".png" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vssx", ".bmp" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vssx", ".gif" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vssx", ".jpg" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vssx", ".pdf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vssx", ".pdfa" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vssx", ".png" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vstm", ".bmp" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vstm", ".gif" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vstm", ".jpg" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vstm", ".pdf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vstm", ".pdfa" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vstm", ".png" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vstx", ".bmp" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vstx", ".gif" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vstx", ".jpg" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vstx", ".pdf" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vstx", ".pdfa" });

            migrationBuilder.DeleteData(
                table: "files_converts",
                keyColumns: new[] { "input", "output" },
                keyValues: new object[] { ".vstx", ".png" });
        }
    }
}