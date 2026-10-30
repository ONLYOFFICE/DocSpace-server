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

namespace ASC.Files.Tests.Tests._06_Operations.CheckPreflight;

/// <summary>
/// Access control of the two preflight endpoints. Both need the right to create in the destination,
/// and neither may reveal anything about a file the caller cannot read. The role/anonymous matrix of
/// each endpoint lives in <c>CheckMoveCopy</c> and <c>CheckDestFolder</c>.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class CheckPreflightPermissionsTests(
    AspireAppFixture fixture)
    : CheckPreflightTestBase(fixture)
{
    [Theory]
    [InlineData(PreflightKind.Move)]
    [InlineData(PreflightKind.CheckDestFolder)]
    public async Task Preflight_UserWithReadAccessToTheRoom_GetsForbiddenForItAsDestination(PreflightKind kind)
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile($"Autotest Preflight Reader {kind}.docx", myDocsFolderId);
        var room = await CreateCustomRoom($"Autotest Preflight Reader Room {kind}");
        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        // Act
        await _filesClient.Authenticate(user);
        var response = await Preflight(kind, fileIds: [file.Id], destFolderId: room.Id);

        // Assert
        response.Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(PreflightKind.Move)]
    [InlineData(PreflightKind.CheckDestFolder)]
    public async Task Preflight_FileTheCallerCannotRead_IsNotRevealed(PreflightKind kind)
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var title = $"Private Plan {kind}.docx";
        var privateFile = await CreateFile(title, myDocsFolderId);
        var room = await CreateCustomRoom($"Autotest Preflight Private Room {kind}");
        await CreateFile(title, room.Id);
        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.ContentCreator);

        // Positive control: the owner, who can read the file, gets it back (a clash, or an accepted file).
        var control = await Preflight(kind, fileIds: [privateFile.Id], destFolderId: room.Id);
        control.Status.Should().Be(HttpStatusCode.OK);
        IdsOf(control).Should().Equal(privateFile.Id);

        // Act
        await _filesClient.Authenticate(user);
        var response = await Preflight(kind, fileIds: [privateFile.Id], destFolderId: room.Id);

        // Assert
        ((int)response.Status).Should().BeLessThan(500);
        response.Entries.Should().BeEmpty();
        response.Body.Should().NotContain("Private Plan");
    }
}
