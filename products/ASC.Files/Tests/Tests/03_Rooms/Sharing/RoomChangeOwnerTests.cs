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

namespace ASC.Files.Tests.Tests._03_Rooms.Sharing;

/// <summary>
/// <c>changeFileOwner</c> over a room: the previous owner keeps the room as its Room manager, and a room the target
/// already owns is returned untouched.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomChangeOwnerTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task ChangeFileOwner_Room_PreviousOwnerStaysAsRoomManager()
    {
        // Arrange
        var previous = await InviteContact(EmployeeType.RoomAdmin);
        var target = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(previous);
        var room = await CreateCustomRoom("Autotest Owner Handover");

        // Act
        var result = (await _sharingApi.ChangeFileOwnerAsync(
            new ChangeOwnerRequestDto(folderIds: [new BatchRequestDtoAllOfFileIds(room.Id)], userId: target.Id),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        result.OfType<FolderDto>().Should().ContainSingle(f => f.Id == room.Id).Which.CreatedBy.Id.Should().Be(target.Id);

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.CreatedBy.Id.Should().Be(target.Id, "the previous owner can still read the room");

        var shares = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        shares.Should().ContainSingle(s => s.SharedToUser != null && s.SharedToUser.Id == target.Id).Which.IsOwner.Should().BeTrue();
        var kept = shares.Should().ContainSingle(s => s.SharedToUser != null && s.SharedToUser.Id == previous.Id).Which;
        kept.IsOwner.Should().BeFalse();
        kept.Access.Should().Be(FileShare.RoomManager);
    }

    [Fact]
    public async Task ChangeFileOwner_RoomAlreadyOwnedByTarget_IsReturnedUntouched()
    {
        // Arrange
        var previous = await InviteContact(EmployeeType.RoomAdmin);
        var target = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(previous);
        var room = await CreateCustomRoom("Autotest Owner Repeat");
        await _sharingApi.ChangeFileOwnerAsync(
            new ChangeOwnerRequestDto(folderIds: [new BatchRequestDtoAllOfFileIds(room.Id)], userId: target.Id),
            TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        var before = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Act
        var result = (await _sharingApi.ChangeFileOwnerAsync(
            new ChangeOwnerRequestDto(folderIds: [new BatchRequestDtoAllOfFileIds(room.Id)], userId: target.Id),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        result.OfType<FolderDto>().Should().ContainSingle(f => f.Id == room.Id).Which.CreatedBy.Id.Should().Be(target.Id);

        var after = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        after.Where(s => s.SharedToUser != null).Select(s => (s.SharedToUser.Id, s.Access, s.IsOwner))
            .Should().BeEquivalentTo(before.Where(s => s.SharedToUser != null).Select(s => (s.SharedToUser.Id, s.Access, s.IsOwner)));
    }
}
