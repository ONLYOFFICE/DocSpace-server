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

namespace ASC.AI.Tests.Tests.McpTests;

/// <summary>The member types the tools surface is checked for.</summary>
public enum McpRole
{
    DocSpaceAdmin,
    RoomAdmin,
    User,
    Guest
}

/// <summary>A client-supplied ("host") tool offered to the model for one request (<c>actionArgs.tools</c>).</summary>
/// <param name="Name">The tool name.</param>
/// <param name="Description">What the tool does, as the model reads it.</param>
/// <param name="InputSchemaJson">The JSON Schema of the arguments.</param>
/// <param name="Enabled">The per-request switch (<c>enabled</c>).</param>
/// <param name="RequireApproval">null leaves the flag out of the body altogether — the case the typed SDK model cannot express.</param>
public sealed record HostTool(string Name, string Description, string InputSchemaJson, bool Enabled = true, bool? RequireApproval = true);

/// <summary>A conversation arranged for a tool-call test: a model of the test's own, a thread in an AI room.</summary>
public sealed record McpChat(StubModelProvider Provider, AiProfile Profile, int RoomId, string ThreadId);

/// <summary>What a raw call to <c>/api/2.0/ai/tools/*</c> answered.</summary>
public sealed record ToolsReply(HttpStatusCode Status, string Text)
{
    public int Code => (int)Status;

