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
/// DELETE files/share on a private room: somebody leaving it takes nothing of its keys along, its owner stays able to
/// open the files, and an administrator outside it has no way to empty it.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomLeaveTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private async Task<(FolderDto Room, int FileId)> CreateRoomWithEncryptedFile(User owner)
    {
        await _filesClient.Authenticate(owner);
        await SetFakeKeys(publicKeyPrefix: "owner");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        return (room, await UploadEncryptedFile(room.Id, "Encrypted.docx"));
    }

    private async Task RemoveShares(int roomId)
    {
        await _sharingApi.RemoveSecurityInfoAsync(new BaseBatchRequestDto(folderIds: [new(roomId)]), TestContext.Current.CancellationToken);
    }

    private async Task AssertLostTheRoom(User user, int roomId)
    {
        await _filesClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task MemberLeaving_LosesTheirKeysAndTheOthersKeepTheirs()
    {
        var leaving = await InviteWithKeys(EmployeeType.User);
        var staying = await InviteWithKeys(EmployeeType.User);
        var (room, fileId) = await CreateRoomWithEncryptedFile(Owner);
        await InviteToRoom(room.Id, leaving, FileShare.Read);
        await InviteToRoom(room.Id, staying, FileShare.Read);
        await WriteKeys(fileId, leaving.Id, staying.Id);

        await _filesClient.Authenticate(leaving);
        await RemoveShares(room.Id);

        await AssertLostTheRoom(leaving, room.Id);
        await _filesClient.Authenticate(Owner);
        (await GetMissing(room.Id, leaving.Id, staying.Id)).Select(m => m.UserId).Should().BeEquivalentTo([leaving.Id]);
    }

    [Fact]
    public async Task OwnerClearingTheAccess_RemovesTheKeysOfTheMembersAndKeepsTheirOwn()
    {
        var member = await InviteWithKeys(EmployeeType.User);
        var (room, fileId) = await CreateRoomWithEncryptedFile(Owner);
        await InviteToRoom(room.Id, member, FileShare.Read);
        await WriteKeys(fileId, Owner.Id, member.Id);

        await RemoveShares(room.Id);

        await AssertLostTheRoom(member, room.Id);
        await _filesClient.Authenticate(Owner);
        (await GetMissing(room.Id, Owner.Id, member.Id)).Select(m => m.UserId).Should().BeEquivalentTo([member.Id]);
    }

    [Fact]
    public async Task OwnerClearingTheAccess_StaysTheOwnerOfTheRoom()
    {
        var (room, fileId) = await CreateRoomWithEncryptedFile(Owner);
        await WriteKeys(fileId, Owner.Id);

        await RemoveShares(room.Id);

        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Should().Contain(s => s.IsOwner && s.SharedToUser != null && s.SharedToUser.Id == Owner.Id);
        (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response.CreatedBy.Id.Should().Be(Owner.Id);
        (await GetMissing(room.Id, Owner.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task OwnerSettingTheirOwnAccessToNone_StaysAndKeepsTheirKeys()
    {
        var (room, fileId) = await CreateRoomWithEncryptedFile(Owner);
        await WriteKeys(fileId, Owner.Id);

        await InviteToRoom(room.Id, Owner, FileShare.None);

        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Should().Contain(s => s.IsOwner);
        (await GetMissing(room.Id, Owner.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task ManagerSettingTheOwnersAccessToNone_LeavesTheOwnerAndTheirKeys()
    {
        var manager = await InviteWithKeys(EmployeeType.RoomAdmin);
        var creator = await InviteContact(EmployeeType.RoomAdmin);
        var (room, fileId) = await CreateRoomWithEncryptedFile(creator);
        await InviteToRoom(room.Id, manager, FileShare.RoomManager);
        await WriteKeys(fileId, creator.Id);

        await _filesClient.Authenticate(manager);
        await InviteToRoom(room.Id, creator, FileShare.None);

        await _filesClient.Authenticate(creator);
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Should().Contain(s => s.IsOwner && s.SharedToUser != null && s.SharedToUser.Id == creator.Id);
        (await GetMissing(room.Id, creator.Id)).Should().BeEmpty();
    }

    /// <summary>The owner normally holds no share row at all, which is what makes None a no-op; inviting themselves gives them one.</summary>
    [Fact]
    public async Task OwnerWithAShareRowOfTheirOwn_KeepsTheirKeysWhenItIsRemoved()
    {
        var (room, fileId) = await CreateRoomWithEncryptedFile(Owner);
        await WriteKeys(fileId, Owner.Id);
        (await ShareRowsOf(room.Id, Owner.Id)).Should().Be(0);

        await InviteToRoom(room.Id, Owner, FileShare.RoomManager);
        (await ShareRowsOf(room.Id, Owner.Id)).Should().Be(1);

        await InviteToRoom(room.Id, Owner, FileShare.None);

        (await ShareRowsOf(room.Id, Owner.Id)).Should().Be(0);
        (await GetMissing(room.Id, Owner.Id)).Should().BeEmpty();
    }

    private async Task<long> ShareRowsOf(int roomId, Guid userId)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT COUNT(*) FROM files_security WHERE entry_id = @room AND subject = @user;";
        command.Parameters.AddWithValue("@room", roomId.ToString());
        command.Parameters.AddWithValue("@user", userId.ToString());

        return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AdminOutsideTheRoom_CannotClearTheAccess()
    {
        var member = await InviteWithKeys(EmployeeType.User);
        var creator = await InviteContact(EmployeeType.RoomAdmin);
        var (room, fileId) = await CreateRoomWithEncryptedFile(creator);
        await InviteToRoom(room.Id, member, FileShare.Read);
        await WriteKeys(fileId, member.Id);

        await _filesClient.Authenticate(Owner);
        var exception = await Assert.ThrowsAsync<ApiException>(() => RemoveShares(room.Id));

        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(creator);
        (await GetMissing(room.Id, member.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task AdminOutsideAnOrdinaryRoom_StillClearsTheAccess()
    {
        var member = await InviteContact(EmployeeType.User);
        var creator = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(creator);
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title: "Autotest Room", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;
        await InviteToRoom(room.Id, member, FileShare.Read);

        await _filesClient.Authenticate(Owner);
        await RemoveShares(room.Id);

        await AssertLostTheRoom(member, room.Id);
    }
}
