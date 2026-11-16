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
/// What a member outside an AI room gets: a DocSpace admin who is not a member of a project reads neither its model,
/// its sources nor its skills, and a member who cannot read an Instructions room is refused its skills folder. The
/// Instructions rooms are listed in the Rooms listing on request (<c>withAiFolder</c>).
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AiHub")]
public class AiRoomOutsiderAccessTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    /// <summary>
    /// Open, no bug number: the requirement bars a DocSpace admin who is not a project member from these reads, and
    /// the chat service refuses such an admin a round in the project, but the Files routes answer 200 to him (the
    /// read check on the project lets a DocSpace admin through).
    /// </summary>
    [Theory]
    [Trait("Category", "Bug")]
    [InlineData("model")]
    [InlineData("sources")]
    [InlineData("skills")]
    public async Task ProjectReads_DocSpaceAdminOutsideTheProject_Return403(string read)
    {
        // Arrange
        var projectId = await CreateProjectAsync();
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        using var response = read switch
        {
            "model" => await Files.GetAsync($"api/2.0/files/rooms/{projectId}/model", TestContext.Current.CancellationToken),
            "sources" => await GetSourcesResponseAsync(projectId),
            _ => await GetRoomSkillsResponseAsync(projectId)
        };

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, $"{read}: {await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
    }

    [Fact]
    public async Task GetRoomAiFolder_UserWhoCannotReadTheRoom_Returns403()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomAiFolderAsync(roomId, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403, exception.ErrorContent!.ToString());
    }

    [Fact]
    public async Task GetRoomAiFolder_RoomReader_Returns200()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();
        var user = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(roomId, user.Id, FileShare.Read);
        await _filesClient.Authenticate(user);

        // Act
        var content = await _roomsApi.GetRoomAiFolderAsync(roomId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        content.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRoomsFolder_AiHubWithAiFolder_ListsTheInstructionsRoomsOnly()
    {
        // Arrange
        var title = $"Autotest Instructions {Guid.NewGuid():N}";
        var projectTitle = $"Autotest Project {Guid.NewGuid():N}";
        await CreateInstructionsRoomAsync(title);
        await CreateProjectAsync(projectTitle);

        // Act
        var listed = (await _roomsApi.GetRoomsFolderAsync(searchArea: SearchArea.AiHub, withAiFolder: true, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        listed.Folders.Select(f => f.Title).Should().Contain(title).And.NotContain(projectTitle);
    }

    /// <summary>
    /// Open, no bug number: the operation's own description says the rooms that provide skills are listed with
    /// <c>GET api/2.0/files/rooms?withAiFolder=true</c>, but without <c>searchArea=AiHub</c> the flag filters the Rooms
    /// section, where no Instructions room lives, and the answer is empty.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task GetRoomsFolder_WithAiFolderAlone_ListsTheInstructionsRoom()
    {
        // Arrange
        var title = $"Autotest Instructions {Guid.NewGuid():N}";
        await CreateInstructionsRoomAsync(title);

        // Act
        var listed = (await _roomsApi.GetRoomsFolderAsync(withAiFolder: true, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        listed.Folders.Should().Contain(f => f.Title == title, "the description of getRoomAiFolder sends callers here");
    }
}
