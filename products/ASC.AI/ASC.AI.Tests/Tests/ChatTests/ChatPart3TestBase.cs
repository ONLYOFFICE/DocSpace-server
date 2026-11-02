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

namespace ASC.AI.Tests.Tests.ChatTests;

/// <summary>
/// Shared helpers of the third part of <c>ai/chat/chat.spec.ts</c>: agents picked in the composer, AI rooms, image
/// generation, threads changed mid-reply or by another actor, provider failures and the thread validation routes.
/// </summary>
/// <remarks>
/// The TypeScript suite ran against real models behind the AI gateway. Here every model is a
/// <see cref="StubModelProvider"/> of the test's own (<see cref="NewProvider"/>), so what the model says is scripted
/// and the assertions are on the server's part: what was stored, streamed and sent to the provider. Answers are queued
/// only after the profile on that provider exists — the profile create sends a streamed tool probe that would take
/// the first one. The assignment helpers come from <see cref="AssignmentsStorageTests.AssignmentsTestBase"/>.
/// </remarks>
public abstract class ChatPart3TestBase(AspireAppFixture fixture) : AssignmentsStorageTests.AssignmentsTestBase(fixture), IDisposable
{
    protected const string ChatCompletionsPath = "/v1/chat/completions";
    protected const string ImageGenerationsPath = "/v1/images/generations";
    protected const string GenerateImageTool = "generate_image";

    /// <summary>The one request every drawing test makes, and the arguments the scripted model draws it with.</summary>
    protected const string PictureRequest = "Generate an image of a red circle on a white background.";
    protected const string DrawingArguments = """{"prompt":"a red circle on a white background"}""";

    // Capability bits of a profile: Chat 0x001, Image 0x002, Tools 0x100. The engine binds ImageGeneration only to a
    // profile with Image, and offers the built-in drawing tool only while such a profile is bound.
    protected const decimal ChatCapabilities = 257m;
    protected const decimal ImageOnlyCapabilities = 2m;
    protected const decimal ChatAndImageCapabilities = 259m;

    private readonly List<StubModelProvider> _providers = [];

