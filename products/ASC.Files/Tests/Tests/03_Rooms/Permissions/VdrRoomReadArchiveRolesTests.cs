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

namespace ASC.Files.Tests.Tests._03_Rooms.Permissions;

/// <summary>
/// Reading and retiring a Virtual Data Room by role: every VDR role reads the room info, the member list and the
/// room history; archiving and deleting the room are the owner's alone, a Room manager included in the refusal.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "VirtualDataRoom")]
public class VdrRoomReadArchiveRolesTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    public static TheoryData<FileShare> VdrMemberRoles => new()
    {
        FileShare.RoomManager,
        FileShare.ContentCreator,
        FileShare.Editing,
        FileShare.FillForms,
        FileShare.Read
    };

    [Theory]
    [MemberData(nameof(VdrMemberRoles))]
    public async Task VdrMemberRole_ReadsRoomInfoMemberListAndHistory(FileShare access)
    {
        // Arrange
        var (room, member) = await CreateVdrWithMember(access, $"Autotest VDR Read {access}");

        // Act
        await _filesClient.Authenticate(member);
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var shares = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Id.Should().Be(room.Id);
        info.RoomType.Should().Be(RoomType.VirtualDataRoom);
        shares.Should().Contain(s => s.SharedToUser != null && s.SharedToUser.Id == member.Id);
        shares.Should().Contain(s => s.SharedToUser != null && s.SharedToUser.Id == Owner.Id);
        history.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(VdrMemberRoles))]
    public async Task ArchiveAndDeleteRoom_VdrMemberRole_Returns403AndRoomStaysActive(FileShare access)
    {
        // Arrange
        var (room, member) = await CreateVdrWithMember(access, $"Autotest VDR Archive {access}");

        // Act
        await _filesClient.Authenticate(member);
        var archive = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));
        var delete = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        archive.ErrorCode.Should().Be(403);
        delete.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.RootFolderType.Should().Be(FolderType.VirtualRooms);
    }

    [Fact]
    public async Task ArchiveAndDeleteRoom_VdrOwner_RoomArchivedThenGone()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest VDR Archive Owner");

        // Act
        await ArchiveRoom(room.Id);
        var archived = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        archived.RootFolderType.Should().Be(FolderType.Archive);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);
    }

    private async Task<(FolderDto Room, User Member)> CreateVdrWithMember(FileShare access, string title)
    {
        var member = await InviteMember(access == FileShare.RoomManager ? EmployeeType.RoomAdmin : EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom(title);
        await InviteToRoom(room.Id, member, access);

        return (room, member);
    }
}