    public JsonElement? Json
    {
        get
        {
            try
            {
                return JsonDocument.Parse(Text).RootElement.Clone();
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }

    /// <summary>The <c>error</c> string of a hard 400/403 body (<c>{"error": "..."}</c>), or null.</summary>
    public string? Error =>
        Json is { ValueKind: JsonValueKind.Object } json && json.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String
            ? error.GetString()
            : null;

    /// <summary>The <c>success</c> flag of a mutation answer, or null when the body carries none.</summary>
    public bool? Success =>
        Json is { ValueKind: JsonValueKind.Object } json && json.TryGetProperty("success", out var success) && success.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? success.GetBoolean()
            : null;

    /// <summary>The message of the <c>error</c> object of a soft refusal (<c>{"success": false, "error": {"message": ...}}</c>), or null.</summary>
    public string? SoftMessage =>
        Json is { ValueKind: JsonValueKind.Object } json
        && json.TryGetProperty("error", out var error)
        && error.ValueKind == JsonValueKind.Object
        && error.TryGetProperty("message", out var message)
            ? message.GetString()
            : null;

    public override string ToString() => $"{Code}: {Text}";
}

/// <summary>
/// Shared helpers of the MCP / tools suites (<c>ai/mcp/*.spec.ts</c>): the public <c>/api/2.0/ai/tools/*</c> surface
/// through the typed SDK, raw calls for the bodies the SDK cannot carry, and a scripted conversation for the cases
/// that drive a tool call through <c>ai/send-with-stream</c>, <c>approve-tool-call</c> and <c>deny-tool-call</c>.
/// </summary>
/// <remarks>
/// What changed against the TypeScript suite. Agents are gone (the new AI concept), so a conversation runs in an AI
/// room. The custom MCP registry and the tool preferences are ONE map per portal (per user for the preferences): the
/// chat service accepts a reachable <c>entityId</c> and drops it (Bug 82863), so the per-agent scoping the TS suite
/// measured has no public counterpart. The model is a <see cref="StubModelProvider"/> of the test's own: which tool it
/// calls is scripted with <see cref="StubModelProvider.Enqueue"/>, and the assertions are on the server's part — the
/// pause, what was stored, what the provider was offered and sent back.
/// <para>
/// Raw HTTP is used for three things the generated client cannot do, and nowhere else: the conversation routes (the
/// response is an ndjson stream the SDK model cannot parse, and its message model drops the tool-call parts), request
/// bodies the typed models cannot express (a missing field, a wrong-typed value, <c>requireApproval</c> left unset —
/// the model always emits it), and answers whose fields the SDK drops.
/// </para>
/// </remarks>
public abstract class McpTestBase(AspireAppFixture fixture) : ChatTests.ChatPart3TestBase(fixture)
{
    protected const string ToolsPath = "/api/2.0/ai/tools";
    protected const string AiChatPath = "/api/2.0/ai/ai";

    /// <summary>A folder id no portal has.</summary>
    protected const string MissingEntityId = "999999999";

    protected const string WebSearch = "web-search";
    protected const string HostServer = "host";

    /// <summary>The user list of disabled tools — the one the chat reads (no tenant list on a fresh portal).</summary>
    protected const string UserScope = "user";

    protected const string UrlRequiredMessage = "Server config requires either 'url' (HTTP) or 'command' (STDIO)";

    protected const string AskForWeather = "What is the weather in Paris? Call the get_weather tool.";

    protected static readonly HostTool _weatherTool = new(
        "get_weather",
        "Get the current weather for a city.",
        """{"type":"object","properties":{"city":{"type":"string","description":"City name"}},"required":["city"],"additionalProperties":false}""");

    /// <summary>A second tool whose input depends on the first's result.</summary>
    protected static readonly HostTool _recommendationTool = new(
        "get_recommendation",
        "Get a clothing recommendation for a given temperature in Celsius. Must be called AFTER get_weather.",
        """{"type":"object","properties":{"temperatureCelsius":{"type":"number","description":"The temperature returned by get_weather, in Celsius"}},"required":["temperatureCelsius"],"additionalProperties":false}""");

    protected static string NoPortalServerMessage(string name) => $"No config provided and no portal-level server named \"{name}\"";

    // ---------------------------------------------------------------------------------------------------------------
    // Server configs

    protected static Dictionary<string, object> UrlConfig(string host = "mcp.example.invalid") =>
        new() { ["url"] = $"https://{host}/sse" };

    protected static Dictionary<string, object> StdioConfig() =>
        new()
        {
            ["command"] = "node",
            ["args"] = new[] { "server.js", "--flag" },
            ["env"] = new Dictionary<string, string> { ["AUTOTEST"] = "1" }
        };

    // ---------------------------------------------------------------------------------------------------------------
    // The tools surface — typed

    protected Task<AiToolsMutationResult> AddServerAsync(string name, object? config, string? entityId = null) =>
        _aiTools.AiToolsAddCustomServerAsync(
            new AiToolsAddCustomServerRequest(name: name, config: config!, entityId: entityId!),
            TestContext.Current.CancellationToken);

    /// <summary>Registers a server and fails the test when the registration was refused, so a later absence means something.</summary>
    protected async Task RegisterAsync(string name, object? config = null, string? entityId = null)
    {
        var result = await AddServerAsync(name, config ?? UrlConfig(), entityId);
        result.Success.Should().BeTrue($"registering {name}: {result.Error?.Message}");
    }

    protected Task<AiToolsMutationResult> UpdateServerAsync(string name, object? config, string? entityId = null) =>
        _aiTools.AiToolsUpdateCustomServerAsync(
            new AiToolsUpdateCustomServerRequest(name: name, config: config!, entityId: entityId!),
            TestContext.Current.CancellationToken);

    protected Task<AiSuccessResponse> RemoveServerAsync(string name, string? entityId = null) =>
        _aiTools.AiToolsRemoveCustomServerAsync(
            new AiToolsRemoveCustomServerRequest(name: name, entityId: entityId!),
            TestContext.Current.CancellationToken);

    protected Task<AiToolsBulkResult> ReplaceAllAsync(Dictionary<string, object> map, string? entityId = null) =>
        _aiTools.AiToolsReplaceAllCustomServersAsync(
            new AiToolsReplaceAllCustomServersRequest(map: map, entityId: entityId!),
            TestContext.Current.CancellationToken);

    protected Task<Dictionary<string, object>> ListServersAsync(string? entityId = null) =>
        _aiTools.AiToolsListCustomServersAsync(entityId, TestContext.Current.CancellationToken);

    protected Task<object> GetServerAsync(string name, string? entityId = null) =>
        _aiTools.AiToolsGetCustomServerAsync(name, entityId, TestContext.Current.CancellationToken);

    protected Task<Dictionary<string, List<string>>> DisabledAsync(string? entityId = null) =>
        _aiTools.AiToolsGetDisabledAsync(UserScope, entityId, TestContext.Current.CancellationToken);

    protected Task<List<string>> AllowedAsync(string? entityId = null) =>
        _aiTools.AiToolsGetAllowAlwaysAsync(entityId, TestContext.Current.CancellationToken);

    protected async Task SetDisabledAsync(string serverType, params string[] toolNames)
    {
        var result = await _aiTools.AiToolsSetDisabledAsync(
            new AiToolsSetDisabledRequest(serverType: serverType, toolNames: [.. toolNames]),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
    }

    protected async Task SetAllowAlwaysAsync(string serverType, string toolName, bool value = true)
    {
        var result = await _aiTools.AiToolsSetAllowAlwaysAsync(
            new AiToolsSetAllowAlwaysRequest(serverType: serverType, toolName: toolName, value: value),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
    }

    protected Task<bool> IsDisabledAsync(string serverType, string toolName, string? entityId = null) =>
        _aiTools.AiToolsIsToolDisabledAsync(serverType, toolName, entityId, TestContext.Current.CancellationToken);

    protected Task<bool> IsAllowedAsync(string serverType, string toolName, string? entityId = null) =>
        _aiTools.AiToolsIsAllowAlwaysAsync(serverType, toolName, entityId, TestContext.Current.CancellationToken);

    /// <summary>The allow-always token the public read returns for one tool: <c>{serverType}_{toolName}</c>.</summary>
    protected static string Token(string serverType, string toolName) => $"{serverType}_{toolName}";

    // ---------------------------------------------------------------------------------------------------------------
    // The tools surface — answers as status + text
    //
    // The helpers below take a request the way a test writes it (a verb, a route, a body, a query) and answer a
    // ToolsReply. A request the generated client can carry goes through the typed SDK method — its status comes from
    // the ApiException of a refusal, which is what that exception is for. Only a request the DTOs cannot carry is sent
    // raw: a missing or null required field, a wrong-typed value, a field the DTO does not have, a body that is not
    // JSON. That is what the malformed-body cases of the suite are for.

    protected async Task<ToolsReply> CallToolsAsync(HttpMethod method, string route, object? body = null, string query = "")
    {
        var json = body is null ? null : body as string ?? JsonSerializer.Serialize(body);

        try
        {
            var typed = await TryTypedToolsAsync(method, route, json, query);

            if (typed is not null)
            {
                return typed;
            }
        }
        catch (ApiException exception)
        {
            return new ToolsReply((HttpStatusCode)exception.ErrorCode, exception.ErrorContent?.ToString() ?? string.Empty);
        }

        // Raw: the request is one the typed client cannot build (see above).
        using var request = new HttpRequestMessage(method, $"{ToolsPath}/{route}{query}");

        if (json is not null)
        {
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        using var response = await _aiChatClient.SendAsync(request, TestContext.Current.CancellationToken);

        return new ToolsReply(response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private static ToolsReply Answer(object? value) =>
        new(
            HttpStatusCode.OK,
            value is null
                ? "null"
                : Newtonsoft.Json.JsonConvert.SerializeObject(value, new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore }));

    private static bool IsString(JsonElement obj, string name) => obj.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String;

    /// <summary>The property is absent or a string (the optional <c>entityId</c>).</summary>
    private static bool IsOptionalString(JsonElement obj, string name) => !obj.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.String;

    private static string? OptionalString(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool OnlyKeys(JsonElement obj, params string[] allowed) => obj.EnumerateObject().All(p => allowed.Contains(p.Name));

    /// <summary>Sends the request through the typed client, or answers null when the typed client cannot carry it.</summary>
    private async Task<ToolsReply?> TryTypedToolsAsync(HttpMethod method, string route, string? json, string query)
    {
        var ct = TestContext.Current.CancellationToken;
        JsonElement body = default;

        if (json is not null)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                body = document.RootElement.Clone();
            }
            catch (JsonException)
            {
                return null;
            }

            if (body.ValueKind != JsonValueKind.Object)
            {
                return null;
            }
        }

        var parameters = System.Web.HttpUtility.ParseQueryString(query.TrimStart('?'));
        string? Param(string key) => parameters.GetValues(key)?.FirstOrDefault();
        var hasBody = json is not null;
        var entityFromQuery = Param("entityId");

        switch (method.Method, route)
        {
            case ("POST", "add-custom-server") or ("PUT", "update-custom-server"):
                if (!hasBody || !OnlyKeys(body, "name", "config", "entityId") || !IsString(body, "name")
                    || !body.TryGetProperty("config", out var config) || config.ValueKind == JsonValueKind.Null || !IsOptionalString(body, "entityId"))
                {
                    return null;
                }

                var name = body.GetProperty("name").GetString()!;
                var parsed = Newtonsoft.Json.Linq.JToken.Parse(config.GetRawText());
                var entity = OptionalString(body, "entityId");

                return Answer(method == HttpMethod.Post
                    ? await _aiTools.AiToolsAddCustomServerAsync(new AiToolsAddCustomServerRequest(name, parsed, entity!), ct)
                    : await _aiTools.AiToolsUpdateCustomServerAsync(new AiToolsUpdateCustomServerRequest(name, parsed, entity!), ct));

            case ("DELETE", "remove-custom-server"):
                if (!hasBody || !OnlyKeys(body, "name", "entityId") || !IsString(body, "name") || !IsOptionalString(body, "entityId"))
                {
                    return null;
                }

                return Answer(await _aiTools.AiToolsRemoveCustomServerAsync(
                    new AiToolsRemoveCustomServerRequest(body.GetProperty("name").GetString()!, OptionalString(body, "entityId")!), ct));

            case ("PUT", "replace-all-custom-servers"):
                if (!hasBody || !OnlyKeys(body, "map", "entityId") || !IsOptionalString(body, "entityId")
                    || !body.TryGetProperty("map", out var map) || map.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                var servers = map.EnumerateObject().ToDictionary(p => p.Name, p => (object)Newtonsoft.Json.Linq.JToken.Parse(p.Value.GetRawText()));

                return Answer(await _aiTools.AiToolsReplaceAllCustomServersAsync(
                    new AiToolsReplaceAllCustomServersRequest(servers, OptionalString(body, "entityId")!), ct));

            case ("PUT", "set-disabled"):
                if (!hasBody || !OnlyKeys(body, "serverType", "toolNames", "entityId") || !IsString(body, "serverType") || !IsOptionalString(body, "entityId")
                    || !body.TryGetProperty("toolNames", out var toolNames) || toolNames.ValueKind != JsonValueKind.Array
                    || toolNames.EnumerateArray().Any(t => t.ValueKind != JsonValueKind.String))
                {
                    return null;
                }

                return Answer(await _aiTools.AiToolsSetDisabledAsync(
                    new AiToolsSetDisabledRequest(
                        serverType: body.GetProperty("serverType").GetString()!,
                        toolNames: [.. toolNames.EnumerateArray().Select(t => t.GetString()!)],
                        entityId: OptionalString(body, "entityId")!),
                    ct));

            case ("PUT", "set-allow-always"):
                if (!hasBody || !OnlyKeys(body, "serverType", "toolName", "value", "entityId") || !IsString(body, "serverType") || !IsString(body, "toolName")
                    || !IsOptionalString(body, "entityId") || !body.TryGetProperty("value", out var flag) || flag.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    return null;
                }

                return Answer(await _aiTools.AiToolsSetAllowAlwaysAsync(
                    new AiToolsSetAllowAlwaysRequest(
                        serverType: body.GetProperty("serverType").GetString()!,
                        toolName: body.GetProperty("toolName").GetString()!,
                        value: flag.GetBoolean(),
                        entityId: OptionalString(body, "entityId")!),
                    ct));

            case ("GET", "list-custom-servers") when !hasBody:
                return Answer(await _aiTools.AiToolsListCustomServersAsync(entityFromQuery, ct));

            case ("GET", "get-custom-server") when !hasBody && Param("name") is { } serverName:
                return Answer(await _aiTools.AiToolsGetCustomServerAsync(serverName, entityFromQuery, ct));

            case ("GET", "get-disabled") when !hasBody && Param("scope") is { } scope:
                return Answer(await _aiTools.AiToolsGetDisabledAsync(scope, entityFromQuery, ct));

            case ("GET", "get-allow-always") when !hasBody:
                return Answer(await _aiTools.AiToolsGetAllowAlwaysAsync(entityFromQuery, ct));

            case ("GET", "list-system-tools") when !hasBody:
                return Answer(await _aiTools.AiToolsListSystemToolsAsync(entityFromQuery, ct));

            // A parameter left out stays raw: the generated client refuses a null required parameter itself, without a
            // request. A blank one is an ordinary value the server rejects, so it goes typed.
            case ("GET", "is-tool-disabled") when !hasBody && Param("serverType") is { } serverType && Param("toolName") is { } toolName:
                return Answer(await _aiTools.AiToolsIsToolDisabledAsync(serverType, toolName, entityFromQuery, ct));

            case ("GET", "is-allow-always") when !hasBody && Param("serverType") is { } serverType && Param("toolName") is { } toolName:
                return Answer(await _aiTools.AiToolsIsAllowAlwaysAsync(serverType, toolName, entityFromQuery, ct));

            default:
                return null;
        }
    }

    protected Task<ToolsReply> GetToolsAsync(string route, string query = "") => CallToolsAsync(HttpMethod.Get, route, null, query);

    protected Task<ToolsReply> PostToolsAsync(string route, object body) => CallToolsAsync(HttpMethod.Post, route, body);

    protected Task<ToolsReply> PutToolsAsync(string route, object body) => CallToolsAsync(HttpMethod.Put, route, body);

    protected Task<ToolsReply> DeleteToolsAsync(string route, object body) => CallToolsAsync(HttpMethod.Delete, route, body);

    // ---------------------------------------------------------------------------------------------------------------
    // Comparing what the SDK hands back

    /// <summary>The JSON text of a value the SDK typed <c>Object</c> (a Newtonsoft token at runtime).</summary>
    protected static string JsonOf(object? value) =>
        value is null ? "null" : Newtonsoft.Json.JsonConvert.SerializeObject(value);

    /// <summary>Whether a config read back is, as JSON, the config that was sent.</summary>
    protected static bool SameConfig(object? actual, object expected) =>
        actual is not null && JsonEquals(JsonOf(actual), JsonSerializer.Serialize(expected));

    protected static void ShouldBeConfig(object? actual, object expected, string because = "")
    {
        SameConfig(actual, expected).Should().BeTrue(
            "the config read back should be the one sent {0}: expected {1} but was {2}",
            because,
            JsonSerializer.Serialize(expected),
            JsonOf(actual));
    }

    /// <summary>The registry holds exactly these servers, each with the config it was given.</summary>
    protected static void ShouldHold(Dictionary<string, object> actual, Dictionary<string, object> expected, string because = "")
    {
        actual.Keys.Should().BeEquivalentTo(expected.Keys, because);

        foreach (var (name, config) in expected)
        {
            ShouldBeConfig(actual[name], config, $"for {name}");
        }
    }

    /// <summary>The whole tools state of the signed-in user in one comparable string.</summary>
    protected async Task<string> SnapshotAsync()
    {
        var servers = (await ListServersAsync()).OrderBy(s => s.Key, StringComparer.Ordinal).Select(s => $"{s.Key}={JsonOf(s.Value)}");
        var disabled = (await DisabledAsync()).OrderBy(s => s.Key, StringComparer.Ordinal).Select(s => $"{s.Key}=[{string.Join(",", s.Value)}]");
        var allowed = (await AllowedAsync()).Order(StringComparer.Ordinal);

        return $"servers: {string.Join("; ", servers)}\ndisabled: {string.Join("; ", disabled)}\nallowed: {string.Join(", ", allowed)}";
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Roles

    /// <summary>A member of the given type, invited by the Owner (a Guest through the people route that activates it).</summary>
    protected Task<User> InviteRoleAsync(McpRole role) => role switch
    {
        McpRole.Guest => InviteGuest(TestContext.Current.CancellationToken),
        McpRole.DocSpaceAdmin => InviteContact(EmployeeType.DocSpaceAdmin, TestContext.Current.CancellationToken),
        McpRole.RoomAdmin => InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken),
        _ => InviteContact(EmployeeType.User, TestContext.Current.CancellationToken)
    };

    /// <summary>Signs the chat client in as <paramref name="user"/> (null: anonymous).</summary>
    protected async Task ActAsAsync(User? user) => await _aiChatClient.Authenticate(user);

    // ---------------------------------------------------------------------------------------------------------------
    // A scripted conversation

    /// <summary>A model of the test's own, a profile on it and a thread in an AI room — nothing queued yet.</summary>
    protected async Task<McpChat> StartChatAsync(string title = "Autotest MCP thread")
    {
        var provider = NewProvider();
        var profile = await CreateProfileOnAsync(provider);
        var roomId = await CreateRoomAsync();
        var thread = await CreateThreadAsync(title, profile.Id, roomId.ToString());

        return new McpChat(provider, profile, roomId, thread.ThreadId);
    }

    /// <summary>Another thread in the same room on the same model.</summary>
    protected async Task<string> NewThreadAsync(McpChat chat, string title = "Autotest MCP next thread") =>
        (await CreateThreadAsync(title, chat.Profile.Id, chat.RoomId.ToString())).ThreadId;

    protected static ScriptedAnswer WeatherCall(string city = "Paris", string callId = "call_weather_1") =>
        StubModelProvider.ToolCall(
            _weatherTool.Name,
            $$"""{"city":"{{city}}","aiChatIntent":"Checking the current weather in {{city}}"}""",
            callId);

    protected static ScriptedAnswer RecommendationCall(int temperature, string callId = "call_recommendation_1") =>
        StubModelProvider.ToolCall(
            _recommendationTool.Name,
            $$"""{"temperatureCelsius":{{temperature}},"aiChatIntent":"Choosing what to wear"}""",
            callId);

    /// <summary>
    /// The SDK's <c>actionArgs</c> for the given client tools. Built from the models; the one thing the generated
    /// <see cref="AiMCPItem"/> cannot express is an UNSET <c>requireApproval</c> (it always emits the flag), which
    /// <see cref="Patched"/> takes back out of the body.
    /// </summary>
    private static AiActionArgs? ActionArgsOf(IEnumerable<HostTool>? tools, string? replacePrompt = null)
    {
        if (tools is null && replacePrompt is null)
        {
            return null;
        }

        return new AiActionArgs(
            tools: tools?.Select(t => new AiMCPItem(
                name: t.Name,
                description: t.Description,
                inputSchema: Newtonsoft.Json.Linq.JToken.Parse(t.InputSchemaJson),
                enabled: t.Enabled,
                requireApproval: t.RequireApproval ?? false)).ToList()!,
            prompt: replacePrompt is null ? null! : new AiActionArgsPrompt(AiActionArgsPrompt.ModeEnum.Replace, replacePrompt));
    }

    /// <summary>
    /// The request body the SDK model serialises to, with the one thing the model cannot carry put right: a flag left
    /// unset (<c>requireApproval</c> of a tool, <c>result</c> of a server-executed approval, <c>allowAlways</c>), which
    /// the generated models always emit.
    /// </summary>
    private static string Patched(string sdkJson, IEnumerable<HostTool>? tools = null, params string[] unset)
    {
        var body = Newtonsoft.Json.Linq.JObject.Parse(sdkJson);

        foreach (var name in unset)
        {
            body.Remove(name);
        }

        if (tools is not null && body["actionArgs"]?["tools"] is Newtonsoft.Json.Linq.JArray array)
        {
            foreach (var (_, index) in tools.Select((t, i) => (t, i)).Where(x => x.t.RequireApproval is null))
            {
                ((Newtonsoft.Json.Linq.JObject)array[index]).Remove("requireApproval");
            }
        }

        return body.ToString(Newtonsoft.Json.Formatting.None);
    }

    // Raw on purpose — an SDK defect: the chat routes answer an ndjson stream of frames, which the SDK declares as one
    // AiChatEvent, so a typed call cannot read them. The request bodies are the SDK's own models (see Patched).
    private async Task<ChatRound> PostChatAsync(string route, string json)
    {
        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            $"{AiChatPath}/{route}",
            json,
            TestContext.Current.CancellationToken);

        return ChatRound.Parse(response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>One chat round with the given client tools offered (<c>actionArgs.tools</c>).</summary>
    protected Task<ChatRound> SendAsync(McpChat chat, string text, IEnumerable<HostTool>? tools = null, string? threadId = null)
    {
        var body = new AiSendStreamBody(
            threadId: threadId ?? chat.ThreadId,
            userMessage: BuildUserMessage(text),
            actionArgs: ActionArgsOf(tools)!,
            entityId: chat.RoomId.ToString(),
            profileId: chat.Profile.Id);

        return PostChatAsync("send-with-stream", Patched(body.ToJson(), tools));
    }

    private static decimal IdxOf(JsonElement pending) => pending.TryGetProperty("idx", out var idx) ? idx.GetInt32() : 0;

    /// <summary>The stored message a pending frame carries, as the SDK message model a decision sends back.</summary>
    private static AiThreadMessageLike MessageOf(JsonElement pending) =>
        Newtonsoft.Json.JsonConvert.DeserializeObject<AiThreadMessageLike>(pending.GetProperty("message").GetRawText())!;

    /// <summary>Approves a paused call. <paramref name="result"/> is what the client says the tool returned (null: a server-executed tool).</summary>
    protected Task<ChatRound> ApproveAsync(
        McpChat chat,
        JsonElement pending,
        object? result = null,
        bool? allowAlways = null,
        IEnumerable<HostTool>? tools = null,
        string? threadId = null)
    {
        // The model requires a result; a server-executed approval sends none, so it is taken out of the body again.
        var body = new AiApproveToolCallRequest(
            result: result is null ? new Newtonsoft.Json.Linq.JObject() : Newtonsoft.Json.Linq.JToken.FromObject(result),
            allowAlways: allowAlways ?? false,
            threadId: threadId ?? chat.ThreadId,
            messageId: pending.GetProperty("messageId").GetString()!,
            idx: IdxOf(pending),
            message: MessageOf(pending),
            actionArgs: ActionArgsOf(tools)!,
            entityId: chat.RoomId.ToString(),
            profileId: chat.Profile.Id);

        var unset = new List<string>();

        if (result is null)
        {
            unset.Add("result");
        }

        if (allowAlways is null)
        {
            unset.Add("allowAlways");
        }

        return PostChatAsync("approve-tool-call", Patched(body.ToJson(), tools, [.. unset]));
    }

    protected Task<ChatRound> DenyAsync(McpChat chat, JsonElement pending, IEnumerable<HostTool>? tools = null, string? threadId = null)
    {
        var body = new AiToolCallData(
            threadId: threadId ?? chat.ThreadId,
            messageId: pending.GetProperty("messageId").GetString()!,
            idx: IdxOf(pending),
            message: MessageOf(pending),
            actionArgs: ActionArgsOf(tools)!,
            entityId: chat.RoomId.ToString(),
            profileId: chat.Profile.Id);

        return PostChatAsync("deny-tool-call", Patched(body.ToJson(), tools));
    }

    protected Task<ChatRound> RegenerateAsync(McpChat chat, IEnumerable<HostTool>? tools = null, string? replacePrompt = null)
    {
        var body = new AiRegenerateStreamRequest(
            threadId: chat.ThreadId,
            actionArgs: ActionArgsOf(tools, replacePrompt)!,
            entityId: chat.RoomId.ToString(),
            profileId: chat.Profile.Id);

        return PostChatAsync("regenerate-stream", Patched(body.ToJson(), tools));
    }

    protected static List<string> FrameTypes(ChatRound round) =>
        [.. round.Frames.Select(f => f.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "")];

    /// <summary>The <c>tool-call-pending</c> frame — the engine's only pause point — or null when the stream did not pause.</summary>
    protected static JsonElement? PendingOf(ChatRound round) =>
        round.OfType("tool-call-pending").Select(f => (JsonElement?)f).FirstOrDefault();

    /// <summary>The pending frame, failing the test with the frame list when the stream did not pause.</summary>
    protected static JsonElement Pending(ChatRound round, string because = "")
    {
        var pending = PendingOf(round);
        pending.Should().NotBeNull($"the stream should pause for the tool {because}; frames were {string.Join(", ", FrameTypes(round))}\n{round}");

        return pending!.Value;
    }

    /// <summary>The <c>tool-call</c> content parts of a stored (or streamed) message, in content order.</summary>
    protected static List<JsonElement> ToolCallParts(JsonElement message)
    {
        if (!message.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return [.. content.EnumerateArray().Where(p => Str(p, "type") == "tool-call")];
    }

    /// <summary>A tool-call part's <c>result</c> as text (a string as itself, anything else as JSON), or null when it has none.</summary>
    protected static string? ResultOf(JsonElement part)
    {
        if (!part.TryGetProperty("result", out var result) || result.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return result.ValueKind == JsonValueKind.String ? result.GetString() : result.GetRawText();
    }

    /// <summary>One argument of a tool-call part, as text.</summary>
    protected static string? ArgText(JsonElement part, string name) =>
        part.TryGetProperty("args", out var args) && args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out var value)
            ? value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText()
            : null;

    protected static string? ToolNameOf(JsonElement part) => Str(part, "toolName");

    /// <summary>The tool-call part a pending frame points at (<c>message.content[idx]</c>).</summary>
    protected static JsonElement PendingPart(JsonElement pending) =>
        pending.GetProperty("message").GetProperty("content")[pending.GetProperty("idx").GetInt32()];

    protected static bool AutoAllowOf(JsonElement pending) =>
        pending.TryGetProperty("autoAllow", out var value) && value.ValueKind == JsonValueKind.True;

    protected static bool ServerExecutedOf(JsonElement pending) =>
        pending.TryGetProperty("serverExecuted", out var value) && value.ValueKind == JsonValueKind.True;

    /// <summary>The single assistant reply of a thread, once it is stored.</summary>
    protected async Task<JsonElement> OnlyReplyAsync(McpChat chat, string? threadId = null)
    {
        var messages = await WaitForAssistantRepliesAsync(threadId ?? chat.ThreadId);
        var replies = AssistantMessages(messages);
        replies.Should().ContainSingle("the thread should hold one assistant message: {0}", string.Join("\n", messages));

        return replies[0];
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Scripting a model that calls a tool by the name it was offered under

    /// <summary>
    /// Makes <paramref name="provider"/> call the offered tool whose name ends with <paramref name="suffix"/> (the
    /// name a registered server's tool reaches the model under is the engine's to choose), and answer
    /// <paramref name="followUp"/> once a tool result is in the conversation.
    /// </summary>
    protected static void ScriptToolOnOffer(StubModelProvider provider, string suffix, string argumentsJson, string followUp = "Done.")
    {
        provider.Server
            .Given(Request.Create().WithPath(ChatCompletionsPath).UsingPost()
                .WithBody((string? body) => body is not null && StubModelProvider.IsStreamed(body) && OfferedTools(body).Any(t => t.EndsWith(suffix, StringComparison.Ordinal))))
            .AtPriority(0)
            .RespondWith(Response.Create().WithCallback(request =>
            {
                var body = request.Body ?? string.Empty;
                var answer = HasToolResult(body)
                    ? StubModelProvider.Text(followUp)
                    : StubModelProvider.ToolCall(OfferedTools(body).First(t => t.EndsWith(suffix, StringComparison.Ordinal)), argumentsJson, "call_mcp_1");

                var message = new ResponseMessage
                {
                    StatusCode = answer.StatusCode,
                    BodyData = new BodyData { BodyAsString = answer.Body, DetectedBodyType = BodyType.String }
                };
                message.AddHeader("Content-Type", answer.ContentType);

                return message;
            }));
    }

    /// <summary>Whether a chat completion request already carries a tool result (a <c>tool</c> role message).</summary>
    protected static bool HasToolResult(string requestBody)
    {
        using var document = JsonDocument.Parse(requestBody);

        return document.RootElement.TryGetProperty("messages", out var messages)
            && messages.ValueKind == JsonValueKind.Array
            && messages.EnumerateArray().Any(m => Str(m, "role") == "tool");
    }

    /// <summary>The tool names offered to the model by the chat rounds (not the profile's tool probe) the provider received.</summary>
    protected static List<string> OfferedToolNames(StubModelProvider provider) =>
        [.. provider.CompletionRequestBodies
            .Where(b => StubModelProvider.IsStreamed(b) && !OfferedTools(b).SequenceEqual(["probe_echo"]))
            .SelectMany(OfferedTools)
            .Distinct()];

    // ---------------------------------------------------------------------------------------------------------------
    // The user's tool permission mode

    protected async Task SetToolModeAsync(AiChatToolPermissionMode mode)
    {
        var result = await _aiPreferences.AiPreferencesSetToolPermissionModeAsync(
            new AiPreferencesSetToolPermissionModeRequest(mode),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        (await _aiPreferences.AiPreferencesGetToolPermissionModeAsync(cancellationToken: TestContext.Current.CancellationToken))
            .Should().Be(mode, "the mode reads back as written");
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Rooms

    /// <summary>The id of a room of the given type made by the Owner.</summary>
    protected async Task<int> NewRoomIdAsync(RoomType type = RoomType.CustomRoom) => (await CreateRoomOfTypeAsync(type)).Id;
}
