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

[Trait("Category", "Create")]
[Trait("Feature", "AI/Projects")]
public class AiProjectCreateTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string AiProjectsPath = "/internal/ai/projects";
    private const string AiProjectRoomType = "12";
    private const string AiProjectsRootFolderType = "41";

    [Fact]
    public async Task CreateProject_AsOwner_ReturnsProjectInProjectsRoot()
    {
        // Act
        using var response = await _ai.PostAsync(AiProjectsPath, new { title = "Autotest Project", color = "FF6600", tags = new[] { "autotest" } }, TestContext.Current.CancellationToken);
        var project = await _ai.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);

        // Assert
        project.GetProperty("title").GetString().Should().Be("Autotest Project");
        project.GetProperty("roomType").GetRawText().Should().Be(AiProjectRoomType);
        project.GetProperty("rootFolderType").GetRawText().Should().Be(AiProjectsRootFolderType);
        project.GetProperty("logo").GetProperty("color").GetString().Should().Be("FF6600");
        project.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).Should().Equal("autotest");
    }

    [Fact]
    public async Task CreateProject_AsOwner_ProjectIsListed()
    {
        // Arrange
        using var created = await _ai.PostAsync(AiProjectsPath, new { title = "Autotest Listed Project" }, TestContext.Current.CancellationToken);
        var project = await _ai.ReadAsync<JsonElement>(created, TestContext.Current.CancellationToken);

        // Act
        using var response = await _ai.GetAsync(AiProjectsPath, TestContext.Current.CancellationToken);
        var content = await _ai.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);

        // Assert
        content.GetProperty("total").GetInt32().Should().Be(1);
        content.GetProperty("folders")[0].GetProperty("id").GetInt32().Should().Be(project.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task CreateProject_AsUser_Returns403()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await _aiClient.Authenticate(user);

        // Act
        using var response = await _ai.PostAsync(AiProjectsPath, new { title = "Autotest Forbidden Project" }, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProject_WithoutTitle_Returns400()
    {
        // Act
        using var response = await _ai.PostAsync(AiProjectsPath, new { color = "FF6600" }, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
