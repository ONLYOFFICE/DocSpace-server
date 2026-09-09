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

/// <summary>POST /files/group/{id}/icon — icon-value validation and addressability.</summary>
[Trait("Category", "Rooms")]
public class RoomGroupIconValidationTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    // These are all well-formed strings the typed DTO accepts fine — the server rejects them for
    // business reasons (unknown/blank/too long icon value), not a wire-format problem.
    public static TheoryData<string, string> BadIcons => new()
    {
        { "whitespace-only", "   " },
        { "none", "none" },
        { "unknown", "invalid-icon-name" },
        { "too-long", new string('x', 300) }
    };

    [Theory]
    [MemberData(nameof(BadIcons))]
    public async Task ChangeIcon_BadIcon_Returns400(string label, string icon)
    {
        // Arrange
        var roomId = await CreateGroupRoomId($"IconBad {label}");
        var created = await CreateRoomGroup($"IconBad {label}", [roomId]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.ChangeRoomGroupIconAsync(
            created.Id, new IconRequest(icon), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    // `icon` typed as anything other than a string (number, boolean, array, object) cannot be
    // expressed through `IconRequest.Icon` (a `string`), so these stay raw.
    public static TheoryData<string, object> WrongTypedIcons => new()
    {
        { "number", 5 },
        { "boolean", true },
        { "array", new[] { 1 } },
        { "object", new { a = 1 } }
    };

    [Theory]
    [MemberData(nameof(WrongTypedIcons))]
    public async Task ChangeIcon_WrongTypedIcon_Returns400(string label, object icon)
    {
        // Arrange
        var roomId = await CreateGroupRoomId($"IconBad {label}");
        var created = await CreateRoomGroup($"IconBad {label}", [roomId]);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: new { icon }, path: $"/{created.Id}/icon");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    /// <summary>
    /// Confirmed contract: unlike every other invalid icon value above, an empty string is the
    /// accepted way to CLEAR the icon.
    /// </summary>
    [Fact]
    public async Task ChangeIcon_EmptyStringIcon_ClearsTheIcon()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("EmptyIcon");
        var created = await CreateRoomGroup("EmptyIcon Group", [roomId], "heart");

        // Act
        var updated = (await _roomGroupsApi.ChangeRoomGroupIconAsync(created.Id, new IconRequest(""), TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Should().NotBeNull();
        var after = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        (after.Icon?.Id).Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ChangeIcon_MissingBody_Returns415()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("IconNoBody");
        var created = await CreateRoomGroup("IconNoBody Group", [roomId]);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, path: $"/{created.Id}/icon", omitBody: true);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)415);
    }

    [Fact]
    public async Task ChangeIcon_TextPlainContentType_Returns415()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("IconTextPlain");
        var created = await CreateRoomGroup("IconTextPlain Group", [roomId]);

        // Act
        using var response = await RoomGroupRaw(
            HttpMethod.Post,
            path: $"/{created.Id}/icon",
            body: JsonSerializer.Serialize(new { icon = "heart" }),
            contentType: "text/plain");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)415);
    }

    // 999999 (non-existent) is covered by the BUG 80922 regression test below.
    public static TheoryData<int> NonAddressableIntegerIds => [0, -1];

    [Theory]
    [MemberData(nameof(NonAddressableIntegerIds))]
    public async Task ChangeIcon_NonAddressableIntegerGroup_Returns404(int id)
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.ChangeRoomGroupIconAsync(
            id, new IconRequest("heart"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    // Does not route to a valid `int id`, so there is no typed call to make.
    [Fact]
    public async Task ChangeIcon_NonIntegerGroupId_Returns404()
    {
        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: new { icon = "heart" }, path: "/not-a-number/icon");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)404);
    }

    [Fact]
    [Trait("Bug", "80922")]
    public async Task ChangeIcon_NonExistentGroup_Returns404()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.ChangeRoomGroupIconAsync(
            999999, new IconRequest("heart"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task ChangeIcon_DeletedGroup_Returns404()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("IconDel");
        var created = await CreateRoomGroup("IconDel Group", [roomId]);
        await _roomGroupsApi.DeleteRoomGroupAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.ChangeRoomGroupIconAsync(
            created.Id, new IconRequest("heart"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }
}
