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
/// <c>PUT /files/rooms/{id}/pin</c> — response contract and the core functional behaviour of
/// pinning a single room. Ordering effects across several rooms live in
/// <see cref="RoomPinOrderingTests"/>, room types in <see cref="RoomPinRoomTypesTests"/>,
/// concurrency/limits in <see cref="RoomPinLimitTests"/>, and validation/lifecycle in
/// <see cref="RoomPinValidationTests"/>. Who may pin is covered in
/// <c>Permissions/RoomPinPermissionsTests</c>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomPinTests(
    AspireAppFixture fixture)
    : RoomPinTestsBase(fixture)
{
    [Fact]
    public async Task PinRoom_OwnRoom_ReturnsPinnedFolderIntegerWrapper()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Pin Contract");

        // Act
        var response = await _roomsApi.PinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.StatusCode.Should().Be(200);
        response.Data.Response.Should().NotBeNull();
        response.Data.Response.Id.Should().Be(room.Id);
        response.Data.Response.Title.Should().Be("Autotest Pin Contract");
        response.Data.Response.RoomType.Should().Be(RoomType.CustomRoom);
        response.Data.Response.Pinned.Should().BeTrue();
    }

    [Fact]
    public async Task PinRoom_NoRequestBody_StillPins()
    {
        // Arrange — the SDK method takes only the path id, mirroring the "no body required" TS case.
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Pin No Body");

        // Act
        var pinned = (await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        pinned.Pinned.Should().BeTrue();
    }

    [Fact]
    public async Task PinRoom_DoesNotRemoveRoomFromList()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Pin Stays");

        // Act
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert — still present (exactly once) and now flagged pinned, not removed.
        var (_, row, count, _) = await FindRoomRow(room.Id);
        count.Should().Be(1);
        row!.Value.Pinned.Should().BeTrue();
    }

    [Fact]
    public async Task PinRoom_IsIdempotent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Pin Idempotent");

        // Act
        var first = await _roomsApi.PinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);
        var second = await _roomsApi.PinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var rows = await GetRoomRows();
        var occurrences = rows.Where(r => r.Id == room.Id).ToList();
        occurrences.Should().HaveCount(1);
        occurrences[0].Pinned.Should().BeTrue();
    }

    [Fact]
    public async Task PinRoom_WorksAgainAfterUnpin()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Pin Again");

        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);
        await _roomsApi.UnpinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        // Act
        var repin = await _roomsApi.PinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        repin.StatusCode.Should().Be(HttpStatusCode.OK);
        repin.Data.Response.Pinned.Should().BeTrue();
    }

    [Fact]
    public async Task PinRoom_IsPerUser_NotGlobal()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Pin PerUser");

        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Read);

        // Returns the pinned flag for the SAME room as seen by the caller currently authenticated
        // on _filesClient. Asserts the room is actually visible first, so a missing room does not
        // masquerade as a per-user difference.
        async Task<bool> PinStateFor(string who)
        {
            var rows = await GetRoomRows();
            var matches = rows.Where(r => r.Id == room.Id).ToList();
            matches.Should().HaveCount(1, $"room {room.Id} should be visible exactly once to {who}");
            return matches[0].Pinned;
        }

        // Act & Assert — owner pins: the SAME room is pinned for owner but not for the member.
        await _filesClient.Authenticate(Owner);
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);
        (await PinStateFor("owner")).Should().BeTrue();

        await _filesClient.Authenticate(member);
        (await PinStateFor("member")).Should().BeFalse();

        // Each user's pin state is fully independent and can be inverted.
        await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        await _roomsApi.UnpinRoomAsync(room.Id, TestContext.Current.CancellationToken);
        (await PinStateFor("owner")).Should().BeFalse();

        await _filesClient.Authenticate(member);
        (await PinStateFor("member")).Should().BeTrue();
    }

    [Fact]
    public async Task PinRoom_DoesNotPinAnotherRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var pinned = await CreateCustomRoom("Autotest Pin Isolated A");
        var other = await CreateCustomRoom("Autotest Pin Isolated B");

        // Act
        await _roomsApi.PinRoomAsync(pinned.Id, TestContext.Current.CancellationToken);

        // Assert
        var rows = await GetRoomRows();
        rows.First(r => r.Id == other.Id).Pinned.Should().BeFalse();
    }

    [Fact]
    public async Task PinThenUnpin_EachStepIsReflectedByGetRoomInfo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Pin RoundTrip");

        // Act & Assert — pin and verify pinned.
        var pin = await _roomsApi.PinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);
        pin.StatusCode.Should().Be(HttpStatusCode.OK);
        var infoAfterPin = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        infoAfterPin.Pinned.Should().BeTrue();

        // Act & Assert — unpin and verify not pinned.
        var unpin = await _roomsApi.UnpinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);
        unpin.StatusCode.Should().Be(HttpStatusCode.OK);
        var infoAfterUnpin = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        infoAfterUnpin.Pinned.Should().BeFalse();
    }
}
