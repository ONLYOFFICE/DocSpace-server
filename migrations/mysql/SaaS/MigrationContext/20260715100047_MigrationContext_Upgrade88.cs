using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade88 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_on",
                table: "files_file_vectorization",
                type: "datetime",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_deleted_on",
                table: "files_file_vectorization",
                column: "deleted_on");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_deleted_on",
                table: "files_file_vectorization");

            migrationBuilder.DropColumn(
                name: "deleted_on",
                table: "files_file_vectorization");
        }
    }
}
