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

    protected const int DefaultPageCount = 100;

    private static readonly JsonSerializerOptions _readJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private PortalClients _clients = null!;

    // The portal and its owner created for this test. Both live on the per-portal client bundle,
    // so the owner Id is always the one belonging to this test's own portal — never shared.
    protected User Owner => _clients.Owner;

    // ASC.AI's internal storage API (/internal/ai). The router does not publish it: only the Node chat service
    // calls it. A test goes here only to check that internal contract, for what has no public route.
    protected HttpClient _aiClient = null!;
    protected RawApiClient _ai = null!;

    // The public AI surface (/api/2.0/ai), served by the Node chat service. Tests go through these.
    protected HttpClient _aiChatClient = null!;
    protected AiProfilesApi _aiProfiles = null!;
    protected AiThreadsApi _aiThreads = null!;
    protected AiChatApi _aiChat = null!;
    protected AiAssignmentsApi _aiAssignments = null!;
    protected AiToolsApi _aiTools = null!;
    protected AiPromptsApi _aiPrompts = null!;
    protected AiPreferencesApi _aiPreferences = null!;
    protected AiSettingsApi _aiSettings = null!;
    protected AiContextApi _aiContext = null!;
    protected AiExportApi _aiExport = null!;
    protected DocSpace.API.SDK.Api.AI.AttachmentsApi _aiAttachments = null!;
    protected DocSpace.API.SDK.Api.AI.EditorToolsApi _aiEditorTools = null!;
    protected DocSpace.API.SDK.Api.AI.VectorizationApi _aiVectorization = null!;
    protected DocSpace.API.SDK.Api.AI.WebSearchApi _aiWebSearch = null!;

    /// <summary>The stub model provider every profile of the run points at.</summary>
    protected StubModelProvider ModelProvider => fixture.ModelProvider;

    /// <summary>
    /// True for a class whose tests convert through the document server (an export that renders a .docx or a .pdf):
    /// its setup then waits until that server is up, instead of each test waiting out its export timeout.
    /// </summary>
    protected virtual bool NeedsDocumentServer => false;

    /// <summary>
    /// True for a class whose tests use a service of <see cref="AspireAppFixture"/>'s <c>LazyResources</c> (Nextcloud):
    /// its setup then waits until those are healthy, which the rest of the suite does not.
    /// </summary>
    protected virtual bool NeedsLazyResources => false;

    public async ValueTask InitializeAsync()
    {
        var setupSw = Stopwatch.StartNew();

        if (NeedsLazyResources)
        {
            // the wait is shared by every class that needs them, so it is not cancelled itself: this test just stops waiting
            await fixture.WaitForLazyResourcesAsync().WaitAsync(TestContext.Current.CancellationToken);
        }

        // Register a brand-new portal for this test and bind a fresh set of clients to it.
        _clients = await fixture.CreatePortalAsync(TestContext.Current.CancellationToken);

        _aiClient = _clients.AiHttpClient;
        _ai = _clients.Ai;

        _aiChatClient = _clients.AiChatHttpClient;
        _aiProfiles = _clients.AiProfilesApi;
        _aiThreads = _clients.AiThreadsApi;
        _aiChat = _clients.AiChatApi;
        _aiAssignments = _clients.AiAssignmentsApi;
        _aiTools = _clients.AiToolsApi;
        _aiPrompts = _clients.AiPromptsApi;
        _aiPreferences = _clients.AiPreferencesApi;
        _aiSettings = _clients.AiSettingsApi;
        _aiContext = _clients.AiContextApi;
        _aiExport = _clients.AiExportApi;
        _aiAttachments = _clients.AiAttachmentsApi;
        _aiEditorTools = _clients.AiEditorToolsApi;
        _aiVectorization = _clients.AiVectorizationApi;
        _aiWebSearch = _clients.AiWebSearchApi;

        await _aiClient.Authenticate(Owner);
        await _aiChatClient.Authenticate(Owner);

        if (NeedsDocumentServer)
        {
            await fixture.WaitForDocumentServerAsync().WaitAsync(TestContext.Current.CancellationToken);
        }

        Timing.Write("setup.total", setupSw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Disposes the test's clients. xUnit calls only this on a class that implements <see cref="IAsyncLifetime"/>, never
    /// <see cref="IDisposable.Dispose"/>: a class that owns more (a stub provider of its own) overrides it and calls base.
    /// </summary>
    public virtual ValueTask DisposeAsync()
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

    /// <summary>An activated guest with a known password, invited by the owner.</summary>
    protected Task<User> InviteGuest(CancellationToken cancellationToken)
    {
        return Invitations.InviteGuestAsync(_clients.PeopleHttpClient, Owner, cancellationToken);
    }

    /// <summary>
    /// The status and error body an SDK call ended with: 200 and an empty body when it succeeded, the <see cref="ApiException"/>
    /// code and content otherwise.
    /// </summary>
    protected static async Task<(int Status, string Error)> OutcomeOfAsync(Func<Task> call)
    {
        try
        {
            await call();
            return (200, string.Empty);
        }
        catch (ApiException exception)
        {
            return (exception.ErrorCode, exception.ErrorContent?.ToString() ?? string.Empty);
        }
    }

    /// <summary>The HTTP status an SDK call ended with: 200 when it succeeded, the <see cref="ApiException"/> code otherwise.</summary>
    protected static async Task<int> StatusOfAsync(Func<Task> call) => (await OutcomeOfAsync(call)).Status;

    // The Files service, for the rooms, folders and files a chat or an export works with. Its client starts unsigned:
    // authenticate FilesHttpClient (as Owner or a member) before the first call.
    protected HttpClient FilesHttpClient => _clients.FilesHttpClient;
    protected FilesApi FilesApi => _clients.FilesApi;
    protected FoldersApi FoldersApi => _clients.FoldersApi;
    protected RoomsApi RoomsApi => _clients.RoomsApi;
    protected OperationsApi OperationsApi => _clients.OperationsApi;
    protected SharingApi SharingApi => _clients.SharingApi;
    protected FilesQuotaApi FilesQuotaApi => _clients.FilesQuotaApi;
    protected DocSpace.API.SDK.Api.Files.ThirdPartyIntegrationApi ThirdPartyIntegrationApi => _clients.ThirdPartyIntegrationApi;
    protected DocSpace.API.SDK.Api.Rooms.AIApi RoomsAiApi => _clients.RoomsAiApi;

    // The People service: member profiles (culture, type). Starts unsigned: authenticate it before the first call.
    protected HttpClient PeopleHttpClient => _clients.PeopleHttpClient;
    protected ProfilesApi ProfilesApi => _clients.ProfilesApi;

    // Web.Api, for portal settings and quotas. Starts unsigned: authenticate it before the first call.
    protected HttpClient WebApiHttpClient => _clients.WebApiHttpClient;
    protected SettingsQuotaApi SettingsQuotaApi => _clients.SettingsQuotaApi;
    protected CommonSettingsApi CommonSettingsApi => _clients.CommonSettingsApi;

    /// <summary>
    /// The profile input a public create takes: an OpenAI-compatible provider on the stub, which the chat service
    /// really dials to check the key and the model.
    /// </summary>
    protected AiCreateProfileInput BuildProfileInput(string? name = null) =>
        new(
            name: name ?? $"profile-{Guid.NewGuid():N}",
            providerType: new AiProviderType(AiBuiltinProviderType.Openai),
            baseUrl: ModelProvider.BaseUrl,
            key: StubModelProvider.Key,
            modelId: StubModelProvider.ModelId);

    /// <summary>Creates a profile through the public API; fails the test when the chat service rejects it.</summary>
    protected async Task<AiProfile> CreateProfileAsync(string? name = null)
    {
        var result = await _aiProfiles.AiProfilesCreateAsync(BuildProfileInput(name), TestContext.Current.CancellationToken);

        // Described only on a failure: the run-wide provider's log holds every request of the run, so building the
        // message on each success made every profile create walk all of it.
        if (!result.Success)
        {
            result.Success.Should().BeTrue($"profiles/create rejected the profile: {result.Error}\n{ModelProvider.Describe()}");
        }

        return result.Profile;
    }

    private AiProfile? _defaultProfile;

    /// <summary>A profile on the stub provider, created once per test on first use.</summary>
    protected async Task<AiProfile> GetDefaultProfileAsync() => _defaultProfile ??= await CreateProfileAsync();

    /// <summary>
    /// Creates a thread through the public API. Without a profile the route answers 404 when no model resolves for
    /// the portal, so a thread is bound to the test's default profile unless one is given.
    /// </summary>
    protected async Task<AiThread> CreateThreadAsync(string? title = null, string? profileId = null, string? entityId = null)
    {
        profileId ??= (await GetDefaultProfileAsync()).Id;

        return await _aiThreads.AiThreadsCreateAsync(
            // The generated constructor is not annotated for nullability; null leaves the field out of the body.
            new AiThreadsCreateRequest(title: title ?? $"thread-{Guid.NewGuid():N}", profileId: profileId!, entityId: entityId!),
            TestContext.Current.CancellationToken);
    }

    protected static AiThreadMessageLike BuildUserMessage(string? text = null) =>
        new(
            role: AiThreadMessageLike.RoleEnum.User,
            content: new AiMessageContent([new AiMessageContentBlock(type: "text", text: text ?? $"message-{Guid.NewGuid():N}")]));

    /// <summary>Stores a user message on a thread without running the model; returns the message id.</summary>
    protected async Task<string> AppendUserMessageAsync(string threadId, string? text = null)
    {
        // The route answers with the whole stored message under `messageId`; its id is the message id.
        var result = await _aiThreads.AiThreadsAppendUserMessageAsync(
            new AiThreadsAppendUserMessageRequest(threadId: threadId, message: BuildUserMessage(text)),
            TestContext.Current.CancellationToken);

        return result.MessageId.Id;
    }

    /// <summary>The text of a stored message: a plain string, or its text blocks joined.</summary>
    protected static string? TextOf(AiThreadMessageLike message) => message.Content?.ActualInstance switch
    {
        string text => text,
        List<AiMessageContentBlock> blocks => string.Concat(blocks.Select(b => b.Text)),
        _ => null
    };

    /// <summary>
    /// Creates a profile on a provider of the test's own — for a test that scripts what the model answers
    /// (<see cref="StubModelProvider.Enqueue"/>) or asserts on what the provider received.
    /// </summary>
    protected async Task<AiProfile> CreateProfileOnAsync(StubModelProvider provider, string? name = null)
    {
        var input = BuildProfileInput(name);
        input.BaseUrl = provider.BaseUrl;

        var result = await _aiProfiles.AiProfilesCreateAsync(input, TestContext.Current.CancellationToken);
        result.Success.Should().BeTrue($"profiles/create rejected the profile: {result.Error}\n{provider.Describe()}");

        return result.Profile;
    }

    /// <summary>
    /// Sends one chat round through <c>ai/send-with-stream</c> as whoever <see cref="_aiChatClient"/> is signed in as,
    /// and reads the whole ndjson stream.
    /// </summary>
    /// <remarks>
    /// Raw on purpose — an SDK defect: the response is an ndjson stream of AiChatEvent frames, but the SDK declares it
    /// as one AiChatEvent, so AiSendWithStreamAsync throws on the second frame. The request body is the SDK model.
    /// </remarks>
    protected async Task<ChatRound> SendWithStreamAsync(
        string? threadId,
        string text,
        string? profileId = null,
        string? entityId = null,
        AiActionArgs? actionArgs = null)
    {
        var body = new AiSendStreamBody(
            threadId: threadId!,
            userMessage: BuildUserMessage(text),
            actionArgs: actionArgs!,
            entityId: entityId!,
            profileId: profileId!);

        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            "/api/2.0/ai/ai/send-with-stream",
            body.ToJson(),
            TestContext.Current.CancellationToken);

        return ChatRound.Parse(response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>Switches AI on or off for the whole portal (<c>settings/ai-access</c>), as the owner.</summary>
    protected async Task SetPortalAiAccessAsync(bool enabled)
    {
        await _clients.WebApiHttpClient.Authenticate(Owner);
        await _clients.CommonSettingsApi.SetTenantAiAccessSettingsAsync(
            new TenantAiAccessSettingsRequestDto(enabled: enabled),
            TestContext.Current.CancellationToken);

        // Web.Api stores the switch; the AI service reads it through its own settings cache, which learns of the
        // change over the cache backplane a moment later. Until then a gated route still answers as before, so wait
        // until a gated read (threads/list) answers the way the new state says. Bounded and not asserted: a caller
        // that may not list threads at all (a guest) simply proceeds after the deadline.
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (DateTime.UtcNow < deadline)
        {
            if (await IsAiRefusedAsync() != enabled)
            {
                return;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Whether a gated read (<c>threads/list</c>) is refused with 403 for the current caller.</summary>
    private async Task<bool> IsAiRefusedAsync()
    {
        try
        {
            await _aiThreads.AiThreadsListAsync(count: 1, cancellationToken: TestContext.Current.CancellationToken);

            return false;
        }
        catch (ApiException e)
        {
            return e.ErrorCode == (int)HttpStatusCode.Forbidden;
        }
    }

    protected static CreateProfileRequestDto BuildCreateDto(string? name = null) =>
        new()
        {
            Name = name ?? $"profile-{Guid.NewGuid():N}",
            ProviderType = "openai",
            BaseUrl = "https://api.openai.com/v1",
            Key = "sk-test-key-" + Guid.NewGuid().ToString("N"),
            ModelId = "gpt-4o-mini",
            Reasoning = new ReasoningConfig { Thinks = false, CanDisable = true, Depths = [ReasoningDepth.None], DefaultDepth = ReasoningDepth.None },
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
            Reasoning = new ReasoningConfig { Thinks = true, CanDisable = false, Depths = [ReasoningDepth.Low, ReasoningDepth.Medium, ReasoningDepth.High], DefaultDepth = ReasoningDepth.Medium },
            Capabilities = Capabilities.Chat | Capabilities.Vision,
            UseResponsesApi = true,
            CanUseTool = false
        };

    protected async Task<ProfileDto> CreateProfileInternalAsync(CreateProfileRequestDto? dto = null)
    {
        dto ??= BuildCreateDto();
        using var response = await _ai.PostAsync(ProfilesPath, dto, TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<ProfileDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task CreateAssignmentInternalAsync(string actionType, Guid profileId, string? entityId = null)
    {
        using var response = await _ai.PostAsync(
            AssignmentsPath,
            new { actionType, profileId, entityId },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    protected async Task<Guid?> ReadAssignmentInternalAsync(string actionType, string? entityId = null)
    {
        var path = BuildScopedAssignmentPath(actionType, entityId);
        using var response = await _ai.GetAsync(path, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var wrapper = await response.Content.ReadFromJsonAsync<RawApiResponse<Guid?>>(
            _readJsonOptions,
            TestContext.Current.CancellationToken);
        return wrapper?.Response;
    }

    protected async Task<Dictionary<string, Guid>> ReadAllAssignmentsInternalAsync(string? entityId = null)
    {
        var path = entityId is null
            ? AssignmentsPath
            : $"{AssignmentsPath}?entityId={entityId}";

        using var response = await _ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<Dictionary<string, Guid>>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<ThreadDto> CreateThreadInternalAsync(string? title = null, Guid? profileId = null, string? entityId = null)
    {
        using var response = await _ai.PostAsync(
            ThreadsPath,
            new { title = title ?? $"thread-{Guid.NewGuid():N}", profileId, entityId },
            TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<ThreadDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<ThreadDto> ReadThreadInternalAsync(Guid id)
    {
        using var response = await _ai.GetAsync($"{ThreadsPath}/{id}", TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<ThreadDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<List<ThreadDto>> ReadAllThreadsInternalAsync(string? entityId = null)
    {
        var page = await ReadThreadsPageInternalAsync(DefaultPageCount, entityId);
        return page.Items;
    }

    protected async Task<ThreadsPageDto> ReadThreadsPageInternalAsync(
        int count,
        string? entityId = null,
        long? cursorLastEditDate = null,
        Guid? cursorId = null,
        string? query = null)
    {
        var queryParams = new List<string> { $"count={count}" };

        if (entityId is not null)
        {
            queryParams.Add($"entityId={entityId}");
        }
        if (query is not null)
        {
            queryParams.Add($"query={Uri.EscapeDataString(query)}");
        }
        if (cursorLastEditDate is not null)
        {
            queryParams.Add($"cursor.lastEditDate={cursorLastEditDate}");
        }
        if (cursorId is not null)
        {
            queryParams.Add($"cursor.id={cursorId}");
        }

        using var response = await _ai.GetAsync(
            $"{ThreadsPath}?{string.Join("&", queryParams)}",
            TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<ThreadsPageDto>(response, TestContext.Current.CancellationToken);
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

    protected async Task<MessageDto> CreateMessageInternalAsync(Guid threadId, string? contents = null)
    {
        using var response = await _ai.PostAsync(
            $"{ThreadsPath}/{threadId}/messages",
            new { contents = contents ?? BuildMessageContents() },
            TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<MessageDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<MessageDto> ReadMessageInternalAsync(Guid id)
    {
        using var response = await _ai.GetAsync($"{MessagesPath}/{id}", TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<MessageDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<List<MessageDto>> ReadMessagesByThreadInternalAsync(Guid threadId, int count = DefaultPageCount)
    {
        var page = await ReadMessagesPageInternalAsync(threadId, count);
        return page.Items;
    }

    protected async Task<MessagesPageDto> ReadMessagesPageInternalAsync(
        Guid threadId,
        int count,
        DateTimeOffset? cursorCreatedAt = null,
        Guid? cursorId = null,
        string? direction = null)
    {
        var query = new List<string> { $"count={count}" };

        if (direction is not null)
        {
            query.Add($"direction={direction}");
        }
        if (cursorCreatedAt is not null)
        {
            query.Add($"cursor.createdAt={Uri.EscapeDataString(cursorCreatedAt.Value.ToString("O"))}");
        }
        if (cursorId is not null)
        {
            query.Add($"cursor.id={cursorId}");
        }

        using var response = await _ai.GetAsync(
            $"{ThreadsPath}/{threadId}/messages?{string.Join("&", query)}",
            TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<MessagesPageDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<int> CreateRoomAsync(string? title = null)
    {
        await _clients.FilesHttpClient.Authenticate(Owner);

        var request = new CreateRoomRequestDto(title ?? $"room-{Guid.NewGuid():N}", roomType: RoomType.AiRoom);
        var room = (await _clients.RoomsApi.CreateRoomAsync(request, TestContext.Current.CancellationToken)).Response;

        return room.Id;
    }

    /// <summary>
    /// Returns the id of the owner's "My documents" root folder in the current test's portal.
    /// </summary>
    protected async Task<int> GetMyDocumentsFolderIdAsync()
    {
        await _clients.FilesHttpClient.Authenticate(Owner);

        var content = (await _clients.FoldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        return content.Current.Id;
    }

    protected async Task<PreferencesDto?> ReadPreferencesInternalAsync(string? entityId = null)
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

    protected async Task UpsertPreferencesInternalAsync(ReasoningDepth? depth, string? entityId = null)
    {
        using var response = await _ai.PutAsync(
            PreferencesPath,
            new { depth, entityId },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    protected async Task<Dictionary<string, ToolPreference>> ReadToolPrefsInternalAsync(string? entityId = null)
    {
        var path = entityId is null
            ? ToolPrefsPath
            : $"{ToolPrefsPath}?entityId={entityId}";

        using var response = await _ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<Dictionary<string, ToolPreference>>(response, TestContext.Current.CancellationToken);
    }

    protected async Task UpsertDisabledToolPrefsInternalAsync(
        IReadOnlyDictionary<string, HashSet<string>> disabled,
        string? entityId = null)
    {
        using var response = await _ai.PutAsync(
            $"{ToolPrefsPath}/disabled",
            new { disabled, entityId },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    protected async Task UpsertAllowAlwaysToolPrefsInternalAsync(
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

    protected async Task<ChatContextDto> ReadChatContextInternalAsync(
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

    protected async Task CreateMcpServerInternalAsync(string name, string? config = null, string? entityId = null)
    {
        using var response = await _ai.PostAsync(
            McpServersPath,
            new { name, config = config ?? BuildMcpConfig(), entityId },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    protected async Task<McpServerDto> ReadMcpServerInternalAsync(string name, string? entityId = null)
    {
        var path = entityId is null
            ? $"{McpServersPath}/{name}"
            : $"{McpServersPath}/{name}?entityId={entityId}";

        using var response = await _ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<McpServerDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<List<McpServerDto>> ReadAllMcpServersInternalAsync(string? entityId = null)
    {
        var path = entityId is null
            ? McpServersPath
            : $"{McpServersPath}?entityId={entityId}";

        using var response = await _ai.GetAsync(path, TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<List<McpServerDto>>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<PromptFolderDto> CreatePromptFolderInternalAsync(string? name = null)
    {
        using var response = await _ai.PostAsync(
            PromptFoldersPath,
            new { name = name ?? $"folder-{Guid.NewGuid():N}" },
            TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<PromptFolderDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<PromptFolderDto> ReadPromptFolderInternalAsync(Guid id)
    {
        using var response = await _ai.GetAsync($"{PromptFoldersPath}/{id}", TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<PromptFolderDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<PromptDto> CreatePromptInternalAsync(string? name = null, string? text = null, Guid? folderId = null)
    {
        using var response = await _ai.PostAsync(
            PromptsPath,
            new { name = name ?? $"prompt-{Guid.NewGuid():N}", text = text ?? "body", folderId },
            TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<PromptDto>(response, TestContext.Current.CancellationToken);
    }

    protected async Task<PromptDto> ReadPromptInternalAsync(Guid id)
    {
        using var response = await _ai.GetAsync($"{PromptsPath}/{id}", TestContext.Current.CancellationToken);
        return await _ai.ReadAsync<PromptDto>(response, TestContext.Current.CancellationToken);
    }
}
