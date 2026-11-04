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


namespace ASC.AI.Tests.ApiFactories;

/// <summary>
/// An OpenAI-compatible model provider standing in for a real one, run in the test process. A profile is created
/// against a provider the Node chat service really dials (the key and model are checked on create), so every
/// profile of the suite points at one of these. The chat service reaches the loopback address because the
/// integration-test profile sets <c>AI__ALLOW_PRIVATE_BASE_URL</c>.
/// </summary>
/// <remarks>
/// What the model "says" is scripted: a streamed chat request takes the next answer queued with
/// <see cref="Enqueue"/> (a text, a tool call, a provider error) and falls back to the default text once the queue
/// is empty. A request without streaming (a thread title) gets a plain completion, and an image request a 1x1 PNG.
/// A test that scripts answers or asserts on what the provider received creates a provider of its own (and a
/// profile on it) instead of using the run-wide one, so other tests cannot take its answers.
/// </remarks>
public sealed class StubModelProvider : IDisposable
{
    public const string ModelId = "stub-model";
    public const string Key = "sk-stub";
    public const string DefaultAnswer = "Stub answer from the provider";
    public const string DefaultTitle = "Stub title";

    /// <summary>The tool the profile tool probe asks the model to call.</summary>
    private const string ProbeToolName = "probe_echo";

    // A 1x1 transparent PNG.
    private const string PixelPng = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";

    private readonly WireMockServer _server = WireMockServer.Start();
    private readonly ConcurrentQueue<ScriptedAnswer> _answers = new();
    private readonly string _defaultAnswer;
    private readonly bool _shared;

    /// <param name="answer">What the model streams back to a chat message when nothing is queued.</param>
    public StubModelProvider(string answer = DefaultAnswer) : this(answer, shared: false)
    {
    }

    private StubModelProvider(string answer, bool shared)
    {
        _defaultAnswer = answer;
        _shared = shared;

        _server
            .Given(Request.Create().WithPath("/v1/models").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBodyAsJson(new
            {
                @object = "list",
                data = new[] { new { id = ModelId, @object = "model", created = 0, owned_by = "test" } }
            }));

        _server
            // The tool probe profiles/create (and a model change on update) sends: answering it with the probe_echo
            // call it asks for is what a tool-capable model does. Answered by the queue instead, it got plain text,
            // so every profile was stored with canUseTool=false and the chat never offered the model a tool.
            .Given(Request.Create().WithPath("/v1/chat/completions").UsingPost().WithBody((string? body) => IsStreamed(body) && IsToolProbe(body)))
            .AtPriority(0)
            .RespondWith(Response.Create().WithCallback(_ => Answer(ToolCall(ProbeToolName, """{"text":"ok"}""", "call_probe"))));

        _server
            // A predicate, not JsonPartialMatcher(new { stream = true }): that one also matched "stream": false, so the
            // non-streamed calls (titles, ai/send) got an SSE body and the chat library failed on `choices[0]`.
            .Given(Request.Create().WithPath("/v1/chat/completions").UsingPost().WithBody((string? body) => IsStreamed(body)))
            .AtPriority(1)
            .RespondWith(Response.Create().WithCallback(_ => Answer(_answers.TryDequeue(out var next) ? next : Text(_defaultAnswer))));

        _server
            .Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .AtPriority(2)
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBodyAsJson(new
            {
                id = "chatcmpl-stub",
                @object = "chat.completion",
                created = 0,
                model = ModelId,
                choices = new[] { new { index = 0, message = new { role = "assistant", content = DefaultTitle }, finish_reason = "stop" } },
                usage = new { prompt_tokens = 5, completion_tokens = 2, total_tokens = 7 }
            }));

