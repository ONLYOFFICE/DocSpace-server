// (c) Copyright Ascensio System SIA 2009-2026
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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