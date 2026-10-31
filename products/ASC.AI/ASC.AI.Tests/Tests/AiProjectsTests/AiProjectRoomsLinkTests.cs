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

namespace ASC.AI.Tests.Tests.AiProjectsTests;

[Trait("Category", "Update")]
[Trait("Feature", "AI/Projects")]
public class AiProjectRoomsLinkTests(AspireAppFixture fixture) : AiProjectRoomsTestBase(fixture)
{
    private const string AgentsPath = "/internal/ai/agents";

    [Fact]
    public async Task GetRooms_NewProject_ReturnsEmptyLists()
    {
        // Arrange
        var projectId = await CreateProjectAsync();

        // Act
        var rooms = await GetRoomsAsync(projectId);

        // Assert
        rooms.GetProperty("instructionsRooms").GetArrayLength().Should().Be(0);
        rooms.GetProperty("knowledgeRooms").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task LinkRooms_AsOwner_ReturnsRoomsSortedByTitle()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var second = await CreateInstructionsRoomAsync("Autotest B Instructions");
        var first = await CreateInstructionsRoomAsync("Autotest A Instructions");

        // Act
        using var response = await LinkRoomsAsync(projectId, second, first);
        var rooms = await GetRoomsAsync(projectId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var instructions = rooms.GetProperty("instructionsRooms").EnumerateArray().ToList();
        instructions.Select(r => r.GetProperty("id").GetInt32()).Should().Equal(first, second);
        instructions.Select(r => r.GetProperty("title").GetString()).Should().Equal("Autotest A Instructions", "Autotest B Instructions");
        instructions.Should().AllSatisfy(r => r.TryGetProperty("logo", out _).Should().BeTrue());
        rooms.GetProperty("knowledgeRooms").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task LinkRooms_AlreadyLinkedAndDuplicateIds_LinksEachRoomOnce()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var linked = await CreateInstructionsRoomAsync();
        var added = await CreateInstructionsRoomAsync();
        using var first = await LinkRoomsAsync(projectId, linked);

        // Act
        using var response = await LinkRoomsAsync(projectId, linked, added, added);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetInstructionsRoomIdsAsync(projectId)).Should().BeEquivalentTo([linked, added]);
    }

    [Fact]
    public async Task LinkRooms_RoomOfAnotherType_Returns400AndLinksNothing()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var instructions = await CreateInstructionsRoomAsync();
        var agent = await CreateRoomAsync();

        // Act
        using var response = await LinkRoomsAsync(projectId, instructions, agent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain(agent.ToString());
        (await GetInstructionsRoomIdsAsync(projectId)).Should().BeEmpty();
    }

    [Fact]
    public async Task LinkRooms_UnknownRoom_Returns400()
    {
        // Arrange
        var projectId = await CreateProjectAsync();

        // Act
        using var response = await LinkRoomsAsync(projectId, int.MaxValue);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LinkRooms_DeletedRoom_Returns400()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var room = await CreateInstructionsRoomAsync();
        await DeleteRoomAsync(room);

        // Act
        using var response = await LinkRoomsAsync(projectId, room);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LinkRooms_EmptyRoomIds_Returns400()
    {
        // Arrange
        var projectId = await CreateProjectAsync();

        // Act
        using var response = await LinkRoomsAsync(projectId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LinkRooms_MoreThanTwoHundredIds_Returns400()
    {
        // Arrange
        var projectId = await CreateProjectAsync();

        // Act
        using var response = await LinkRoomsAsync(projectId, [.. Enumerable.Range(1, 201)]);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LinkRooms_RoomTheCallerCannotRead_Returns400()
    {
        // Arrange
        var room = await CreateInstructionsRoomAsync();

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        await _aiClient.Authenticate(roomAdmin);
        var projectId = await CreateProjectAsync();

        // Act
        using var response = await LinkRoomsAsync(projectId, room);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetInstructionsRoomIdsAsync(projectId)).Should().BeEmpty();
    }

    [Fact]
    public async Task LinkRooms_AgentInsteadOfProject_Returns404()
    {
        // Arrange
        var room = await CreateInstructionsRoomAsync();
        using var created = await _ai.PostAsync(AgentsPath, new { title = "Autotest Agent" }, TestContext.Current.CancellationToken);
        var agent = await _ai.ReadAsync<JsonElement>(created, TestContext.Current.CancellationToken);

        // Act
        using var response = await LinkRoomsAsync(agent.GetProperty("id").GetInt32(), room);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LinkRooms_UserWithoutAccess_Returns403()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var room = await CreateInstructionsRoomAsync();

        var user = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await _aiClient.Authenticate(user);

        // Act
        using var response = await LinkRoomsAsync(projectId, room);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetRooms_UserWithoutAccess_Returns403()
    {
        // Arrange
        var projectId = await CreateProjectAsync();

        var user = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await _aiClient.Authenticate(user);

        // Act
        using var response = await _ai.GetAsync($"{AiProjectsPath}/{projectId}/rooms", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
