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
/// The skills of one room of the AI rooms section: an Instructions room answers its own skills, an AI project the
/// skills of the Instructions rooms linked to it.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiRoomSkillsTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task GetSkills_InstructionsRoom_ReturnsItsSkills()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync("Autotest Own Skills");
        var fileId = await UploadSkillAsync(roomId, "brand-voice", "Writes in the brand voice");

        // Act
        var result = await GetRoomSkillsAsync(roomId);

        // Assert
        var room = result.Rooms.Should().ContainSingle().Subject;
        room.Id.Should().Be(roomId);
        room.Skills.Select(s => (s.Id, s.Name, s.Description)).Should().Equal((fileId.ToString(), "brand-voice", "Writes in the brand voice"));
    }

    [Fact]
    public async Task GetSkills_ProjectWithLinkedRooms_ReturnsThemSortedByTitleWithTheirSkills()
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var second = await CreateInstructionsRoomAsync("Autotest B Instructions");
        var first = await CreateInstructionsRoomAsync("Autotest A Instructions");
        await UploadSkillAsync(first, "brand-voice");
        await UploadSkillAsync(second, "pdf-review");
        using var linked = await LinkSourcesAsync(projectId, second, first);

        // Act
        var result = await GetRoomSkillsAsync(projectId);

        // Assert
        result.Rooms.Select(r => r.Id).Should().Equal(first, second);
        result.Rooms[0].Skills.Select(s => s.Name).Should().Equal("brand-voice");
        result.Rooms[1].Skills.Select(s => s.Name).Should().Equal("pdf-review");
    }

    [Fact]
    public async Task GetSkills_ProjectWithoutLinkedRooms_ReturnsEmptyList()
    {
        // Arrange
        var projectId = await CreateProjectAsync();

        // Act
        var result = await GetRoomSkillsAsync(projectId);

        // Assert
        result.Rooms.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSkills_AgentRoom_Returns404()
    {
        // Arrange
        var agentId = (await CreateAiRoom("Autotest Agent")).Id;

        // Act
        using var response = await GetRoomSkillsResponseAsync(agentId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSkills_UnknownRoom_Returns404()
    {
        // Act
        using var response = await GetRoomSkillsResponseAsync(int.MaxValue);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSkills_ProjectUserWithoutAccess_Returns403()
    {
        // Arrange
        var projectId = await CreateProjectAsync();

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        using var response = await GetRoomSkillsResponseAsync(projectId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSkills_InstructionsRoomUserWithoutAccess_Returns403()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        using var response = await GetRoomSkillsResponseAsync(roomId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
