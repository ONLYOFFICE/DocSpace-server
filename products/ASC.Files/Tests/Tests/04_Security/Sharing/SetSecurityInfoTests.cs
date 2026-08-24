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
/// The two share-writing endpoints: <c>PUT /api/2.0/files/share</c> (<c>SetSecurityInfo</c>,
/// batch) and <c>PUT /api/2.0/files/file/{fileId}/share</c> (<c>SetFileSecurityInfo</c>, single
/// file) - who may call them and what a malformed payload does.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class SetSecurityInfoTests(
    AspireAppFixture fixture)
    : SharingTestBase(fixture)
{
    [Fact]
    [Trait("Bug", "79284")]
    public async Task SetSecurityInfo_EmptyAccessField_IgnoresEntryAndReturns200()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Share File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        // Act - "access" is not expressible through the typed FileShare enum, so this goes over
        // raw HTTP; that is the one carve-out this suite needs.
        var json = JsonSerializer.Serialize(new
        {
            fileIds = new[] { file.Id },
            share = new[] { new { shareTo = user.Id, access = "" } },
            notify = true
        });

        var (statusCode, body) = await PutShareRaw(json);

        // Assert
        statusCode.Should().Be(HttpStatusCode.OK);
        body.RootElement.GetProperty("statusCode").GetInt32().Should().Be(200);
        body.RootElement.GetProperty("response").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task SetSecurityInfo_User_CannotSetSecurityInfoOnOwnerFile_Returns403()
    {
        var file = await CreateFileInMy("Autotest Share File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.SetSecurityInfoAsync(new SecurityInfoRequestDto
            {
                FileIds = [new(file.Id)],
                Share = [new() { ShareTo = Owner.Id, Access = FileShare.ReadWrite }],
                Notify = false
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    [Trait("Bug", "83263")]
    public async Task SetSecurityInfo_Guest_CannotSetSecurityInfoOnOwnerFile_Returns403()
    {
        var file = await CreateFileInMy("Autotest Share File.docx", Owner);
        var guest = await InviteGuest();

        await ShareFile(file.Id, guest.Id, FileShare.Read);

        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.SetSecurityInfoAsync(new SecurityInfoRequestDto
            {
                FileIds = [new(file.Id)],
                Share = [new() { ShareTo = Owner.Id, Access = FileShare.ReadWrite }],
                Notify = false
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var securityInfos = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var guestEntry = securityInfos.FirstOrDefault(e => e.SharedToUser?.Id == guest.Id);
        guestEntry.Should().NotBeNull();
        guestEntry!.Access.Should().Be(FileShare.Read);

        securityInfos.Should().NotContain(e => e.SharedToUser != null && e.SharedToUser.Id == Owner.Id && !e.IsOwner);
    }

    [Fact]
    [Trait("Bug", "79156")]
    public async Task SetFileSecurityInfo_SharingMessageTooLong_Returns400()
    {
        var file = await CreateFileInMy("Autotest Share File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        var longMessage = new string('a', 256);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.SetFileSecurityInfoAsync(file.Id, new SecurityInfoSimpleRequestDto
            {
                Share = [new() { ShareTo = user.Id, Access = FileShare.Read }],
                Notify = true,
                SharingMessage = longMessage
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetFileSecurityInfo_User_CannotSetFileSecurityInfoOnOwnerFile_Returns403()
    {
        var file = await CreateFileInMy("Autotest Share File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await ShareFile(file.Id, Owner.Id, FileShare.ReadWrite));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    [Trait("Bug", "83263")]
    public async Task SetFileSecurityInfo_Guest_CannotSetFileSecurityInfoOnOwnerFile_Returns403()
    {
        var file = await CreateFileInMy("Autotest Share File.docx", Owner);
        var guest = await InviteGuest();

        await ShareFile(file.Id, guest.Id, FileShare.Read);

        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await ShareFile(file.Id, Owner.Id, FileShare.ReadWrite));

        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var securityInfos = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var guestEntry = securityInfos.FirstOrDefault(e => e.SharedToUser?.Id == guest.Id);
        guestEntry.Should().NotBeNull();
        guestEntry!.Access.Should().Be(FileShare.Read);

        securityInfos.Should().NotContain(e => e.SharedToUser != null && e.SharedToUser.Id == Owner.Id && !e.IsOwner);
    }

    [Fact]
    public async Task SetFileSecurityInfo_RoomAdmin_CannotShareFileWithGuestBelongingToAnotherUser()
    {
        var guest = await InviteGuest();
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        var room = await CreateCustomRoom("Autotest Share File Guest");
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = guest.Id, Access = FileShare.Read }, new() { Id = roomAdmin.Id, Access = FileShare.RoomManager }]
        }, TestContext.Current.CancellationToken);

        var file = await CreateFile("Autotest Share File.docx", room.Id);

        await _filesClient.Authenticate(roomAdmin);
        var securityInfos = await ShareFile(file.Id, guest.Id, FileShare.Read);

        securityInfos.Select(e => e.SharedToUser?.Id).Should().NotContain(guest.Id);
    }
}
