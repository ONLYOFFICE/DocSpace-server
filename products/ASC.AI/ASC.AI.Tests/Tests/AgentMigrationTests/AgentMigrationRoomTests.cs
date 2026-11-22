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
/// Requirements "Conversion of an AI agent into an AI project", "The room of a converted agent becomes a Knowledge
/// room" and "An agent without knowledge leaves no room": what the sections, the room and its files answer after
/// the conversion.
/// </summary>
/// <remarks>
/// The stand runs with vectorization off, so every knowledge file here is arranged by
/// <see cref="AgentMigrationTestBase.AddKnowledgeFileAsync"/> and every case also holds for a portal without it.
/// </remarks>
[Trait("Category", "Migration")]
[Trait("Feature", "AI/AgentMigration")]
public class AgentMigrationRoomTests(AspireAppFixture fixture) : AgentMigrationTestBase(fixture)
{
    // The minimal valid 1x1 PNG the room logo suites of the Files tests use.
    private const string LogoImageBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==";

    [Fact]
    public async Task Convert_Agent_ListsAProjectWithItsTitleAndOwnerAndNoAgent()
    {
        // Arrange - the agent belongs to a Room admin, so the owner of the project is neither the caller of the
        // route nor the system account the conversion runs under
        var author = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        var title = NewTitle("Agent");
        var agent = await CreateAgentAsync(title, author);

        (await AgentsInAnySectionAsync()).Select(room => room.Id).Should().Contain(agent.Id, "the agent is listed before the conversion");

        // Act
        var result = await ConvertAgentsAsync();

        // Assert
        result.Migrated.Should().Be(1);

        var project = await ProjectOfAsync(title);
        project.Id.Should().NotBe(agent.Id, "the project is a new room");
        project.CreatedBy.Id.Should().Be(author.Id);

        (await AgentsInAnySectionAsync()).Should().BeEmpty("no room of the AI room type is left in any section");
    }

