// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.AI.Tests.Tests;

[Collection("Test Collection")]
public class BaseTest(AspireAppFixture fixture) : IAsyncLifetime
{
    protected const string ProfilesPath = "/api/2.0/ai/integration/profiles";
    protected const string ProfilesBatchPath = "/api/2.0/ai/integration/profiles/batch";
    protected const string AssignmentsPath = "/api/2.0/ai/integration/assignments";
    protected const string ThreadsPath = "/api/2.0/ai/integration/threads";
    protected const string MessagesPath = "/api/2.0/ai/integration/messages";
    protected const string McpServersPath = "/api/2.0/ai/integration/mcp-servers";
    protected const string PreferencesPath = "/api/2.0/ai/integration/preferences";

    private static readonly JsonSerializerOptions _readJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    protected readonly AspireAppFixture Fixture = fixture;
    protected readonly HttpClient AiClient = fixture.AiHttpClient;
    protected readonly AiApiClient Ai = fixture.AiApi;

    private readonly Func<Task> _resetDatabase = fixture.ResetDatabaseAsync;

    public async ValueTask InitializeAsync()
    {
        await Initializer.InitializeAsync(Fixture);
        await AiClient.Authenticate(Initializer.Owner);
    }

    public async ValueTask DisposeAsync()
    {
        await _resetDatabase();
    }

    protected static CreateProfileRequestDto BuildCreateDto(string? name = null) =>
        new()
        {
            Name = name ?? $"profile-{Guid.NewGuid():N}",
            ProviderType = "openai",
            BaseUrl = "https://api.openai.com/v1",
            Key = "sk-test-key-" + Guid.NewGuid().ToString("N"),
            ModelId = "gpt-4o-mini",
            Reasoning = false,
            Capabilities = Capabilities.Chat
        };

    protected static UpdateProfileBody BuildUpdateBody(string? name = null) =>
        new()
        {
            Name = name ?? $"updated-{Guid.NewGuid():N}",
            ProviderType = "anthropic",
            BaseUrl = "https://api.anthropic.com/v1",
            Key = "sk-ant-" + Guid.NewGuid().ToString("N"),
            ModelId = "claude-sonnet-4-6",
            Reasoning = true,
            Capabilities = Capabilities.Chat | Capabilities.Vision
        };

