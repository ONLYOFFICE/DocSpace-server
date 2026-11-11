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

    /// <summary>
    /// Changing the owner of a room the caller does not own, by caller type: the Owner and a DocSpace admin
    /// hand a Room admin's room to another Room admin, who becomes the owner while the previous owner stays as
    /// Room manager; a User or a Guest who are members of the room get 403 and the owner is unchanged.
    /// <paramref name="actorType"/> null is the portal Owner.
    /// </summary>
    [Theory]
    [InlineData(null, true)]
    [InlineData(EmployeeType.DocSpaceAdmin, true)]
    [InlineData(EmployeeType.User, false)]
    [InlineData(EmployeeType.Guest, false)]
    public async Task ChangeFileOwner_ForeignRoom_ByCallerType(EmployeeType? actorType, bool allowed)
    {
        // Arrange
        var previous = await InviteContact(EmployeeType.RoomAdmin);
        var target = await InviteContact(EmployeeType.RoomAdmin);
        var actor = actorType switch
        {
            null => Owner,
            EmployeeType.Guest => await InviteGuest(),
            { } type => await InviteContact(type)
        };

        await _filesClient.Authenticate(previous);
        var room = await CreateCustomRoom($"Autotest Foreign Owner Change {actorType?.ToString() ?? "Owner"}");

        if (actorType is EmployeeType.User or EmployeeType.Guest)
        {
            await InviteToRoom(room.Id, actor, FileShare.Read);
        }

        await _filesClient.Authenticate(actor);
        var request = new ChangeOwnerRequestDto(folderIds: [new BatchRequestDtoAllOfFileIds(room.Id)], userId: target.Id);

        // Act + Assert
        if (allowed)
        {
            var result = (await _sharingApi.ChangeFileOwnerAsync(request, TestContext.Current.CancellationToken)).Response;
            result.OfType<FolderDto>().Should().ContainSingle(f => f.Id == room.Id).Which.CreatedBy.Id.Should().Be(target.Id);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _sharingApi.ChangeFileOwnerAsync(request, TestContext.Current.CancellationToken));
            exception.ErrorCode.Should().Be(403);
        }

        await _filesClient.Authenticate(Owner);
        var shares = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var owner = shares.Should().ContainSingle(s => s.IsOwner).Which;
        owner.SharedToUser.Id.Should().Be(allowed ? target.Id : previous.Id);

        if (allowed)
        {
            shares.Should().ContainSingle(s => s.SharedToUser != null && s.SharedToUser.Id == previous.Id).Which.Access.Should().Be(FileShare.RoomManager);
        }
    }

    /// <summary>The new owner of a room must be a Room admin or higher: a User or a Guest is refused with 403.</summary>
    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task ChangeFileOwner_RoomToUserOrGuest_Forbidden(EmployeeType targetType)
    {
        // Arrange
        var target = targetType == EmployeeType.Guest ? await InviteGuest() : await InviteContact(targetType);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Owner Change To {targetType}");

        await _filesClient.Authenticate(admin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _sharingApi.ChangeFileOwnerAsync(
                new ChangeOwnerRequestDto(folderIds: [new BatchRequestDtoAllOfFileIds(room.Id)], userId: target.Id),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.CreatedBy.Id.Should().Be(Owner.Id);
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
