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
/// Requirement "Conversion of an AI agent into an AI project": the settings of an agent that the project takes over
/// (tool preferences, MCP servers, chat preferences, colour, cover, custom tags) and the marks it does not (Recent,
/// New).
/// </summary>
/// <remarks>
/// Tool preferences, MCP servers and chat preferences scoped to an agent have no public route: the chat service
/// stores them under no entity. They are arranged and read through the internal storage API of ASC.AI, as the
/// chat service reads them.
/// </remarks>
[Trait("Category", "Migration")]
[Trait("Feature", "AI/AgentMigration")]
public class AgentMigrationSettingsTests(AspireAppFixture fixture) : AgentMigrationTestBase(fixture)
{
    private const int NewTagType = 1;
    private const int RecentTagType = 16;
    private const int FolderEntryType = 1;

    [Fact]
    public async Task Convert_AgentWithToolPreferences_MovesThemToTheProject()
    {
        // Arrange
        var title = NewTitle("Tools agent");
        var agent = await CreateAgentAsync(title);

        await _aiClient.Authenticate(Owner);
        await UpsertDisabledToolPrefsInternalAsync(new Dictionary<string, HashSet<string>> { ["server-1"] = ["tool-a"] }, agent.Id.ToString());
        await UpsertAllowAlwaysToolPrefsInternalAsync(new Dictionary<string, HashSet<string>> { ["server-1"] = ["tool-b"] }, agent.Id.ToString());

        (await ReadToolPrefsInternalAsync(agent.Id.ToString())).Should().ContainKey("server-1", "the agent has tool preferences before the conversion");

        // Act
        await ConvertAgentsAsync();

        // Assert
        var project = await ProjectOfAsync(title);

        await _aiClient.Authenticate(Owner);
        var prefs = await ReadToolPrefsInternalAsync(project.Id.ToString());

        prefs.Should().ContainKey("server-1");
        prefs["server-1"].Disabled.Should().BeEquivalentTo(["tool-a"]);
        prefs["server-1"].AllowAlways.Should().BeEquivalentTo(["tool-b"]);
    }

    [Fact]
    public async Task Convert_AgentWithAnMcpServer_MovesItToTheProject()
    {
        // Arrange
        var title = NewTitle("Mcp agent");
        var agent = await CreateAgentAsync(title);
        var name = $"autotest-{Guid.NewGuid():N}";

        await _aiClient.Authenticate(Owner);
        await CreateMcpServerInternalAsync(name, entityId: agent.Id.ToString());

        (await ReadAllMcpServersInternalAsync(agent.Id.ToString())).Select(server => server.Name).Should().Contain(name);

        // Act
        await ConvertAgentsAsync();

        // Assert
        var project = await ProjectOfAsync(title);

        await _aiClient.Authenticate(Owner);
        (await ReadAllMcpServersInternalAsync(project.Id.ToString())).Select(server => server.Name).Should().Equal(name);
        (await ReadAllMcpServersInternalAsync()).Select(server => server.Name).Should().NotContain(name, "the server stays scoped to the project");
    }

    [Fact]
    public async Task Convert_AgentWithPreferences_MovesThemToTheProject()
    {
        // Arrange
        var title = NewTitle("Preferences agent");
        var agent = await CreateAgentAsync(title);

        await _aiClient.Authenticate(Owner);
        await UpsertPreferencesInternalAsync(ReasoningDepth.Low);
        await UpsertPreferencesInternalAsync(ReasoningDepth.High, agent.Id.ToString());

        (await ReadPreferencesInternalAsync(agent.Id.ToString()))!.Depth.Should().Be(ReasoningDepth.High);

        // Act
        await ConvertAgentsAsync();

        // Assert
        var project = await ProjectOfAsync(title);

        await _aiClient.Authenticate(Owner);
        (await ReadPreferencesInternalAsync(project.Id.ToString()))!.Depth.Should().Be(ReasoningDepth.High);
        (await ReadPreferencesInternalAsync())!.Depth.Should().Be(ReasoningDepth.Low, "the preferences without an entity are not touched");
    }

