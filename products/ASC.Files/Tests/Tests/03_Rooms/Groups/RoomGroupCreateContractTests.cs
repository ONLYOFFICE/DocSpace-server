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

/// <summary>POST /files/group — body and HTTP-method contract.</summary>
[Trait("Category", "Rooms")]
public class RoomGroupCreateContractTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    // RoomGroupRequestDto has no reachable parameterless constructor for a test to call — the only
    // one is `protected RoomGroupRequestDto()` (deserialization-only, see the [JsonConstructor] in
    // RoomGroupRequestDto.cs), and the public constructor requires non-null name/icon/rooms and
    // throws ArgumentNullException otherwise. An actually-empty body has no typed equivalent.
    [Fact]
    public async Task Create_EmptyBody_Returns400()
    {
        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: new { });

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    public async Task Create_MissingBody_Returns415()
    {
        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, omitBody: true);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)415);
    }

    [Fact]
    public async Task Create_MalformedJson_Returns400NotInternalError()
    {
        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: "{ not valid json ");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)400);
    }

    [Fact]
    public async Task Create_TextPlainContentType_Returns415()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("CT");

        // Act
        using var response = await RoomGroupRaw(
            HttpMethod.Post,
            body: JsonSerializer.Serialize(new { name = "CT", icon = "star", rooms = new[] { roomId } }),
            contentType: "text/plain");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)415);
    }

    public static TheoryData<string> UnsupportedMethods => ["PUT", "DELETE", "PATCH"];

    [Theory]
    [MemberData(nameof(UnsupportedMethods))]
    public async Task Create_UnsupportedMethod_Returns405(string method)
    {
        // Act
        using var response = await RoomGroupRaw(new HttpMethod(method), body: new { });

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)405);
    }
}
