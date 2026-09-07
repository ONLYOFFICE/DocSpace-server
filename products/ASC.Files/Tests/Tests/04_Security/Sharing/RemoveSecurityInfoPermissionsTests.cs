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

namespace ASC.Files.Tests.Tests._04_Security.Sharing;

/// <summary>
/// Access control for <c>DELETE /api/2.0/files/share</c> (<c>RemoveSecurityInfo</c>): who may
/// revoke sharing rights on someone else's file. Functional coverage lives in
/// <see cref="RemoveSecurityInfoTests"/>.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class RemoveSecurityInfoPermissionsTests(
    AspireAppFixture fixture)
    : SharingTestBase(fixture)
{
    [Fact]
    public async Task RemoveSecurityInfo_Unauthenticated_Returns401()
    {
        var file = await CreateFileInMy("Autotest Remove Security File.docx", Owner);

        await _filesClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.RemoveSecurityInfoAsync(new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task RemoveSecurityInfo_Owner_RemovesOwnFile_Returns200()
    {
        var file = await CreateFileInMy("Autotest Remove Security File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        var result = (await _sharingApi.RemoveSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveSecurityInfo_DocSpaceAdmin_RemovesOwnerFile_Returns200()
    {
        var file = await CreateFileInMy("Autotest Remove Security File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(docSpaceAdmin);

        var result = (await _sharingApi.RemoveSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveSecurityInfo_RoomAdmin_RemovesFileInOwnRoom_Returns200()
    {
        var room = await CreateCollaborationRoom("Autotest Remove Security Room");
        var file = await CreateFile("Autotest Remove Security File.docx", room.Id);

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = roomAdmin.Id, Access = FileShare.RoomManager }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(roomAdmin);
        var result = (await _sharingApi.RemoveSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        result.Should().BeTrue();
    }

    /// <summary>
    /// BUG 83262: a user without set-access rights got 200 from RemoveSecurityInfo on someone else's
    /// file. Fixed in the <c>FileStorageService.RemoveAceAsync</c> rewrite — callers who cannot set
    /// access are rejected unless their only access is via an external link (self-removal).
    /// </summary>
    [Fact]
    [Trait("Bug", "83262")]
    public async Task RemoveSecurityInfo_User_CannotRemoveSharingFromOwnerFile_Returns403()
    {
        var file = await CreateFileInMy("Autotest Remove Security File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.RemoveSecurityInfoAsync(new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// BUG 83262: a read-only guest got 200 from RemoveSecurityInfo on the owner's file. Fixed in
    /// the <c>FileStorageService.RemoveAceAsync</c> rewrite rejecting callers who cannot set access.
    /// </summary>
    [Fact]
    [Trait("Bug", "83262")]
    public async Task RemoveSecurityInfo_Guest_CannotRemoveSharingFromOwnerFile_Returns403()
    {
        var file = await CreateFileInMy("Autotest Remove Security File.docx", Owner);
        var guest = await InviteGuest();

        await ShareFile(file.Id, guest.Id, FileShare.Read);

        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.RemoveSecurityInfoAsync(new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// BUG 83262: an unrelated user could target another user's file by id (IDOR) and got 200.
    /// Fixed in the <c>FileStorageService.RemoveAceAsync</c> rewrite rejecting callers who cannot
    /// set access.
    /// </summary>
    [Fact]
    [Trait("Bug", "83262")]
    public async Task RemoveSecurityInfo_User_CannotRemoveSharingFromAnotherUsersFile_Idor_Returns403()
    {
        var file = await CreateFileInMy("Autotest Remove Security File.docx", Owner);
        var targetUser = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, targetUser.Id, FileShare.Read);

        var attacker = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(attacker);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.RemoveSecurityInfoAsync(new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
