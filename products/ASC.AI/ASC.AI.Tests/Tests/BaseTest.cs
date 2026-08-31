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

public class BaseTest(AspireAppFixture fixture) : IAsyncLifetime
{
    protected const string ProfilesPath = "/internal/ai/profiles";
    protected const string ProfilesBatchPath = "/internal/ai/profiles/batch";
    protected const string AssignmentsPath = "/internal/ai/assignments";
    protected const string ThreadsPath = "/internal/ai/threads";
    protected const string MessagesPath = "/internal/ai/messages";
    protected const string McpServersPath = "/internal/ai/mcp-servers";
    protected const string PromptsPath = "/internal/ai/prompts";
    protected const string PromptFoldersPath = "/internal/ai/prompt-folders";
    protected const string PreferencesPath = "/internal/ai/preferences";
    protected const string ToolPrefsPath = "/internal/ai/tool-prefs";
    protected const string ChatContextPath = "/internal/ai/chat-context";

    protected const string SystemToolsServerType = "00000000-0000-0000-0000-000000000001";

    private static readonly JsonSerializerOptions _readJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private PortalClients _clients = null!;

    // The portal and its owner created for this test. Both live on the per-portal client bundle,
    // so the owner Id is always the one belonging to this test's own portal — never shared.
    protected User Owner => _clients.Owner;

    protected HttpClient _aiClient = null!;
    protected RawApiClient _ai = null!;

    public async ValueTask InitializeAsync()
    {
        var setupSw = Stopwatch.StartNew();

        // Register a brand-new portal for this test and bind a fresh set of clients to it.
        _clients = await fixture.CreatePortalAsync(TestContext.Current.CancellationToken);

        _aiClient = _clients.AiHttpClient;
        _ai = _clients.Ai;

        await _aiClient.Authenticate(Owner);

        Timing.Write("setup.total", setupSw.ElapsedMilliseconds);
    }

