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
/// The AI rooms section while AI is turned off on the portal (`POST api/2.0/settings/ai-access`): every entry of the
/// section is refused with 403, and it is all back once AI is turned on again.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiHubAiAccessTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task CreateInstructionsRoom_AiDisabled_Returns403()
    {
        // Arrange
        await SetAiAccessAsync(false);

        // Act
        using var response = await CreateHubRoomResponseAsync(new { title = "instructions", roomType = InstructionsRoomType });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProject_AiDisabled_Returns403()
    {
        // Arrange
        await SetAiAccessAsync(false);

        // Act
        using var response = await CreateHubRoomResponseAsync(new { title = "project", roomType = AiProjectRoomType });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSection_AiDisabled_Returns403()
    {
        // Arrange
        await CreateInstructionsRoomAsync();
        await SetAiAccessAsync(false);

        // Act
        using var response = await Files.GetAsync("api/2.0/files/rooms?searchArea=AiHub", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetRoom_AiDisabled_Returns403()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();
        await SetAiAccessAsync(false);

        // Act
        using var response = await GetRoomResponseAsync(roomId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateRoom_AiDisabled_Returns403()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        await SetAiAccessAsync(false);

        // Act
        using var response = await UpdateRoomResponseAsync(projectId, new { title = "renamed" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteRoom_AiDisabled_Returns403()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();
        await SetAiAccessAsync(false);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomAsync(roomId, new DeleteRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetRoomContent_AiDisabled_Returns403()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();
        await SetAiAccessAsync(false);

        // Act
        using var response = await GetFolderContentResponseAsync(roomId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UploadIntoInstructionsRoom_AiDisabled_Returns403()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();
        await SetAiAccessAsync(false);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await UploadSkillAsync(roomId, "reviewer"));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ReadFile_AiDisabled_Returns403()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();
        var fileId = await UploadSkillAsync(roomId, "reviewer");
        await SetAiAccessAsync(false);

        // Act
        using var file = await GetFileResponseAsync(fileId);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetPresignedUriAsync(fileId, TestContext.Current.CancellationToken));

        // Assert
        file.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task AiEnabledAgain_RoomAndFileReadable()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();
        var fileId = await UploadSkillAsync(roomId, "reviewer");
        await SetAiAccessAsync(false);
        await SetAiAccessAsync(true);

        // Act
        using var content = await GetFolderContentResponseAsync(roomId);
        using var file = await GetFileResponseAsync(fileId);

        // Assert
        content.StatusCode.Should().Be(HttpStatusCode.OK);
        var listing = await Files.ReadAsync<JsonElement>(content, TestContext.Current.CancellationToken);
        listing.GetProperty("files").EnumerateArray().Select(f => f.GetProperty("id").GetInt32()).Should().Contain(fileId);
        file.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task SetAiAccessAsync(bool enabled)
    {
        await _webApiClient.Authenticate(Owner);
        await _commonSettingsApi.SetTenantAiAccessSettingsAsync(new TenantAiAccessSettingsRequestDto(enabled), TestContext.Current.CancellationToken);
    }
}
