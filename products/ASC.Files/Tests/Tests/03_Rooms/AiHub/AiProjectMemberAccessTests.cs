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
/// Read access a member of an AI project gets to the rooms linked to the project. The pinned SDK cannot deserialize
/// entries of the AI rooms section (it has no folder type 41), so the room and its files are read over raw HTTP.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiProjectMemberAccessTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task LinkedRoom_ProjectMember_ReadsRoomAndItsFile()
    {
        // Arrange
        var (projectId, roomId, fileId) = await CreateLinkedRoomWithSkillAsync();
        var member = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(projectId, member.Id, FileShare.ContentCreator);

        await _filesClient.Authenticate(member);

        // Act
        using var content = await GetFolderContentResponseAsync(roomId);
        using var file = await GetFileResponseAsync(fileId);

        // Assert
        content.StatusCode.Should().Be(HttpStatusCode.OK);
        var listing = await Files.ReadAsync<JsonElement>(content, TestContext.Current.CancellationToken);
        listing.GetProperty("files").EnumerateArray().Select(f => f.GetProperty("id").GetInt32()).Should().Contain(fileId);
        listing.GetProperty("current").GetProperty("access").GetInt32().Should().Be((int)FileShare.Read);
        file.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LinkedRoom_NotAProjectMember_Returns403()
    {
        // Arrange
        var (_, roomId, fileId) = await CreateLinkedRoomWithSkillAsync();
        var outsider = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(outsider);

        // Act
        using var content = await GetFolderContentResponseAsync(roomId);
        using var file = await GetFileResponseAsync(fileId);

        // Assert
        content.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        file.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LinkedRoom_ProjectMemberThroughGroup_ReadsFile()
    {
        // Arrange
        var (projectId, _, fileId) = await CreateLinkedRoomWithSkillAsync();
        var member = await InviteContact(EmployeeType.User);
        var groupId = await CreateGroupAsync(member);
        await ShareRoomAsync(projectId, groupId, FileShare.ContentCreator);

        await _filesClient.Authenticate(member);

        // Act
        using var file = await GetFileResponseAsync(fileId);

        // Assert
        file.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LinkedRoom_AfterUnlink_ProjectMemberLosesAccess()
    {
        // Arrange
        var (projectId, roomId, fileId) = await CreateLinkedRoomWithSkillAsync();
        var member = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(projectId, member.Id, FileShare.ContentCreator);
        using var unlinked = await UnlinkSourcesAsync(projectId, roomId);

        await _filesClient.Authenticate(member);

        // Act
        using var file = await GetFileResponseAsync(fileId);

        // Assert
        unlinked.StatusCode.Should().Be(HttpStatusCode.NoContent);
        file.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LinkedRoom_RemovedFromProject_LosesAccess()
    {
        // Arrange
        var (projectId, _, fileId) = await CreateLinkedRoomWithSkillAsync();
        var member = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(projectId, member.Id, FileShare.ContentCreator);
        await ShareRoomAsync(projectId, member.Id, FileShare.None);

        await _filesClient.Authenticate(member);

        // Act
        using var file = await GetFileResponseAsync(fileId);

        // Assert
        file.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LinkedRoom_ContentCreatorThroughGroup_IsNotDowngradedByProject()
    {
        // Arrange
        var (projectId, roomId, _) = await CreateLinkedRoomWithSkillAsync();
        var member = await InviteContact(EmployeeType.User);
        var groupId = await CreateGroupAsync(member);
        await ShareRoomAsync(roomId, groupId, FileShare.ContentCreator);
        await ShareRoomAsync(projectId, member.Id, FileShare.ContentCreator);

        await _filesClient.Authenticate(member);

        // Act
        using var content = await GetFolderContentResponseAsync(roomId);

        // Assert
        var listing = await Files.ReadAsync<JsonElement>(content, TestContext.Current.CancellationToken);
        listing.GetProperty("current").GetProperty("access").GetInt32().Should().Be((int)FileShare.ContentCreator);
    }

    [Fact]
    public async Task LinkedRoom_IsNotListedInProjectMembersSection()
    {
        // Arrange
        var (projectId, roomId, _) = await CreateLinkedRoomWithSkillAsync();
        var member = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(projectId, member.Id, FileShare.ContentCreator);

        await _filesClient.Authenticate(member);

        // Act
        var section = await GetAiHubSectionAsync();

        // Assert
        IdsOf(section).Should().Contain(projectId).And.NotContain(roomId);
    }

    [Fact]
    public async Task RoomsSkills_ProjectMember_GetsLinkedRoom()
    {
        // Arrange
        var (projectId, roomId, _) = await CreateLinkedRoomWithSkillAsync();
        var member = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(projectId, member.Id, FileShare.ContentCreator);

        await _filesClient.Authenticate(member);

        // Act
        var result = await GetRoomsSkillsAsync(roomId);

        // Assert
        result.Rooms.Should().ContainSingle().Which.Id.Should().Be(roomId);
    }

    private async Task<(int ProjectId, int RoomId, int FileId)> CreateLinkedRoomWithSkillAsync()
    {
        var projectId = await CreateProjectAsync();
        var roomId = await CreateInstructionsRoomAsync();
        var fileId = await UploadSkillAsync(roomId, "reviewer");

        using var linked = await LinkSourcesAsync(projectId, roomId);
        linked.StatusCode.Should().Be(HttpStatusCode.NoContent);

        return (projectId, roomId, fileId);
    }
}
