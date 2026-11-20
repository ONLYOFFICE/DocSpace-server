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

namespace ASC.Files.Tests.Tests._08_Privacy;

/// <summary>
/// Who may give a private room a new owner besides its owner: a DocSpace admin who is a member of the room, whatever
/// their role there, and no administrator from outside. The new owner still has to hold every file key.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomHandOverTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    /// <summary>A private room of a room admin, with an encrypted file and an heir who manages it and holds the file's key.</summary>
    private async Task<(FolderDto Room, User RoomOwner, User Heir)> RoomWithAnHeir()
    {
        var heir = await InviteWithKeys(EmployeeType.RoomAdmin);
        var roomOwner = await InviteWithKeys(EmployeeType.RoomAdmin);
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
        var fileId = await UploadEncryptedFile(room.Id, "Encrypted.docx");
        await InviteToRoom(room.Id, heir, FileShare.RoomManager);
        (await WriteKeys(fileId, heir.Id)).Should().BeEmpty();

        return (room, roomOwner, heir);
    }

    private async Task ChangeOwner(int roomId, Guid userId)
    {
        await _sharingApi.ChangeFileOwnerAsync(
            new ChangeOwnerRequestDto(folderIds: [new(roomId)], userId: userId),
            TestContext.Current.CancellationToken);
    }

    private async Task<Guid> RoomOwnerAs(User reader, int roomId)
    {
        await _filesClient.Authenticate(reader);

        return (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response.CreatedBy.Id;
    }

    [Fact]
    public async Task MemberAdministrator_HandsTheRoomToAKeyHolder()
    {
        // The weakest role a member has: the administrator only views the room.
        var administrator = await InviteWithKeys(EmployeeType.DocSpaceAdmin);
        var (room, _, heir) = await RoomWithAnHeir();
        await InviteToRoom(room.Id, administrator, FileShare.Read);

        await _filesClient.Authenticate(administrator);
        await ChangeOwner(room.Id, heir.Id);

        (await RoomOwnerAs(heir, room.Id)).Should().Be(heir.Id);
    }

    [Fact]
    public async Task MemberAdministrator_CannotGiveTheRoomToSomebodyWithoutTheFileKey()
    {
        var administrator = await InviteWithKeys(EmployeeType.DocSpaceAdmin);
        var keyless = await InviteWithKeys(EmployeeType.RoomAdmin);
        var (room, roomOwner, _) = await RoomWithAnHeir();
        await InviteToRoom(room.Id, administrator, FileShare.Read);
        await InviteToRoom(room.Id, keyless, FileShare.RoomManager);

        await _filesClient.Authenticate(administrator);
        var exception = await Assert.ThrowsAsync<ApiException>(() => ChangeOwner(room.Id, keyless.Id));

        exception.ErrorCode.Should().Be(409);
        (await RoomOwnerAs(roomOwner, room.Id)).Should().Be(roomOwner.Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AdministratorOutside_IsRefused(bool portalOwner)
    {
        var administrator = portalOwner ? Owner : await InviteWithKeys(EmployeeType.DocSpaceAdmin);
        var (room, roomOwner, heir) = await RoomWithAnHeir();

        await _filesClient.Authenticate(administrator);
        var exception = await Assert.ThrowsAsync<ApiException>(() => ChangeOwner(room.Id, heir.Id));

        exception.ErrorCode.Should().Be(403);
        (await RoomOwnerAs(roomOwner, room.Id)).Should().Be(roomOwner.Id);
    }
}
