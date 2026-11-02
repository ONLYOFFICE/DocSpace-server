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
/// The AI model of an AI project: `GET` and `PUT api/2.0/files/rooms/{id}/model`. The Files suite starts neither the
/// AI service nor an AI gateway, so the portal has no AI model profile to select and a successful `PUT` cannot be
/// reached here; the tests cover reading, access and the refusals.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiProjectModelTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task GetModel_NoModelSelected_ReturnsNoProfile()
    {
        // Arrange
        var projectId = await CreateProjectAsync();

        // Act
        using var response = await GetModelResponseAsync(projectId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ProfileIdOf(await Files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task GetModel_ProjectReader_Returns200()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var member = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(projectId, member.Id, FileShare.ContentCreator);

        await _filesClient.Authenticate(member);

        // Act
        using var response = await GetModelResponseAsync(projectId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetModel_ProjectReaderThroughGroup_Returns200()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var member = await InviteContact(EmployeeType.User);
        var groupId = await CreateGroupAsync(member);
        await ShareRoomAsync(projectId, groupId, FileShare.ContentCreator);

        await _filesClient.Authenticate(member);

        // Act
        using var response = await GetModelResponseAsync(projectId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetModel_NotAProjectMember_Returns403()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var outsider = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(outsider);

        // Act
        using var response = await GetModelResponseAsync(projectId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetModel_InstructionsRoom_Returns404()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();

        // Act
        using var response = await GetModelResponseAsync(roomId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetModel_ProjectReader_Returns403()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var member = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(projectId, member.Id, FileShare.ContentCreator);

        await _filesClient.Authenticate(member);

        // Act
        using var response = await SetModelResponseAsync(projectId, Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetModel_UnknownProfile_Returns400()
    {
        // Arrange
        var projectId = await CreateProjectAsync();

        // Act
        using var response = await SetModelResponseAsync(projectId, Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ProfileIdOf(await GetModelAsync(projectId)).Should().BeNull();
    }

    [Fact]
    public async Task SetModel_EmptyProfileId_Returns400()
    {
        // Arrange
        var projectId = await CreateProjectAsync();

        // Act
        using var response = await SetModelResponseAsync(projectId, Guid.Empty);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetModel_ProjectManagerWithUnknownProfile_PassesAccessCheckAndReturns400()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var manager = await InviteContact(EmployeeType.RoomAdmin);
        await ShareRoomAsync(projectId, manager.Id, FileShare.RoomManager);

        await _filesClient.Authenticate(manager);

        // Act
        using var response = await SetModelResponseAsync(projectId, Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetModel_InstructionsRoom_Returns404()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();

        // Act
        using var response = await SetModelResponseAsync(roomId, Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private Task<HttpResponseMessage> GetModelResponseAsync(int projectId)
    {
        return Files.GetAsync($"api/2.0/files/rooms/{projectId}/model", TestContext.Current.CancellationToken);
    }

    private async Task<JsonElement> GetModelAsync(int projectId)
    {
        using var response = await GetModelResponseAsync(projectId);

        return await Files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> SetModelResponseAsync(int projectId, Guid profileId)
    {
        return Files.PutAsync($"api/2.0/files/rooms/{projectId}/model", new { profileId }, TestContext.Current.CancellationToken);
    }

    private static Guid? ProfileIdOf(JsonElement model)
    {
        return model.TryGetProperty("profileId", out var profileId) && profileId.ValueKind != JsonValueKind.Null
            ? profileId.GetGuid()
            : null;
    }
}
