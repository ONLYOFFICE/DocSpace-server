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


namespace ASC.Files.Tests.Tests._03_Rooms.AiHub;

/// <summary>
/// A room of the AI rooms section takes only the settings that mean something for it through the shared room routes:
/// a title, a color, a cover, tags, a logo and, except for an AI project, a storage quota. Anything else is refused
/// with 400 instead of being dropped silently.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiHubRoomSettingsTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task CreateProject_WithQuota_Returns400()
    {
        // Act
        using var response = await CreateHubRoomResponseAsync(new { title = "Autotest Quota Project", roomType = AiProjectRoomType, quota = 1048576 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        IdsOf(await GetAiHubSectionAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task CreateProject_WithDenyDownload_Returns400()
    {
        // Act
        using var response = await CreateHubRoomResponseAsync(new { title = "Autotest Deny Download Project", roomType = AiProjectRoomType, denyDownload = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        IdsOf(await GetAiHubSectionAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task CreateInstructionsRoom_WithIndexing_Returns400()
    {
        // Act
        using var response = await CreateHubRoomResponseAsync(new { title = "Autotest Indexed Instructions", roomType = InstructionsRoomType, indexing = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        IdsOf(await GetAiHubSectionAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateProject_WithQuota_Returns400()
    {
        // Arrange
        var projectId = await CreateProjectAsync("Autotest Project");

        // Act
        using var response = await UpdateRoomResponseAsync(projectId, new { quota = 1048576 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateInstructionsRoom_WithDenyDownload_Returns400()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync("Autotest Instructions");

        // Act
        using var response = await UpdateRoomResponseAsync(roomId, new { title = "Autotest Renamed Instructions", denyDownload = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateInstructionsRoom_TitleAndColor_AppliesChanges()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync("Autotest Instructions");

        // Act
        using var response = await UpdateRoomResponseAsync(roomId, new { title = "Autotest Renamed Instructions", color = "AB12CD" });
        var room = await Files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);

        // Assert
        room.GetProperty("title").GetString().Should().Be("Autotest Renamed Instructions");
        room.GetProperty("logo").GetProperty("color").GetString().Should().Be("AB12CD");
    }
}
