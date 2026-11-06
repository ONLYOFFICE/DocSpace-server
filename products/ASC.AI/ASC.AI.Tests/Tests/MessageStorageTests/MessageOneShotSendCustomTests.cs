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
/// <c>ai/send-custom</c> — a free-form one-turn call against a caller-supplied system prompt, answered as one JSON
/// message or (<c>isStream: true</c>) as an ndjson stream. No thread, no history, nothing persisted.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Messages")]
public class MessageOneShotSendCustomTests(AspireAppFixture fixture) : MessagePart2TestBase(fixture)
{
    [Fact]
    [Trait("Bug", "82836")]
    public async Task SendCustom_BothForms_ReachTheModel()
    {
        using var provider = new StubModelProvider();
        await CreateProfileOnAsync(provider);
        AnswerEveryCompletionWith(provider, "4");

        // Both used to fail: isStream:false never reached the model (500), isStream:true answered with an empty
        // assistant message carrying a gateway auth failure.
        var nonStreaming = await _aiChat.AiSendCustomAsync(
            new AiSendCustomRequest(isStream: false, systemPrompt: "Answer with a single word.", userMessage: BuildUserMessage("What is 2+2?")),
            TestContext.Current.CancellationToken);

        nonStreaming.Role.Should().Be(AiThreadMessageLike.RoleEnum.Assistant);
        nonStreaming.Status?.Type.Should().NotBe("incomplete", nonStreaming.ToJson());
        TextOf(nonStreaming).Should().Be("4", $"the model's answer, unstreamed\n{provider.Describe()}");

        var streaming = await SendCustomStreamAsync("Answer with a single word.", "What is 2+2?");

        streaming.StatusCode.Should().Be(HttpStatusCode.OK, streaming.ToString());
        streaming.Frames.Should().NotBeEmpty(streaming.ToString());

        // ndjson rather than one object: the message, then the closing envelope.
        var last = streaming.Frames[^1];
        last.TryGetProperty("isEnd", out var isEnd).Should().BeTrue(streaming.ToString());
        isEnd.ValueKind.Should().Be(JsonValueKind.True, "the stream closes");

        last.TryGetProperty("responseMessage", out var responseMessage).Should().BeTrue(streaming.ToString());
        (responseMessage.TryGetProperty("status", out var status) && status.TryGetProperty("error", out _))
            .Should().BeFalse($"send-custom must not fail: {streaming}");
        FrameText(last, "responseMessage").Should().Be("4", $"the model's answer, streamed\n{provider.Describe()}");
    }

    [Fact]
    [Trait("Bug", "84299")]
    public async Task SendCustom_MissingSystemPrompt_IsAValidationErrorNotACrash()
    {
        await CreateProfileAsync();

        // Raw on purpose: systemPrompt is a required constructor parameter of the SDK request.
        var body = $$"""{"isStream":false,"userMessage":{{BuildUserMessage("Reply with the single word OK.").ToJson()}}}""";

        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(SendCustomPath, body, TestContext.Current.CancellationToken);
        var answer = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"a missing required field is a validation error, not a crash: {answer}");
    }

    [Fact]
    [Trait("Bug", "84300")]
    public async Task SendCustom_IsStreamAsAString_IsRefused()
    {
        await CreateProfileAsync();

        // Raw on purpose: a string where the DTO declares a boolean. "false" used to be read as truthy, switching the
        // answer to the ndjson stream.
        var body = $$"""{"isStream":"false","systemPrompt":"Answer briefly.","userMessage":{{BuildUserMessage("What is 2+2? Reply with just the number.").ToJson()}}}""";

        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(SendCustomPath, body, TestContext.Current.CancellationToken);
        var answer = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"a value outside the declared boolean type is a validation error: {answer}");
    }

    [Fact]
    public async Task SendCustom_TheSystemPromptOfEachCallIsTheOneSentToTheModel()
    {
        using var provider = new StubModelProvider();
        await CreateProfileOnAsync(provider);
        AnswerEveryCompletionWith(provider, "Hello.");

        var firstQuestion = $"Hi. {Guid.NewGuid():N}";
        var first = await _aiChat.AiSendCustomAsync(
            new AiSendCustomRequest(isStream: false, systemPrompt: "Finish every reply with the exact token ZZPROMPTAZZ.", userMessage: BuildUserMessage(firstQuestion)),
            TestContext.Current.CancellationToken);
        first.Status?.Type.Should().NotBe("incomplete", first.ToJson());

        var secondQuestion = $"Hi. {Guid.NewGuid():N}";
        var second = await _aiChat.AiSendCustomAsync(
            new AiSendCustomRequest(isStream: false, systemPrompt: "Finish every reply with the exact token ZZPROMPTBZZ.", userMessage: BuildUserMessage(secondQuestion)),
            TestContext.Current.CancellationToken);
        second.Status?.Type.Should().NotBe("incomplete", second.ToJson());

        RequestsAsking(provider, firstQuestion, streamedOnly: false).Should().ContainSingle(provider.Describe())
            .Which.Should().Contain("ZZPROMPTAZZ", "the first call's system prompt reached the model");

        var secondRequest = RequestsAsking(provider, secondQuestion, streamedOnly: false).Should().ContainSingle(provider.Describe()).Subject;
        secondRequest.Should().Contain("ZZPROMPTBZZ", "the second call's system prompt reached the model");
        secondRequest.Should().NotContain("ZZPROMPTAZZ", "a system prompt does not carry over to the next call");
    }

    [Fact]
    public async Task SendCustom_NeitherIsStreamMode_PersistsAThread()
    {
        using var provider = new StubModelProvider();
        await CreateProfileOnAsync(provider);
        AnswerEveryCompletionWith(provider, "OK");

        var nonStreaming = await _aiChat.AiSendCustomAsync(
            new AiSendCustomRequest(isStream: false, systemPrompt: "Answer briefly.", userMessage: BuildUserMessage("Reply with the single word OK.")),
            TestContext.Current.CancellationToken);
        nonStreaming.Id.Should().BeNull("the one-shot answer carries no thread-side identity");

        var streaming = await SendCustomStreamAsync("Answer briefly.", "Reply with the single word OK.");
        streaming.StatusCode.Should().Be(HttpStatusCode.OK, streaming.ToString());

        foreach (var frame in streaming.Frames)
        {
            frame.TryGetProperty("id", out _).Should().BeFalse($"a frame carries no thread-side id: {frame}");
            (frame.TryGetProperty("responseMessage", out var message) && message.TryGetProperty("id", out _))
                .Should().BeFalse($"a streamed message carries no thread-side id: {frame}");
        }

        (await _aiThreads.AiThreadsListAsync(cancellationToken: TestContext.Current.CancellationToken))
            .Should().BeEmpty("neither form created a thread");
    }

    /// <summary>Sends <c>send-custom</c> with <c>isStream: true</c> and reads the whole ndjson stream.</summary>
    /// <remarks>
    /// Raw on purpose — an SDK defect: with <c>isStream: true</c> the route answers an ndjson stream, but the SDK
    /// declares a single <see cref="AiThreadMessageLike"/>, so the typed call cannot read it. The body is the SDK model.
    /// </remarks>
    private async Task<ChatRound> SendCustomStreamAsync(string systemPrompt, string text)
    {
        var body = new AiSendCustomRequest(isStream: true, systemPrompt: systemPrompt, userMessage: BuildUserMessage(text));

        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(SendCustomPath, body.ToJson(), TestContext.Current.CancellationToken);

        return ChatRound.Parse(response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
