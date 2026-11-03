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
/// "A thread is started by its first message, and named after it." <c>ai/send-with-stream</c> with no threadId opens
/// the thread itself — announced by a leading <c>thread-title</c> frame under the placeholder name — and the generated
/// title replaces the placeholder after the round. <c>threads/create</c> is the "I typed a name" route: it wants a
/// title, and the thread it makes is kept even with no message in it.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Threads")]
public class ChatFirstMessageTests(AspireAppFixture fixture) : ChatPart1TestBase(fixture)
{
    private const string AutoTitleQuestion = "What is the capital of France? Answer in one word.";

    [Fact]
    public async Task Create_ThreadWithNoMessage_IsKeptAndListed()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();
        var empty = await CreateThreadAsync("Autotest never used", profile.Id);
        (await ReadMessagesAsync(empty.ThreadId)).Should().BeEmpty("the thread really is empty");

        // The used thread is the control: it proves the list is being read.
        var used = await CreateThreadAsync("Autotest used", profile.Id);
        await AppendUserMessageAsync(used.ThreadId, "hello");
        (await ReadMessagesAsync(used.ThreadId)).Should().ContainSingle();

        // Act
        var read = await GetThreadAsync(empty.ThreadId);
        var listed = await ListThreadIdsAsync();

        // Assert
        read.ThreadId.Should().Be(empty.ThreadId);
        read.Title.Should().Be("Autotest never used");
        listed.Should().BeEquivalentTo(new[] { empty.ThreadId, used.ThreadId }, "the unused thread was not pruned when a used one appeared");
    }

    /// <summary>
    /// <c>create</c> never names a thread for the caller: every spelling of "you pick one" is a 400. A missing title is
    /// covered by <c>ThreadCreateTests.Create_MissingTitle_Returns400</c>.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_BlankTitle_Returns400(string title)
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await CreateThreadAsync(title, profile.Id));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Bad Request");
        await AssertOnlyATitledThreadIsListedAsync(profile.Id);
    }

    [Fact]
    public async Task Create_NullTitle_Returns400()
    {
        // Arrange
        var profile = await GetDefaultProfileAsync();

        // Act
        // Raw on purpose: title is a required constructor parameter of AiThreadsCreateRequest, so the SDK never sends
        // an explicit null.
        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            $"{PublicThreadsPath}/create",
            $$"""{ "title": null, "profileId": "{{profile.Id}}" }""",
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, content);
        content.Should().Contain("Bad Request");
        await AssertOnlyATitledThreadIsListedAsync(profile.Id);
    }

    [Fact]
    public async Task SendWithStream_NoThreadId_OpensTheThread()
    {
        // Arrange
        var profile = await GetOwnProfileAsync();
        (await ListThreadsAsync()).Should().BeEmpty("the user starts with no threads");
        Provider.Enqueue(StubModelProvider.Text("Paris"), StubModelProvider.Text("OK"));

        // Act
        var round = await SendWithStreamAsync(null, AutoTitleQuestion, profile.Id);

        // Assert
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());

        // The new thread is announced in the opening frame, before the question is acknowledged — this is where the
        // client picks up the id it did not have.
        var types = round.Frames.Select(FrameType).ToList();
        var where = $"frames were {string.Join(", ", types)}";
        types[0].Should().Be("thread-title", where);
        types.IndexOf("user-message-stored").Should().Be(1, where);

        var threadId = FrameString(round.Frames[0], "threadId");
        threadId.Should().NotBeNullOrEmpty("the opening frame names the new thread");
        FrameString(round.Frames[0], "profileId").Should().Be(profile.Id);

        // A real, listed thread, carrying the question and the answer the model produced.
        (await ListThreadIdsAsync()).Should().Equal(threadId!);

        var messages = await WaitForAssistantRepliesAsync(threadId!);
        UserMessages(messages).Select(TextOf).Should().Equal(new[] { AutoTitleQuestion }, "the question opened the thread and is stored once");
        AssertHealthyReplies(messages, "Paris");

        // An empty threadId is the same call, not a validation error: it opens a second thread.
        var emptyId = await SendWithStreamAsync("", "Say OK.", profile.Id);

        emptyId.StatusCode.Should().Be(HttpStatusCode.OK, emptyId.ToString());
        emptyId.StreamError.Should().BeNull(emptyId.ToString());
        FrameType(emptyId.Frames[0]).Should().Be("thread-title", emptyId.ToString());

        var secondThreadId = FrameString(emptyId.Frames[0], "threadId");
        secondThreadId.Should().NotBeNullOrEmpty().And.NotBe(threadId, "threadId \"\" is not the first thread");
        (await ListThreadIdsAsync()).Should().BeEquivalentTo(new[] { threadId, secondThreadId });
    }

    /// <summary>
    /// The real title is generated and swapped in after the stream — by design, so the first reply is not delayed by
    /// it — hence the opening frame carries the placeholder and the rename is polled for.
    /// </summary>
    [Fact]
    [Trait("Bug", "83081")]
    public async Task SendWithStream_FirstQuestion_RenamesTheThreadFromThePlaceholder()
    {
        // Arrange
        var profile = await GetOwnProfileAsync();
        Provider.Enqueue(StubModelProvider.Text("Paris"), StubModelProvider.Text("Rome"));

        // Act
        var round = await SendWithStreamAsync(null, AutoTitleQuestion, profile.Id);

        // Assert
        round.StreamError.Should().BeNull(round.ToString());
        FrameType(round.Frames[0]).Should().Be("thread-title", round.ToString());
        FrameString(round.Frames[0], "title").Should().Be(DefaultThreadTitle, "the opening frame carries the placeholder");

        var threadId = FrameString(round.Frames[0], "threadId");
        threadId.Should().NotBeNullOrEmpty();
        AssertHealthyReplies(await WaitForAssistantRepliesAsync(threadId!), "Paris");

        var renamed = await WaitForTitleAsync(threadId!, t => t != DefaultThreadTitle);
        renamed.Should().Be(StubModelProvider.DefaultTitle, $"the first question renames the thread away from the placeholder\n{Provider.Describe()}");
        (await ListThreadsAsync()).Single(t => t.ThreadId == threadId).Title.Should().Be(renamed, "get-by-id and list agree");

        // A later turn carries no title frame, and the earlier rename sticks.
        var second = await SendWithStreamAsync(threadId, "And the capital of Italy? One word.", profile.Id);

        second.StreamError.Should().BeNull(second.ToString());
        second.OfType("thread-title").Should().BeEmpty("no title frame on a later turn");
        AssertHealthyReplies(await WaitForAssistantRepliesAsync(threadId!, 2), "Paris", "Rome");
        (await GetThreadAsync(threadId!)).Title.Should().Be(renamed);
    }

    private async Task AssertOnlyATitledThreadIsListedAsync(string profileId)
    {
        // The refusal left nothing behind, and the list is being read: a titled create is the only thread in it.
        var titled = await CreateThreadAsync("Autotest titled", profileId);

        (await ListThreadIdsAsync()).Should().Equal(titled.ThreadId);
    }
}
