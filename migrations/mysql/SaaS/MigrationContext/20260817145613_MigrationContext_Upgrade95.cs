using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade95 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_files_roomgroup_files_folder_internal_room_id",
                table: "files_roomgroup");

            migrationBuilder.AddForeignKey(
                name: "FK_files_roomgroup_files_folder_internal_room_id",
                table: "files_roomgroup",
                column: "internal_room_id",
                principalTable: "files_folder",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_files_roomgroup_files_folder_internal_room_id",
                table: "files_roomgroup");

            migrationBuilder.AddForeignKey(
                name: "FK_files_roomgroup_files_folder_internal_room_id",
                table: "files_roomgroup",
                column: "internal_room_id",
                principalTable: "files_folder",
                principalColumn: "id");
        }
    }
}
