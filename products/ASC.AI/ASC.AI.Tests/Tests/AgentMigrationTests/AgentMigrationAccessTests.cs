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


namespace ASC.AI.Tests.Tests.AgentMigrationTests;

/// <summary>
/// Requirements "Members of a converted agent" and "Access to the Knowledge room of a converted agent", and the
/// scenario "Pinned agent": who is a member of the project, who keeps a role in the room and who loses it.
/// </summary>
/// <remarks>
/// A role is given to the lowest account that can hold it: Room manager to a Room admin, the other roles to a User.
/// </remarks>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/AgentMigration")]
public class AgentMigrationAccessTests(AspireAppFixture fixture) : AgentMigrationTestBase(fixture)
{
    private const int FileEntryTypeFile = 2;

    [Fact]
    public async Task Convert_ManagerCreatorAndViewer_CarriesTheRolesToTheProject()
    {
        // Arrange
        var title = NewTitle("Team agent");
        var agent = await CreateAgentAsync(title);
        var manager = await InviteToAgentAsync(agent.Id, EmployeeType.RoomAdmin, FileShare.RoomManager);
        var creator = await InviteToAgentAsync(agent.Id, EmployeeType.User, FileShare.ContentCreator);
        var viewer = await InviteToAgentAsync(agent.Id, EmployeeType.User, FileShare.Read);

        // Act
        await ConvertAgentsAsync();

        // Assert
        var shares = await RoomSharesAsync((await ProjectOfAsync(title)).Id);

        AccessOf(shares, manager.Id).Should().Be(FileShare.ContentCreator, "a project has no Room manager role");
        AccessOf(shares, creator.Id).Should().Be(FileShare.ContentCreator);
        AccessOf(shares, viewer.Id).Should().Be(FileShare.ContentCreator, "a Viewer of the agent becomes a Content creator of the project");
    }

    [Fact]
    public async Task Convert_GroupInvitedAsViewer_MakesItsMembersContentCreators()
    {
        // Arrange - the member with a role of their own is a Room admin: nobody below may be a Room manager
        var title = NewTitle("Group agent");
        var agent = await CreateAgentAsync(title);
        var plain = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        var manager = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        var groupId = await CreateGroupAsync(plain.Id, manager.Id);

        await ShareRoomAsync(agent.Id, groupId, FileShare.Read);
        await ShareRoomAsync(agent.Id, manager.Id, FileShare.RoomManager);

        (await RoomSharesAsync(agent.Id)).Should().Contain(share => share.SharedToGroup != null && share.SharedToGroup.Id == groupId, "the group is a member of the agent");

        // Act
        await ConvertAgentsAsync();

        // Assert
        var shares = await RoomSharesAsync((await ProjectOfAsync(title)).Id);

        shares.Should().NotContain(share => share.SharedToGroup != null, "a project has users only");
        AccessOf(shares, plain.Id).Should().Be(FileShare.ContentCreator);
        AccessOf(shares, manager.Id).Should().Be(FileShare.ContentCreator, "a Room manager of the agent is a Content creator of the project");
    }

    [Fact]
    public async Task Convert_UserJoiningTheGroupLater_IsNoMemberOfTheProject()
    {
        // Arrange
        var title = NewTitle("Group agent");
        var agent = await CreateAgentAsync(title);
        var early = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        var late = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        var groupId = await CreateGroupAsync(early.Id);

        await ShareRoomAsync(agent.Id, groupId, FileShare.Read);
        await ConvertAgentsAsync();

        // Act
        await PeopleHttpClient.Authenticate(Owner);
        await GroupApi.AddMembersToAsync(groupId, new MembersRequest([late.Id]), TestContext.Current.CancellationToken);

        // Assert - the member the group had at the conversion is there, the later one is not
        var shares = await RoomSharesAsync((await ProjectOfAsync(title)).Id);

        Lists(shares, early.Id).Should().BeTrue();
        Lists(shares, late.Id).Should().BeFalse("the members of the group are fixed at the moment of the conversion");
    }