    [Fact]
    public async Task Convert_AgentOfTheAgentsSection_BecomesAProjectToo()
    {
        // Arrange - an agent under the root of the AI agents section, where 4.0 created them
        var title = NewTitle("Section agent");
        var agentId = await CreateAgentInAgentsSectionAsync(title);

        (await RoomInfoAsync(agentId)).RootFolderType.Should().Be(FolderType.AiAgents);

        // Act
        var result = await ConvertAgentsAsync();

        // Assert
        result.Migrated.Should().Be(1);
        (await ProjectOfAsync(title)).RootFolderType.Should().Be(FolderType.AiHub);
        (await AgentsInAnySectionAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Convert_Twice_KeepsOneProjectForTheAgent()
    {
        // Arrange
        var title = NewTitle("Agent");
        var agent = await CreateAgentAsync(title);
        await AddKnowledgeFileAsync(agent.Id, "autotest-knowledge.txt");

        await ConvertAgentsAsync();
        var first = await ProjectOfAsync(title);

        // Act
        var second = await ConvertAgentsAsync();

        // Assert
        second.Migrated.Should().Be(0);
        (await ProjectOfAsync(title)).Id.Should().Be(first.Id);
        (await ProjectSourcesAsync(first.Id)).KnowledgeRooms.Select(room => room.Id).Should().Equal(agent.Id);
    }

    [Fact]
    public async Task Convert_AgentWithAKnowledgeFile_KeepsItsIdAsALinkedKnowledgeRoom()
    {
        // Arrange
        var title = NewTitle("Sales bot");
        var agent = await CreateAgentAsync(title);
        var knowledge = await AgentFolderAsync(agent.Id, FolderType.Knowledge);
        var file = await AddKnowledgeFileAsync(agent.Id, "autotest-knowledge.txt");

        // Act
        await ConvertAgentsAsync();

        // Assert
        var room = await RoomInfoAsync(agent.Id);
        room.RoomType.Should().Be(RoomType.KnowledgeRoom);
        room.Title.Should().Be($"{title} ({knowledge.Title})");

        var project = await ProjectOfAsync(title);
        (await ProjectSourcesAsync(project.Id)).KnowledgeRooms.Select(source => source.Id).Should().Equal(agent.Id);

        await FilesHttpClient.Authenticate(Owner);
        var info = (await FilesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Id.Should().Be(file.Id);
        info.FolderId.Should().Be(agent.Id, "the knowledge files lie directly in the room");

        var folderStatus = await StatusOfAsync(() => FoldersApi.GetFolderInfoAsync(knowledge.Id, TestContext.Current.CancellationToken));
        folderStatus.Should().Be(404, "the Knowledge folder is gone");
    }

    [Fact]
    public async Task Convert_KnowledgeFile_MovesItsModifiedOn()
    {
        // Arrange
        var agent = await CreateAgentAsync(NewTitle("Indexed bot"));
        var file = await AddKnowledgeFileAsync(agent.Id, "autotest-knowledge.txt");
        var past = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await using (var connection = await OpenDatabaseAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "UPDATE files_file SET modified_on = @past WHERE id = @id";
            command.Parameters.AddWithValue("@past", past);
            command.Parameters.AddWithValue("@id", file.Id);

            (await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken)).Should().BeGreaterThan(0);
        }

        // Act
        await ConvertAgentsAsync();

        // Assert - the periodic pass of the full-text index reads the files by modified_on
        await using (var connection = await OpenDatabaseAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT folder_id, modified_on FROM files_file WHERE id = @id";
            command.Parameters.AddWithValue("@id", file.Id);

            await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            var rows = 0;

            while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            {
                rows++;
                reader.GetInt32(0).Should().Be(agent.Id, "the knowledge file lies directly in the room");
                reader.GetDateTime(1).Should().BeAfter(past, "the move of a knowledge file into the room is a change the index has to pick up");
            }

            rows.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task Convert_AgentWithKnowledge_DeletesTheKnowledgeFolderFromTheIndex()
    {
        // Arrange
        var agent = await CreateAgentAsync(NewTitle("Index cleanup bot"));
        var knowledge = await AgentFolderAsync(agent.Id, FolderType.Knowledge);
        await AddKnowledgeFileAsync(agent.Id, "autotest-knowledge.txt");

        await using var probe = await ListenToEventBusAsync(nameof(FolderIndexIntegrationEvent));

        // Act
        await ConvertAgentsAsync();

        // Assert - the API never answers with a folder the database no longer holds, so the event is read off the bus
        var deletions = await probe.ReadAsync<FolderIndexIntegrationEvent>(
            e => e.Action == FolderIndexAction.Delete && e.FolderIds?.Contains(knowledge.Id) == true,
            TestContext.Current.CancellationToken);

        deletions.Should().ContainSingle();
    }

    [Fact]
    public async Task Convert_AgentTitleAtTheLimit_CutsTheAgentTitleAndKeepsTheSuffix()
    {
        // Arrange
        var title = NewTitle("Long").PadRight(MaxTitleLength, 'x');
        var agent = await CreateAgentAsync(title);
        agent.Title.Should().HaveLength(MaxTitleLength, "the agent takes a title of the greatest length");

        var knowledge = await AgentFolderAsync(agent.Id, FolderType.Knowledge);
        await AddKnowledgeFileAsync(agent.Id, "autotest-knowledge.txt");

        // Act
        await ConvertAgentsAsync();

        // Assert
        var suffix = $" ({knowledge.Title})";
        var room = await RoomInfoAsync(agent.Id);

        room.Title.Should().HaveLength(MaxTitleLength);
        room.Title.Should().Be(title[..(MaxTitleLength - suffix.Length)] + suffix);
        (await ProjectOfAsync(title)).Title.Should().Be(title, "the project keeps the whole title of the agent");
    }

    [Fact]
    public async Task Convert_VectorizationOff_StillMakesAndLinksTheKnowledgeRoom()
    {
        // Arrange
        var title = NewTitle("Agent");
        var agent = await CreateAgentAsync(title);
        var knowledge = await AgentFolderAsync(agent.Id, FolderType.Knowledge);

        // What "vectorization is off" means to a caller: the Knowledge folder refuses a new file, to the owner too.
        var refused = await StatusOfAsync(async () =>
        {
            await OperationsApi.CreateUploadSessionInFolderAsync(
                knowledge.Id,
                new SessionRequest("autotest-refused.txt", 4, createNewIfExist: true),
                TestContext.Current.CancellationToken);
        });
        refused.Should().Be(403, "vectorization is off on the portal");

        await AddKnowledgeFileAsync(agent.Id, "autotest-knowledge.txt");

        // Act
        var result = await ConvertAgentsAsync();

        // Assert
        result.Migrated.Should().Be(1);
        (await RoomInfoAsync(agent.Id)).RoomType.Should().Be(RoomType.KnowledgeRoom);

        var project = await ProjectOfAsync(title);
        (await ProjectSourcesAsync(project.Id)).KnowledgeRooms.Select(source => source.Id).Should().Equal(agent.Id);
    }

    [Fact]
    public async Task Convert_AgentWithoutKnowledge_LeavesNoRoom()
    {
        // Arrange
        var title = NewTitle("Agent");
        var agent = await CreateAgentAsync(title);

        // Act
        var result = await ConvertAgentsAsync();

        // Assert
        result.Migrated.Should().Be(1);

        var project = await ProjectOfAsync(title);
        var sources = await ProjectSourcesAsync(project.Id);
        (sources.KnowledgeRooms ?? []).Should().BeEmpty();
        (sources.InstructionsRooms ?? []).Should().BeEmpty();

        await FilesHttpClient.Authenticate(Owner);
        var roomStatus = await StatusOfAsync(() => RoomsApi.GetRoomInfoAsync(agent.Id, TestContext.Current.CancellationToken));
        roomStatus.Should().Be(404);
    }

    [Fact]
    public async Task Convert_AgentWithALogo_GivesTheProjectALogo()
    {
        // Arrange
        var title = NewTitle("Logo agent");
        var agent = await CreateAgentAsync(title);
        await SetLogoAsync(agent.Id);

        (await RoomInfoAsync(agent.Id)).Logo.Original.Should().NotBeNullOrEmpty("the agent has a logo before the conversion");

        // Act
        await ConvertAgentsAsync();

        // Assert
        (await ProjectOfAsync(title)).Logo.Original.Should().NotBeNullOrEmpty("the logo of the agent is copied to the project");
    }

    [Fact]
    public async Task Convert_AgentWithALogoAndNoKnowledge_RemovesTheLogoFilesOfTheDeletedRoom()
    {
        await WaitForLazyResourcesAsync();

        var title = NewTitle("Logo files agent");
        var agent = await CreateAgentAsync(title);
        await SetLogoAsync(agent.Id);

        var agentLogo = PathOf((await RoomInfoAsync(agent.Id)).Logo.Original);
        await StudioHttpClient.Authenticate(Owner);

        using (var before = await StudioHttpClient.GetAsync(agentLogo, TestContext.Current.CancellationToken))
        {
            before.StatusCode.Should().Be(HttpStatusCode.OK, "the storage holds the logo of the agent before the conversion");
        }

        await ConvertAgentsAsync();

        var projectLogo = PathOf((await ProjectOfAsync(title)).Logo.Original);

        using (var room = await StudioHttpClient.GetAsync(agentLogo, TestContext.Current.CancellationToken))
        {
            room.StatusCode.Should().Be(HttpStatusCode.NotFound, "the room of an agent without knowledge files is deleted with its logo files");
        }

        using var project = await StudioHttpClient.GetAsync(projectLogo, TestContext.Current.CancellationToken);
        project.StatusCode.Should().Be(HttpStatusCode.OK, "the copy of the logo made for the project stays");
    }

    [Fact]
    public async Task Convert_KnowledgeWithAFolder_ConvertsTheAgentAndDeletesTheFolder()
    {
        var title = NewTitle("Nested knowledge agent");
        var agent = await CreateAgentAsync(title);
        var file = await AddKnowledgeFileAsync(agent.Id, "autotest-flat-knowledge.txt");

        var outputs = await AgentFolderAsync(agent.Id, FolderType.ChatOutputs);
        var folder = (await FoldersApi.CreateFolderAsync(outputs.Id, new CreateFolderRequest("Nested"), TestContext.Current.CancellationToken)).Response;
        var nested = await UploadAsync(folder.Id, "autotest-nested-knowledge.txt");
        await MoveFolderToKnowledgeAsync(agent.Id, folder.Id);

        var result = await ConvertAgentsAsync();

        result.Migrated.Should().Be(1);
        await ProjectOfAsync(title);

        await FilesHttpClient.Authenticate(Owner);
        var content = (await FoldersApi.GetFolderByFolderIdAsync(agent.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        (content.Files ?? []).OfType<FileDto>().Select(entry => entry.Id).Should().BeEquivalentTo([file.Id], "the file of the Knowledge folder is in the Knowledge room");
        (content.Folders ?? []).Should().BeEmpty("the folder inside the Knowledge folder is deleted");

        (await StatusOfAsync(() => FoldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken))).Should().Be(404);
        (await StatusOfAsync(() => FilesApi.GetFileInfoAsync(nested.Id, cancellationToken: TestContext.Current.CancellationToken))).Should().Be(404);
    }

    private async Task MoveFolderToKnowledgeAsync(int agentId, int folderId)
    {
        var outputs = await AgentFolderAsync(agentId, FolderType.ChatOutputs);
        var knowledge = await AgentFolderAsync(agentId, FolderType.Knowledge);

        await using var connection = await OpenDatabaseAsync();
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            UPDATE files_folder SET parent_id = @knowledge WHERE id = @folder AND parent_id = @outputs;
            UPDATE files_folder_tree SET parent_id = @knowledge
            WHERE parent_id = @outputs
              AND folder_id IN (SELECT folder_id FROM (SELECT folder_id FROM files_folder_tree WHERE parent_id = @folder) subtree);
            """;
        command.Parameters.AddWithValue("@knowledge", knowledge.Id);
        command.Parameters.AddWithValue("@outputs", outputs.Id);
        command.Parameters.AddWithValue("@folder", folderId);

        (await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken)).Should().BeGreaterThanOrEqualTo(2, $"the folder {folderId} lies in Chat outputs");
    }

    private async Task SetLogoAsync(int roomId)
    {
        await using var image = new MemoryStream(Convert.FromBase64String(LogoImageBase64));

        var uploaded = await RoomsApi.UploadRoomLogoAsync(
            new FileParameter("logo.png", "image/png", image),
            TestContext.Current.CancellationToken);

        await RoomsApi.CreateRoomLogoAsync(
            roomId,
            new LogoRequest(uploaded.Response.Data?.ToString() ?? string.Empty, 0, 0, 1, 1),
            TestContext.Current.CancellationToken);
    }

    private static string PathOf(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var absolute) && absolute.Scheme.StartsWith("http", StringComparison.Ordinal)
            ? absolute.PathAndQuery
            : url;
    }

    [Fact]
    public async Task Convert_KnowledgeFile_IsFoundByTitleInTheKnowledgeRoom()
    {
        // Arrange
        var title = NewTitle("Search agent");
        var agent = await CreateAgentAsync(title);
        var word = $"needle{Guid.NewGuid():N}";
        var file = await AddKnowledgeFileAsync(agent.Id, $"{word}.txt");

        // Act
        await ConvertAgentsAsync();

        // Assert - the index follows the conversion through an event, so the search is polled
        await FilesHttpClient.Authenticate(Owner);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        List<int> found;

        while (true)
        {
            var content = (await FoldersApi.GetFolderByFolderIdAsync(
                agent.Id,
                filterValue: word,
                cancellationToken: TestContext.Current.CancellationToken)).Response;

            found = [.. (content.Files ?? []).OfType<FileDto>().Select(entry => entry.Id)];

            if (found.Contains(file.Id) || DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        found.Should().Equal(file.Id);
    }

    [Fact]
    public async Task Convert_AgentWithKnowledge_ChargesNoUserWithTheSizeOfTheRoom()
    {
        // Arrange - the root of the AI rooms section is created by the owner's first listing of it, so a room
        // charged on its way under that root would be charged to the owner
        var bystander = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        await ListRoomsAsync(SearchArea.AiHub);
        await EnableUserQuotaAsync(100 * 1024 * 1024);

        var title = NewTitle("Sized agent");
        var agent = await CreateAgentAsync(title);
        var knowledgeFile = await AddKnowledgeFileAsync(agent.Id, "autotest-knowledge.txt", new byte[4096]);
        var outputsFile = await AddOutputsFileAsync(agent.Id, "autotest-output.txt", bytes: new byte[1024]);

        var ownerBefore = (await ProfileOfAsync(Owner)).UsedSpace ?? 0;
        var bystanderBefore = (await ProfileOfAsync(bystander)).UsedSpace ?? 0;

        // Act
        await ConvertAgentsAsync();

        // Assert - the Chat outputs are the one thing the owner is charged with: seeing that charge arrive shows
        // the counters were read after the conversion, and its exact size shows the room was not added to it
        var owner = await WaitForProfileAsync(Owner, profile => (profile.UsedSpace ?? 0) >= ownerBefore + outputsFile.Size);

        (owner.UsedSpace ?? 0).Should().Be(ownerBefore + outputsFile.Size, $"the room holds {knowledgeFile.Size} bytes of knowledge that stay with the room");
        ((await ProfileOfAsync(bystander)).UsedSpace ?? 0).Should().Be(bystanderBefore);
        ((await RoomInfoAsync(agent.Id)).UsedSpace ?? 0).Should().Be(knowledgeFile.Size);
    }
}
