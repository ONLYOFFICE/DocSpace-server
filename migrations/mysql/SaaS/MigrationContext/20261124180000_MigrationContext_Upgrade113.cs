using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <summary>
    /// Takes the guests made before guests were kept out of private rooms out of them, once: for every account in the
    /// guests group, its access records on private rooms and on the folders and files inside them, its wrapped file keys
    /// and its key pair (the <c>e2e</c> row of <c>account_links</c>). No audit record, revocation record or socket event
    /// is written: private rooms were not released, so the data is small (design decision 19 of
    /// private-rooms-server-followups). A guest owns no room, so no room changes owner. Running it again deletes nothing.
    /// </summary>
    public partial class MigrationContext_Upgrade113 : Migration
    {
        private const string GuestsGroup = "aced04fa-dd96-4b35-af3e-346bf1eb972d";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A folder record: the folder is a private room or lies in one (the tree holds a row of every folder for itself).
            migrationBuilder.Sql($@"
DELETE s FROM files_security s
JOIN core_usergroup g ON g.tenant = s.tenant_id AND g.userid = s.subject AND g.groupid = '{GuestsGroup}' AND g.ref_type = 0 AND g.removed = 0
WHERE s.entry_type = 1
  AND EXISTS (
    SELECT 1 FROM files_folder_tree t
    JOIN files_room_settings r ON r.tenant_id = s.tenant_id AND r.room_id = t.parent_id AND r.private = 1
    WHERE t.folder_id = s.internal_entry_id);");

            // A file record: the folder of the file is a private room or lies in one.
            migrationBuilder.Sql($@"
DELETE s FROM files_security s
JOIN core_usergroup g ON g.tenant = s.tenant_id AND g.userid = s.subject AND g.groupid = '{GuestsGroup}' AND g.ref_type = 0 AND g.removed = 0
WHERE s.entry_type = 2
  AND EXISTS (
    SELECT 1 FROM files_file f
    JOIN files_folder_tree t ON t.folder_id = f.folder_id
    JOIN files_room_settings r ON r.tenant_id = f.tenant_id AND r.room_id = t.parent_id AND r.private = 1
    WHERE f.tenant_id = s.tenant_id AND f.id = s.internal_entry_id);");

            // File keys exist only for the files of private rooms.
            migrationBuilder.Sql($@"
DELETE k FROM files_file_keys k
JOIN core_usergroup g ON g.tenant = k.tenant_id AND g.userid = k.user_id AND g.groupid = '{GuestsGroup}' AND g.ref_type = 0 AND g.removed = 0;");

            // An account id belongs to one portal, so the key pair has no tenant column of its own.
            migrationBuilder.Sql($@"
DELETE a FROM account_links a
JOIN core_usergroup g ON g.userid = a.id AND g.groupid = '{GuestsGroup}' AND g.ref_type = 0 AND g.removed = 0
WHERE a.provider = 'e2e';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The deleted records cannot be brought back.
        }
    }
}
