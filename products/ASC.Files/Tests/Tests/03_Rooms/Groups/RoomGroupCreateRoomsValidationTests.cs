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

namespace ASC.Files.Tests.Tests._03_Rooms.Groups;

/// <summary>
/// POST /files/group — validation of the <c>rooms</c> field.
///
/// CONTRACT DISCUSSION: the field currently has three inconsistent rejection paths —
/// <c>rooms: null</c> and a non-array both fail body validation (400), but a MISSING field and an
/// EMPTY array fail as a business rule (403, "At least one room must be provided") — the server
/// treats "missing" the same as "empty" and skips field validation. A missing required field ought
/// to be a 400 body-validation error, and even the empty-array rule returning 403 instead of
/// 400/409 is debatable; both are captured below as the two BUG 82575 regression tests they are.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomGroupCreateRoomsValidationTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    [Fact]
    public async Task Create_EmptyRoomsArray_RejectedAndCreatesNothing()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.AddRoomGroupAsync(
            new RoomGroupRequestDto("Empty Rooms", "star", []),
            cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Select(g => g.Name).Should().NotContain("Empty Rooms");
    }

    // `rooms` is a required constructor argument on the typed DTO — there is no way to omit it,
    // so this stays raw.
    [Fact]
    [Trait("Bug", "82575")]
    public async Task Create_MissingRoomsField_ShouldBe400BodyValidationError()
    {
        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: new { name = "No Rooms Field", icon = "star" });

        // Assert — a missing required field must fail body validation, distinct from the
        // empty-array business rule. The server currently returns 403.
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    // `rooms: null` throws client-side (ArgumentNullException) if constructed through the typed
    // DTO, so it never reaches the server — this stays raw to exercise the actual wire contract.
    [Fact]
    public async Task Create_RoomsNull_Returns400()
    {
        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: new { name = "Null Rooms", icon = "star", rooms = (object?)null });

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    public static TheoryData<string, object> NonArrayRooms => new()
    {
        { "string", "abc" },
        { "number", 5 },
        { "object", new { a = 1 } }
    };

    [Theory]
    [MemberData(nameof(NonArrayRooms))]
    public async Task Create_RoomsAsNonArray_Returns400(string label, object rooms)
    {
        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: new { name = $"Rooms {label}", icon = "star", rooms });

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    /// <summary>
    /// <c>rooms</c> is typed number[]. Lenient deserialization currently coerces a numeric string
    /// ("999999" -> 999999) instead of rejecting the wrong element type, and then routes it into
    /// the room-lookup/business path, which surfaces 403 for the (still non-existent) id.
    /// </summary>
    [Fact]
    [Trait("Bug", "82575")]
    public async Task Create_NumericStringRoomId_ShouldBe400TypeError()
    {
        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: """{"name":"String Id","icon":"star","rooms":["999999"]}""");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    [Trait("Bug", "82575")]
    public async Task Create_NullRoomElement_ShouldBe400TypeError()
    {
        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: """{"name":"Null Elem","icon":"star","rooms":[null]}""");

        // Assert — currently silently dropped instead of rejected (403 for the resulting empty set).
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    [Trait("Bug", "82576")]
    public async Task Create_FractionalRoomId_ShouldBe400NotInternalError()
    {
        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: """{"name":"Float Id","icon":"star","rooms":[1.5]}""");

        // Assert — the server currently returns 500.
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    /// <summary>
    /// A well-formed positive id that does not exist should be 404 (the server even reports "The
    /// required folder was not found"), but currently returns 403. It must not be a 500 either.
    /// </summary>
    [Fact]
    [Trait("Bug", "82577")]
    public async Task Create_NonExistentRoomId_ShouldBe404()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.AddRoomGroupAsync(
            new RoomGroupRequestDto("Room non-existent", "star", [new DuplicateRequestDtoAllOfFileIds(999999)]),
            cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    public static TheoryData<string, int> InvalidValueIds => new()
    {
        { "zero", 0 },
        { "negative", -1 }
    };

    /// <summary>0 and negative ids are structurally invalid (room ids are positive) and should fail body validation.</summary>
    [Theory]
    [MemberData(nameof(InvalidValueIds))]
    [Trait("Bug", "82575")]
    public async Task Create_InvalidValueRoomId_ShouldBe400InvalidValueError(string label, int id)
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.AddRoomGroupAsync(
            new RoomGroupRequestDto($"Room {label}", "star", [new DuplicateRequestDtoAllOfFileIds(id)]),
            cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    /// <summary>
    /// Confirmed contract: the create is intentionally NOT atomic. The response reports the
    /// non-existent room (403), but the group is still created from the rooms that could be resolved.
    /// </summary>
    [Fact]
    public async Task Create_MixedValidAndNonExistentRooms_RefusedButGroupStillCreated()
    {
        // Arrange
        var validId = await CreateGroupRoomId("Atomic Valid");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.AddRoomGroupAsync(
            new RoomGroupRequestDto("Atomic Create", "star", [new DuplicateRequestDtoAllOfFileIds(validId), new DuplicateRequestDtoAllOfFileIds(999999)]),
            cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Select(g => g.Name).Should().Contain("Atomic Create");
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    [Trait("Bug", "82587")]
    public async Task Create_DuplicateRoomIds_ShouldDedupInsteadOfFail()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Dup Room");

        // Act & Assert — the server currently returns 500 instead of deduplicating; the correct
        // contract is a plain successful create (no ApiException).
        await _roomGroupsApi.AddRoomGroupAsync(
            new RoomGroupRequestDto("Dup Rooms", "star", [new DuplicateRequestDtoAllOfFileIds(roomId), new DuplicateRequestDtoAllOfFileIds(roomId)]),
            cancellationToken: TestContext.Current.CancellationToken);
    }
}
