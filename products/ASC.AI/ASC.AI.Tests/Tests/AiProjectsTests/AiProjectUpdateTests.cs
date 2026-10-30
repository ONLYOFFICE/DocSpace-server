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
public class AiProjectUpdateTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string AiProjectsPath = "/internal/ai/projects";
    private const string AgentsPath = "/internal/ai/agents";

    [Fact]
    public async Task UpdateProject_AsOwner_AppliesChanges()
    {
        // Arrange
        var projectId = await CreateProjectAsync("Autotest Project");

        // Act
        using var response = await _ai.PutAsync($"{AiProjectsPath}/{projectId}", new { title = "Autotest Renamed Project", color = "AB12CD", tags = new[] { "autotest" } }, TestContext.Current.CancellationToken);
        var project = await _ai.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);

        // Assert
        project.GetProperty("title").GetString().Should().Be("Autotest Renamed Project");
        project.GetProperty("logo").GetProperty("color").GetString().Should().Be("AB12CD");
        project.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).Should().Equal("autotest");
    }

    [Fact]
    public async Task UpdateProject_RoomOnlyProperty_Returns400()
    {
        // Arrange
        var projectId = await CreateProjectAsync("Autotest Project");

        // Act
        using var response = await _ai.PutAsync($"{AiProjectsPath}/{projectId}", new { quota = 1048576 }, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProject_AgentId_Returns404()
    {
        // Arrange
        using var created = await _ai.PostAsync(AgentsPath, new { title = "Autotest Agent" }, TestContext.Current.CancellationToken);
        var agent = await _ai.ReadAsync<JsonElement>(created, TestContext.Current.CancellationToken);

        // Act
        using var response = await _ai.PutAsync($"{AiProjectsPath}/{agent.GetProperty("id").GetInt32()}", new { title = "Autotest Renamed Agent" }, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProject_UserWithoutAccess_Returns403()
    {
        // Arrange
        var projectId = await CreateProjectAsync("Autotest Hidden Project");

        var user = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await _aiClient.Authenticate(user);

        // Act
        using var response = await _ai.PutAsync($"{AiProjectsPath}/{projectId}", new { title = "Autotest Renamed Project" }, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<int> CreateProjectAsync(string title)
    {
        using var response = await _ai.PostAsync(AiProjectsPath, new { title }, TestContext.Current.CancellationToken);
        var project = await _ai.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);

        return project.GetProperty("id").GetInt32();
    }
}
