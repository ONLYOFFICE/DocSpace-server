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

namespace ASC.AI.Tests.Tests.MessageStorageTests;

/// <summary>
/// Helpers for the per-message, regenerate and stop tests: a provider of the test's own to script what the model
/// answers, the regenerate stream, a stream the client hangs up on, and polling of the history the chat service
/// writes after a stream has ended.
/// </summary>
public abstract class MessagePart1TestBase(AspireAppFixture fixture) : MessageTestBase(fixture), IDisposable
{
    protected const string RegenerateStreamPath = "/api/2.0/ai/ai/regenerate-stream";
    protected const string SendWithStreamPath = "/api/2.0/ai/ai/send-with-stream";

    /// <summary>A well-formed message id no message has.</summary>
    protected const string UnknownMessageId = "019fcc1d-478e-749f-85df-7427ca64566b";

    /// <summary>The word a complete answer of the slow provider ends with; its absence marks an unfinished one.</summary>
    protected const string Sentinel = "FINISHED";

    /// <summary>What the slow provider streams before it stalls.</summary>
    protected const string PartialAnswer = "1. Typography began with movable type";

    // Chat | Image | Tools (0x001 | 0x002 | 0x100): a profile the Chat slot accepts (see AssignmentsTestBase).
    private const decimal ChatCapabilities = 259m;

