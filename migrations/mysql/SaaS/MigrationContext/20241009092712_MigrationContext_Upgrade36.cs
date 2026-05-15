// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

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