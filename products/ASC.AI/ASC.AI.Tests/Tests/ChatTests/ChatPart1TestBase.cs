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
/// Shared helpers of the chat suites ported from <c>ai/chat/chat.spec.ts</c> (threads, send, stream, open-or-create,
/// titles). The TypeScript suite ran every case inside an AI agent; the agent routes are gone and threads are one list
/// per user whatever entity the chat is opened in (by design, Bug 82719), so these tests chat with no entity at all.
/// </summary>
/// <remarks>
/// A test that scripts what the model answers, or asserts what the model was sent, uses <see cref="Provider"/> — a
/// stand-in model of its own — through <see cref="GetOwnProfileAsync"/>; answers are queued only after that profile
/// exists, because the profile create sends a streamed tool probe that would take the first one.
/// </remarks>
public abstract class ChatPart1TestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    /// <summary>What the chat service names every thread a first message opens, until the generated title lands.</summary>
    protected const string DefaultThreadTitle = "New chat";

    protected const string PublicThreadsPath = "/api/2.0/ai/threads";
    protected const string SendWithStreamPath = "/api/2.0/ai/ai/send-with-stream";

    private StubModelProvider? _provider;
    private AiProfile? _ownProfile;

    /// <summary>The test's own stand-in model, started on first use.</summary>
    protected StubModelProvider Provider => _provider ??= new StubModelProvider();

    public override async ValueTask DisposeAsync()
    {
        _provider?.Dispose();
        await base.DisposeAsync();
    }

    /// <summary>
    /// A profile on <see cref="Provider"/>, created once per test as whoever <see cref="BaseTest._aiChatClient"/> is
    /// signed in as — call it as the owner. The first profile of a portal becomes its default.
    /// </summary>
    protected async Task<AiProfile> GetOwnProfileAsync() => _ownProfile ??= await CreateProfileOnAsync(Provider);

    protected Task<AiThread> GetThreadAsync(string threadId) =>
        _aiThreads.AiThreadsGetByIdAsync(threadId, TestContext.Current.CancellationToken);

    protected Task<List<AiThread>> ListThreadsAsync(int? count = null, string? cursor = null, string? query = null) =>
        _aiThreads.AiThreadsListAsync(count: count, cursor: cursor, query: query, cancellationToken: TestContext.Current.CancellationToken);

    protected async Task<List<string>> ListThreadIdsAsync() => (await ListThreadsAsync()).Select(t => t.ThreadId).ToList();

    protected Task<List<AiThreadMessageLike>> ReadMessagesAsync(string threadId) =>
        _aiThreads.AiThreadsReadMessagesAsync(threadId, cancellationToken: TestContext.Current.CancellationToken);

    protected static List<AiThreadMessageLike> UserMessages(IEnumerable<AiThreadMessageLike> messages) =>
        messages.Where(m => m.Role == AiThreadMessageLike.RoleEnum.User).ToList();

    protected static List<AiThreadMessageLike> AssistantMessages(IEnumerable<AiThreadMessageLike> messages) =>
        messages.Where(m => m.Role == AiThreadMessageLike.RoleEnum.Assistant).ToList();

    /// <summary>
    /// Reads the thread until it holds <paramref name="count"/> assistant replies or the deadline passes, and returns
    /// what it last saw — the reply is stored by the chat round, so a bare read right after the send can race it.
    /// </summary>
    protected async Task<List<AiThreadMessageLike>> WaitForAssistantRepliesAsync(string threadId, int count = 1)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var messages = await ReadMessagesAsync(threadId);

            if (AssistantMessages(messages).Count >= count || DateTime.UtcNow >= deadline)
            {
                return messages;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// The replies stored on a thread are exactly the scripted ones and none of them ended as a failure — a refused
    /// inference is still stored as an assistant message, so "an assistant message exists" proves nothing.
    /// </summary>
    protected void AssertHealthyReplies(IEnumerable<AiThreadMessageLike> messages, params string[] expectedReplies)
    {
        var replies = AssistantMessages(messages);
        var context = $"{string.Join(" | ", replies.Select(r => $"{r.Status?.Type}: {TextOf(r)}"))}\n{Provider.Describe()}";

        replies.Select(TextOf).Should().Equal(expectedReplies, context);
        replies.Where(r => r.Status?.Type == "incomplete").Should().BeEmpty(context);
    }

    /// <summary>The bodies of the streamed chat requests <see cref="Provider"/> received that carry <paramref name="text"/>.</summary>
    /// <remarks>A title request is not streamed and the profile's tool probe carries no user text, so neither is here.</remarks>
    protected List<string> ChatRequestsCarrying(string text) =>
        Provider.CompletionRequestBodies.Where(b => StubModelProvider.IsStreamed(b) && b.Contains(text, StringComparison.Ordinal)).ToList();

    /// <summary>
    /// Reads the thread until its title satisfies <paramref name="until"/> or the deadline passes; returns the last
    /// title seen. The generated title is written after the round that opened the thread, not before it returns.
    /// </summary>
    protected async Task<string?> WaitForTitleAsync(string threadId, Func<string?, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var title = (await GetThreadAsync(threadId)).Title;

            if (until(title) || DateTime.UtcNow >= deadline)
            {
                return title;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// A streamed text reply sent in as many content chunks as there are <paramref name="pieces"/> —
    /// <see cref="StubModelProvider.Text"/> always splits in two, too few to tell a stream from one frame at the end.
    /// </summary>
    protected static ScriptedAnswer TextInPieces(params string[] pieces)
    {
        var chunks = new List<object> { StubModelProvider.Chunk(new { role = "assistant", content = "" }) };
        chunks.AddRange(pieces.Select(p => StubModelProvider.Chunk(new { content = p })));
        chunks.Add(StubModelProvider.Chunk(new { }, "stop"));

        return new ScriptedAnswer(200, StubModelProvider.Sse([.. chunks]));
    }

    protected static string? FrameType(JsonElement frame) => FrameString(frame, "type");

    protected static string? FrameString(JsonElement frame, string name) =>
        frame.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>The text of the message a frame carries, "" when it carries none.</summary>
    protected static string FrameText(JsonElement frame) => ChatRound.TextOfFrame(frame) ?? "";

    /// <summary>The id of the message a frame carries in <c>message.id</c>, or null.</summary>
    protected static string? FrameMessageId(JsonElement frame) =>
        frame.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.Object ? FrameString(message, "id") : null;
}
