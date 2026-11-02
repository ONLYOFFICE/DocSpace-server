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
/// Helpers of the message suites that run a model round: the OpenAI-compatible stream, the one-shot routes
/// (<c>ai/send</c>, <c>ai/send-custom</c>), the copy and markdown checks. What the model answers is scripted on a
/// <see cref="StubModelProvider"/> of the test's own, so a test asserts what the server did with the answer — what it
/// stored, streamed and sent to the model — never what a model chose to say.
/// </summary>
public abstract class MessagePart2TestBase(AspireAppFixture fixture) : MessageTestBase(fixture)
{
    protected const string SendPath = "/api/2.0/ai/ai/send";
    protected const string SendCustomPath = "/api/2.0/ai/ai/send-custom";
    protected const string SendWithStreamOpenAiPath = "/api/2.0/ai/ai/send-with-stream-openai";
    protected const string RegenerateStreamPath = "/api/2.0/ai/ai/regenerate-stream";

    protected const string EmptyUserMessageError = "userMessage must contain non-empty text content or an attachment";

    // Fixed ids, so a second AnswerEveryCompletionWith on the same provider replaces the first instead of adding to it.
    private static readonly Guid _streamedAnswerMapping = Guid.Parse("6d7f2a51-0c55-4f8e-9a51-3c3a8e0f1a01");
    private static readonly Guid _plainAnswerMapping = Guid.Parse("6d7f2a51-0c55-4f8e-9a51-3c3a8e0f1a02");

    /// <summary>Rendered-HTML tells: none of these may appear in a markdown reply.</summary>
    protected static string[] HtmlTells { get; } =
    [
        "<h1", "<h2", "<h3", "<strong", "<em>", "<ol", "<ul", "<li", "<table", "<thead", "<pre", "<code", "<blockquote",
        "<hr", "<a href", "<p>"
    ];

    /// <summary>Escaping tells: pasting <c>&amp;amp;</c> or <c>\|</c> is the same defect as pasting <c>&lt;b&gt;</c>.</summary>
    protected static string[] EscapeTells { get; } = ["&lt;", "&gt;", "&amp;", "&quot;", "&#", "\\*", "\\#", "\\|", "\\_"];

    /// <summary>
    /// The answer a user copies or saves, as markdown. Every construct carries a word of its own, so a check can name
    /// the construct that broke.
    /// </summary>
    protected static string MarkdownAnswer { get; } = string.Join("\n",
        "# COPYCHECK",
        "",
        "**BOLDWORD** and *ITALICWORD*",
        "",
        "- LISTONE",
        "- LISTTWO",
        "",
        "```js",
        "const answer = 42;",
        "```",
        "",
        "| CELLA | CELLB |",
        "| --- | --- |",
        "| 1 | 2 |");

    /// <summary>The constructs of a reply the copy/render checks name, beyond <see cref="MarkdownAnswer"/>.</summary>
    protected static string RenderBlock { get; } = string.Join("\n",
        "## RENDERCHECK",
        "",
        "**BOLDWORD**, *ITALICWORD*, `INLINEWORD`, ~~STRUCKWORD~~",
        "",
        "### RENDERSUB",
        "",
        "1. ORDEREDONE",
        "2. ORDEREDTWO",
        "",
        "- OUTERONE",
        "  - NESTEDONE",
        "",
        "> QUOTEWORD",
        "",
        "[LINKWORD](https://example.invalid/docs?a=1&b=2#frag)",
        "",
        "| CELLA | CELLB |",
        "| --- | --- |",
        "| 1 | 2 |",
        "",
        "```python",
        "print(\"CODEWORD\")",
        "```",
        "",
        "---");

    /// <summary>A chat round on a thread of its own whose model answer is scripted, with the reply as it was stored.</summary>
    protected sealed record ScriptedReply(string ProfileId, string ThreadId, ChatRound Round, AiThreadMessageLike Reply)
    {
        public string Text => TextOf(Reply) ?? string.Empty;
    }

