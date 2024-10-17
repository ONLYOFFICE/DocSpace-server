using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade35 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO core_usergroup (userid, groupid, ref_type, removed, tenant, last_modified)
                SELECT userid, '88f11e7c-7407-4bea-b4cb-070010cdbb6b', 0, 0, tenant, CURRENT_TIMESTAMP
                FROM core_usergroup
                WHERE groupid = 'aced04fa-dd96-4b35-af3e-346bf1eb972d'
                AND ref_type = 0
                AND removed = 0
                ON DUPLICATE KEY UPDATE removed = 0, last_modified = CURRENT_TIMESTAMP;
                
                DELETE FROM core_usergroup
                WHERE groupid = 'aced04fa-dd96-4b35-af3e-346bf1eb972d'
                AND ref_type = 0
                AND removed = 0;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