    /// <summary>How long a reply that is being written is polled for.</summary>
    protected static TimeSpan ReplyTimeout { get; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long a refused or no-op request is watched before the thread is called unchanged: a generation the stub
    /// serves lands well within it, so a read straight after the refusal would prove nothing.
    /// </summary>
    protected static TimeSpan SettleTime { get; } = TimeSpan.FromSeconds(5);

    /// <summary>How long the slow provider stalls mid-answer before it sends the rest and the sentinel.</summary>
    protected static TimeSpan StallTime { get; } = TimeSpan.FromSeconds(15);

    private StubModelProvider? _provider;

    /// <summary>A provider of this test's own, started on first use.</summary>
    protected StubModelProvider Provider => _provider ??= new();

    public void Dispose()
    {
        _provider?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>A profile on <see cref="Provider"/> and a thread bound to it.</summary>
    protected async Task<(AiProfile Profile, string ThreadId)> CreateScriptedThreadAsync(string? entityId = null)
    {
        var profile = await CreateProfileOnAsync(Provider);
        var thread = await CreateThreadAsync(profileId: profile.Id, entityId: entityId);

        return (profile, thread.ThreadId);
    }

    /// <summary>A profile on <paramref name="provider"/> that the Chat slot accepts.</summary>
    protected async Task<AiProfile> CreateChatProfileOnAsync(StubModelProvider provider)
    {
        var input = BuildProfileInput();
        input.BaseUrl = provider.BaseUrl;
        input.Capabilities = ChatCapabilities;

        var result = await _aiProfiles.AiProfilesCreateAsync(input, TestContext.Current.CancellationToken);
        result.Success.Should().BeTrue($"profiles/create rejected the profile: {result.Error}\n{provider.Describe()}");

        return result.Profile;
    }

    /// <summary>
    /// Sends one question on a thread of <see cref="Provider"/> with <paramref name="answer"/> scripted as the reply,
    /// and waits for that reply to be stored; returns the history.
    /// </summary>
    protected async Task<List<AiThreadMessageLike>> SendAnsweredAsync(
        string threadId,
        string question,
        string answer,
        string? profileId = null,
        string? entityId = null)
    {
        var assistantsBefore = Replies(await ReadMessagesAsync(threadId)).Count;
        Provider.Enqueue(StubModelProvider.Text(answer));

        var round = await SendWithStreamAsync(threadId, question, profileId, entityId);
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());

        var messages = await WaitForMessagesAsync(threadId, m =>
            Replies(m).Count > assistantsBefore && TextOf(Replies(m)[^1]) == answer);

        Replies(messages).Select(TextOf).Should().Contain(answer, $"the reply is stored on the thread\n{Provider.Describe()}");

        return messages;
    }

    /// <summary>
    /// Runs <c>ai/regenerate-stream</c> as whoever <see cref="BaseTest._aiChatClient"/> is signed in as and reads the
    /// whole ndjson stream.
    /// </summary>
    /// <remarks>
    /// Raw on purpose — the SDK defect send-with-stream has too: the response is an ndjson stream of AiChatEvent
    /// frames, but the SDK declares it as one AiChatEvent, so AiRegenerateStreamAsync throws on the second frame.
    /// The request body is the SDK model.
    /// </remarks>
    protected async Task<ChatRound> RegenerateAsync(string threadId, string? profileId = null, string? entityId = null)
    {
        // The generated constructor is not annotated for nullability; null leaves the field out of the body.
        var body = new AiRegenerateStreamRequest(threadId: threadId, entityId: entityId!, profileId: profileId!);

        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            RegenerateStreamPath,
            body.ToJson(),
            TestContext.Current.CancellationToken);

        return ChatRound.Parse(response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>The id the stream reports for the reply it opened (the <c>message-start</c> frame).</summary>
    protected static string? StartedMessageId(ChatRound round) =>
        round.OfType("message-start")
            .Select(f => f.TryGetProperty("messageId", out var id) && id.ValueKind == JsonValueKind.String ? id.GetString() : null)
            .FirstOrDefault(id => id is not null);

    protected static List<AiThreadMessageLike> Replies(IEnumerable<AiThreadMessageLike> messages) =>
        [.. messages.Where(m => m.Role == AiThreadMessageLike.RoleEnum.Assistant)];

    protected static List<AiThreadMessageLike> Questions(IEnumerable<AiThreadMessageLike> messages) =>
        [.. messages.Where(m => m.Role == AiThreadMessageLike.RoleEnum.User)];

    /// <summary>
    /// Polls the history until <paramref name="until"/> holds or the deadline passes, and returns the last read
    /// either way — the assertion that follows shows what was there.
    /// </summary>
    protected async Task<List<AiThreadMessageLike>> WaitForMessagesAsync(
        string threadId,
        Func<List<AiThreadMessageLike>, bool> until,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? ReplyTimeout);

        while (true)
        {
            var messages = await ReadMessagesAsync(threadId);

            if (until(messages) || DateTime.UtcNow >= deadline)
            {
                return messages;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Polls until a reply other than <paramref name="oldId"/> is stored and <paramref name="oldId"/> is gone.</summary>
    protected Task<List<AiThreadMessageLike>> WaitForReplacedReplyAsync(string threadId, string oldId, int expectedReplies = 1) =>
        WaitForMessagesAsync(threadId, m =>
        {
            var replies = Replies(m);

            return replies.Count >= expectedReplies
                && replies.All(r => r.Id != oldId)
                && replies.All(r => !string.IsNullOrEmpty(TextOf(r)));
        });

    /// <summary>The number of streamed chat requests a provider received — one per model round.</summary>
    protected static int StreamedRequests(StubModelProvider provider) => provider.CompletionRequestBodies.Count(IsStreamed);

    /// <summary>The body of the last streamed chat request a provider received.</summary>
    protected static string? LastStreamedRequest(StubModelProvider provider) => provider.CompletionRequestBodies.LastOrDefault(IsStreamed);

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

    /// <summary>Invites a member into a room as a content creator, as the owner.</summary>
    protected async Task InviteToRoomAsync(int roomId, User member)
    {
        await RoomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest(
                invitations: [new RoomInvitation(id: member.Id, access: DocSpace.API.SDK.Model.FileShare.ContentCreator)],
                notify: false),
            TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Makes every streamed chat request of <paramref name="provider"/> stall: it streams <see cref="PartialAnswer"/>,
    /// then sends nothing for <see cref="StallTime"/>, then the rest of the answer ending in <see cref="Sentinel"/>.
    /// Returns the mapping id, for <see cref="WireMockServer.DeleteMapping(Guid)"/> once the stall is no longer wanted.
    /// </summary>
    /// <remarks>Add it after the profile is created: the profile's tool probe is a streamed request as well.</remarks>
    protected static Guid StallStreams(StubModelProvider provider)
    {
        var mappingId = Guid.NewGuid();

        provider.Server
            .Given(Request.Create().WithPath("/v1/chat/completions").UsingPost()
                .WithBody((string? body) => StubModelProvider.IsStreamed(body)))
            .WithGuid(mappingId)
            .AtPriority(0)
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/event-stream")
                .WithSseBody(async (_, queue) =>
                {
                    queue.Write(SseChunk(new { role = "assistant", content = "" }, null));
                    queue.Write(SseChunk(new { content = PartialAnswer }, null));

                    await Task.Delay(StallTime);

                    queue.Write(SseChunk(new { content = $" and the essay is done. {Sentinel}" }, null));
                    queue.Write(SseChunk(new { }, "stop"));
                    queue.Write("data: [DONE]\n\n");
                    queue.Close();
                }));

        return mappingId;
    }

    private static string SseChunk(object delta, string? finishReason) =>
        "data: " + JsonSerializer.Serialize(new
        {
            id = "chatcmpl-stall",
            @object = "chat.completion.chunk",
            created = 0,
            model = StubModelProvider.ModelId,
            choices = new[] { new { index = 0, delta, finish_reason = finishReason } }
        }) + "\n\n";

    /// <summary>
    /// Sends a question through <c>send-with-stream</c> and hangs up on the first frame that carries reply text
    /// (<c>message-start</c> or <c>message-delta</c>) — the client's stop gesture: the chat widget has no stop route, it aborts the request.
    /// </summary>
    protected async Task<CutStream> SendAndHangUpOnFirstDeltaAsync(string threadId, string text, string? profileId)
    {
        var body = new AiSendStreamBody(threadId: threadId, userMessage: BuildUserMessage(text), profileId: profileId!);

        using var hangUp = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        // A stream that never shows a delta must not hold the test past the stall.
        hangUp.CancelAfter(StallTime + TimeSpan.FromSeconds(30));

        var framesSeen = new List<string>();
        string? streamedText = null;
        var status = HttpStatusCode.OK;

        // Raw on purpose — the send-with-stream SDK defect, and a typed call cannot be cut mid-body anyway.
        using var request = new HttpRequestMessage(HttpMethod.Post, SendWithStreamPath)
        {
            Content = new StringContent(body.ToJson(), System.Text.Encoding.UTF8, "application/json")
        };

        try
        {
            using var response = await _aiChatClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, hangUp.Token);
            status = response.StatusCode;

            await using var stream = await response.Content.ReadAsStreamAsync(hangUp.Token);
            using var reader = new StreamReader(stream);

            while (await reader.ReadLineAsync(hangUp.Token) is { } line)
            {
                var frame = ChatRound.Parse(status, line);
                framesSeen.AddRange(frame.Frames.Select(f => f.TryGetProperty("type", out var t) ? t.GetString() ?? "?" : "?"));

                // message-start already carries the first provider chunk's text, so it counts as the first content:
                // waiting for a message-delta would wait out the stall and cut after the answer was complete.
                var replyText = frame.Frames
                    .Where(f => f.TryGetProperty("type", out var t) && t.GetString() is "message-start" or "message-delta")
                    .Select(ChatRound.TextOfFrame)
                    .LastOrDefault(t => !string.IsNullOrEmpty(t));

                if (replyText is not null)
                {
                    streamedText = replyText;
                    break;
                }
            }

            // Cancelling the read and disposing the unread response drops the connection.
            await hangUp.CancelAsync();
        }
        catch (OperationCanceledException) when (!TestContext.Current.CancellationToken.IsCancellationRequested)
        {
            // The hang-up itself, or the safety timeout; streamedText tells which.
        }

        return new CutStream(status, streamedText, framesSeen);
    }

    /// <summary>
    /// Reads the history and returns it with each message's status: the <c>reason</c> (<c>cancelled</c> vs
    /// <c>error</c>) and the <c>error</c> of an incomplete reply are what tells a stopped reply from a failed one.
    /// </summary>
    protected async Task<List<StoredMessage>> ReadStoredMessagesAsync(string threadId)
    {
        var messages = await _aiThreads.AiThreadsReadMessagesAsync(threadId, cancellationToken: TestContext.Current.CancellationToken);

        return [.. messages.Select(StoredMessage.From)];
    }

    /// <summary>Polls the history until <paramref name="until"/> holds or the deadline passes; returns the last read.</summary>
    protected async Task<List<StoredMessage>> WaitForStoredMessagesAsync(string threadId, Func<List<StoredMessage>, bool> until)
    {
        var deadline = DateTime.UtcNow + ReplyTimeout;

        while (true)
        {
            var messages = await ReadStoredMessagesAsync(threadId);

            if (until(messages) || DateTime.UtcNow >= deadline)
            {
                return messages;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>What a hung-up stream showed before the cut.</summary>
    protected sealed record CutStream(HttpStatusCode StatusCode, string? StreamedText, IReadOnlyList<string> FramesSeen)
    {
        public bool CutMidReply => StreamedText is not null;

        public override string ToString() => $"HTTP {(int)StatusCode}, frames [{string.Join(",", FramesSeen)}], streamed \"{StreamedText}\"";
    }

    /// <summary>One stored message flattened for the assertions, status included.</summary>
    protected sealed record StoredMessage(string? Id, string? Role, string Text, string? StatusType, string? StatusReason, string? StatusError)
    {
        public bool IsAssistant => Role == "assistant";

        public bool IsUser => Role == "user";

        public static StoredMessage From(AiThreadMessageLike message) =>
            new(
                message.Id,
                message.Role switch
                {
                    AiThreadMessageLike.RoleEnum.User => "user",
                    AiThreadMessageLike.RoleEnum.Assistant => "assistant",
                    _ => message.Role.ToString().ToLowerInvariant()
                },
                message.Content?.ActualInstance switch
                {
                    string text => text,
                    List<AiMessageContentBlock> blocks => string.Concat(blocks.Where(b => b.Type == "text").Select(b => b.Text)),
                    _ => string.Empty
                },
                message.Status?.Type,
                message.Status?.Reason,
                message.Status?.Error?.ToJson());

        public override string ToString() => $"{Role} {Id} [{StatusType}/{StatusReason}] \"{Text}\"";
    }
}
