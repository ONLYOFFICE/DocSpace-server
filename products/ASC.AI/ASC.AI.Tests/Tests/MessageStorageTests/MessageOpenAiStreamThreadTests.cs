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
/// <c>ai/send-with-stream-openai</c> does what <c>send-with-stream</c> does, in a different wire format: thread
/// creation, persistence, the history sent to the model, profile precedence, the one-request prompt override and a
/// hang-up mid-stream all have to behave the same way. Which model a round reached is told by which stub provider
/// received the request — every stub serves the same model id.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Messages")]
public class MessageOpenAiStreamThreadTests(AspireAppFixture fixture) : MessagePart2TestBase(fixture)
{
    [Fact]
    public async Task SendWithStreamOpenAi_NoThreadId_CreatesThreadAndPersistsBothMessages()
    {
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);
        var question = $"Reply with the single word OK. {Guid.NewGuid():N}";

        var stream = await SendWithStreamOpenAiAsync(BuildStreamBody(question, profileId: profile.Id));
        var context = $"{stream}\n{provider.Describe()}";

        stream.StatusCode.Should().Be(HttpStatusCode.OK, context);
        stream.EndsWithDone.Should().BeTrue(context);
        stream.Text.Should().NotBeEmpty(context);

        // No threadId went out, so the route had to open one of its own — the contract send-with-stream has.
        var threads = await _aiThreads.AiThreadsListAsync(cancellationToken: TestContext.Current.CancellationToken);
        threads.Should().ContainSingle(context);

        var messages = await WaitForMessagesAsync(threads[0].ThreadId, list => AssistantMessages(list).Any(m => !string.IsNullOrEmpty(TextOf(m))));

        var asked = UserMessages(messages).Should().ContainSingle($"the question is stored once: {Describe(messages)}").Subject;
        TextOf(asked).Should().Be(question);