    [Fact]
    public async Task Convert_AgentWithAColourACoverAndACustomTag_GivesThemToTheProject()
    {
        // Arrange
        var title = NewTitle("Styled agent");
        var agent = await CreateAgentAsync(title);
        var tag = $"autotest-{Guid.NewGuid().ToString("N")[..10]}";

        await FilesHttpClient.Authenticate(Owner);
        var cover = (await RoomsApi.GetRoomCoversAsync(TestContext.Current.CancellationToken)).Response[0].Id;
        await RoomsApi.ChangeRoomCoverAsync(agent.Id, new CoverRequestDto("1A2B3C", cover), TestContext.Current.CancellationToken);
        await RoomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tag), TestContext.Current.CancellationToken);
        await RoomsApi.AddRoomTagsAsync(agent.Id, new BatchTagsRequestDto([tag]), TestContext.Current.CancellationToken);

        await AddKnowledgeFileAsync(agent.Id, "autotest-knowledge.txt");

        var before = await RoomInfoAsync(agent.Id);
        before.Logo.Color.Should().Be("1A2B3C");
        before.Tags.Should().Contain(tag);

        // Act
        await ConvertAgentsAsync();

        // Assert
        var project = await ProjectOfAsync(title);

        project.Logo.Color.Should().Be("1A2B3C");
        project.Logo.Cover.Id.Should().Be(cover);
        project.Tags.Should().Equal(tag);
        (await RoomInfoAsync(agent.Id)).Tags.Should().BeNullOrEmpty("the custom tags move from the room to the project");
    }

    [Fact]
    public async Task Convert_AgentWithLifetimeWatermarkDenyDownloadAndIndexing_DropsThemFromTheKnowledgeRoom()
    {
        // Arrange
        var title = NewTitle("Restricted agent");
        var agent = await CreateAgentAsync(title);

        await FilesHttpClient.Authenticate(Owner);
        await RoomsApi.UpdateRoomAsync(
            agent.Id,
            new UpdateRoomRequest(
                denyDownload: true,
                lifetime: new RoomDataLifetimeDto(deletePermanently: false, period: RoomDataLifetimePeriod.Day, value: 30, enabled: true),
                watermark: new WatermarkRequestDto(enabled: true, additions: WatermarkAdditions.UserName, text: "Conf", rotate: 0, imageScale: 100)),
            TestContext.Current.CancellationToken);
        await TurnIndexingOnAsync(agent.Id);

        await AddKnowledgeFileAsync(agent.Id, "autotest-knowledge.txt");

        var before = await RoomInfoAsync(agent.Id);
        before.Indexing.Should().BeTrue();
        before.DenyDownload.Should().BeTrue();
        before.Lifetime.Should().NotBeNull();
        before.Watermark.Should().NotBeNull();

        // Act
        await ConvertAgentsAsync();

        // Assert
        var room = await RoomInfoAsync(agent.Id);

        room.RoomType.Should().Be(RoomType.KnowledgeRoom);
        room.Indexing.Should().BeFalse();
        room.DenyDownload.Should().BeFalse();
        room.Lifetime.Should().BeNull("the lifetime would go on deleting the knowledge files");
        room.Watermark.Should().BeNull();
    }

    [Fact]
    public async Task Convert_AgentWithNewAndRecentMarks_DropsThem()
    {
        // Arrange - a member gets a "new" mark on the agent when the owner adds a file to it; no route marks a room
        // as recent, so that mark is written into the database
        var title = NewTitle("Marked agent");
        var agent = await CreateAgentAsync(title);
        var member = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await ShareRoomAsync(agent.Id, member.Id, FileShare.ContentCreator);

        await AddKnowledgeFileAsync(agent.Id, "autotest-knowledge.txt");
        await AddOutputsFileAsync(agent.Id, "autotest-output.txt");
        await AddRecentMarkAsync(agent.Id, member.Id);

        (await WaitForMarksAsync(agent.Id, NewTagType)).Should().BeGreaterThan(0, "the member has a new mark on the agent");
        (await MarksOnAsync(agent.Id, RecentTagType)).Should().BeGreaterThan(0);

        // Act
        await ConvertAgentsAsync();

        // Assert - the room keeps the id of the agent, so a mark left on it would still be read
        var project = await ProjectOfAsync(title);

        (await MarksOnAsync(agent.Id, NewTagType)).Should().Be(0);
        (await MarksOnAsync(agent.Id, RecentTagType)).Should().Be(0);
        (await MarksOnAsync(project.Id, NewTagType)).Should().Be(0, "a mark of the agent is not moved to the project");
        (await MarksOnAsync(project.Id, RecentTagType)).Should().Be(0);
    }

    /// <summary>The marks of one type linked to a folder, for any user.</summary>
    private async Task<long> MarksOnAsync(int folderId, int tagType)
    {
        await using var connection = await OpenDatabaseAsync();
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT COUNT(*) FROM files_tag_link l
            JOIN files_tag t ON t.tenant_id = l.tenant_id AND t.id = l.tag_id
            WHERE l.entry_type = @entryType AND l.entry_id = @id AND t.flag = @flag
            """;
        command.Parameters.AddWithValue("@entryType", FolderEntryType);
        command.Parameters.AddWithValue("@id", folderId.ToString());
        command.Parameters.AddWithValue("@flag", tagType);

        return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>The "new" marks are written after the upload answers, so they are polled.</summary>
    private async Task<long> WaitForMarksAsync(int folderId, int tagType)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var count = await MarksOnAsync(folderId, tagType);

            if (count > 0 || DateTime.UtcNow >= deadline)
            {
                return count;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    private async Task TurnIndexingOnAsync(int roomId)
    {
        await using var connection = await OpenDatabaseAsync();
        await using var command = connection.CreateCommand();

        command.CommandText = "UPDATE files_room_settings SET indexing = 1 WHERE room_id = @id";
        command.Parameters.AddWithValue("@id", roomId);

        (await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    private async Task AddRecentMarkAsync(int folderId, Guid userId)
    {
        await using var connection = await OpenDatabaseAsync();
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO files_tag (tenant_id, name, owner, flag)
            SELECT tenant_id, 'recent', @owner, @flag FROM files_folder WHERE id = @id;
            INSERT INTO files_tag_link (tenant_id, tag_id, entry_type, entry_id, create_by, create_on, tag_count)
            SELECT tenant_id, LAST_INSERT_ID(), @entryType, @id, @owner, UTC_TIMESTAMP(), 0 FROM files_folder WHERE id = @id;
            """;
        command.Parameters.AddWithValue("@owner", userId.ToString());
        command.Parameters.AddWithValue("@flag", RecentTagType);
        command.Parameters.AddWithValue("@entryType", FolderEntryType);
        command.Parameters.AddWithValue("@id", folderId);

        (await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken)).Should().Be(2);
    }
}
