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
/// The chat round trip against a stand-in model: a profile on a provider of its own (so the requests the provider
/// saw belong to this test alone), a thread on it, one message sent through <c>send-with-stream</c>. Proves the
/// Node chat service streams what the provider answered and stores it as the assistant reply, with no real model
/// and no AI gateway.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Chat")]
public class ChatStreamTests(AspireAppFixture fixture) : BaseTest(fixture), IDisposable
{
    private const string Answer = "Stubbed answer from the provider";

    private readonly StubModelProvider _provider = new(Answer);

    public void Dispose()
    {
        _provider.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SendWithStream_ProfileOnStubProvider_StreamsAndStoresTheProviderAnswer()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var userText = $"question-{Guid.NewGuid():N}";

        var input = BuildProfileInput();
        input.BaseUrl = _provider.BaseUrl;
        var created = await _aiProfiles.AiProfilesCreateAsync(input, cancellationToken);
        created.Success.Should().BeTrue($"the provider check failed: {created.Error}\n{_provider.Describe()}");
        var profileId = created.Profile.Id;

        var thread = await CreateThreadAsync(title: "stub thread", profileId: profileId);

        // Raw on purpose — an SDK defect: the response is an ndjson stream of AiChatEvent frames, but the SDK
        // declares it as one AiChatEvent, so AiSendWithStreamAsync throws "Additional text encountered after
        // finished reading JSON content" on the second frame. The request body is the SDK model all the same.
        using var send = await new RawApiClient(_aiChatClient).PostRawAsync(
            "/api/2.0/ai/ai/send-with-stream",
            new AiSendStreamBody(threadId: thread.ThreadId, userMessage: BuildUserMessage(userText), profileId: profileId).ToJson(),
            cancellationToken);
        var stream = await send.Content.ReadAsStringAsync(cancellationToken);
        var context = $"stream: {stream}\n{_provider.Describe()}";

        send.StatusCode.Should().Be(HttpStatusCode.OK, context);
        stream.Should().NotContain("\"type\":\"error\"", context);
        stream.Should().Contain(Answer[..(Answer.Length / 2)], context).And.Contain(Answer[(Answer.Length / 2)..], context);

        _provider.CompletionRequestBodies.Should().Contain(body => body.Contains(userText), context);

        var messages = await _aiThreads.AiThreadsReadMessagesAsync(thread.ThreadId, cancellationToken: cancellationToken);

        messages.Where(m => m.Role == AiThreadMessageLike.RoleEnum.Assistant).Select(TextOf)
            .Should().Contain(Answer, "the reply is stored on the thread");
    }

    [Fact]
    public async Task SendWithStream_ScriptedAnswers_AreServedInOrder()
    {
        var profile = await CreateProfileOnAsync(_provider);
        var thread = await CreateThreadAsync(profileId: profile.Id);
        // 400, not 500: the chat service's OpenAI client retries a 5xx on its own and would take the next answer.
        _provider.Enqueue(StubModelProvider.Text("first scripted"), StubModelProvider.Error(400, "scripted failure"));

        var first = await SendWithStreamAsync(thread.ThreadId, "one", profile.Id);
        var second = await SendWithStreamAsync(thread.ThreadId, "two", profile.Id);
        var third = await SendWithStreamAsync(thread.ThreadId, "three", profile.Id);

        first.StreamError.Should().BeNull(first.ToString());
        first.AssistantText.Should().Be("first scripted", first.ToString());

        var failed = second.IncompleteStatus;
        failed.Should().NotBeNull($"a provider failure ends the reply as incomplete: {second}\n{_provider.Describe()}");
        failed!.Value.GetProperty("reason").GetString().Should().Be("error");
        failed.Value.GetProperty("error").GetProperty("message").GetString().Should().Contain("scripted failure");

        third.AssistantText.Should().Be(Answer, "the queue is empty again, so the default answer comes back");
    }
}