        var reply = AssistantMessages(messages).Should().ContainSingle(Describe(messages)).Subject;
        TextOf(reply).Should().Be(stream.Text, "the reply is stored as streamed");
    }

    [Fact]
    public async Task SendWithStreamOpenAi_ExistingThread_CarriesItsContextForward()
    {
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);
        var thread = await CreateThreadAsync(profileId: profile.Id);
        const string firstAnswer = "Noted, the code word is kept.";
        provider.Enqueue(StubModelProvider.Text(firstAnswer), StubModelProvider.Text("TANGERINE"));

        var firstQuestion = $"Remember the code word TANGERINE. Reply with just: OK. {Guid.NewGuid():N}";
        var secondQuestion = $"What code word did I ask you to remember? {Guid.NewGuid():N}";

        var first = await SendWithStreamOpenAiAsync(BuildStreamBody(firstQuestion, thread.ThreadId, profile.Id));
        first.StatusCode.Should().Be(HttpStatusCode.OK, first.ToString());

        var second = await SendWithStreamOpenAiAsync(BuildStreamBody(secondQuestion, thread.ThreadId, profile.Id));
        second.StatusCode.Should().Be(HttpStatusCode.OK, second.ToString());

        // The second question can only be answered from the first one's context, so the model has to be sent it.
        var secondRequest = RequestsAsking(provider, secondQuestion).Should().ContainSingle(provider.Describe()).Subject;
        secondRequest.Should().Contain(firstQuestion, "the second round carries the first one's question to the model");
        secondRequest.Should().Contain(firstAnswer, "and the first one's answer");

        // One conversation, not two — the second call reused the given threadId rather than opening its own.
        var threads = await _aiThreads.AiThreadsListAsync(cancellationToken: TestContext.Current.CancellationToken);
        threads.Select(t => t.ThreadId).Should().Equal(thread.ThreadId);

        var messages = await WaitForMessagesAsync(thread.ThreadId, list => AssistantMessages(list).Count >= 2);
        UserMessages(messages).Should().HaveCount(2, Describe(messages));
        AssistantMessages(messages).Should().HaveCount(2, Describe(messages));
    }

    [Fact]
    public async Task SendWithStreamOpenAi_NoProfileId_KeepsTheThreadModel_AndAnExplicitOneMovesIt()
    {
        using var defaultProvider = new StubModelProvider();
        using var threadProvider = new StubModelProvider();
        using var explicitProvider = new StubModelProvider();

        // The first profile of the portal takes the Default assignment, so a round that fell back to the assignment
        // instead of keeping the thread's model would land on defaultProvider.
        await CreateProfileOnAsync(defaultProvider);
        var threadProfile = await CreateProfileOnAsync(threadProvider);
        var explicitProfile = await CreateProfileOnAsync(explicitProvider);
        var thread = await CreateThreadAsync(profileId: threadProfile.Id);

        var viaThread = $"no profileId {Guid.NewGuid():N}";
        var stream = await SendWithStreamOpenAiAsync(BuildStreamBody(viaThread, thread.ThreadId));
        stream.StatusCode.Should().Be(HttpStatusCode.OK, stream.ToString());
        stream.ErrorFrames.Should().BeEmpty(stream.ToString());

        RequestsAsking(threadProvider, viaThread).Should().NotBeEmpty(
            $"no profileId keeps the thread on the model it was created with\n{threadProvider.Describe()}\n{defaultProvider.Describe()}");
        RequestsAsking(defaultProvider, viaThread).Should().BeEmpty("the round did not fall back to the Default assignment");

        var viaExplicit = $"explicit profileId {Guid.NewGuid():N}";
        stream = await SendWithStreamOpenAiAsync(BuildStreamBody(viaExplicit, thread.ThreadId, explicitProfile.Id));
        stream.StatusCode.Should().Be(HttpStatusCode.OK, stream.ToString());
        stream.ErrorFrames.Should().BeEmpty(stream.ToString());

        RequestsAsking(explicitProvider, viaExplicit).Should().NotBeEmpty(
            $"an explicit profileId moves the round to its model, same precedence as send-with-stream\n{explicitProvider.Describe()}");
        RequestsAsking(threadProvider, viaExplicit).Should().BeEmpty();
    }

    [Fact]
    public async Task SendWithStreamOpenAi_PromptReplace_AppliesForOneRequestAndDoesNotStick()
    {
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);
        var thread = await CreateThreadAsync(profileId: profile.Id);

        const string marker = "ZZOPENAIPROMPTZZ";
        var replace = new AiActionArgs(prompt: new AiActionArgsPrompt(
            AiActionArgsPrompt.ModeEnum.Replace,
            $"You are a helpful test assistant. Keep answers very short. Formatting rule: finish every reply with the exact token {marker}."));

        var withOverride = $"Hi there! {Guid.NewGuid():N}";
        var first = await SendWithStreamOpenAiAsync(BuildStreamBody(withOverride, thread.ThreadId, profile.Id, replace));
        first.StatusCode.Should().Be(HttpStatusCode.OK, first.ToString());

        var withoutOverride = $"Hi again! {Guid.NewGuid():N}";
        var second = await SendWithStreamOpenAiAsync(BuildStreamBody(withoutOverride, thread.ThreadId, profile.Id));
        second.StatusCode.Should().Be(HttpStatusCode.OK, second.ToString());

        RequestsAsking(provider, withOverride).Should().ContainSingle(provider.Describe())
            .Which.Should().Contain(marker, "the per-request prompt reached the model");
        RequestsAsking(provider, withoutOverride).Should().ContainSingle(provider.Describe())
            .Which.Should().NotContain(marker, "the override does not persist to the next request");
    }

    [Fact]
    public async Task SendWithStreamOpenAi_HangingUpMidStream_CancelsTheGeneration_AndTheThreadStaysUsable()
    {
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);
        var thread = await CreateThreadAsync(profileId: profile.Id);

        var slowQuestion = $"slow-{Guid.NewGuid():N}";
        var resumeQuestion = $"resume-{Guid.NewGuid():N}";

        // The model takes a minute to answer the slow question — and only that round: the resumed one carries the
        // slow question in its history, so it is told apart by its own question.
        provider.Server
            .Given(Request.Create()
                .WithPath("/v1/chat/completions")
                .UsingPost()
                .WithBody(new RegexMatcher($"(?s)^(?!.*{resumeQuestion})(?=.*\"stream\":\\s*true).*{slowQuestion}")))
            .AtPriority(0)
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/event-stream")
                .WithBody(StubModelProvider.Text("an answer that came too late").Body)
                .WithDelay(TimeSpan.FromSeconds(60)));

        using (var hangUp = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken))
        {
            hangUp.CancelAfter(TimeSpan.FromSeconds(5));

            var send = async () => await SendWithStreamOpenAiAsync(BuildStreamBody(slowQuestion, thread.ThreadId, profile.Id), hangUp.Token);

            // The client hangs up before the reply finished — the event under test.
            await send.Should().ThrowAsync<OperationCanceledException>();
        }

        // A cancelled reply is left behind, the same as the plain endpoint's.
        var settled = await WaitForMessagesAsync(thread.ThreadId, list => AssistantMessages(list).Count > 0, timeoutSeconds: 30);
        var abandoned = AssistantMessages(settled).FirstOrDefault();

        abandoned.Should().NotBeNull($"the abandoned turn left a reply behind: {Describe(settled)}\n{provider.Describe()}");

        var resumed = await SendWithStreamOpenAiAsync(BuildStreamBody(resumeQuestion, thread.ThreadId, profile.Id));
        var context = $"{resumed}\n{provider.Describe()}";

        resumed.StatusCode.Should().Be(HttpStatusCode.OK, context);
        resumed.Text.Should().NotBeEmpty(context);

        var messages = await WaitForMessagesAsync(thread.ThreadId, list => AssistantMessages(list).Count >= 2);
        var replies = AssistantMessages(messages);

        replies.Should().HaveCount(2, $"two turns, two replies: {Describe(messages)}");
        replies[1].Id.Should().NotBe(abandoned!.Id, "the new answer is its own message, not a reuse of the abandoned one");
    }
}