    [Fact]
    public async Task Convert_GuestViewerOfTheAgent_IsNotCarriedToTheProject()
    {
        // Arrange
        var title = NewTitle("Guest agent");
        var agent = await CreateAgentAsync(title);
        var viewer = await InviteToAgentAsync(agent.Id, EmployeeType.User, FileShare.Read);
        var guest = await InviteGuest(TestContext.Current.CancellationToken);

        await ShareRoomAsync(agent.Id, guest.Id, FileShare.Read);

        Lists(await RoomSharesAsync(agent.Id), guest.Id).Should().BeTrue("the Guest is a Viewer of the agent");

        // Act
        await ConvertAgentsAsync();

        // Assert - the User with the same role is carried, so the Guest is left out as a Guest
        var shares = await RoomSharesAsync((await ProjectOfAsync(title)).Id);

        Lists(shares, viewer.Id).Should().BeTrue();
        Lists(shares, guest.Id).Should().BeFalse();
    }

    [Fact]
    public async Task Convert_ManagersAndContentCreators_KeepTheirRoleInTheKnowledgeRoomAndViewersLoseIt()
    {
        // Arrange
        var title = NewTitle("Room agent");
        var agent = await CreateAgentAsync(title);
        await AddKnowledgeFileAsync(agent.Id, "autotest-knowledge.txt");

        var manager = await InviteToAgentAsync(agent.Id, EmployeeType.RoomAdmin, FileShare.RoomManager);
        var creator = await InviteToAgentAsync(agent.Id, EmployeeType.User, FileShare.ContentCreator);
        var viewer = await InviteToAgentAsync(agent.Id, EmployeeType.User, FileShare.Read);

        // A group can be invited to an agent as a Viewer only: the API refuses a group as a Content creator
        // ("The role is not available for this user type", FileSharing), so no test builds a group that keeps the room.
        var viewersGroup = await CreateGroupAsync((await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken)).Id);
        await ShareRoomAsync(agent.Id, viewersGroup, FileShare.Read);

        var before = await RoomSharesAsync(agent.Id);
        Lists(before, viewer.Id).Should().BeTrue("the Viewer is a member of the agent");
        GroupIdsOf(before).Should().Equal(viewersGroup);

        // Act
        await ConvertAgentsAsync();

        // Assert
        var shares = await RoomSharesAsync(agent.Id);