    public void Dispose()
    {
        foreach (var provider in _providers)
        {
            provider.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>A stand-in model of the test's own, disposed with the test.</summary>
    protected StubModelProvider NewProvider(string answer = StubModelProvider.DefaultAnswer)
    {
        var provider = new StubModelProvider(answer);
        _providers.Add(provider);

        return provider;
    }

    /// <summary>A profile on <paramref name="provider"/> declaring exactly <paramref name="capabilities"/>.</summary>
    protected async Task<AiProfile> CreateCapableProfileAsync(StubModelProvider provider, decimal capabilities)
    {
        var input = BuildProfileInput();
        input.BaseUrl = provider.BaseUrl;
        input.Capabilities = capabilities;

        var result = await _aiProfiles.AiProfilesCreateAsync(input, TestContext.Current.CancellationToken);
        result.Success.Should().BeTrue($"profiles/create rejected the profile: {result.Error}\n{provider.Describe()}");

        return result.Profile;
    }

    /// <summary>
    /// A chat model that cannot draw (the portal's first profile, so its default) and a model that can, bound to
    /// ImageGeneration — each on a provider of its own, so the requests each one saw are told apart.
    /// </summary>
    protected async Task<(StubModelProvider Chat, AiProfile ChatProfile, StubModelProvider Image, AiProfile ImageProfile)> ArrangeDrawingAsync()
    {
        var chat = NewProvider();
        var chatProfile = await CreateCapableProfileAsync(chat, ChatCapabilities);
        var image = NewProvider();
        var imageProfile = await CreateCapableProfileAsync(image, ChatAndImageCapabilities);

        await AssignAsync(AiActionType.ImageGeneration, imageProfile.Id);

        return (chat, chatProfile, image, imageProfile);
    }

    /// <summary>The names of the tools a chat request offered the model (<c>tools[].function.name</c>).</summary>
    protected static List<string> OfferedTools(string requestBody)
    {
        using var document = JsonDocument.Parse(requestBody);

        if (!document.RootElement.TryGetProperty("tools", out var tools) || tools.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return [.. tools.EnumerateArray()
            .Select(t => t.TryGetProperty("function", out var f) ? Str(f, "name") : null)
            .OfType<string>()];
    }

    /// <summary>The per-request system prompt the composer sends: <c>actionArgs.prompt {mode: "replace"}</c>.</summary>
    protected static AiActionArgs ReplacePrompt(string text) =>
        new(prompt: new AiActionArgsPrompt(AiActionArgsPrompt.ModeEnum.Replace, text));

    /// <summary>
    /// The bodies of the streamed chat requests <paramref name="provider"/> received that carry
    /// <paramref name="text"/> — a chat round, never the title request or the profile's tool probe.
    /// </summary>
    protected static List<string> StreamedRequestsCarrying(StubModelProvider provider, string text) =>
        provider.CompletionRequestBodies.Where(b => IsStreamed(b) && b.Contains(text, StringComparison.Ordinal)).ToList();

    /// <summary>
    /// Makes every streamed chat request that <paramref name="when"/> accepts wait <paramref name="delay"/> before
    /// <paramref name="answer"/> comes back — a reply that is still being written while the test acts. Wins over the
    /// scripted queue (priority 0).
    /// </summary>
    protected static void AnswerSlowly(StubModelProvider provider, TimeSpan delay, string answer, Func<string, bool>? when = null) =>
        provider.Server
            .Given(Request.Create().WithPath(ChatCompletionsPath).UsingPost()
                .WithBody((string? body) => body is not null && IsStreamed(body) && (when is null || when(body))))
            .AtPriority(0)
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/event-stream")
                .WithBody(StubModelProvider.Text(answer).Body)
                .WithDelay(delay));

    /// <summary>
    /// Starts a chat round and hands control back as soon as the question is stored — the earliest proof the round
    /// is in flight — while the provider is still writing. The round is not awaited.
    /// </summary>
    protected async Task<Task<ChatRound>> StartReplyAsync(string threadId, string text, string? profileId = null)
    {
        var inFlight = SendWithStreamAsync(threadId, text, profileId);
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (!inFlight.IsCompleted && DateTime.UtcNow < deadline)
        {
            var (_, messages) = await TryReadMessagesRawAsync(threadId);

            if (messages.Any(IsUser))
            {
                break;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        return inFlight;
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Stored messages, read raw

    /// <summary>
    /// The stored messages of a thread as raw JSON.
    /// </summary>
    /// <remarks>
    /// Raw on purpose — an SDK defect: <c>AiThreadMessageLikeStatus</c> keeps only <c>type</c> and drops the
    /// <c>reason</c> and <c>error {code, message}</c> a failed reply carries, and
    /// <c>AiThreadMessageLikeContentAnyOfInner</c> keeps only <c>type</c> and <c>text</c>, dropping the
    /// <c>toolName</c> and <c>result</c> of a tool-call part. Both are what these tests assert.
    /// </remarks>
    protected async Task<List<JsonElement>> ReadMessagesRawAsync(string threadId)
    {
        var (status, messages) = await TryReadMessagesRawAsync(threadId);
        status.Should().Be(HttpStatusCode.OK, $"read-messages of thread {threadId}");

        return messages;
    }

    /// <summary>The status of read-messages and the messages it returned (empty on a failure).</summary>
    protected async Task<(HttpStatusCode Status, List<JsonElement> Messages)> TryReadMessagesRawAsync(string threadId)
    {
        using var response = await new RawApiClient(_aiChatClient).GetAsync(
            $"/api/2.0/ai/threads/read-messages?threadId={Uri.EscapeDataString(threadId)}",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return (response.StatusCode, []);
        }

        using var document = JsonDocument.Parse(body);

        return (response.StatusCode, document.RootElement.ValueKind == JsonValueKind.Array
            ? [.. document.RootElement.EnumerateArray().Select(m => m.Clone())]
            : []);
    }

    /// <summary>
    /// Reads the thread until it holds <paramref name="count"/> assistant replies or the deadline passes, and returns
    /// what it last saw: the reply is stored by the round, so a bare read right after the send can race it.
    /// </summary>
    protected async Task<List<JsonElement>> WaitForAssistantRepliesAsync(string threadId, int count = 1)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var messages = await ReadMessagesRawAsync(threadId);

            if (messages.Count(IsAssistant) >= count || DateTime.UtcNow >= deadline)
            {
                return messages;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    protected static bool IsUser(JsonElement message) => Str(message, "role") == "user";

    protected static bool IsAssistant(JsonElement message) => Str(message, "role") == "assistant";

    protected static List<JsonElement> UserMessages(IEnumerable<JsonElement> messages) => [.. messages.Where(IsUser)];

    protected static List<JsonElement> AssistantMessages(IEnumerable<JsonElement> messages) => [.. messages.Where(IsAssistant)];

    /// <summary>The text of a stored message: a plain string, or its text parts joined.</summary>
    protected static string MessageText(JsonElement message)
    {
        if (!message.TryGetProperty("content", out var content))
        {
            return "";
        }

        return content.ValueKind switch
        {
            JsonValueKind.String => content.GetString() ?? "",
            JsonValueKind.Array => string.Concat(content.EnumerateArray()
                .Where(p => Str(p, "type") == "text")
                .Select(p => Str(p, "text"))),
            _ => ""
        };
    }

    /// <summary><c>status.type</c>, <c>status.reason</c>, <c>status.error.code</c> and <c>status.error.message</c>.</summary>
    protected static (string? Type, string? Reason, string? Code, string? Message) StatusOf(JsonElement message)
    {
        if (!message.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.Object)
        {
            return (null, null, null, null);
        }

        var error = status.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.Object ? e : (JsonElement?)null;

        return (Str(status, "type"), Str(status, "reason"), error is null ? null : Str(error.Value, "code"), error is null ? null : Str(error.Value, "message"));
    }

    /// <summary>A reply that finished: it carries no failure, and text.</summary>
    protected static void AssertHealthyReply(JsonElement reply, string because)
    {
        var status = StatusOf(reply);
        status.Reason.Should().NotBe("error", $"{because}: {reply}");
        status.Code.Should().BeNull($"{because}: {reply}");
        MessageText(reply).Should().NotBeEmpty($"{because}: {reply}");
    }

    /// <summary>The tool-call parts of a stored message: the tool name and its result, if it has one.</summary>
    protected static List<(string? Name, JsonElement? Result)> ToolCallsOf(JsonElement message)
    {
        if (!message.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return [.. content.EnumerateArray()
            .Where(p => Str(p, "type") == "tool-call")
            .Select(p => (Str(p, "toolName"), p.TryGetProperty("result", out var r) && r.ValueKind != JsonValueKind.Null ? r : (JsonElement?)null))];
    }

    protected static string? Str(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    // ---------------------------------------------------------------------------------------------------------------
    // Rooms, folders and members (the Files side)

    /// <summary>Creates a room of the given type as the Owner, optionally with chat settings; returns it.</summary>
    protected async Task<FolderDto> CreateRoomOfTypeAsync(RoomType roomType, string? title = null, ChatSettings? chatSettings = null)
    {
        await FilesHttpClient.Authenticate(Owner);

        var room = await RoomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title ?? $"room-{Guid.NewGuid():N}", roomType: roomType, chatSettings: chatSettings!),
            TestContext.Current.CancellationToken);

        return room.Response;
    }

    /// <summary>Grants <paramref name="user"/> the access level in the room, as the Owner, without a notification.</summary>
    protected async Task ShareRoomAsync(int roomId, User user, DocSpace.API.SDK.Model.FileShare access)
    {
        await FilesHttpClient.Authenticate(Owner);

        await RoomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest(invitations: [new RoomInvitation(user.Id, access)], notify: false),
            TestContext.Current.CancellationToken);
    }

    /// <summary>The id of <paramref name="user"/>'s own My Documents.</summary>
    protected async Task<int> MyDocumentsOfAsync(User user)
    {
        await FilesHttpClient.Authenticate(user);

        var content = (await FoldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        return content.Current.Id;
    }

    /// <summary>The files of a folder, read as <paramref name="user"/>.</summary>
    protected async Task<List<FileDto>> ListFilesAsync(User user, int folderId)
    {
        await FilesHttpClient.Authenticate(user);

        var content = (await FoldersApi.GetFolderByFolderIdAsync(
            folderId,
            count: DefaultPageCount,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        return [.. (content.Files ?? []).OfType<FileDto>()];
    }

    /// <summary>
    /// The ids of a folder's files once two consecutive listings agree: a fresh portal or member keeps filling My
    /// Documents with the sample documents in the background, so a baseline taken straight away grows on its own.
    /// </summary>
    protected async Task<HashSet<int>> StableFileIdsAsync(User user, int folderId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        var previous = (await ListFilesAsync(user, folderId)).Select(f => f.Id).ToHashSet();

        while (true)
        {
            await Task.Delay(2000, TestContext.Current.CancellationToken);
            var current = (await ListFilesAsync(user, folderId)).Select(f => f.Id).ToHashSet();

            if (current.SetEquals(previous) || DateTime.UtcNow >= deadline)
            {
                return current;
            }

            previous = current;
        }
    }

    /// <summary>
    /// Waits for a file that is not in <paramref name="known"/> to turn up in the folder; null when none did. Matched
    /// on ids, not on a name: the picture's file name is the server's to choose.
    /// </summary>
    protected async Task<FileDto?> WaitForNewFileAsync(User user, int folderId, IReadOnlySet<int> known)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var fresh = (await ListFilesAsync(user, folderId)).FirstOrDefault(f => !known.Contains(f.Id));

            if (fresh is not null || DateTime.UtcNow >= deadline)
            {
                return fresh;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }

    private static bool IsStreamed(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);

            return document.RootElement.TryGetProperty("stream", out var stream) && stream.ValueKind == JsonValueKind.True;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
