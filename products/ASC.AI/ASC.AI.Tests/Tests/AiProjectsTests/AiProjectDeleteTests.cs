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

[Trait("Category", "Delete")]
[Trait("Feature", "AI/Projects")]
public class AiProjectDeleteTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string AiProjectsPath = "/internal/ai/projects";
    private const string AgentsPath = "/internal/ai/agents";
    private static readonly TimeSpan _deletionDeadline = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task DeleteProject_AsOwner_ProjectDisappears()
    {
        // Arrange
        var projectId = await CreateProjectAsync("Autotest Project");

        // Act
        using (var response = await _ai.DeleteAsync($"{AiProjectsPath}/{projectId}", new { deleteAfter = false }, TestContext.Current.CancellationToken))
        {
            response.EnsureSuccessStatusCode();
        }

        // Assert
        var status = await WaitForStatusAsync($"{AiProjectsPath}/{projectId}", HttpStatusCode.NotFound);
        status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProject_AgentId_Returns404()
    {
        // Arrange
        using var created = await _ai.PostAsync(AgentsPath, new { title = "Autotest Agent" }, TestContext.Current.CancellationToken);
        var agent = await _ai.ReadAsync<JsonElement>(created, TestContext.Current.CancellationToken);

        // Act
        using var response = await _ai.DeleteAsync($"{AiProjectsPath}/{agent.GetProperty("id").GetInt32()}", new { deleteAfter = false }, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProject_UserWithoutAccess_Returns403()
    {
        // Arrange
        var projectId = await CreateProjectAsync("Autotest Hidden Project");

        var user = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await _aiClient.Authenticate(user);

        // Act
        using var response = await _ai.DeleteAsync($"{AiProjectsPath}/{projectId}", new { deleteAfter = false }, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<int> CreateProjectAsync(string title)
    {
        using var response = await _ai.PostAsync(AiProjectsPath, new { title }, TestContext.Current.CancellationToken);
        var project = await _ai.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);

        return project.GetProperty("id").GetInt32();
    }

    private async Task<HttpStatusCode> WaitForStatusAsync(string path, HttpStatusCode expected)
    {
        var deadline = DateTime.UtcNow + _deletionDeadline;
        HttpStatusCode status;

        do
        {
            using var response = await _ai.GetAsync(path, TestContext.Current.CancellationToken);
            status = response.StatusCode;
            if (status == expected)
            {
                break;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
        while (DateTime.UtcNow < deadline);

        return status;
    }
}
