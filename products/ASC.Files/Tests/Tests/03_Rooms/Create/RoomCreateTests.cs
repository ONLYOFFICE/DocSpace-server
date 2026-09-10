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
/// The basic contract of <c>POST /files/rooms</c>: one room per supported type is created, the
/// response echoes what was asked for, and repeated calls never collide on id.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "RoomCreate")]
public class RoomCreateTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    public static TheoryData<RoomType> SupportedRoomTypes =>
    [
        RoomType.CustomRoom,
        RoomType.EditingRoom,
        RoomType.FillingFormsRoom,
        RoomType.PublicRoom,
        RoomType.VirtualDataRoom
    ];

    [Theory]
    [MemberData(nameof(SupportedRoomTypes))]
    public async Task CreateRoom_SupportedType_Created(RoomType roomType)
    {
        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto($"Autotest {roomType}", roomType: roomType),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Title.Should().Be($"Autotest {roomType}");
        room.RoomType.Should().Be(roomType);
        room.Id.Should().BePositive();
    }

    [Fact]
    public async Task CreateRoom_MinimalPayload_AppliesSafeDefaults()
    {
        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Defaults", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Private.Should().BeFalse();
        room.Indexing.Should().BeFalse();
        room.DenyDownload.Should().BeFalse();
        room.Pinned.Should().BeFalse();
    }

    [Fact]
    public async Task CreateRoom_Created_AccessibleViaGetRoomInfo()
    {
        // Arrange
        var created = await CreateCustomRoom("Autotest GetInfo");

        // Act
        var info = (await _roomsApi.GetRoomInfoAsync(created.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Id.Should().Be(created.Id);
        info.Title.Should().Be(created.Title);
        info.RoomType.Should().Be(created.RoomType);
    }

    [Fact]
    public async Task CreateRoom_DuplicateTitle_AllowedWithUniqueIds()
    {
        // Arrange
        const string title = "Duplicate Title";

        // Act
        var room1 = await CreateCustomRoom(title);
        var room2 = await CreateCustomRoom(title);

        // Assert
        room1.Id.Should().NotBe(room2.Id);
        room1.Title.Should().Be(title);
        room2.Title.Should().Be(title);
    }

    [Fact]
    public async Task CreateRoom_Multiple_HaveUniqueIds()
    {
        // Act
        var ids = new List<int>();
        for (var i = 0; i < 5; i++)
        {
            var room = await CreateCustomRoom($"Autotest Unique {i}");
            ids.Add(room.Id);
        }

        // Assert
        ids.Distinct().Should().HaveCount(ids.Count);
    }
}