    /// <summary>
    /// Opens a thread on a profile of <paramref name="provider"/>, has the model answer <paramref name="answer"/> to
    /// <paramref name="question"/> through <c>send-with-stream</c>, and returns the round and the stored reply.
    /// </summary>
    protected async Task<ScriptedReply> ReplyWithAsync(StubModelProvider provider, string answer, string question = "Hi there!")
    {
        var profile = await CreateProfileOnAsync(provider);
        var thread = await CreateThreadAsync(profileId: profile.Id);

        // After the profile: its create sends a streamed tool probe that would take the answer.
        provider.Enqueue(StubModelProvider.Text(answer));

        var round = await SendWithStreamAsync(thread.ThreadId, question, profile.Id);

        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull($"{round}\n{provider.Describe()}");
        round.IncompleteStatus.Should().BeNull($"{round}\n{provider.Describe()}");

        var messages = await WaitForMessagesAsync(thread.ThreadId, list => AssistantMessages(list).Any(m => !string.IsNullOrEmpty(TextOf(m))));
        var reply = AssistantMessages(messages).FirstOrDefault();

        reply.Should().NotBeNull($"the reply was not stored: {Describe(messages)}");

        return new ScriptedReply(profile.Id, thread.ThreadId, round, reply!);
    }

    /// <summary>
    /// Has <paramref name="provider"/> answer every chat completion with <paramref name="text"/>, streamed or not, ahead
    /// of its queue. For the one-shot routes, which may call the provider either way.
    /// </summary>
    protected static void AnswerEveryCompletionWith(StubModelProvider provider, string text)
    {
        provider.Server
            .Given(Request.Create().WithPath("/v1/chat/completions").UsingPost().WithBody((string? body) => StubModelProvider.IsStreamed(body)))
            .WithGuid(_streamedAnswerMapping)
            .AtPriority(0)
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/event-stream")
                .WithBody(StubModelProvider.Text(text).Body));

