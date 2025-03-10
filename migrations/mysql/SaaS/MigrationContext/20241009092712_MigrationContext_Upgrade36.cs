using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade36 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE files_security
                SET security = 11
                WHERE subject_type = 3 AND security = 9;"
            );

            migrationBuilder.Sql(@"
                SET @GroupEveryone = 'c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e';
                SET @GroupGuest = 'aced04fa-dd96-4b35-af3e-346bf1eb972d';
                SET @GroupRoomAdmin = 'abef62db-11a8-4673-9d32-ef1d8af19dc0';
                SET @GroupDocSpaceAdmin = 'cd84e66b-b803-40fc-99f9-b2969a54a1de';
                SET @GroupUser = '88f11e7c-7407-4bea-b4cb-070010cdbb6b';

                CREATE TEMPORARY TABLE temp_guests AS
                SELECT userid
                FROM core_usergroup
                WHERE groupid = @GroupGuest AND removed = 0;

                DELETE FROM core_usergroup
                WHERE userid IN (SELECT userid FROM temp_guests) 
                    AND groupid NOT IN (
                                        @GroupEveryone,
                                        @GroupGuest,
                                        @GroupRoomAdmin,
                                        @GroupDocSpaceAdmin,
                                        @GroupUser
                                        )
                    AND removed = 0;

                DROP TEMPORARY TABLE temp_guests;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
