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
/// How <c>POST /files/rooms</c> handles the room title at its content boundaries: length, script,
/// and characters the server sanitizes to an underscore (<c>"</c>, <c>\</c>, <c>&lt;</c>, <c>&gt;</c>).
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "RoomCreate")]
public class RoomCreateTitleTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task CreateRoom_LongValidTitle_Accepted()
    {
        // Arrange
        var title = new string('A', 100);

        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title, roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Title.Should().Be(title);
    }

    [Fact]
    public async Task CreateRoom_UnicodeTitle_CjkPreservedEmojiSanitized()
    {
        // Arrange
        const string title = "Room \U0001F389 测试 ファイル";
        const string expected = "Room __ 测试 ファイル";

        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title, roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Title.Should().Be(expected);
    }

    [Fact]
    public async Task CreateRoom_SpecialCharactersInTitle_SanitizedToUnderscores()
    {
        // Arrange
        // The API replaces ", \, <, > with "_"; & is preserved.
        const string title = "Room \"with\" \\slashes & <html> tags";
        const string expected = "Room _with_ _slashes & _html_ tags";

        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title, roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Title.Should().Be(expected);
    }

    [Fact]
    public async Task CreateRoom_SqlInjectionLikeTitle_StoredWithoutServerError()
    {
        // Arrange
        const string title = "'; DROP TABLE rooms; --";

        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title, roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        // Apostrophe, semicolon and dash are not in the forbidden set - stored as-is.
        room.Title.Should().Be(title);
    }

    [Fact]
    public async Task CreateRoom_XssPayloadInTitle_Sanitized()
    {
        // Arrange
        // <, >, /, " are all replaced with "_".
        const string title = "<script>alert(\"xss\")</script>";
        const string expected = "_script_alert(_xss_)__script_";

        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title, roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Title.Should().Be(expected);
    }

    [Fact]
    public async Task CreateRoom_MixedRtlLtrTitle_Preserved()
    {
        // Arrange
        const string title = "Mixed العربية 中文 עברית text";

        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title, roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Title.Should().Be(title);
    }
}