        _server
            .Given(Request.Create().WithPath("/v1/images/generations").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/json").WithBodyAsJson(new
            {
                created = 0,
                data = new[] { new { b64_json = PixelPng } }
            }));
    }

    /// <summary>
    /// The provider of the whole run (<see cref="AspireAppFixture.ModelProvider"/>): every test's profiles may point at
    /// it, so it answers with the default text only and refuses <see cref="Enqueue"/>.
    /// </summary>
    public static StubModelProvider CreateShared() => new(DefaultAnswer, shared: true);

    /// <summary>The <c>baseUrl</c> a profile is given to talk to this provider.</summary>
    public string BaseUrl => $"http://127.0.0.1:{_server.Port}/v1";

    /// <summary>The WireMock behind the provider, for a stub no helper covers (add it at priority 0 to win).</summary>
    /// <exception cref="InvalidOperationException">
    /// On the run-wide provider: a mapping added to it, or one deleted from it, changes the answers of every parallel test.
    /// </exception>
    public WireMockServer Server => _shared
        ? throw new InvalidOperationException(
            "The run-wide model provider is shared by every test: a mapping changed on it changes every parallel chat. "
            + "Create a StubModelProvider of the test's own and a profile on it (CreateProfileOnAsync) to stub it.")
        : _server;

    /// <summary>The bodies of the chat completion requests the provider received, oldest first.</summary>
    public IEnumerable<string> CompletionRequestBodies => _server.LogEntries
        .Where(e => e.RequestMessage?.Path?.EndsWith("/chat/completions", StringComparison.Ordinal) == true)
        .Select(e => e.RequestMessage!.Body ?? string.Empty);

    /// <summary>Queues the answers the next streamed chat requests get, one each, in order.</summary>
    /// <remarks>
    /// The streamed tool probe a profile create sends (<c>probe_echo</c>) is answered by its own mapping and takes
    /// nothing from the queue; queuing after the profile is created is still the clearer order.
    /// </remarks>
    /// <exception cref="InvalidOperationException">On the run-wide provider, where a parallel test would take the answer.</exception>
    public void Enqueue(params ScriptedAnswer[] answers)
    {
        if (_shared)
        {
            throw new InvalidOperationException(
                "The run-wide model provider is shared by every test: an answer queued on it goes to whichever chat asks first. "
                + "Create a StubModelProvider of the test's own and a profile on it (CreateProfileOnAsync) to script answers.");
        }

        foreach (var answer in answers)
        {
            _answers.Enqueue(answer);
        }
    }

    /// <summary>A streamed text reply, split in two chunks on the wire.</summary>
    public static ScriptedAnswer Text(string text)
    {
        var half = text.Length / 2;

        return new ScriptedAnswer(200, Sse(
            Delta(new { role = "assistant", content = "" }),
            Delta(new { content = text[..half] }),
            Delta(new { content = text[half..] }),
            Finish("stop")));
    }

    /// <summary>
    /// A streamed reply that calls a tool. The chat service runs the tool and asks the provider again with its
    /// result, so queue the follow-up answer (usually a <see cref="Text"/>) right after this one.
    /// </summary>
    public static ScriptedAnswer ToolCall(string toolName, string argumentsJson = "{}", string callId = "call_stub_1") =>
        new(200, Sse(
            Delta(new { role = "assistant", tool_calls = new[] { new { index = 0, id = callId, type = "function", function = new { name = toolName, arguments = "" } } } }),
            Delta(new { tool_calls = new[] { new { index = 0, function = new { arguments = argumentsJson } } } }),
            Finish("tool_calls")));

    /// <summary>A provider failure: the HTTP status and the OpenAI-shaped error body a real provider sends.</summary>
    /// <remarks>
    /// The OpenAI client inside the chat service retries 408, 409, 429 and 5xx twice on its own, so one such answer
    /// is swallowed by a retry that takes the next one. For a failure that reaches the chat, queue a non-retried
    /// status (400, 401, 403, 404) or the same retried one three times.
    /// </remarks>
    public static ScriptedAnswer Error(int statusCode, string message = "Stub provider failure") =>
        new(statusCode, JsonSerializer.Serialize(new { error = new { message, type = "invalid_request_error", code = (string?)null } }), "application/json");

    /// <summary>Every request the provider saw and how it answered — for an assertion message.</summary>
    public string Describe() =>
        "provider saw:\n" + string.Join("\n", _server.LogEntries.Select(e =>
            $"  {e.RequestMessage?.Method} {e.RequestMessage?.Path} -> {e.ResponseMessage?.StatusCode} {Shorten(e.RequestMessage?.Body)}"));

    private static string Shorten(string? body) =>
        body is null ? "" : body.Length <= 600 ? body : body[..600] + "…";

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }

    private static ResponseMessage Answer(ScriptedAnswer answer)
    {
        var message = new ResponseMessage
        {
            StatusCode = answer.StatusCode,
            BodyData = new BodyData { BodyAsString = answer.Body, DetectedBodyType = BodyType.String }
        };
        message.AddHeader("Content-Type", answer.ContentType);

        return message;
    }

    /// <summary>Whether a chat completion request asks for a stream (<c>"stream": true</c>).</summary>
    public static bool IsStreamed(string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(body);

            return document.RootElement.TryGetProperty("stream", out var stream) && stream.ValueKind == JsonValueKind.True;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Whether a chat completion request is the tool probe: it offers exactly the <c>probe_echo</c> tool.</summary>
    private static bool IsToolProbe(string? body)
    {
        try
        {
            using var document = JsonDocument.Parse(body!);

            return document.RootElement.TryGetProperty("tools", out var tools)
                && tools.ValueKind == JsonValueKind.Array
                && tools.GetArrayLength() == 1
                && tools[0].TryGetProperty("function", out var function)
                && function.TryGetProperty("name", out var name)
                && name.GetString() == ProbeToolName;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>An SSE body of the given <see cref="Chunk"/>s, closed with <c>data: [DONE]</c>.</summary>
    public static string Sse(params object[] chunks) =>
        string.Concat(chunks.Select(SseEvent)) + "data: [DONE]\n\n";

    /// <summary>One <see cref="Chunk"/> as an SSE event (<c>data: {...}</c> and a blank line).</summary>
    public static string SseEvent(object chunk) => $"data: {JsonSerializer.Serialize(chunk)}\n\n";

    private static object Delta(object delta) => Chunk(delta, null);

    private static object Finish(string reason) => Chunk(new { }, reason);

    /// <summary>One <c>chat.completion.chunk</c> of a streamed reply: the delta, and the finish reason on the last one.</summary>
    public static object Chunk(object delta, string? finishReason = null) => new
    {
        id = "chatcmpl-stub",
        @object = "chat.completion.chunk",
        created = 0,
        model = ModelId,
        choices = new[] { new { index = 0, delta, finish_reason = finishReason } }
    };
}

/// <summary>One scripted reply of <see cref="StubModelProvider"/>: what the provider answers one chat request with.</summary>
public sealed record ScriptedAnswer(int StatusCode, string Body, string ContentType = "text/event-stream");
