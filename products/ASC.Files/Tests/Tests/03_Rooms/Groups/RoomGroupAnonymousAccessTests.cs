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

/// <summary>Every room-group endpoint requires authentication: anonymous or invalid-token requests get 401.</summary>
[Trait("Category", "Rooms")]
public class RoomGroupAnonymousAccessTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    #region Anonymous access control

    [Fact]
    public async Task Create_Anonymous_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: new { name = "Anon", icon = "star", rooms = new[] { 1 } });

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)401);
    }

    [Fact]
    public async Task GetInfo_Anonymous_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Get, path: "/1");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)401);
    }

    [Fact]
    public async Task GetList_Anonymous_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Get);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)401);
    }

    [Fact]
    public async Task Update_Anonymous_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Put, body: new { groupName = "Hacked" }, path: "/1");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)401);
    }

    [Fact]
    public async Task ChangeIcon_Anonymous_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: new { icon = "heart" }, path: "/1/icon");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)401);
    }

    [Fact]
    public async Task Delete_Anonymous_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Delete, path: "/1");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)401);
    }

    #endregion

    #region Invalid token access control

    private void UseInvalidToken()
    {
        _filesClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "garbage.token");
    }

    [Fact]
    public async Task Create_InvalidToken_Returns401()
    {
        // Arrange
        UseInvalidToken();

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: new { name = "Bad Token", icon = "star", rooms = new[] { 1 } });

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)401);
    }

    [Fact]
    public async Task GetInfo_InvalidToken_Returns401()
    {
        // Arrange
        UseInvalidToken();

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Get, path: "/1");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)401);
    }

    [Fact]
    public async Task GetList_InvalidToken_Returns401()
    {
        // Arrange
        UseInvalidToken();

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Get);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)401);
    }

    [Fact]
    public async Task Update_InvalidToken_Returns401()
    {
        // Arrange
        UseInvalidToken();

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Put, body: new { groupName = "X" }, path: "/1");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)401);
    }

    [Fact]
    public async Task ChangeIcon_InvalidToken_Returns401()
    {
        // Arrange
        UseInvalidToken();

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Post, body: new { icon = "heart" }, path: "/1/icon");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)401);
    }

    [Fact]
    public async Task Delete_InvalidToken_Returns401()
    {
        // Arrange
        UseInvalidToken();

        // Act
        using var response = await RoomGroupRaw(HttpMethod.Delete, path: "/1");

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)401);
    }

    #endregion
}
