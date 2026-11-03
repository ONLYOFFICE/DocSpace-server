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
/// Failures that never become a reply in the thread: a request the backend cannot parse, a body past the size limit,
/// a reply abandoned half-way. None of them may cost the conversation — the next question is answered normally.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatHiddenFailureTests(AspireAppFixture fixture) : ChatPart3TestBase(fixture)
{
    [Fact]
    [Trait("Bug", "83045")]
    public async Task SendWithStream_UnparseableProfileId_IsRefusedWith400AndLeavesTheThreadAlone()
    {
        // Was BUG 83045: a profileId that is not a GUID got HTTP 200 whose whole body was
        // {"type":"error","message":"stream error"} — no code, nothing stored, nothing a chat could render.
        var provider = NewProvider();
        var profile = await CreateProfileOnAsync(provider);
        var thread = await CreateThreadAsync("bad profile thread", profile.Id);
        provider.Enqueue(StubModelProvider.Text("OK"), StubModelProvider.Text("OK again"));

        // Control: the thread answers before the bad send.
        var control = await SendWithStreamAsync(thread.ThreadId, "Reply with OK.", profile.Id);
        control.AssistantText.Should().Be("OK", control.ToString());

        var bad = await SendWithStreamAsync(thread.ThreadId, "This one goes nowhere.", "not-a-guid");

        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"a request the backend cannot run says what was wrong with it: {bad}");
        bad.StreamError.Should().BeNull(bad.ToString());

        var afterBad = await ReadStoredMessagesAsync(thread.ThreadId);
        UserMessages(afterBad).Should().HaveCount(1, "the refusal never touched the thread");
        AssertHealthyReply(AssistantMessages(afterBad).Should().ContainSingle().Subject, "still just the control turn");

        var next = await SendWithStreamAsync(thread.ThreadId, "Reply with OK again.", profile.Id);
        next.StreamError.Should().BeNull(next.ToString());

        var settled = await WaitForAssistantRepliesAsync(thread.ThreadId, 2);
        AssistantMessages(settled).Should().HaveCount(2);
        AssertHealthyReply(AssistantMessages(settled)[^1], "the thread itself is unharmed");
    }

    [Fact]
    public async Task SendWithStream_MessagePastTheSizeLimit_IsRefusedWithoutTouchingTheThread()
    {
        // The "limit exceeded" case an API test can reach: refused before the model is called, with 413 and no
        // frames. What matters is the other half — the thread is untouched and the next message works.
        var provider = NewProvider();
        var profile = await CreateProfileOnAsync(provider);
        var thread = await CreateThreadAsync("oversized thread", profile.Id);

        var refused = await SendWithStreamAsync(thread.ThreadId, string.Concat(Enumerable.Repeat("word ", 400_000)), profile.Id);

        refused.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge, refused.StatusCode.ToString());
        refused.Frames.Should().BeEmpty();
        (await ReadStoredMessagesAsync(thread.ThreadId)).Should().BeEmpty("nothing of the refused message was stored");

        provider.Enqueue(StubModelProvider.Text("OK"));
        var next = await SendWithStreamAsync(thread.ThreadId, "Reply with OK.", profile.Id);

        next.StreamError.Should().BeNull(next.ToString());
        AssertHealthyReply(AssistantMessages(await WaitForAssistantRepliesAsync(thread.ThreadId)).Should().ContainSingle().Subject, "the next message works");
    }

    [Fact]
    public async Task SendWithStream_ReplyAbandonedHalfway_LeavesTheThreadAbleToCarryOn()
    {
        // The timeout case: a reply that never finishes, abandoned by the client hanging up while the provider is
        // still silent. The abandoned turn must not take the conversation with it.
        var provider = NewProvider();
        var profile = await CreateProfileOnAsync(provider);
        var thread = await CreateThreadAsync("abandoned reply thread", profile.Id);
        var stalledQuestion = $"stalled-{Guid.NewGuid():N}";
        var nextQuestion = $"next-{Guid.NewGuid():N}";
        AnswerSlowly(
            provider,
            TimeSpan.FromSeconds(30),
            "never seen",
            body => body.Contains(stalledQuestion, StringComparison.Ordinal) && !body.Contains(nextQuestion, StringComparison.Ordinal));

        using var hangUp = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        // Raw on purpose — an SDK defect: send-with-stream answers an ndjson stream that the SDK declares as one
        // AiChatEvent; the request body is the SDK model.
        var stalled = new RawApiClient(_aiChatClient).PostRawAsync(
            "/api/2.0/ai/ai/send-with-stream",
            new AiSendStreamBody(threadId: thread.ThreadId, userMessage: BuildUserMessage(stalledQuestion), profileId: profile.Id).ToJson(),
            hangUp.Token);

        await WaitForQuestionStoredAsync(thread.ThreadId, stalled);
        stalled.IsCompleted.Should().BeFalse("the reply was still pending when the client hung up");
        await hangUp.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => (await stalled).Dispose());

        var abandoned = await WaitForAssistantRepliesAsync(thread.ThreadId);
        AssistantMessages(abandoned).Should().NotBeEmpty($"the abandoned reply is stored: {string.Join("\n", abandoned)}");

        provider.Enqueue(StubModelProvider.Text("OK"));
        var next = await SendWithStreamAsync(thread.ThreadId, nextQuestion, profile.Id);

        next.StatusCode.Should().Be(HttpStatusCode.OK, next.ToString());
        next.StreamError.Should().BeNull(next.ToString());

        var settled = await WaitForAssistantRepliesAsync(thread.ThreadId, 2);
        var replies = AssistantMessages(settled);
        replies.Should().HaveCount(2, string.Join("\n", settled));
        AssertHealthyReply(replies[^1], "the next question is answered normally");
        MessageText(replies[^1]).Should().Be("OK");
        UserMessages(settled).Should().HaveCount(2);
    }

    private async Task WaitForQuestionStoredAsync(string threadId, Task pending)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (!pending.IsCompleted && DateTime.UtcNow < deadline)
        {
            if ((await ReadStoredMessagesAsync(threadId)).Any(IsUser))
            {
                return;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }
    }
}