    public ValueTask DisposeAsync()
    {
        // Each test owns its portal and clients; nothing is shared, so just dispose the clients.
        _clients.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Creates and registers a new member of the given type in the current test's portal.
    /// </summary>
    protected Task<User> InviteContact(EmployeeType employeeType, CancellationToken cancellationToken)
    {
        return Invitations.InviteContactAsync(_clients.ProfilesApi, _clients.PeopleHttpClient, employeeType, Owner, cancellationToken);
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
        using var response = await _ai.PostAsync(ProfilesPath, dto, TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<ProfileDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task CreateAssignmentAsync(string actionType, Guid profileId, string? entityId = null)
    {
        using var response = await _ai.PostAsync(
            AssignmentsPath,
            new { actionType, profileId, entityId },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    protected async Task<Guid?> ReadAssignmentAsync(string actionType, string? entityId = null)
    {
        var path = BuildScopedAssignmentPath(actionType, entityId);
        using var response = await _ai.GetAsync(path, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var wrapper = await response.Content.ReadFromJsonAsync<RawApiResponse<Guid?>>(
            _readJsonOptions,
            TestContext.Current.CancellationToken);
        return wrapper?.Response;
    }

    protected async Task<Dictionary<string, Guid>> ReadAllAssignmentsAsync(string? entityId = null)
    {
        var path = entityId is null
            ? AssignmentsPath
            : $"{AssignmentsPath}?entityId={entityId}";

        using var response = await _ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<Dictionary<string, Guid>>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<ThreadDto> CreateThreadAsync(string? title = null, Guid? profileId = null, string? entityId = null)
    {
        using var response = await _ai.PostAsync(
            ThreadsPath,
            new { title = title ?? $"thread-{Guid.NewGuid():N}", profileId, entityId },
            TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<ThreadDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<ThreadDto> ReadThreadAsync(Guid id)
    {
        using var response = await _ai.GetAsync($"{ThreadsPath}/{id}", TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<ThreadDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<List<ThreadDto>> ReadAllThreadsAsync(string? entityId = null)
    {
        var path = entityId is null
            ? ThreadsPath
            : $"{ThreadsPath}?entityId={entityId}";

        using var response = await _ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<List<ThreadDto>>(response, TestContext.Current.CancellationToken);
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
        using var response = await _ai.PostAsync(
            $"{ThreadsPath}/{threadId}/messages",
            new { contents = contents ?? BuildMessageContents() },
            TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<MessageDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<MessageDto> ReadMessageAsync(Guid id)
    {
        using var response = await _ai.GetAsync($"{MessagesPath}/{id}", TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<MessageDto>(response, TestContext.Current.CancellationToken);
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

        using var response = await _ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<List<MessageDto>>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<int> CreateRoomAsync(string? title = null)
    {
        await _clients.FilesHttpClient.Authenticate(Owner);

        var body = new
        {
            title = title ?? $"room-{Guid.NewGuid():N}",
            roomType = "AiRoom"
        };

        using var response = await _clients.FilesApi.PostAsync(
            "/api/2.0/files/rooms",
            body,
            TestContext.Current.CancellationToken);
        var room = await _clients.FilesApi.ReadAsync<RoomFolderDto>(response, TestContext.Current.CancellationToken);
        return room.Id;
    }

    /// <summary>
    /// Returns the id of the owner's "My documents" root folder in the current test's portal.
    /// </summary>
    protected async Task<int> GetMyDocumentsFolderIdAsync()
    {
        await _clients.FilesHttpClient.Authenticate(Owner);

        using var response = await _clients.FilesApi.GetAsync("/api/2.0/files/@my", TestContext.Current.CancellationToken);
        var content = await _clients.FilesApi.ReadAsync<FolderContentDto>(response, TestContext.Current.CancellationToken);
        return content.Current.Id;
    }

    protected async Task<PreferencesDto?> ReadPreferencesAsync(string? entityId = null)
    {
        var path = entityId is null
            ? PreferencesPath
            : $"{PreferencesPath}?entityId={entityId}";

        using var response = await _ai.GetAsync(path, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var wrapper = await response.Content.ReadFromJsonAsync<RawApiResponse<PreferencesDto>>(
            _readJsonOptions,
            TestContext.Current.CancellationToken);
        return wrapper?.Response;
    }

    protected async Task UpsertPreferencesAsync(bool? deepMode, string? entityId = null)
    {
        using var response = await _ai.PutAsync(
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

        using var response = await _ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<Dictionary<string, ToolPreference>>(response, TestContext.Current.CancellationToken);
    }

    protected async Task UpsertDisabledToolPrefsAsync(
        IReadOnlyDictionary<string, HashSet<string>> disabled,
        string? entityId = null)
    {
        using var response = await _ai.PutAsync(
            $"{ToolPrefsPath}/disabled",
            new { disabled, entityId },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    protected async Task UpsertAllowAlwaysToolPrefsAsync(
        IReadOnlyDictionary<string, HashSet<string>> allowAlways,
        string? entityId = null)
    {
        using var response = await _ai.PutAsync(
            $"{ToolPrefsPath}/allow-always",
            new { allowAlways, entityId },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    protected static string BuildChatContextPath(
        Guid? threadId = null,
        string? entityId = null,
        string? contextEntityId = null,
        bool? includeMessages = null)
    {
        var query = new List<string>();
        if (threadId is not null)
        {
            query.Add($"threadId={threadId}");
        }
        if (entityId is not null)
        {
            query.Add($"entityId={entityId}");
        }
        if (contextEntityId is not null)
        {
            query.Add($"contextEntityId={contextEntityId}");
        }
        if (includeMessages is not null)
        {
            query.Add($"includeMessages={includeMessages.Value.ToString().ToLowerInvariant()}");
        }

        return query.Count > 0 ? $"{ChatContextPath}?{string.Join("&", query)}" : ChatContextPath;
    }

    protected async Task<ChatContextDto> ReadChatContextAsync(
        Guid? threadId = null,
        string? entityId = null,
        string? contextEntityId = null,
        bool? includeMessages = null)
    {
        var path = BuildChatContextPath(threadId, entityId, contextEntityId, includeMessages);
        using var response = await _ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<ChatContextDto>(response, TestContext.Current.CancellationToken);
    }

    private static string BuildScopedAssignmentPath(string actionType, string? entityId) =>
        entityId is null
            ? $"{AssignmentsPath}/{actionType}"
            : $"{AssignmentsPath}/{actionType}?entityId={entityId}";

    protected static string BuildMcpConfig(string? url = null) =>
        $$"""{"transport":"http","url":"{{url ?? "https://example.com/mcp"}}"}""";

    protected async Task CreateMcpServerAsync(string name, string? config = null, string? entityId = null)
    {
        using var response = await _ai.PostAsync(
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

        using var response = await _ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<McpServerDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<List<McpServerDto>> ReadAllMcpServersAsync(string? entityId = null)
    {
        var path = entityId is null
            ? McpServersPath
            : $"{McpServersPath}?entityId={entityId}";

        using var response = await _ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<List<McpServerDto>>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<PromptFolderDto> CreatePromptFolderAsync(string? name = null)
    {
        using var response = await _ai.PostAsync(
            PromptFoldersPath,
            new { name = name ?? $"folder-{Guid.NewGuid():N}" },
            TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<PromptFolderDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<PromptFolderDto> ReadPromptFolderAsync(Guid id)
    {
        using var response = await _ai.GetAsync($"{PromptFoldersPath}/{id}", TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<PromptFolderDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<PromptDto> CreatePromptAsync(string? name = null, string? text = null, Guid? folderId = null)
    {
        using var response = await _ai.PostAsync(
            PromptsPath,
            new { name = name ?? $"prompt-{Guid.NewGuid():N}", text = text ?? "body", folderId },
            TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<PromptDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<PromptDto> ReadPromptAsync(Guid id)
    {
        using var response = await _ai.GetAsync($"{PromptsPath}/{id}", TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<PromptDto>(response, TestContext.Current.CancellationToken);
    }

    private sealed record RoomFolderDto(int Id);

    private sealed record FolderContentDto(RoomFolderDto Current);
}
