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

/// <summary>POST /files/group/{id}/icon — positive changes and no-op bodies.</summary>
[Trait("Category", "Rooms")]
public class RoomGroupIconTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    [Fact]
    public async Task ChangeIcon_NewIcon_ReflectedInInfoAndList()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Icon Room");
        var created = await CreateRoomGroup("Icon Group", [roomId], "star");

        // Act
        var updated = (await _roomGroupsApi.ChangeRoomGroupIconAsync(created.Id, new IconRequest("heart"), TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Id.Should().Be(created.Id);
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Icon.Id.Should().Be("heart");
        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Single(g => g.Id == created.Id).Icon.Id.Should().Be("heart");
    }

    [Fact]
    public async Task ChangeIcon_NameAndRooms_AreUnchanged()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Icon Keep Room");
        var created = await CreateRoomGroup("Icon Keep", [roomId], "star");

        // Act
        await _roomGroupsApi.ChangeRoomGroupIconAsync(created.Id, new IconRequest("flag"), TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Name.Should().Be("Icon Keep");
        info.TotalRooms.Should().Be(1);
        info.Rooms.Select(r => r.Title).Should().Contain("Icon Keep Room");
    }

    [Fact]
    public async Task ChangeIcon_SequentialChanges_EachTakeEffect()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Seq Icon Room");
        var created = await CreateRoomGroup("Seq Icon", [roomId], "star");

        foreach (var icon in new[] { "heart", "flag", "folder" })
        {
            // Act
            var updated = (await _roomGroupsApi.ChangeRoomGroupIconAsync(created.Id, new IconRequest(icon), TestContext.Current.CancellationToken)).Response;
            updated.Should().NotBeNull();

            // Assert
            var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
            info.Icon.Id.Should().Be(icon);
        }
    }

    [Fact]
    public async Task ChangeIcon_ReapplyingCurrentIcon_IsIdempotent()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Idem Icon Room");
        var created = await CreateRoomGroup("Idem Icon", [roomId], "heart");

        // Act
        var updated = (await _roomGroupsApi.ChangeRoomGroupIconAsync(created.Id, new IconRequest("heart"), TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Should().NotBeNull();
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Icon.Id.Should().Be("heart");
    }

    public static TheoryData<string> ValidIcons => [.. _validGroupIcons];

    [Theory]
    [MemberData(nameof(ValidIcons))]
    public async Task ChangeIcon_ValidIcon_Accepted(string icon)
    {
        // Arrange
        var roomId = await CreateGroupRoomId($"IconVal {icon}");
        var created = await CreateRoomGroup($"IconVal {icon}", [roomId], "star");

        // Act
        var updated = (await _roomGroupsApi.ChangeRoomGroupIconAsync(created.Id, new IconRequest(icon), TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Should().NotBeNull();
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Icon.Id.Should().Be(icon);
    }

    // `IconRequest.Icon` is `[DataMember(Name = "icon", EmitDefaultValue = true)]`, so a
    // default-constructed instance serialises to `{"icon":null}`, not `{}` — the two payloads are
    // not the same request, so an actually-empty body can only be sent raw.
    [Fact]
    public async Task ChangeIcon_EmptyObjectBody_IsNoOp()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("NoopEmpty");
        var created = await CreateRoomGroup("NoopEmpty Group", [roomId], "heart");

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: new { }, path: $"/{created.Id}/icon");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)200);
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Icon.Id.Should().Be("heart");
    }

    [Fact]
    public async Task ChangeIcon_IconNull_IsNoOp()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("NoopNull");
        var created = await CreateRoomGroup("NoopNull Group", [roomId], "heart");

        // Act
        var updated = (await _roomGroupsApi.ChangeRoomGroupIconAsync(created.Id, new IconRequest(icon: null!), TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Should().NotBeNull();
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Icon.Id.Should().Be("heart");
    }
}
