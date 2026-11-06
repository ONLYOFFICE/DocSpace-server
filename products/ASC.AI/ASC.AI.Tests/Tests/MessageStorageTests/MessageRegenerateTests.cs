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
/// <c>ai/regenerate-stream</c>: the button under an assistant reply drops that reply and asks the model the same
/// question again. The route takes a thread and no message, so the only reply it can regenerate is the last one.
/// The model's answers are scripted, so "a new reply" is told apart from "the old reply" by id and by text.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Messages")]
public class MessageRegenerateTests(AspireAppFixture fixture) : MessagePart1TestBase(fixture)
{
    private const string Question = "Reply with the single word OK.";

    [Fact]
    public async Task RegenerateStream_ReplacesTheLastReply_AndKeepsTheQuestion()
    {
        var (profile, threadId) = await CreateScriptedThreadAsync();
        var before = await SendAnsweredAsync(threadId, Question, "first answer", profile.Id);
        var question = Questions(before).Single();
        var firstReply = Replies(before).Single();

        Provider.Enqueue(StubModelProvider.Text("second answer"));
        var round = await RegenerateAsync(threadId, profile.Id);

        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());

        // The regenerate opens a fresh streaming lifecycle of its own.
        round.OfType("message-start").Should().NotBeEmpty(round.ToString());
        round.OfType("message-end").Should().NotBeEmpty(round.ToString());
        var streamedId = StartedMessageId(round);
        streamedId.Should().NotBeNullOrEmpty("the stream reports the new message id");
        streamedId.Should().NotBe(firstReply.Id);

        // The stored replacement lands after the response body is complete, so it is polled for.
        var after = await WaitForReplacedReplyAsync(threadId, firstReply.Id);

        // Replaced, not appended to: exactly one reply, with a new id and the new answer.
        var reply = Replies(after).Should().ContainSingle().Subject;
        reply.Id.Should().Be(streamedId);
        TextOf(reply).Should().Be("second answer");

        // The question survives untouched.
        var questionAfter = Questions(after).Should().ContainSingle().Subject;
        questionAfter.Id.Should().Be(question.Id);
        TextOf(questionAfter).Should().Be(Question);

        // And the thread keeps its profile.
        var thread = await _aiThreads.AiThreadsGetByIdAsync(threadId, TestContext.Current.CancellationToken);
        thread.ProfileId.Should().Be(profile.Id);
    }

    [Fact]
    public async Task RegenerateStream_EmptyThread_GeneratesNothing()
    {
        var (profile, threadId) = await CreateScriptedThreadAsync();
        var requestsBefore = StreamedRequests(Provider);

        var round = await RegenerateAsync(threadId, profile.Id);

        // Whatever it answers, an empty thread must not gain a reply out of nowhere.
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        var messages = await WaitForMessagesAsync(threadId, m => m.Count > 0, SettleTime);
        messages.Should().BeEmpty();
        StreamedRequests(Provider).Should().Be(requestsBefore, "there was no question to ask the model");
    }

    // Used to arrive in the shape of BUG 82723 on send-with-stream — HTTP 200 with an error frame in the body. A
    // thread that does not exist is now refused before any stream is opened.
    [Fact]
    public async Task RegenerateStream_UnknownThread_Returns404BeforeAnyStream()
    {
        var profile = await GetDefaultProfileAsync();

        var round = await RegenerateAsync(Guid.NewGuid().ToString(), profile.Id);

        round.StatusCode.Should().Be(HttpStatusCode.NotFound, round.ToString());
        round.StreamError.Should().BeNull("the refusal is the status, not a stream frame");
    }

    // The earlier turns have to survive verbatim (a client that shows the button under every reply would be
    // promising something the API cannot do), and the history has to stay a question/answer alternation.
    [Fact]
    public async Task RegenerateStream_MultiTurnThread_LosesOnlyItsLastReply()
    {
        var (profile, threadId) = await CreateScriptedThreadAsync();
        await SendAnsweredAsync(threadId, "Reply with the single word ONE.", "ONE", profile.Id);
        var before = await SendAnsweredAsync(threadId, "Reply with the single word TWO.", "TWO", profile.Id);

        var questionsBefore = Questions(before);
        questionsBefore.Should().HaveCount(2, "both questions are stored");
        var replies = Replies(before);
        replies.Should().HaveCount(2);
        var (firstReply, lastReply) = (replies[0], replies[1]);

        Provider.Enqueue(StubModelProvider.Text("TWO again"));
        var round = await RegenerateAsync(threadId, profile.Id);
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());

        var after = await WaitForReplacedReplyAsync(threadId, lastReply.Id, expectedReplies: 2);

        // The first answer is the same object, not a re-run that happened to say the same thing.
        var repliesAfter = Replies(after);
        repliesAfter.Should().HaveCount(2);
        repliesAfter[0].Id.Should().Be(firstReply.Id, "the earlier reply is untouched");
        TextOf(repliesAfter[0]).Should().Be("ONE");
        repliesAfter[1].Id.Should().NotBe(lastReply.Id, "only the last reply was replaced");
        TextOf(repliesAfter[1]).Should().Be("TWO again");

        // Both questions survive, in order, and the transcript still alternates.
        Questions(after).Select(m => m.Id).Should().Equal(questionsBefore.Select(m => m.Id));
        after.Select(m => m.Role).Should().Equal(
            AiThreadMessageLike.RoleEnum.User,
            AiThreadMessageLike.RoleEnum.Assistant,
            AiThreadMessageLike.RoleEnum.User,
            AiThreadMessageLike.RoleEnum.Assistant);

        // The regenerated round asked the model with the earlier turn as history.
        LastStreamedRequest(Provider).Should().Contain("Reply with the single word ONE.").And.Contain("Reply with the single word TWO.");
    }

    // The composer cannot produce this state — the button hangs off an assistant reply — but the API can be asked
    // anyway, and either answer is defensible: answer the dangling question, or do nothing. Losing or duplicating
    // the question is not, so that is what is asserted; the reply count is deliberately left open.
    [Fact]
    public async Task RegenerateStream_LastMessageIsAnUnansweredQuestion_KeepsTheQuestion()
    {
        var (profile, threadId) = await CreateScriptedThreadAsync();
        var questionId = await AppendAsync(threadId, Question);

        Provider.Enqueue(StubModelProvider.Text("late answer"));
        var round = await RegenerateAsync(threadId, profile.Id);
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());

        var messages = await WaitForMessagesAsync(threadId, m => Replies(m).Any(r => TextOf(r) == "late answer"), SettleTime);

        var question = Questions(messages).Should().ContainSingle("the question is neither lost nor duplicated").Subject;
        question.Id.Should().Be(questionId);
        TextOf(question).Should().Be(Question);

        var replies = Replies(messages);
        replies.Should().HaveCountLessThanOrEqualTo(1, "one regenerate produces at most one reply");
        if (replies.Count == 1)
        {
            TextOf(replies[0]).Should().Be("late answer");
        }
    }
}
