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
/// Access control for <c>POST /api/2.0/files/share</c> (<c>GetSecurityInfo</c>): who may look up
/// the sharing rights of a file. Functional coverage lives in <see cref="GetSecurityInfoTests"/>.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class GetSecurityInfoPermissionsTests(
    AspireAppFixture fixture)
    : SharingTestBase(fixture)
{
    [Fact]
    public async Task GetSecurityInfo_Unauthenticated_Returns401()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);

        await _filesClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetSecurityInfoAsync(new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetSecurityInfo_Owner_OnOwnFile_Succeeds()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSecurityInfo_DocSpaceAdmin_OnRoomFile_Succeeds()
    {
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);

        var room = await CreateCollaborationRoom("Autotest Security Info Room");
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = docSpaceAdmin.Id, Access = FileShare.Editing }]
        }, TestContext.Current.CancellationToken);

        var file = await CreateFile("Autotest Security Info File.docx", room.Id);

        await _filesClient.Authenticate(docSpaceAdmin);
        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSecurityInfo_RoomAdmin_OnRoomFile_Succeeds()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        var room = await CreateCollaborationRoom("Autotest Security Info Room");
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = roomAdmin.Id, Access = FileShare.RoomManager }]
        }, TestContext.Current.CancellationToken);

        var file = await CreateFile("Autotest Security Info File.docx", room.Id);

        await _filesClient.Authenticate(roomAdmin);
        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSecurityInfo_UserWithFileAccess_Succeeds()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        await _filesClient.Authenticate(user);
        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().NotBeNull();
    }

    [Fact]
    [Trait("Bug", "82675")]
    public async Task GetSecurityInfo_UserWithoutFileAccess_Returns403()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetSecurityInfoAsync(new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetSecurityInfo_GuestWithFileAccess_Succeeds()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);
        var guest = await InviteGuest();

        await ShareFile(file.Id, guest.Id, FileShare.Read);

        await _filesClient.Authenticate(guest);
        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().NotBeNull();
    }

    [Fact]
    [Trait("Bug", "82675")]
    public async Task GetSecurityInfo_GuestWithoutFileAccess_Returns403()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);
        var guest = await InviteGuest();

        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetSecurityInfoAsync(new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
