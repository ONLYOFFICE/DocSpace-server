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
/// An AI project is a room of the AI rooms section: it is created, read, changed and deleted through the room routes,
/// is listed in the section next to the Instructions and Knowledge rooms and, like them, is never archived and has no
/// templates.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiProjectRoomTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task CreateProject_AsOwner_LandsInAiHubRoot()
    {
        // Act
        using var response = await CreateHubRoomResponseAsync(new { title = "Autotest Project", roomType = AiProjectRoomType, color = "FF6600", tags = new[] { "autotest" } });
        var project = await Files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);
        var section = await GetAiHubSectionAsync();

        // Assert
        project.GetProperty("title").GetString().Should().Be("Autotest Project");
        project.GetProperty("roomType").GetInt32().Should().Be(AiProjectRoomType);
        project.GetProperty("rootFolderType").GetInt32().Should().Be(AiHubFolderType);
        project.GetProperty("parentId").GetInt32().Should().Be(section.GetProperty("current").GetProperty("id").GetInt32());
        project.GetProperty("logo").GetProperty("color").GetString().Should().Be("FF6600");
        project.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).Should().Equal("autotest");
    }

    [Fact]
    public async Task CreateProject_AsUser_Returns403()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        using var response = await CreateHubRoomResponseAsync(new { title = "Autotest Forbidden Project", roomType = AiProjectRoomType });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProject_WithoutTitle_Returns400()
    {
        // Act
        using var response = await CreateHubRoomResponseAsync(new { roomType = AiProjectRoomType, color = "FF6600" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAiHubSection_ProjectAndInstructionsRoom_ListsBoth()
    {
        // Arrange
        var projectId = await CreateProjectAsync("Autotest Hub Project");
        var roomId = await CreateInstructionsRoomAsync("Autotest Hub Instructions");

        // Act
        var ids = IdsOf(await GetAiHubSectionAsync());

        // Assert
        ids.Should().Contain([projectId, roomId]);
    }

    [Fact]
    public async Task GetAiHubSection_FilteredByProjectType_ListsOnlyProjects()
    {
        // Arrange
        var projectId = await CreateProjectAsync("Autotest Filtered Project");
        var roomId = await CreateInstructionsRoomAsync("Autotest Filtered Instructions");

        // Act
        var ids = IdsOf(await GetAiHubSectionAsync($"&type={AiProjectRoomType}"));

        // Assert
        ids.Should().Contain(projectId);
        ids.Should().NotContain(roomId);
    }

    [Fact]
    public async Task GetProject_UserWithoutAccess_Returns403()
    {
        // Arrange
        var projectId = await CreateProjectAsync("Autotest Hidden Project");

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        using var response = await GetRoomResponseAsync(projectId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateProject_AsOwner_AppliesChanges()
    {
        // Arrange
        var projectId = await CreateProjectAsync("Autotest Project");

        // Act
        using var response = await UpdateRoomResponseAsync(projectId, new { title = "Autotest Renamed Project", color = "AB12CD", tags = new[] { "autotest" } });
        var project = await Files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);

        // Assert
        project.GetProperty("title").GetString().Should().Be("Autotest Renamed Project");
        project.GetProperty("logo").GetProperty("color").GetString().Should().Be("AB12CD");
        project.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).Should().Equal("autotest");
    }

    [Fact]
    public async Task UpdateProject_UserWithoutAccess_Returns403()
    {
        // Arrange
        var projectId = await CreateProjectAsync("Autotest Hidden Project");

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        using var response = await UpdateRoomResponseAsync(projectId, new { title = "Autotest Renamed Project" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteProject_AsOwner_ProjectDisappears()
    {
        // Arrange
        var projectId = await CreateProjectAsync("Autotest Deleted Project");

        // Act
        await DeleteRoomAndWaitAsync(projectId);

        // Assert
        using var response = await GetRoomResponseAsync(projectId);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        IdsOf(await GetAiHubSectionAsync()).Should().NotContain(projectId);
    }

    [Fact]
    public async Task DeleteProject_UserWithoutAccess_Returns403()
    {
        // Arrange
        var projectId = await CreateProjectAsync("Autotest Hidden Project");

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomAsync(projectId, new DeleteRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ArchiveProject_Returns403()
    {
        // Arrange
        var projectId = await CreateProjectAsync("Autotest Archived Project");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ArchiveRoomAsync(projectId, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateRoomTemplate_FromProject_Returns403()
    {
        // Arrange
        var projectId = await CreateProjectAsync("Autotest Project Template");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTemplateAsync(new RoomTemplateDto(projectId, "Autotest Project Template"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
