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

namespace ASC.Files.Tests.Tests._06_Operations.CheckDestFolder;

[Trait("Category", "Bug")]
[Trait("Feature", "Files")]
public class CheckDestFolderBugTests(
    AspireAppFixture fixture)
    : CheckDestFolderTestBase(fixture)
{
    /// <summary>
    /// BUG 82103: checkdestfolder accepted an archived room as destination instead of answering 403.
    /// Fixed by checking <c>CanCreateAsync</c> on the destination in
    /// <c>MoveOrCopyDestFolderCheckAsync</c>.
    /// </summary>
    [Fact]
    [Trait("Bug", "82103")]
    public async Task CheckDestFolder_ArchivedRoom_Returns403()
    {
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest CheckDestFolder Archived Dest.docx", myDocsFolderId);

        var room = await CreateCustomRoom("Autotest CheckDestFolder Archived Room");
        await ArchiveRoom(room.Id);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CheckDestFolder(fileIds: [file.Id], destFolderId: room.Id));

        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// BUG 82158: a non-existent destination folder did not yield 404. Fixed by throwing
    /// <c>ItemNotFoundException</c> for a null destination folder in
    /// <c>MoveOrCopyDestFolderCheckAsync</c>.
    /// </summary>
    [Fact]
    [Trait("Bug", "82158")]
    public async Task CheckDestFolder_NonExistentDestFolderId_Returns404()
    {
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest CheckDestFolder NonExistent Dest.docx", myDocsFolderId);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CheckDestFolder(fileIds: [file.Id], destFolderId: 999999999));

        exception.ErrorCode.Should().Be(404);
    }

    /// <summary>
    /// BUG 82159: omitting destFolderId did not yield 400. Fixed by rejecting a missing
    /// destFolderId with <c>ArgumentException</c> in
    /// <c>OperationController.CheckMoveOrCopyDestFolder</c>.
    /// </summary>
    [Fact]
    [Trait("Bug", "82159")]
    public async Task CheckDestFolder_NoDestFolderIdSpecified_Returns400()
    {
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest CheckDestFolder No Dest.docx", myDocsFolderId);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CheckDestFolder(fileIds: [file.Id]));

        exception.ErrorCode.Should().Be(400);
    }

    /// <summary>
    /// BUG 82104: a user with no access to the destination room was not rejected with 403. Fixed by
    /// checking <c>CanCreateAsync</c> on the destination in <c>MoveOrCopyDestFolderCheckAsync</c>.
    /// </summary>
    [Fact]
    [Trait("Bug", "82104")]
    public async Task CheckDestFolder_UserWithoutDestAccess_Returns403()
    {
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        var userMyDocsFolderId = await GetUserFolderIdAsync(user);
        var file = await CreateFile("Autotest CheckDestFolder Perm NoAccess File.docx", userMyDocsFolderId);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest CheckDestFolder Perm NoAccess Room");

        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CheckDestFolder(fileIds: [file.Id], destFolderId: room.Id));

        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// BUG 82104: same for guests — no 403 for an inaccessible destination. Fixed by the
    /// <c>CanCreateAsync</c> check in <c>MoveOrCopyDestFolderCheckAsync</c>.
    /// </summary>
    [Fact]
    [Trait("Bug", "82104")]
    public async Task CheckDestFolder_GuestWithoutDestAccess_Returns403()
    {
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest CheckDestFolder Perm Guest File.docx", myDocsFolderId);

        var room = await CreateCustomRoom("Autotest CheckDestFolder Perm Guest Room");

        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CheckDestFolder(fileIds: [file.Id], destFolderId: room.Id, deleteAfter: false));

        exception.ErrorCode.Should().Be(403);
    }
}
