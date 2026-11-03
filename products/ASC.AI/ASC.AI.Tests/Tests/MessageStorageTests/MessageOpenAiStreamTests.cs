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
/// <c>ai/send-with-stream-openai</c> — the chat round of <c>send-with-stream</c> re-encoded as an OpenAI-compatible
/// SSE stream: the wire format itself, a tool call and a provider failure as they look in it.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Messages")]
public class MessageOpenAiStreamTests(AspireAppFixture fixture) : MessagePart2TestBase(fixture)
{
    [Fact]
    public async Task SendWithStreamOpenAi_StreamsOpenAiChunksAndTerminatesWithDone()
    {
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);
        var thread = await CreateThreadAsync(profileId: profile.Id);
        const string answer = "OK, answered by the stub";
        provider.Enqueue(StubModelProvider.Text(answer));

        var stream = await SendWithStreamOpenAiAsync(BuildStreamBody("Reply with the single word OK.", thread.ThreadId, profile.Id));
        var context = $"{stream}\n{provider.Describe()}";

        stream.StatusCode.Should().Be(HttpStatusCode.OK, context);
        stream.Chunks.Should().NotBeEmpty(context);
        stream.EndsWithDone.Should().BeTrue($"the stream ends with data: [DONE]; {context}");

        stream.Chunks.Select(c => c.GetProperty("id").GetString()).Distinct()
            .Should().ContainSingle($"every chunk belongs to one completion; {context}");
        stream.Chunks.Should().AllSatisfy(chunk =>
        {
            chunk.GetProperty("object").GetString().Should().Be("chat.completion.chunk");
            chunk.GetProperty("model").GetString().Should().Be(profile.ModelId, "a chunk names the model of the resolved profile");
        });

        stream.FinishReasons.Should().Contain("stop", $"the stream reports why it stopped; {context}");
        stream.Text.Should().Be(answer, "the chunks assemble into what the model answered");
    }

    [Fact]
    public async Task SendWithStreamOpenAi_ToolCall_SurfacesAsToolCallsAndLeaksNoInternalFrameType()
    {
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);
        var thread = await CreateThreadAsync(profileId: profile.Id);

        var weatherTool = new AiMCPItem(
            name: "get_weather",
            description: "Get the current weather for a city.",
            inputSchema: new
            {
                type = "object",
                properties = new { city = new { type = "string", description = "City name" } },
                required = new[] { "city" },
                additionalProperties = false
            },
            enabled: true,
            requireApproval: true);

        // The model calls the tool; the follow-up answer is there in case the round does not pause on it.
        provider.Enqueue(
            StubModelProvider.ToolCall("get_weather", """{"city":"Paris"}"""),
            StubModelProvider.Text("It is sunny in Paris."));

        var stream = await SendWithStreamOpenAiAsync(BuildStreamBody(
            "What is the weather in Paris? Call the get_weather tool.",
            thread.ThreadId,
            profile.Id,
            new AiActionArgs(tools: [weatherTool])));
        var context = $"{stream}\n{provider.Describe()}";

        stream.StatusCode.Should().Be(HttpStatusCode.OK, context);
        stream.EndsWithDone.Should().BeTrue($"the stream still ends with [DONE] on a pause; {context}");

        // Nothing of send-with-stream's own ndjson vocabulary is meant to be visible through this wire shape.
        foreach (var chunk in stream.Chunks)
        {
            chunk.GetRawText().Should().NotMatchRegex(
                "tool-call-pending|message-start|message-end|message-delta|user-message-stored",
                "an internal frame type leaked into the OpenAI stream");
        }

        stream.FinishReasons.Should().Contain("tool_calls", $"the stream closes on the tool call; {context}");
        stream.ToolCalls.Should().NotBeEmpty($"the model's tool call is represented in the stream; {context}");
        stream.ToolCalls
            .Select(call => call.TryGetProperty("function", out var function) && function.TryGetProperty("name", out var name) ? name.GetString() : null)
            .Should().Contain("get_weather", context);

        // The pause is stored through the same mechanism as the plain endpoint's, so it resumes through the shared
        // approve/deny routes.
        var deadline = DateTime.UtcNow.AddSeconds(20);
        List<AiThreadMessageLike> messages;
        List<AiMessageContentBlock> toolCalls;

        while (true)
        {
            messages = await _aiThreads.AiThreadsReadMessagesAsync(thread.ThreadId, cancellationToken: TestContext.Current.CancellationToken);
            toolCalls = [.. messages
                .Where(m => m.Role == AiThreadMessageLike.RoleEnum.Assistant)
                .SelectMany(m => m.Content?.ActualInstance as List<AiMessageContentBlock> ?? [])
                .Where(block => block.Type == "tool-call")];

            if (toolCalls.Count > 0 || DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        var history = string.Join("\n", messages.Select(m => m.ToJson()));
        toolCalls.Should().ContainSingle($"the paused reply is stored and holds the one tool call: {history}")
            .Which.ToolName.Should().Be("get_weather", history);
    }

    [Fact]
    public async Task SendWithStreamOpenAi_ProviderRefusesTheRequest_ReportsTheFailureInsideTheStream()
    {
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);
        var thread = await CreateThreadAsync(profileId: profile.Id);

        // 400: not retried by the chat service's OpenAI client, so the failure reaches the round.
        const string refusal = "the stub refuses to hold a conversation";
        provider.Enqueue(StubModelProvider.Error(400, refusal));

        var stream = await SendWithStreamOpenAiAsync(BuildStreamBody("Reply with the single word OK.", thread.ThreadId, profile.Id));
        var context = $"{stream}\n{provider.Describe()}";

        stream.StatusCode.Should().Be(HttpStatusCode.OK, $"the refusal is reported inside the stream; {context}");
        stream.EndsWithDone.Should().BeTrue($"the stream still ends cleanly; {context}");

        // The wire shape the SDK's AiOpenAIStreamError documents: one data line carrying an `error` object and no
        // `choices`, with the fields message, type, code and param.
        stream.ErrorFrames.Should().ContainSingle($"the refusal must be readable as an OpenAI error frame; {context}");
        var error = stream.ErrorFrames[0].GetProperty("error");

        error.GetProperty("code").GetString().Should().Be("bad_request", context);
        error.GetProperty("type").GetString().Should().Be("invalid_request_error", context);
        error.GetProperty("message").GetString().Should().Contain(refusal, context);
        error.TryGetProperty("param", out _).Should().BeTrue($"param is required (nullable, not optional) in the documented shape; {context}");

        var messages = await WaitForMessagesAsync(thread.ThreadId, list => AssistantMessages(list).Count > 0);
        var failedTurn = AssistantMessages(messages).FirstOrDefault();

        failedTurn.Should().NotBeNull($"the failed turn is stored: {Describe(messages)}");
        failedTurn!.Status.Should().NotBeNull($"a failed turn carries its status: {Describe(messages)}");
        failedTurn.Status.Type.Should().Be("incomplete", Describe(messages));
    }
}