    protected async Task<ProfileDto> CreateProfileAsync(CreateProfileRequestDto? dto = null)
    {
        dto ??= BuildCreateDto();
        using var response = await Ai.PostAsync(ProfilesPath, dto, TestContext.Current.CancellationToken);
        return await Ai.ReadAsync<ProfileDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task CreateAssignmentAsync(string actionType, Guid profileId, string? entityId = null)
    {
        using var response = await Ai.PostAsync(
            AssignmentsPath,
            new { actionType, profileId, entityId },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    protected async Task<Guid?> ReadAssignmentAsync(string actionType, string? entityId = null)
    {
        var path = BuildScopedAssignmentPath(actionType, entityId);
        using var response = await Ai.GetAsync(path, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<Guid?>>(
            _readJsonOptions,
            TestContext.Current.CancellationToken);
        return wrapper?.Response;
    }

    protected async Task<Dictionary<string, Guid>> ReadAllAssignmentsAsync(string? entityId = null)
    {
        var path = entityId is null
            ? AssignmentsPath
            : $"{AssignmentsPath}?entityId={entityId}";

        using var response = await Ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await Ai.ReadAsync<Dictionary<string, Guid>>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<ThreadDto> CreateThreadAsync(string? title = null, Guid? profileId = null, string? entityId = null)
    {
        using var response = await Ai.PostAsync(
            ThreadsPath,
            new { title = title ?? $"thread-{Guid.NewGuid():N}", profileId, entityId },
            TestContext.Current.CancellationToken);
        return await Ai.ReadAsync<ThreadDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<ThreadDto> ReadThreadAsync(Guid id)
    {
        using var response = await Ai.GetAsync($"{ThreadsPath}/{id}", TestContext.Current.CancellationToken);
        return await Ai.ReadAsync<ThreadDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<List<ThreadDto>> ReadAllThreadsAsync(string? entityId = null)
    {
        var path = entityId is null
            ? ThreadsPath
            : $"{ThreadsPath}?entityId={entityId}";

        using var response = await Ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await Ai.ReadAsync<List<ThreadDto>>(response, TestContext.Current.CancellationToken);
    }

    protected static string BuildMessageContents(string? text = null) =>
        $$"""[{"$type":"text","text":"{{text ?? $"message-{Guid.NewGuid():N}"}}"}]""";

    protected static bool JsonEquals(string? left, string? right)
    {
        if (left is null || right is null)
        {
            return left == right;
        }

        return System.Text.Json.Nodes.JsonNode.DeepEquals(
            System.Text.Json.Nodes.JsonNode.Parse(left),
            System.Text.Json.Nodes.JsonNode.Parse(right));
    }

    protected async Task<MessageDto> CreateMessageAsync(Guid threadId, string? contents = null)
    {
        using var response = await Ai.PostAsync(
            $"{ThreadsPath}/{threadId}/messages",
            new { contents = contents ?? BuildMessageContents() },
            TestContext.Current.CancellationToken);
        return await Ai.ReadAsync<MessageDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<MessageDto> ReadMessageAsync(Guid id)
    {
        using var response = await Ai.GetAsync($"{MessagesPath}/{id}", TestContext.Current.CancellationToken);
        return await Ai.ReadAsync<MessageDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<List<MessageDto>> ReadMessagesByThreadAsync(Guid threadId, int? limit = null, int? startIndex = null)
    {
        var query = new List<string>();
        if (limit is not null)
        {
            query.Add($"limit={limit}");
        }
        if (startIndex is not null)
        {
            query.Add($"startIndex={startIndex}");
        }

        var path = $"{ThreadsPath}/{threadId}/messages";
        if (query.Count > 0)
        {
            path += "?" + string.Join("&", query);
        }

        using var response = await Ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await Ai.ReadAsync<List<MessageDto>>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<int> CreateRoomAsync(string? title = null)
    {
        await Fixture.FilesHttpClient.Authenticate(Initializer.Owner);

        var body = new
        {
            title = title ?? $"room-{Guid.NewGuid():N}",
            roomType = "AiRoom"
        };

        using var response = await Fixture.FilesApi.PostAsync(
            "/api/2.0/files/rooms",
            body,
            TestContext.Current.CancellationToken);
        var room = await Fixture.FilesApi.ReadAsync<RoomFolderDto>(response, TestContext.Current.CancellationToken);
        return room.Id;
    }

    protected async Task<PreferencesDto?> ReadPreferencesAsync(string? entityId = null)
    {
        var path = entityId is null
            ? PreferencesPath
            : $"{PreferencesPath}?entityId={entityId}";

        using var response = await Ai.GetAsync(path, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<PreferencesDto>>(
            _readJsonOptions,
            TestContext.Current.CancellationToken);
        return wrapper?.Response;
    }

    protected async Task UpsertPreferencesAsync(bool? deepMode, string? entityId = null)
    {
        using var response = await Ai.PutAsync(
            PreferencesPath,
            new { deepMode, entityId },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static string BuildScopedAssignmentPath(string actionType, string? entityId) =>
        entityId is null
            ? $"{AssignmentsPath}/{actionType}"
            : $"{AssignmentsPath}/{actionType}?entityId={entityId}";

    protected static string BuildMcpConfig(string? url = null) =>
        $$"""{"transport":"http","url":"{{url ?? "https://example.com/mcp"}}"}""";

    protected async Task CreateMcpServerAsync(string name, string? config = null, string? entityId = null)
    {
        using var response = await Ai.PostAsync(
            McpServersPath,
            new { name, config = config ?? BuildMcpConfig(), entityId },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    protected async Task<McpServerDto> ReadMcpServerAsync(string name, string? entityId = null)
    {
        var path = entityId is null
            ? $"{McpServersPath}/{name}"
            : $"{McpServersPath}/{name}?entityId={entityId}";

        using var response = await Ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await Ai.ReadAsync<McpServerDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<List<McpServerDto>> ReadAllMcpServersAsync(string? entityId = null)
    {
        var path = entityId is null
            ? McpServersPath
            : $"{McpServersPath}?entityId={entityId}";

        using var response = await Ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await Ai.ReadAsync<List<McpServerDto>>(response, TestContext.Current.CancellationToken);
    }

    private sealed record RoomFolderDto(int Id);
}
