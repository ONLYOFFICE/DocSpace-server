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
/// A provider failure in a chat. The send is HTTP 200 either way; the failure arrives as the terminal
/// <c>message-incomplete</c> frame and is stored in the thread as an assistant message with empty content and
/// <c>status {type: "incomplete", reason: "error", error: {code, message}}</c> — <c>code</c> the machine-readable half
/// a client localises by, <c>message</c> the provider's string.
/// </summary>
/// <remarks>
/// The failures are scripted on the provider. 400 and 401 are used because the chat service's OpenAI client retries
/// a 5xx on its own and would take the next answer instead.
/// </remarks>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatProviderFailureTests(AspireAppFixture fixture) : ChatPart3TestBase(fixture)
{
    [Fact]
    public async Task SendWithStream_ModelThatCannotServe_FailsTheReplyAndAnotherModelAnswersInTheSameThread()
    {
        var good = NewProvider();
        var goodProfile = await CreateProfileOnAsync(good);
        var bad = NewProvider();
        var badProfile = await CreateProfileOnAsync(bad);
        var thread = await CreateThreadAsync("unavailable model thread", badProfile.Id);
        var question = $"failing-{Guid.NewGuid():N}";
        bad.Enqueue(StubModelProvider.Error(400, "model is not a chat model"));
        good.Enqueue(StubModelProvider.Text("OK"));

        var refused = await SendWithStreamAsync(thread.ThreadId, question, badProfile.Id);

        refused.StatusCode.Should().Be(HttpStatusCode.OK, refused.ToString());
        refused.OfType("message-incomplete").Should().NotBeEmpty($"{refused}\n{bad.Describe()}");

        var failedTurn = await WaitForAssistantRepliesAsync(thread.ThreadId);
        var failure = StatusOf(AssistantMessages(failedTurn).Should().ContainSingle().Subject);
        failure.Type.Should().Be("incomplete", string.Join("\n", failedTurn));
        failure.Reason.Should().Be("error");
        failure.Code.Should().Be("bad_request");
        failure.Message.Should().Contain("not a chat model");
        MessageText(AssistantMessages(failedTurn)[0]).Should().BeEmpty();
        MessageText(UserMessages(failedTurn).Should().ContainSingle().Subject).Should().Be(question);

        // The failed turn does not corrupt the thread: it is still readable and editable.
        (await _aiThreads.AiThreadsRenameAsync(
            new AiThreadsRenameRequest(thread.ThreadId, "renamed after the failure"),
            TestContext.Current.CancellationToken)).Success.Should().BeTrue();
        (await _aiThreads.AiThreadsGetByIdAsync(thread.ThreadId, TestContext.Current.CancellationToken)).Title
            .Should().Be("renamed after the failure");

        // Picking a model that can answer is all it takes — same thread, no reset.
        var retried = await SendWithStreamAsync(thread.ThreadId, question, goodProfile.Id);

        retried.StatusCode.Should().Be(HttpStatusCode.OK, retried.ToString());
        retried.StreamError.Should().BeNull(retried.ToString());

        var settled = await WaitForAssistantRepliesAsync(thread.ThreadId, 2);
        var replies = AssistantMessages(settled);

        replies.Should().HaveCount(2, string.Join("\n", settled));
        StatusOf(replies[0]).Code.Should().Be("bad_request");
        AssertHealthyReply(replies[1], "the second model answered");
        MessageText(replies[1]).Should().Be("OK");
        UserMessages(settled).Should().HaveCount(2);
    }

    [Fact]
    public async Task RegenerateStream_ProviderRefuses_ReportsTheFailureAndKeepsTheQuestion()
    {
        // The gesture a user makes next: "try again". A regenerate while the provider refuses has to come back as a
        // reported failure — not a 500, and not a thread that loses its history or gains a second question.
        var provider = NewProvider();
        var profile = await CreateProfileOnAsync(provider);
        var thread = await CreateThreadAsync("retry thread", profile.Id);
        provider.Enqueue(StubModelProvider.Text("Hi there"));

        var sent = await SendWithStreamAsync(thread.ThreadId, "Say hi", profile.Id);
        sent.StreamError.Should().BeNull(sent.ToString());
        await WaitForAssistantRepliesAsync(thread.ThreadId);

        provider.Enqueue(StubModelProvider.Error(401, "Incorrect API key provided"));

        // Raw on purpose — an SDK defect: regenerate-stream answers an ndjson stream of AiChatEvent frames, but the
        // SDK declares one AiChatEvent and throws on the second frame. The request body is the SDK model.
        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            "/api/2.0/ai/ai/regenerate-stream",
            new AiRegenerateStreamRequest(threadId: thread.ThreadId, profileId: profile.Id).ToJson(),
            TestContext.Current.CancellationToken);
        var again = ChatRound.Parse(response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        again.StatusCode.Should().Be(HttpStatusCode.OK, again.ToString());
        var failed = again.IncompleteStatus;
        failed.Should().NotBeNull($"the refusal is reported as an incomplete reply: {again}\n{provider.Describe()}");
        failed!.Value.GetProperty("reason").GetString().Should().Be("error");
        failed.Value.GetProperty("error").GetProperty("code").GetString().Should().Be("auth");

        var after = await ReadStoredMessagesAsync(thread.ThreadId);
        MessageText(UserMessages(after).Should().ContainSingle("the thread did not gain a second question").Subject)
            .Should().Be("Say hi");
    }
}
