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

/// <summary>POST /files/group — validation of the <c>name</c> and <c>icon</c> fields.</summary>
[Trait("Category", "Rooms")]
public class RoomGroupCreateNameIconValidationTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    public static TheoryData<string, string?> BadNames => new()
    {
        { "missing", null },
        { "null", null },
        { "empty", "" },
        { "whitespace-only", "   " }
    };

    [Theory]
    [MemberData(nameof(BadNames))]
    public async Task Create_BadName_Returns400(string label, string? name)
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Nm");
        var body = label == "missing"
            ? (object)new { icon = "star", rooms = new[] { roomId } }
            : new { name, icon = "star", rooms = new[] { roomId } };

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: body);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    public async Task Create_TooLongName_Returns400NotInternalError()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("TooLong");

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: new { name = new string('n', 300), icon = "star", rooms = new[] { roomId } });

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    public static TheoryData<string, string?> BadIcons => new()
    {
        { "missing", null },
        { "null", null },
        { "empty", "" },
        { "whitespace-only", "   " },
        { "unknown", "invalid-icon-name" }
    };

    [Theory]
    [MemberData(nameof(BadIcons))]
    public async Task Create_BadIcon_Returns400(string label, string? icon)
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Ic");
        var body = label == "missing"
            ? (object)new { name = "Icon Val", rooms = new[] { roomId } }
            : new { name = "Icon Val", icon, rooms = new[] { roomId } };

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: body);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    [Trait("Bug", "80921")]
    public async Task Create_IconNone_Returns400()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Room for Invalid Icon Group");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomGroupsApi.AddRoomGroupAsync(
            new RoomGroupRequestDto("Invalid Icon Group", "none", [new DuplicateRequestDtoAllOfFileIds(roomId)]),
            cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }
}
