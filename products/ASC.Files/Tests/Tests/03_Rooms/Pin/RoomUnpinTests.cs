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

namespace ASC.Files.Tests.Tests._03_Rooms.Pin;

/// <summary>
/// <c>PUT /files/rooms/{id}/unpin</c> — response contract and the core functional behaviour of
/// unpinning a room. Unpin mirrors pin and, like pin, is a per-user action gated by
/// <c>security.Pin</c> (see <c>Permissions/RoomUnpinPermissionsTests</c>). Room types live in
/// <see cref="RoomUnpinRoomTypesTests"/>, the pin-limit interaction in
/// <see cref="RoomUnpinLimitTests"/>, and validation/lifecycle in
/// <see cref="RoomUnpinValidationTests"/>. The round trip back to a room's natural sort position
/// is already covered by <c>RoomPinOrderTests</c>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomUnpinTests(
    AspireAppFixture fixture)
    : RoomPinTestsBase(fixture)
{
    [Fact]
    public async Task UnpinRoom_PinnedRoom_ReturnsUnpinnedFolderIntegerWrapper()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unpin Contract");
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Act
        var response = await _roomsApi.UnpinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.StatusCode.Should().Be(200);
        response.Data.Response.Should().NotBeNull();
        response.Data.Response.Id.Should().Be(room.Id);
        response.Data.Response.Title.Should().Be("Autotest Unpin Contract");
        response.Data.Response.RoomType.Should().Be(RoomType.CustomRoom);
        response.Data.Response.Pinned.Should().BeFalse();

        // The effect is visible in the list too: still present, now unpinned.
        var (_, row, count, _) = await FindRoomRow(room.Id);
        count.Should().Be(1);
        row!.Value.Pinned.Should().BeFalse();
    }

    [Fact]
    public async Task UnpinRoom_NoRequestBody_StillUnpins()
    {
        // Arrange — the SDK method takes only the path id, mirroring the "no body required" TS case.
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unpin No Body");
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Act
        var unpinned = (await _roomsApi.UnpinRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        unpinned.Pinned.Should().BeFalse();
    }

    [Fact]
    public async Task UnpinRoom_NeverPinnedRoom_ReturnsOk()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unpin Fresh");

        // Act
        var response = await _roomsApi.UnpinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Response.Pinned.Should().BeFalse();
    }

    [Fact]
    public async Task UnpinRoom_DoesNotRemoveRoomFromList()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unpin Stays");

        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.UnpinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert — still present (exactly once) and now flagged unpinned, not removed.
        var (_, row, count, _) = await FindRoomRow(room.Id);
        count.Should().Be(1);
        row!.Value.Pinned.Should().BeFalse();
    }

    [Fact]
    public async Task UnpinRoom_IsIdempotent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unpin Idempotent");
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Act
        var first = await _roomsApi.UnpinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);
        var second = await _roomsApi.UnpinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var occurrences = (await GetRoomRows()).Where(r => r.Id == room.Id).ToList();
        occurrences.Should().HaveCount(1);
        occurrences[0].Pinned.Should().BeFalse();
    }

    [Fact]
    public async Task PinUnpinUnpinPinUnpin_Sequence_LeavesTheRoomUnpinned()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unpin Sequence");

        // Act
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);
        await _roomsApi.UnpinRoomAsync(room.Id, TestContext.Current.CancellationToken);
        var midUnpin = await _roomsApi.UnpinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);
        var finalUnpin = await _roomsApi.UnpinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        midUnpin.StatusCode.Should().Be(HttpStatusCode.OK);
        finalUnpin.StatusCode.Should().Be(HttpStatusCode.OK);

        // The toggling never corrupts state: the room is present once and unpinned.
        var (_, row, count, _) = await FindRoomRow(room.Id);
        count.Should().Be(1);
        row!.Value.Pinned.Should().BeFalse();
    }

    [Fact]
    public async Task UnpinRoom_ReturnsTheRoomToTheUnpinnedGroup()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var a = await CreateCustomRoom("Autotest Unpin Group A");
        var b = await CreateCustomRoom("Autotest Unpin Group B");

        await _roomsApi.PinRoomAsync(a.Id, TestContext.Current.CancellationToken);
        await _roomsApi.PinRoomAsync(b.Id, TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.UnpinRoomAsync(b.Id, TestContext.Current.CancellationToken);

        // Assert
        var rows = await GetRoomRows();
        var aRow = rows.First(r => r.Id == a.Id);
        var bRow = rows.First(r => r.Id == b.Id);

        aRow.Pinned.Should().BeTrue();
        bRow.Pinned.Should().BeFalse();
        // Still-pinned A is above the now-unpinned B.
        rows.IndexOf(aRow).Should().BeLessThan(rows.IndexOf(bRow));
    }

    [Fact]
    public async Task UnpinRoom_DoesNotDeleteTheRoomOrChangeMembersOrRoles()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unpin NoSideEffect");

        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Editing);

        var membersBefore = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken))
            .Response.Select(m => (m.SharedToUser.Id, m.Access)).ToList();

        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Act
        var response = await _roomsApi.UnpinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Room still exists...
        var info = await _roomsApi.GetRoomInfoWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);
        info.StatusCode.Should().Be(HttpStatusCode.OK);
        info.Data.Response.Id.Should().Be(room.Id);

        // ...and its members/roles are untouched.
        var membersAfter = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken))
            .Response.Select(m => (m.SharedToUser.Id, m.Access)).ToList();
        membersAfter.Should().BeEquivalentTo(membersBefore);
    }
}
