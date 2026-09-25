using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade99 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "folder_type",
                table: "files_group",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Until now the section of a room group was derived from the rooms it holds: a group whose
            // rooms are all form-filling rooms showed up in Forms, everything else in Rooms. The column
            // has to start out saying exactly that, otherwise every existing Forms group would move to
            // Rooms on upgrade. A group that references a third-party room stays in Rooms: that room's
            // type lives in files_thirdparty_account and its id here is a composite provider id, so the
            // two cannot be joined in SQL.
            migrationBuilder.Sql(
                """
                UPDATE files_group g
                SET g.folder_type = 15
                WHERE EXISTS (
                        SELECT 1 FROM files_roomgroup r
                        WHERE r.tenant_id = g.tenant_id AND r.group_id = g.id)
                  AND NOT EXISTS (
                        SELECT 1 FROM files_roomgroup r
                        LEFT JOIN files_folder f ON f.tenant_id = r.tenant_id AND f.id = r.internal_room_id
                        WHERE r.tenant_id = g.tenant_id AND r.group_id = g.id
                          AND (f.folder_type IS NULL OR f.folder_type <> 15));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "folder_type",
                table: "files_group");
        }
    }
}
