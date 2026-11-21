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
/// Helpers of the suites that convert AI agents into AI projects. The conversion has one entry point a test can
/// drive, the internal route <c>POST /internal/ai/agents/migration</c>: the background pass is switched off in the
/// <c>integration-test</c> profile, so nothing converts an agent a test is still building.
/// </summary>
/// <remarks>
/// An agent is a room of the AI room type, created through <c>createRoom</c> as the Files suite does. Every helper
/// signs the client it uses in as the account it names (the owner unless said otherwise), so a test never depends
/// on who the previous call acted as.
/// </remarks>
public abstract class AgentMigrationTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const string AgentsMigrationPath = "/internal/ai/agents/migration";

    /// <summary>The greatest length a room title may have (<c>Global.MaxTitle</c>).</summary>
    protected const int MaxTitleLength = 170;

    private const string AgentsPath = "/internal/ai/agents";
    private const string UserQuotaSettingsPath = "/api/2.0/settings/userquotasettings";

    protected sealed record UploadedFile(int Id, string Title, long Size);

    protected static string NewTitle(string name) => $"Autotest {name} {Guid.NewGuid().ToString("N")[..8]}";

    protected Task<HttpResponseMessage> PostAgentsMigrationAsync()
    {
        return _ai.PostAsync(AgentsMigrationPath, null, TestContext.Current.CancellationToken);
    }

    /// <summary>Calls the route as whoever the internal client is signed in as.</summary>
    protected async Task<AgentsMigrationDto> MigrateAgentsAsync()
    {
        using var response = await PostAgentsMigrationAsync();

        return await _ai.ReadAsync<AgentsMigrationDto>(response, TestContext.Current.CancellationToken);
    }

    /// <summary>Converts the agents of the portal as the owner and fails the test when an agent failed.</summary>
    protected async Task<AgentsMigrationDto> ConvertAgentsAsync()
    {
        await _aiClient.Authenticate(Owner);

        var result = await MigrateAgentsAsync();

        result.Failed.Should().Be(0, "no agent of the test may fail to convert");

        return result;
    }

    /// <summary>Creates an agent through <c>createRoom</c>: it lands under the rooms root, as agents do since 4.1.</summary>
    protected async Task<FolderDto> CreateAgentAsync(string title, User? owner = null)
    {
        await FilesHttpClient.Authenticate(owner ?? Owner);

        return (await RoomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title, roomType: RoomType.AiRoom),
            TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>
    /// Creates an agent under the root of the AI agents section, where 4.0 kept them. Raw: only the internal route
    /// of ASC.AI parents an agent there, and the chat service publishes no route for it.
    /// </summary>
    protected async Task<int> CreateAgentInAgentsSectionAsync(string title)
    {
        await _aiClient.Authenticate(Owner);

        using var response = await _ai.PostAsync(AgentsPath, new { title }, TestContext.Current.CancellationToken);
        var agent = await _ai.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);

        return agent.GetProperty("id").GetInt32();
    }

    /// <summary>One of the two built-in folders of an agent, Knowledge or Chat outputs.</summary>
    protected async Task<FolderDto> AgentFolderAsync(int agentId, FolderType type)
    {
        await FilesHttpClient.Authenticate(Owner);

        var folders = (await FoldersApi.GetFoldersAsync(agentId, TestContext.Current.CancellationToken)).Response ?? [];
        var found = folders.OfType<FolderDto>().FirstOrDefault(folder => folder.Type == type);

        found.Should().NotBeNull($"the agent {agentId} has a {type} folder; it holds {string.Join(", ", folders.Select(f => f.Title))}");

        return found!;
    }

    protected async Task<UploadedFile> UploadAsync(int folderId, string name, User? actor = null, byte[]? bytes = null)
    {
        await FilesHttpClient.Authenticate(actor ?? Owner);

        bytes ??= System.Text.Encoding.UTF8.GetBytes($"Autotest agent migration file {name}");

        var session = (await OperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest(name, bytes.Length, createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        var uploaded = (await OperationsApi.UploadSessionAsync(
            folderId,
            session.Id,
            new FileParameter(name, "application/octet-stream", new MemoryStream(bytes)),
            TestContext.Current.CancellationToken)).Response.File;

        return new UploadedFile(uploaded.Id, uploaded.Title, bytes.Length);
    }

    /// <summary>Uploads a file into the Chat outputs folder of the agent.</summary>
    protected async Task<UploadedFile> AddOutputsFileAsync(int agentId, string name, User? actor = null, byte[]? bytes = null)
    {
        var outputs = await AgentFolderAsync(agentId, FolderType.ChatOutputs);

        return await UploadAsync(outputs.Id, name, actor, bytes);
    }

    /// <summary>
    /// Puts a file into the Knowledge folder of the agent. The stand has no embedding provider, so vectorization is
    /// off and the API refuses every create in a Knowledge folder (<c>FileSecurity</c>): the file is uploaded where
    /// the agent takes it, into Chat outputs, and its parent is changed in the database. The conversion reads the
    /// knowledge files by that parent, which is the state a portal with vectorization on holds.
    /// </summary>
    protected async Task<UploadedFile> AddKnowledgeFileAsync(int agentId, string name, byte[]? bytes = null)
    {
        var file = await AddOutputsFileAsync(agentId, name, bytes: bytes);

        await MoveToKnowledgeAsync(agentId, file);

        return file;
    }

    /// <summary>
    /// Changes the parent of a Chat outputs file to the Knowledge folder in the database, with the size and file
    /// counters of the two folders, so that what the conversion moves and charges is what the folders really hold.
    /// </summary>
    protected async Task MoveToKnowledgeAsync(int agentId, UploadedFile file)
    {
        var outputs = await AgentFolderAsync(agentId, FolderType.ChatOutputs);
        var knowledge = await AgentFolderAsync(agentId, FolderType.Knowledge);

        await using var connection = await OpenDatabaseAsync();
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            UPDATE files_file SET folder_id = @knowledge WHERE id = @id AND folder_id = @outputs;
            UPDATE files_folder SET counter = counter - @size, filesCount = filesCount - 1 WHERE id = @outputs;
            UPDATE files_folder SET counter = counter + @size, filesCount = filesCount + 1 WHERE id = @knowledge;
            """;
        command.Parameters.AddWithValue("@knowledge", knowledge.Id);
        command.Parameters.AddWithValue("@outputs", outputs.Id);
        command.Parameters.AddWithValue("@id", file.Id);
        command.Parameters.AddWithValue("@size", file.Size);

        (await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken)).Should().BeGreaterThanOrEqualTo(3, $"the file {file.Id} lies in Chat outputs");

        var moved = (await FilesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        moved.FolderId.Should().Be(knowledge.Id, "the Files service reads the parent of the file from the database");
    }

    /// <summary>The rooms one section lists to the account, the owner unless said otherwise.</summary>
    protected async Task<List<FolderDto>> ListRoomsAsync(SearchArea area, User? actor = null)
    {
        await FilesHttpClient.Authenticate(actor ?? Owner);

        var content = (await RoomsApi.GetRoomsFolderAsync(searchArea: area, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return [.. (content.Folders ?? []).OfType<FolderDto>()];
    }

    /// <summary>The projects of the AI rooms section that carry the title.</summary>
    protected async Task<List<FolderDto>> ProjectsTitledAsync(string title, User? actor = null)
    {
        return [.. (await ListRoomsAsync(SearchArea.AiHub, actor)).Where(room => room.RoomType == RoomType.AiProjectRoom && room.Title == title)];
    }

    /// <summary>The one project the conversion made for the agent with that title.</summary>
    protected async Task<FolderDto> ProjectOfAsync(string agentTitle, User? actor = null)
    {
        var projects = await ProjectsTitledAsync(agentTitle, actor);

        return projects.Should().ContainSingle($"one project is made for the agent \"{agentTitle}\"").Subject;
    }

    /// <summary>Every room of the AI room type the owner can still find, section by section.</summary>
    protected async Task<List<FolderDto>> AgentsInAnySectionAsync()
    {
        var agents = new List<FolderDto>();

        foreach (var area in new[] { SearchArea.Active, SearchArea.AiAgents, SearchArea.AiHub, SearchArea.Archive })
        {
            agents.AddRange((await ListRoomsAsync(area)).Where(room => room.RoomType == RoomType.AiRoom));
        }

        return agents;
    }

    protected async Task<FolderDto> RoomInfoAsync(int roomId, User? actor = null)
    {
        await FilesHttpClient.Authenticate(actor ?? Owner);

        return (await RoomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
    }

    protected async Task<AiProjectSourcesDto> ProjectSourcesAsync(int projectId, User? actor = null)
    {
        await FilesHttpClient.Authenticate(actor ?? Owner);

        return (await RoomsAiApi.GetProjectSourcesAsync(projectId, TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>The folders "My documents" lists to the account.</summary>
    protected async Task<List<FolderDto>> MyDocumentsFoldersAsync(User user)
    {
        await FilesHttpClient.Authenticate(user);

        var content = (await FoldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        return [.. (content.Folders ?? []).OfType<FolderDto>()];
    }

    /// <summary>The HTTP status of a file read by the account.</summary>
    protected async Task<int> FileReadStatusAsync(int fileId, User user)
    {
        await FilesHttpClient.Authenticate(user);

        return await StatusOfAsync(() => FilesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>Gives a user or a group a role in a room, as the owner.</summary>
    protected async Task ShareRoomAsync(int roomId, Guid subjectId, FileShare access)
    {
        await FilesHttpClient.Authenticate(Owner);

        await RoomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest([new RoomInvitation(subjectId, access)], notify: false),
            TestContext.Current.CancellationToken);
    }

    /// <summary>The access list of a room, as the owner sees it.</summary>
    protected async Task<List<FileShareDto>> RoomSharesAsync(int roomId)
    {
        await FilesHttpClient.Authenticate(Owner);

        return (await RoomsApi.GetRoomSecurityInfoAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    protected static FileShare? AccessOf(List<FileShareDto> shares, Guid userId)
    {
        return shares.FirstOrDefault(share => share.SharedToUser?.Id == userId)?.Access;
    }

    protected static bool Lists(List<FileShareDto> shares, Guid userId)
    {
        return shares.Exists(share => share.SharedToUser?.Id == userId);
    }

    /// <summary>
    /// Switches the per-user storage quota on. Raw: the SDK has no operation for
    /// <c>POST api/2.0/settings/userquotasettings</c>.
    /// </summary>
    protected async Task EnableUserQuotaAsync(long defaultQuota)
    {
        await WebApiHttpClient.Authenticate(Owner);

        using var response = await new RawApiClient(WebApiHttpClient).PostAsync(
            UserQuotaSettingsPath,
            new { enableQuota = true, defaultQuota },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Polls the profile of an account: the used space is written by the AI service and read through the People
    /// service, whose copy of the quota rows follows on a notification.
    /// </summary>
    protected async Task<EmployeeFullDto> WaitForProfileAsync(User user, Func<EmployeeFullDto, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var profile = await ProfileOfAsync(user);

            if (until(profile) || DateTime.UtcNow >= deadline)
            {
                return profile;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>The profile of an account as the owner reads it, for its used space and quota.</summary>
    protected async Task<EmployeeFullDto> ProfileOfAsync(User user)
    {
        await PeopleHttpClient.Authenticate(Owner);

        return (await ProfilesApi.GetProfileByUserIdAsync(user.Id.ToString(), TestContext.Current.CancellationToken)).Response;
    }
}
