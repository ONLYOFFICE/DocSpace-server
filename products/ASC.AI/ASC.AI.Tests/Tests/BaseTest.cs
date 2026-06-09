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

namespace ASC.AI.Tests.Tests;

[Collection("Test Collection")]
public class BaseTest(AspireAppFixture fixture) : IAsyncLifetime
{
    protected const string ProfilesPath = "/internal/ai/integration/profiles";
    protected const string ProfilesBatchPath = "/internal/ai/integration/profiles/batch";
    protected const string AssignmentsPath = "/internal/ai/integration/assignments";
    protected const string ThreadsPath = "/internal/ai/integration/threads";
    protected const string MessagesPath = "/internal/ai/integration/messages";
    protected const string McpServersPath = "/internal/ai/integration/mcp-servers";
    protected const string PreferencesPath = "/internal/ai/integration/preferences";
    protected const string ToolPrefsPath = "/internal/ai/integration/tool-prefs";

    protected const string SystemToolsServerType = "00000000-0000-0000-0000-000000000001";

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
            Capabilities = Capabilities.Chat,
            UseResponsesApi = false,
            CanUseTool = true
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
            Capabilities = Capabilities.Chat | Capabilities.Vision,
            UseResponsesApi = true,
            CanUseTool = false
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

    protected async Task<Dictionary<string, ToolPreference>> ReadToolPrefsAsync(string? entityId = null)
    {
        var path = entityId is null
            ? ToolPrefsPath
            : $"{ToolPrefsPath}?entityId={entityId}";

        using var response = await Ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await Ai.ReadAsync<Dictionary<string, ToolPreference>>(response, TestContext.Current.CancellationToken);
    }

    protected async Task UpsertDisabledToolPrefsAsync(
        IReadOnlyDictionary<string, HashSet<string>> disabled,
        string? entityId = null)
    {
        using var response = await Ai.PutAsync(
            $"{ToolPrefsPath}/disabled",
            new { disabled, entityId },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    protected async Task UpsertAllowAlwaysToolPrefsAsync(
        IReadOnlyDictionary<string, HashSet<string>> allowAlways,
        string? entityId = null)
    {
        using var response = await Ai.PutAsync(
            $"{ToolPrefsPath}/allow-always",
            new { allowAlways, entityId },
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
