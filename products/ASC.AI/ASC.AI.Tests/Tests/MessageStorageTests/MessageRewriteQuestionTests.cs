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
/// Editing a question is not a re-ask. There is no edit of a sent message in the composer, but
/// <c>update-message</c> rewrites any stored message in place; what is testable is the consequence: an edit changes
/// the transcript and nothing else, and a regenerate is what produces a new answer.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Messages")]
public class MessageRewriteQuestionTests(AspireAppFixture fixture) : MessagePart1TestBase(fixture)
{
    private const string FirstQuestion = "Reply with the single word ONE.";
    private const string EditedQuestion = "Reply with the single word TWO.";

    [Fact]
    public async Task UpdateMessage_RewritingTheQuestion_LeavesTheAnswerStanding()
    {
        var (profile, threadId) = await CreateScriptedThreadAsync();
        var before = await SendAnsweredAsync(threadId, FirstQuestion, "ONE", profile.Id);
        var question = Questions(before).Single();
        var reply = Replies(before).Single();
        var requestsBefore = StreamedRequests(Provider);

        var edited = await _aiThreads.AiThreadsUpdateMessageAsync(
            new AiThreadsUpdateMessageRequest(messageId: question.Id, message: BuildUserMessage(EditedQuestion)),
            TestContext.Current.CancellationToken);
        edited.Success.Should().BeTrue();

        // Long enough that an answer triggered by the edit would have arrived.
        var afterEdit = await WaitForMessagesAsync(threadId, m => Replies(m).Count != 1 || Replies(m)[0].Id != reply.Id, SettleTime);

        TextOf(Questions(afterEdit).Single()).Should().Be(EditedQuestion, "the transcript now shows the edited question");
        var replyAfterEdit = Replies(afterEdit).Should().ContainSingle("the edit did not produce a second answer").Subject;
        replyAfterEdit.Id.Should().Be(reply.Id, "nor did it replace the first one");
        TextOf(replyAfterEdit).Should().Be("ONE");
        StreamedRequests(Provider).Should().Be(requestsBefore, $"the edit did not ask the model\n{Provider.Describe()}");

        // The positive control: the thread is perfectly able to produce a new answer, the edit just is not what
        // asks for one. Neutral about replace-vs-append after an edit, which is not this test's subject.
        Provider.Enqueue(StubModelProvider.Text("TWO"));
        var regenerated = await RegenerateAsync(threadId, profile.Id);
        regenerated.StatusCode.Should().Be(HttpStatusCode.OK, regenerated.ToString());
        regenerated.StreamError.Should().BeNull(regenerated.ToString());

        var afterRegenerate = await WaitForMessagesAsync(threadId, m => Replies(m).Any(r => r.Id != reply.Id && TextOf(r) == "TWO"));
        var newReply = Replies(afterRegenerate).Where(r => r.Id != reply.Id).Should()
            .ContainSingle("the regenerate produced the answer the edit did not").Subject;
        TextOf(newReply).Should().Be("TWO");

        // The regenerate asked the model the edited question.
        LastStreamedRequest(Provider).Should().Contain(EditedQuestion);
    }

    /// <summary>
    /// <c>update-message</c> kept the message id but stamped <c>createdAt</c> with the time of the edit, and
    /// <c>read-messages</c> is ordered by <c>createdAt</c>, so editing the question of an answered turn moved it
    /// after its own answer. Fixed; kept as a regression guard, because the in-place rewrite test reads the message
    /// back by id and never looks at the order.
    /// </summary>
    [Fact]
    [Trait("Bug", "83037")]
    public async Task UpdateMessage_EditedQuestion_StaysBeforeItsAnswer()
    {
        var (profile, threadId) = await CreateScriptedThreadAsync();
        var before = await SendAnsweredAsync(threadId, FirstQuestion, "ONE", profile.Id);

        // The premise: before the edit the transcript is in the only order that makes sense.
        before.Select(m => m.Role).Should().Equal(
            [AiThreadMessageLike.RoleEnum.User, AiThreadMessageLike.RoleEnum.Assistant],
            "the question comes before its answer to begin with");
        var question = Questions(before).Single();

        var edited = await _aiThreads.AiThreadsUpdateMessageAsync(
            new AiThreadsUpdateMessageRequest(messageId: question.Id, message: BuildUserMessage(EditedQuestion)),
            TestContext.Current.CancellationToken);
        edited.Success.Should().BeTrue();

        var afterEdit = await ReadMessagesAsync(threadId);

        // The edit landed — otherwise an unchanged order below would prove nothing.
        var questionAfter = Questions(afterEdit).Single();
        TextOf(questionAfter).Should().Be(EditedQuestion, "the edit was stored");
        questionAfter.Id.Should().Be(question.Id, "and it is the same message, not a new one");

        afterEdit.Select(m => m.Role).Should().Equal(
            [AiThreadMessageLike.RoleEnum.User, AiThreadMessageLike.RoleEnum.Assistant],
            "an edit must not move the question after the answer it produced");
    }
}
