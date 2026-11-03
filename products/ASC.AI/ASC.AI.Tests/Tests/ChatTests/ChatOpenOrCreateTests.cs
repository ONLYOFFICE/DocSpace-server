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
/// <c>POST threads/open-or-create</c>: opening an existing thread replays its stored history whole — and does not store
/// the first message, start a second thread or retag the thread's model; without a threadId it starts a thread from
/// the first message.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Threads")]
public class ChatOpenOrCreateTests(AspireAppFixture fixture) : ChatPart1TestBase(fixture)
{
    private const string OpenOrCreatePath = "/api/2.0/ai/threads/open-or-create";

    [Fact]
    public async Task OpenOrCreate_ExistingEmptyThread_IsReusedAndNothingIsStored()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        var thread = await CreateThreadAsync("Existing thread", profile.Id);

        // Act
        var opened = await OpenAsync(thread.ThreadId, profile);

        // Assert
        opened.ThreadId.Should().Be(thread.ThreadId, "the existing thread is reused");
        opened.PriorMessages.Should().NotBeNull().And.BeEmpty();

        // Opening does not send the message, and no second thread appeared.
        (await ReadMessagesAsync(thread.ThreadId)).Should().BeEmpty();
        (await ListThreadIdsAsync()).Should().Equal(thread.ThreadId);
    }

    [Fact]
    public async Task OpenOrCreate_ThreadWithAConversation_ReplaysItWhole()
    {
        // Arrange — one real turn, so the history carries both roles, plus an unanswered question, so the transcript
        // is not a sequence of complete pairs a replay could reconstruct by shape.
        var profile = await GetOwnProfileAsync();
        var thread = await CreateThreadAsync("Autotest thread with a history", profile.Id);
        Provider.Enqueue(StubModelProvider.Text("ORANGE"));

        var sent = await SendWithStreamAsync(thread.ThreadId, "Reply with the single word ORANGE and nothing else.", profile.Id);
        sent.StreamError.Should().BeNull(sent.ToString());
        AssertHealthyReplies(await WaitForAssistantRepliesAsync(thread.ThreadId), "ORANGE");

        await AppendUserMessageAsync(thread.ThreadId, "Autotest unanswered question");

        var stored = await ReadMessagesAsync(thread.ThreadId);
        stored.Should().HaveCount(3);

        // Act
        var opened = await OpenAsync(thread.ThreadId, profile);

        // Assert
        opened.ThreadId.Should().Be(thread.ThreadId, "the existing thread is reused");

        // What comes back is the stored transcript itself: same messages, same order, same ids.
        var replayed = opened.PriorMessages;
        replayed.Select(m => m.Id).Should().Equal(stored.Select(m => m.Id));
        replayed.Select(m => m.Role).Should().Equal(
            AiThreadMessageLike.RoleEnum.User, AiThreadMessageLike.RoleEnum.Assistant, AiThreadMessageLike.RoleEnum.User);
        replayed.Select(TextOf).Should().Equal(stored.Select(TextOf));
        TextOf(replayed[1]).Should().Contain("ORANGE", "the assistant's answer is part of the replay");

        // Opening a thread that already exists still does not store its firstMessage, nor start a second thread.
        (await ReadMessagesAsync(thread.ThreadId)).Select(m => m.Id).Should().Equal(stored.Select(m => m.Id));
        (await ListThreadIdsAsync()).Should().Equal(thread.ThreadId);
    }

    [Fact]
    public async Task OpenOrCreate_ProfileIdWithoutTheProfileObject_Returns404()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        var thread = await CreateThreadAsync("Existing thread", profile.Id);

        var body = System.Text.Json.Nodes.JsonNode.Parse(
            new AiThreadsOpenOrCreateRequest(threadId: thread.ThreadId, profile: profile, profileId: profile.Id, firstMessage: BuildUserMessage("hello")).ToJson())!
            .AsObject();
        body.Remove("profile");

        // Act
        // Raw on purpose: profile is a required constructor parameter of AiThreadsOpenOrCreateRequest, so the SDK
        // never sends a body with the profileId alone.
        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(OpenOrCreatePath, body.ToJsonString(), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, content);
        content.Should().Contain("an AI profile is required to open or create a thread");

        // The whole profile object is what the route binds on.
        var opened = await OpenAsync(thread.ThreadId, profile);
        opened.ThreadId.Should().Be(thread.ThreadId);
        opened.PriorMessages.Should().BeEmpty();
        (await ReadMessagesAsync(thread.ThreadId)).Should().BeEmpty("opening still does not store the first message");
    }

    /// <summary>
    /// The open call carries a whole profile object, so "the thread's model comes back when it is opened" holds only if
    /// that object is the client's default pick and not a write — otherwise looking at a thread would retag it.
    /// </summary>
    [Fact]
    public async Task OpenOrCreate_WithAnotherProfile_LeavesTheThreadsModelAlone()
    {
        // Arrange
        var first = await GetOwnProfileAsync();
        var second = await CreateProfileOnAsync(Provider);
        var thread = await CreateThreadAsync("Autotest thread on the first model", first.Id);
        Provider.Enqueue(StubModelProvider.Text("OK"));

        // Act
        var opened = await OpenAsync(thread.ThreadId, second);

        // Assert
        opened.ThreadId.Should().Be(thread.ThreadId, "the existing thread is reused");
        (await GetThreadAsync(thread.ThreadId)).ProfileId.Should().Be(first.Id, "the thread keeps the model it was created with");
        (await ListThreadsAsync()).Single(t => t.ThreadId == thread.ThreadId).ProfileId
            .Should().Be(first.Id, "and the list the picker is drawn from was not retagged either");

        // Positive control: a send carrying the other profile — the documented way of moving it — does move it.
        var sent = await SendWithStreamAsync(thread.ThreadId, "Reply with the single word OK.", second.Id);
        sent.StreamError.Should().BeNull(sent.ToString());
        await WaitForAssistantRepliesAsync(thread.ThreadId);

        (await GetThreadAsync(thread.ThreadId)).ProfileId.Should().Be(second.Id, "a send does move it");
    }

    /// <summary>
    /// Nothing here pages the replay, so a server-side cap would not truncate the history into a first page the client
    /// can ask past — it would drop the rest for good. Built with append-user-message: forty stored messages and no
    /// inference, so the length under test is the store's.
    /// </summary>
    [Fact]
    public async Task OpenOrCreate_LongHistory_IsReplayedWholeAndInOrder()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        var thread = await CreateThreadAsync("Autotest thread with a long history", profile.Id);

        var texts = Enumerable.Range(1, 40).Select(i => $"Autotest history message {i}").ToList();
        foreach (var text in texts)
        {
            await AppendUserMessageAsync(thread.ThreadId, text);
        }

        var stored = await ReadMessagesAsync(thread.ThreadId);
        stored.Select(TextOf).Should().Equal(texts, "read-messages returns the whole thread, in order");

        // Act
        var opened = await OpenAsync(thread.ThreadId, profile);

        // Assert — compared as whole lists: a cap keeping the newest and one keeping the oldest both have to fail.
        opened.ThreadId.Should().Be(thread.ThreadId);
        opened.PriorMessages.Select(TextOf).Should().Equal(texts, "opening replays the whole thread, in order");
        opened.PriorMessages.Select(m => m.Id).Should().Equal(stored.Select(m => m.Id));
    }

    /// <summary>
    /// The "no thread yet, start one from this message" call: the same body that opens an existing thread, minus the
    /// threadId. It used to answer 500 and leave nothing behind.
    /// </summary>
    [Fact]
    [Trait("Bug", "82826")]
    public async Task OpenOrCreate_WithoutThreadId_CreatesAThreadFromTheFirstMessage()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();

        // Act
        var created = await _aiThreads.AiThreadsOpenOrCreateAsync(
            new AiThreadsOpenOrCreateRequest(
                profile: profile,
                profileId: profile.Id,
                firstMessage: BuildUserMessage("Reply with the single word OK.")),
            TestContext.Current.CancellationToken);

        // Assert
        created.ThreadId.Should().NotBeNullOrEmpty();
        created.PriorMessages.Should().BeEmpty();
        (await GetThreadAsync(created.ThreadId)).ThreadId.Should().Be(created.ThreadId);
        (await ListThreadIdsAsync()).Should().Equal(created.ThreadId);
    }

    private Task<AiOpenOrCreateResult> OpenAsync(string threadId, AiProfile profile) =>
        _aiThreads.AiThreadsOpenOrCreateAsync(
            new AiThreadsOpenOrCreateRequest(threadId: threadId, profile: profile, profileId: profile.Id, firstMessage: BuildUserMessage("hello")),
            TestContext.Current.CancellationToken);
}
