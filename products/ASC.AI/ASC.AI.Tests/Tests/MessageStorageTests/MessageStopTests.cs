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
/// Stopping a running generation. There is no stop route: the chat widget's stop aborts the in-flight
/// <c>send-with-stream</c> request, so hanging up on the stream IS the stop gesture, and what the backend does
/// about it is what is tested here.
/// </summary>
/// <remarks>
/// The provider is made to stall mid-answer (<see cref="MessagePart1TestBase.StallStreams"/>): it streams a first
/// fragment, then nothing for <see cref="MessagePart1TestBase.StallTime"/>, then the rest ending in
/// <see cref="MessagePart1TestBase.Sentinel"/>. The client hangs up on the first fragment, so the cut lands
/// mid-generation by construction, and a reply that ever shows the sentinel ran to its end after the stop.
/// What a stop leaves behind is the reply marked incomplete/cancelled, keeping what had been generated so far —
/// the developers' answer on BUG 84028, the same as Stop in the composer.
/// </remarks>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Messages")]
[Trait("Bug", "84028")]
public class MessageStopTests(AspireAppFixture fixture) : MessagePart1TestBase(fixture)
{
    private const string LongQuestion = "Write a detailed essay about the history of typography.";

    [Fact]
    public async Task SendWithStream_HangUpMidStream_CancelsTheGeneration()
    {
        var (profile, threadId) = await CreateScriptedThreadAsync();
        StallStreams(Provider);

        var sentAt = Stopwatch.StartNew();
        var cut = await SendAndHangUpOnFirstDeltaAsync(threadId, LongQuestion, profile.Id);

        cut.CutMidReply.Should().BeTrue($"the connection was cut right after real content started streaming — {cut}");
        cut.StreamedText.Should().NotContain(Sentinel, "the cut landed mid-generation");

        var abandoned = await WaitForCancelledReplyAsync(threadId);

        // Watched past the moment the provider would have finished the answer, so "not finished" cannot be "not
        // finished yet".
        var remaining = StallTime + TimeSpan.FromSeconds(5) - sentAt.Elapsed;
        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining, TestContext.Current.CancellationToken);
        }

        var settled = await ReadMessagesRawAsync(threadId);
        var reply = settled.Where(m => m.IsAssistant).Should().ContainSingle($"one reply for the stopped turn: {string.Join("; ", settled)}").Subject;
        reply.Id.Should().Be(abandoned.Id);
        reply.Text.Should().NotContain(Sentinel, "a generation the user stopped must not run to its end");
        reply.Text.Should().Be(abandoned.Text, "the stopped reply no longer grows");
        reply.StatusReason.Should().Be("cancelled");

        // The question is kept, so the turn can be retried.
        settled.Where(m => m.IsUser).Should().ContainSingle();
    }

    [Fact]
    public async Task SendWithStream_AfterHangUp_TheThreadAnswersAgain()
    {
        var (profile, threadId) = await CreateScriptedThreadAsync();
        var stall = StallStreams(Provider);

        var cut = await SendAndHangUpOnFirstDeltaAsync(threadId, LongQuestion, profile.Id);
        cut.CutMidReply.Should().BeTrue($"the connection was cut — {cut}");

        // The cancellation is allowed to settle before the next turn: sending into a thread the backend is still
        // writing to is a different test.
        var abandoned = await WaitForCancelledReplyAsync(threadId);

        Provider.Server.DeleteMapping(stall);
        Provider.Enqueue(StubModelProvider.Text("OK"));

        var resumed = await SendWithStreamAsync(threadId, "Reply with the single word OK.", profile.Id);
        resumed.StatusCode.Should().Be(HttpStatusCode.OK, resumed.ToString());
        resumed.StreamError.Should().BeNull(resumed.ToString());
        resumed.OfType("message-end").Should().NotBeEmpty(resumed.ToString());

        var messages = await WaitForRawMessagesAsync(threadId, m => m.Count(x => x.IsAssistant && x.Text == "OK") == 1);
        var context = string.Join("; ", messages);

        messages.Where(m => m.IsUser).Should().HaveCount(2, context);
        var replies = messages.Where(m => m.IsAssistant).ToList();
        replies.Should().HaveCount(2, context);

        // The new answer is its own message, healthy, and did not overwrite or continue the abandoned one.
        replies[1].Id.Should().NotBe(abandoned.Id, context);
        replies[1].Text.Should().Be("OK", context);
        replies[1].StatusError.Should().BeNull(context);

        // The cancelled turn is left as it was: still its own message, still marked cancelled. Comparing the text
        // alone would miss a build that reused the abandoned message for the new answer.
        replies[0].Id.Should().Be(abandoned.Id, "the abandoned reply is still there");
        replies[0].StatusReason.Should().Be("cancelled", "the abandoned reply was not taken over by the next turn");
    }

    [Fact]
    public async Task RegenerateStream_AfterHangUp_ReplacesTheAbandonedReply()
    {
        var (profile, threadId) = await CreateScriptedThreadAsync();
        var stall = StallStreams(Provider);

        var cut = await SendAndHangUpOnFirstDeltaAsync(threadId, LongQuestion, profile.Id);
        cut.CutMidReply.Should().BeTrue($"the connection was cut — {cut}");

        var abandoned = await WaitForCancelledReplyAsync(threadId);

        Provider.Server.DeleteMapping(stall);
        var complete = $"The whole essay. {Sentinel}";
        Provider.Enqueue(StubModelProvider.Text(complete));

        var round = await RegenerateAsync(threadId, profile.Id);
        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());

        var messages = await WaitForRawMessagesAsync(threadId, m =>
            m.Where(x => x.IsAssistant).All(x => x.Id != abandoned.Id) && m.Any(x => x.IsAssistant && x.Text == complete));
        var context = string.Join("; ", messages);

        // Regenerate replaces rather than appends: the abandoned reply is gone and a complete answer took its place.
        var reply = messages.Where(m => m.IsAssistant).Should().ContainSingle(context).Subject;
        reply.Id.Should().NotBe(abandoned.Id, context);
        reply.Text.Should().Be(complete, context);
        messages.Where(m => m.IsUser).Should().ContainSingle(context);
    }

    /// <summary>
    /// Waits for the stopped turn's reply to be marked cancelled and checks what it holds: incomplete, cancelled,
    /// not an error, and the fragment generated before the hang-up — kept, and unfinished.
    /// </summary>
    private async Task<RawMessage> WaitForCancelledReplyAsync(string threadId)
    {
        var messages = await WaitForRawMessagesAsync(threadId, m => m.Any(x => x.IsAssistant && x.StatusReason == "cancelled"));
        var context = $"{string.Join("; ", messages)}\n{Provider.Describe()}";

        var reply = messages.Where(m => m.IsAssistant).Should().ContainSingle($"the abandoned turn left a reply behind: {context}").Subject;
        reply.StatusType.Should().Be("incomplete", context);
        reply.StatusReason.Should().Be("cancelled", context);
        reply.StatusError.Should().BeNull("a cancellation is not an error");
        reply.Text.Should().Contain(PartialAnswer, "the part generated before the hang-up is kept");
        reply.Text.Should().NotContain(Sentinel, "the kept text is an unfinished answer");

        return reply;
    }
}