        AccessOf(shares, manager.Id).Should().Be(FileShare.RoomManager);
        AccessOf(shares, creator.Id).Should().Be(FileShare.ContentCreator);
        Lists(shares, viewer.Id).Should().BeFalse("a Viewer reads the room through the project only");
        GroupIdsOf(shares).Should().BeEmpty("a group invited as a Viewer reads the room through the project only");
    }

    [Fact]
    public async Task Convert_ViewerOfTheAgent_ReadsTheKnowledgeThroughTheProject()
    {
        // Arrange
        var title = NewTitle("Room agent");
        var agent = await CreateAgentAsync(title);
        var file = await AddKnowledgeFileAsync(agent.Id, "autotest-knowledge.txt");
        var viewer = await InviteToAgentAsync(agent.Id, EmployeeType.User, FileShare.Read);

        Lists(await RoomSharesAsync(agent.Id), viewer.Id).Should().BeTrue("the Viewer is listed in the agent");

        // Act
        await ConvertAgentsAsync();

        // Assert
        (await FileReadStatusAsync(file.Id, viewer)).Should().Be(200);
        Lists(await RoomSharesAsync(agent.Id), viewer.Id).Should().BeFalse("the Viewer holds no role in the Knowledge room");
    }

    [Fact]
    public async Task Convert_GuestViewerOfTheAgent_Gets403ForAKnowledgeFile()
    {
        // Arrange
        var title = NewTitle("Room agent");
        var agent = await CreateAgentAsync(title);
        var file = await AddKnowledgeFileAsync(agent.Id, "autotest-knowledge.txt");
        var guest = await InviteGuest(TestContext.Current.CancellationToken);

        await ShareRoomAsync(agent.Id, guest.Id, FileShare.Read);

        // The Guest reads the file while it is knowledge of the agent: the 403 below is the conversion.
        (await FileReadStatusAsync(file.Id, guest)).Should().Be(200);

        // Act
        await ConvertAgentsAsync();

        // Assert
        (await FileReadStatusAsync(file.Id, guest)).Should().Be(403);
    }

    [Fact]
    public async Task Convert_ExternalLinkOfAKnowledgeFile_IsRemoved()
    {
        // Arrange - a file in a Knowledge folder takes no new link, so the link is made while the file lies in
        // Chat outputs and travels with it into Knowledge
        var title = NewTitle("Link agent");
        var agent = await CreateAgentAsync(title);
        var file = await AddOutputsFileAsync(agent.Id, "autotest-linked.txt");

        await FilesHttpClient.Authenticate(Owner);
        await FilesApi.SetFileExternalLinkAsync(
            file.Id,
            new FileLinkRequest(access: FileShare.Read, title: "Autotest link"),
            TestContext.Current.CancellationToken);

        var links = (await FilesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        links.Should().NotBeEmpty("the file has an external link");

        await MoveToKnowledgeAsync(agent.Id, file);
        (await StoredSharesOfFileAsync(file.Id)).Should().BeGreaterThan(0, "the link is stored for the knowledge file");

        // Act
        await ConvertAgentsAsync();

        // Assert - read from the database, so that the assertion does not depend on who may list the links of a
        // file in a Knowledge room
        (await StoredSharesOfFileAsync(file.Id)).Should().Be(0);
        (await RoomInfoAsync(agent.Id)).RoomType.Should().Be(RoomType.KnowledgeRoom);
    }

    [Fact]
    public async Task Convert_AgentPinnedByAMember_PinsTheProjectForThatMember()
    {
        // Arrange
        var title = NewTitle("Pinned agent");
        var agent = await CreateAgentAsync(title);
        var member = await InviteToAgentAsync(agent.Id, EmployeeType.User, FileShare.Read);

        await FilesHttpClient.Authenticate(member);
        await RoomsApi.PinRoomAsync(agent.Id, TestContext.Current.CancellationToken);

        (await RoomInfoAsync(agent.Id, member)).Pinned.Should().BeTrue("the member pinned the agent");

        // Act
        await ConvertAgentsAsync();

        // Assert - a pin belongs to the user who set it
        (await ProjectOfAsync(title, member)).Pinned.Should().BeTrue();
        (await ProjectOfAsync(title)).Pinned.Should().BeFalse();
    }

    private async Task<User> InviteToAgentAsync(int agentId, EmployeeType type, FileShare access)
    {
        var member = await InviteContact(type, TestContext.Current.CancellationToken);

        await ShareRoomAsync(agentId, member.Id, access);

        return member;
    }

    /// <summary>Creates a group of the given accounts; the owner is its manager, as a group needs one.</summary>
    private async Task<Guid> CreateGroupAsync(params Guid[] members)
    {
        await PeopleHttpClient.Authenticate(Owner);

        var group = (await GroupApi.AddGroupAsync(
            new GroupRequestDto(members: [.. members], groupManager: Owner.Id, groupName: $"autotest-{Guid.NewGuid().ToString("N")[..10]}"),
            TestContext.Current.CancellationToken)).Response;

        return group.Id;
    }

    private static List<Guid> GroupIdsOf(List<FileShareDto> shares)
    {
        return [.. shares.Where(share => share.SharedToGroup != null).Select(share => share.SharedToGroup.Id)];
    }

    private async Task<long> StoredSharesOfFileAsync(int fileId)
    {
        await using var connection = await OpenDatabaseAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT COUNT(*) FROM files_security WHERE entry_id = @id AND entry_type = @type";
        command.Parameters.AddWithValue("@id", fileId.ToString());
        command.Parameters.AddWithValue("@type", FileEntryTypeFile);

        return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }
}
