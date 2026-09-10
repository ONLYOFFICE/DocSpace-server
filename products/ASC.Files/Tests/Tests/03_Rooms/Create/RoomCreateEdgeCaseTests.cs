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

namespace ASC.Files.Tests.Tests._03_Rooms.Create;

/// <summary>
/// Concurrency, bulk payload and response-shape edge cases of <c>POST /files/rooms</c>.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "RoomCreate")]
public class RoomCreateEdgeCaseTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task CreateRoom_Parallel5Rooms_ProduceUniqueIds()
    {
        // Act
        var tasks = Enumerable.Range(0, 5).Select(i => _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto($"Autotest Parallel {i}", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken));

        var results = await Task.WhenAll(tasks);

        // Assert
        var ids = results.Select(r => r.Response.Id).ToList();
        ids.Distinct().Should().HaveCount(ids.Count);
    }

    [Fact]
    public async Task CreateRoom_RapidIdenticalRequests_AllSucceedWithUniqueIds()
    {
        // Arrange
        var request = new CreateRoomRequestDto("Autotest Rapid", roomType: RoomType.CustomRoom);

        // Act
        var ids = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            var room = (await _roomsApi.CreateRoomAsync(request, TestContext.Current.CancellationToken)).Response;
            ids.Add(room.Id);
        }

        // Assert
        ids.Distinct().Should().HaveCount(ids.Count);
    }

    [Fact]
    public async Task CreateRoom_LargeTagsArray_50Tags_Accepted()
    {
        // Arrange
        var stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var tags = Enumerable.Range(0, 50).Select(i => $"autotest-bulk-{stamp}-{i}").ToList();

        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest LargePayload", roomType: RoomType.CustomRoom, tags: tags),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Tags.Count.Should().BeGreaterThanOrEqualTo(tags.Count);
    }

    [Fact]
    public async Task CreateRoom_Response_HasExpectedSchemaFields()
    {
        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Schema", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Id.Should().BePositive();
        room.Title.Should().NotBeNullOrEmpty();
        room.RoomType.Should().Be(RoomType.CustomRoom);
        room.Created.Should().NotBeNull();
        room.CreatedBy.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateRoom_Response_DoesNotLeakSensitiveFields()
    {
        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Leak", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        var json = room.ToJson();
        json.Should().NotMatchRegex("(?i)\"password\"\\s*:");
        json.Should().NotMatchRegex("(?i)\"bearer\"\\s*:");
        json.Should().NotMatchRegex("(?i)\"connectionstring\"\\s*:");
        json.Should().NotMatchRegex("(?i)\"secret\"\\s*:");
    }
}
