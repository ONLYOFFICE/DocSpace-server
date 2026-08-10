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

/// <summary>PUT /files/group/{id} — body/method contract, addressability and sequential updates.</summary>
[Trait("Category", "Rooms")]
public class RoomGroupUpdateContractTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    [Fact]
    public async Task Update_EmptyObjectBody_IsNoOp()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("EmptyObj");
        var created = await CreateRoomGroup("EmptyObj Group", [roomId]);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Put, body: new { }, path: $"/{created.Id}");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)200);
        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Name.Should().Be("EmptyObj Group");
        info.TotalRooms.Should().Be(1);
    }

    [Fact]
    public async Task Update_MissingBody_Returns415()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("NoBody");
        var created = await CreateRoomGroup("NoBody Group", [roomId]);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Put, path: $"/{created.Id}", omitBody: true);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)415);
    }

    [Fact]
    public async Task Update_MalformedJson_Returns400()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("BadJson");
        var created = await CreateRoomGroup("BadJson Group", [roomId]);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Put, path: $"/{created.Id}", body: "{ broken");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    public async Task Update_TextPlainContentType_Returns415()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("TextPlain");
        var created = await CreateRoomGroup("TextPlain Group", [roomId]);

        // Act
        using var response = await RoomGroupRaw(
            HttpMethod.Put,
            path: $"/{created.Id}",
            body: JsonSerializer.Serialize(new { groupName = "X" }),
            contentType: "text/plain");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)415);
    }

    public static TheoryData<string> BadIds => ["0", "-1", "999999", "not-a-number"];

    [Theory]
    [MemberData(nameof(BadIds))]
    public async Task Update_NonAddressableGroup_Returns404(string id)
    {
        // Act
        using var response = await RoomGroupRaw(HttpMethod.Put, body: new { groupName = "X" }, path: $"/{id}");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)404);
    }

    [Fact]
    public async Task Update_DeletedGroup_Returns404()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("DelUpd");
        var created = await CreateRoomGroup("DelUpd Group", [roomId]);
        await _roomGroupsApi.DeleteRoomGroupAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id, new UpdateRoomGroupRequest(groupName: "X"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task Update_TwoSequentialUpdates_BothApply()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(2, "Seq");
        var created = await CreateRoomGroup("Seq One", [ids[0]]);

        // Act
        await _roomGroupsApi.UpdateRoomGroupAsync(created.Id, new UpdateRoomGroupRequest(groupName: "Seq Two"), TestContext.Current.CancellationToken);
        var updated = (await _roomGroupsApi.UpdateRoomGroupAsync(
            created.Id,
            new UpdateRoomGroupRequest(roomsToAdd: [new DuplicateRequestDtoAllOfFileIds(ids[1])]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Name.Should().Be("Seq Two");
        updated.TotalRooms.Should().Be(2);
    }
}