        // Priority 1: a streamed request is taken by the mapping above first, a plain one beats the provider's own
        // plain answer (priority 2).
        provider.Server
            .Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .WithGuid(_plainAnswerMapping)
            .AtPriority(1)
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBodyAsJson(new
            {
                id = "chatcmpl-stub",
                @object = "chat.completion",
                created = 0,
                model = StubModelProvider.ModelId,
                choices = new[] { new { index = 0, message = new { role = "assistant", content = text }, finish_reason = "stop" } },
                usage = new { prompt_tokens = 5, completion_tokens = 2, total_tokens = 7 }
            }));
    }

    /// <summary>
    /// The chat completion requests <paramref name="provider"/> received whose last user message carries
    /// <paramref name="question"/> — the request of that one round, not a later one that has it in its history.
    /// </summary>
    /// <param name="streamedOnly">
    /// Leave out the non-streamed requests: on a thread those are the title generation, which quotes the question too.
    /// </param>
    protected static List<string> RequestsAsking(StubModelProvider provider, string question, bool streamedOnly = true) =>
        provider.CompletionRequestBodies.Where(body => Asks(body, question, streamedOnly)).ToList();

    private static bool Asks(string body, string question, bool streamedOnly)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (streamedOnly && !(root.TryGetProperty("stream", out var stream) && stream.ValueKind == JsonValueKind.True))
            {
                return false;
            }

            if (!root.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var lastUser = messages.EnumerateArray()
                .LastOrDefault(m => m.TryGetProperty("role", out var role) && role.GetString() == "user");

            return lastUser.ValueKind == JsonValueKind.Object && TextOfProviderMessage(lastUser).Contains(question, StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string TextOfProviderMessage(JsonElement message)
    {
        if (!message.TryGetProperty("content", out var content))
        {
            return string.Empty;
        }

        return content.ValueKind switch
        {
            JsonValueKind.String => content.GetString() ?? string.Empty,
            JsonValueKind.Array => string.Concat(content.EnumerateArray()
                .Select(part => part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String ? text.GetString() : null)),
            _ => string.Empty
        };
    }

    protected static AiSendStreamBody BuildStreamBody(
        string text,
        string? threadId = null,
        string? profileId = null,
        AiActionArgs? actionArgs = null,
        string? entityId = null) =>
        // The generated constructor is not annotated for nullability; null leaves the field out of the body.
        new(threadId: threadId!, userMessage: BuildUserMessage(text), actionArgs: actionArgs!, entityId: entityId!, profileId: profileId!);

    /// <summary>
    /// Sends one round through <c>ai/send-with-stream-openai</c> and reads the whole SSE stream.
    /// </summary>
    /// <remarks>
    /// Raw on purpose — an SDK defect: the response is a server-sent-events stream of <c>chat.completion.chunk</c>
    /// objects ended by <c>data: [DONE]</c>, but the SDK declares one <see cref="AiOpenAIStreamChunk"/> as the JSON
    /// body, so <c>AiSendWithStreamOpenAIAsync</c> cannot read it. The request body is the SDK model.
    /// </remarks>
    protected async Task<OpenAiStream> SendWithStreamOpenAiAsync(AiSendStreamBody body, CancellationToken? cancellationToken = null)
    {
        var token = cancellationToken ?? TestContext.Current.CancellationToken;

        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(SendWithStreamOpenAiPath, body.ToJson(), token);

        return OpenAiStream.Parse(response.StatusCode, await response.Content.ReadAsStringAsync(token));
    }

    /// <summary>Regenerates the last reply of a thread through <c>ai/regenerate-stream</c>.</summary>
    /// <remarks>
    /// Raw on purpose — the same SDK defect as <c>send-with-stream</c>: an ndjson stream declared as one
    /// <see cref="AiChatEvent"/>. The request body is the SDK model.
    /// </remarks>
    protected async Task<ChatRound> RegenerateStreamAsync(string threadId, string? profileId = null, AiActionArgs? actionArgs = null)
    {
        var body = new AiRegenerateStreamRequest(threadId: threadId, actionArgs: actionArgs!, profileId: profileId!);

        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            RegenerateStreamPath,
            body.ToJson(),
            TestContext.Current.CancellationToken);

        return ChatRound.Parse(response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Reads a thread until <paramref name="until"/> holds or the deadline passes, and returns the last read either way —
    /// the reply of a round is written while the stream closes, so a bare read can come too early.
    /// </summary>
    protected async Task<List<AiThreadMessageLike>> WaitForMessagesAsync(
        string threadId,
        Func<List<AiThreadMessageLike>, bool> until,
        int timeoutSeconds = 20)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

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

    protected static List<AiThreadMessageLike> AssistantMessages(IEnumerable<AiThreadMessageLike> messages) =>
        messages.Where(m => m.Role == AiThreadMessageLike.RoleEnum.Assistant).ToList();

    protected static List<AiThreadMessageLike> UserMessages(IEnumerable<AiThreadMessageLike> messages) =>
        messages.Where(m => m.Role == AiThreadMessageLike.RoleEnum.User).ToList();

    /// <summary>A thread's messages in one line each, for an assertion message.</summary>
    protected static string Describe(IEnumerable<AiThreadMessageLike> messages) =>
        string.Join("\n", messages.Select(m => $"  {m.Role} {m.Id} [{m.Status?.Type}] {TextOf(m)}"));

    /// <summary>The text of an ndjson frame's <c>message</c> (or <c>responseMessage</c>): a string, or its text parts joined.</summary>
    /// <remarks>The same reading as <see cref="ChatRound.AssistantText"/>, whose frame reader is private.</remarks>
    protected static string? FrameText(JsonElement frame, string property = "message")
    {
        if (!frame.TryGetProperty(property, out var message) || !message.TryGetProperty("content", out var content))
        {
            return null;
        }

        return content.ValueKind switch
        {
            JsonValueKind.String => content.GetString(),
            JsonValueKind.Array => string.Concat(content.EnumerateArray()
                .Where(b => b.TryGetProperty("type", out var t) && t.GetString() == "text")
                .Select(b => b.TryGetProperty("text", out var x) ? x.GetString() : null)),
            _ => null
        };
    }

    /// <summary>
    /// Stores <paramref name="payload"/> as a user message and reads it back everywhere a client reads it from — by id,
    /// from the history, and again after an edit. <paramref name="fragments"/> are checked before the byte-identity
    /// assertion: they name the construct that broke, which a whole-block string diff does not.
    /// </summary>
    protected async Task ExpectStoredVerbatimAsync(string payload, IEnumerable<string> fragments)
    {
        var threadId = await CreateMessageThreadAsync();
        var messageId = await AppendAsync(threadId, payload);

        var stored = TextOf(await GetMessageAsync(messageId));

        foreach (var fragment in fragments)
        {
            stored.Should().Contain(fragment, $"get-message-by-id did not return {JsonSerializer.Serialize(fragment)} as it was sent");
        }

        stored.Should().Be(payload, "get-message-by-id changed the text");

        var history = await ReadMessagesAsync(threadId);
        history.Should().ContainSingle();
        TextOf(history[0]).Should().Be(payload, "read-messages changed the text");

        // The edit path is a second write, and it is the one a "normalise before saving" step would plausibly live on.
        var edited = $"{payload}\n\nEDITEDTAIL";
        var update = await _aiThreads.AiThreadsUpdateMessageAsync(
            new AiThreadsUpdateMessageRequest(messageId: messageId, message: BuildUserMessage(edited)),
            TestContext.Current.CancellationToken);
        update.Success.Should().BeTrue();

        TextOf(await GetMessageAsync(messageId)).Should().Be(edited, "update-message changed the text");
    }

    /// <summary>
    /// The SSE body of <c>send-with-stream-openai</c>: the <c>data:</c> lines, the JSON frames among them, and the
    /// OpenAI chunk fields a client reads.
    /// </summary>
    protected sealed record OpenAiStream(HttpStatusCode StatusCode, string Raw, IReadOnlyList<string> DataLines, IReadOnlyList<JsonElement> Frames)
    {
        public static OpenAiStream Parse(HttpStatusCode statusCode, string raw)
        {
            var dataLines = raw.Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("data:", StringComparison.Ordinal))
                .Select(line => line["data:".Length..].Trim())
                .ToList();

            var frames = new List<JsonElement>();

            foreach (var line in dataLines.Where(l => l != "[DONE]"))
            {
                try
                {
                    frames.Add(JsonDocument.Parse(line).RootElement.Clone());
                }
                catch (JsonException)
                {
                    // A partial line of a cut stream: not a frame.
                }
            }

            return new OpenAiStream(statusCode, raw, dataLines, frames);
        }

        /// <summary>The stream closed cleanly: its last data line is the <c>[DONE]</c> sentinel.</summary>
        public bool EndsWithDone => DataLines.Count > 0 && DataLines[^1] == "[DONE]";

        public IReadOnlyList<JsonElement> Chunks =>
            Frames.Where(f => f.TryGetProperty("object", out var o) && o.GetString() == "chat.completion.chunk").ToList();

        /// <summary>The OpenAI error envelopes (<c>{"error": {...}}</c>, no <c>choices</c>).</summary>
        public IReadOnlyList<JsonElement> ErrorFrames => Frames.Where(f => f.TryGetProperty("error", out _)).ToList();

        private IEnumerable<JsonElement> Choices => Chunks.SelectMany(c =>
            c.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array ? choices.EnumerateArray() : Enumerable.Empty<JsonElement>());

        public IReadOnlyList<string?> FinishReasons => Choices
            .Select(c => c.TryGetProperty("finish_reason", out var f) && f.ValueKind == JsonValueKind.String ? f.GetString() : null)
            .ToList();

        /// <summary>The text assembled from the <c>delta.content</c> of every chunk.</summary>
        public string Text => string.Concat(Choices.Select(c =>
            c.TryGetProperty("delta", out var d) && d.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String
                ? content.GetString()
                : null));

        /// <summary>Every entry of the <c>delta.tool_calls</c> arrays.</summary>
        public IReadOnlyList<JsonElement> ToolCalls => Choices
            .SelectMany(c => c.TryGetProperty("delta", out var d) && d.TryGetProperty("tool_calls", out var calls) && calls.ValueKind == JsonValueKind.Array
                ? calls.EnumerateArray()
                : Enumerable.Empty<JsonElement>())
            .ToList();

        public override string ToString() => $"{(int)StatusCode}: {Raw}";
    }
}
