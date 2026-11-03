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

namespace ASC.AI.Tests.Tests.EditorToolTests;

/// <summary>
/// Shared plumbing of the editor-tools suites ported from <c>ai/editor-tools/*.spec.ts</c>:
/// <c>GET /api/2.0/ai/editor-tools/list</c> and <c>POST /api/2.0/ai/editor-tools/call</c>.
/// </summary>
/// <remarks>
/// The pair is a server-side executor: <c>list</c> publishes the tools of the chat service's composed catalogue (the
/// three <c>onlyoffice_generate_*</c> generators from ASC.AI, plus the DocSpace MCP server's REST tools when that
/// container runs) and <c>call</c> runs one as the caller. A tool that fails still answers 200 and says so inside
/// <c>result</c>, so a call is read through <see cref="Outcome"/>. Requests are raw JSON on purpose: the tests send
/// names and entity ids of the wrong type and arguments that are not an object, which the typed DTO cannot carry, and
/// the published input schemas are free-form JSON that the generated model types as <c>Dictionary&lt;string, object&gt;</c>.
/// The typed client is used where the claim is about the contract it describes.
/// </remarks>
public abstract class EditorToolTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const string ListPath = "/api/2.0/ai/editor-tools/list";
    protected const string CallPath = "/api/2.0/ai/editor-tools/call";

    protected const string UnknownToolName = "Unknown tool name";
    protected const string MissingEntityId = "999999999";

    protected const string DocxTool = "onlyoffice_generate_docx";
    protected const string PresentationTool = "onlyoffice_generate_presentation";
    protected const string FormTool = "onlyoffice_generate_form";

    public static readonly string[] GeneratorTools = [DocxTool, PresentationTool, FormTool];

    /// <summary>The generators and the extension each writes.</summary>
    public static TheoryData<string, string> Generators => new()
    {
        { DocxTool, ".docx" },
        { PresentationTool, ".pptx" },
        { FormTool, ".pdf" }
    };

    public static TheoryData<EmployeeType> NonGuestRoles => new()
    {
        EmployeeType.DocSpaceAdmin,
        EmployeeType.RoomAdmin,
        EmployeeType.User
    };

    /// <summary>The typed client of the editor-tools routes, on the chat client.</summary>
    protected DocSpace.API.SDK.Api.AI.EditorToolsApi SdkEditorTools => _aiEditorTools;

    /// <summary>One answer of an editor-tools route.</summary>
    protected sealed record Reply(int Status, string Text, string? ContentType = null)
    {
        public JsonElement Json
        {
            get
            {
                try
                {
                    using var document = JsonDocument.Parse(Text);

                    return document.RootElement.Clone();
                }
                catch (JsonException)
                {
                    return default;
                }
            }
        }

        public string? Error => Str("error");

        public string? Str(string name) =>
            Json.ValueKind == JsonValueKind.Object && Json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

        public override string ToString() => $"{Status}: {Text}";
    }

    /// <summary>A tool as <c>list</c> publishes it.</summary>
    protected sealed record Tool(string Name, string Description, JsonElement InputSchema, bool RequireApproval)
    {
        public IEnumerable<string> Required =>
            InputSchema.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array
                ? required.EnumerateArray().Select(e => e.GetString()!)
                : [];

        /// <summary>Properties a caller must send: required and without a default.</summary>
        public IEnumerable<string> TrulyRequired => Required.Where(key =>
            Property(key).ValueKind != JsonValueKind.Object || !Property(key).TryGetProperty("default", out _));

        public bool IsGenerator => Name.StartsWith("onlyoffice_generate_", StringComparison.Ordinal);

        public JsonElement Property(string key) =>
            InputSchema.TryGetProperty("properties", out var properties) && properties.TryGetProperty(key, out var property)
                ? property
                : default;
    }

    /// <summary>What a tool call produced, read the way the chat service wraps it.</summary>
    protected sealed record Outcome(string Raw, bool IsError, string Text, JsonElement Json)
    {
        /// <summary>The file a generator reports having created, or null.</summary>
        public GeneratedFile? File =>
            Json.ValueKind == JsonValueKind.Object && Json.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object
                && data.TryGetProperty("id", out var id) && id.TryGetInt32(out var fileId)
                    ? new GeneratedFile(
                        fileId,
                        data.TryGetProperty("title", out var title) ? title.GetString() ?? string.Empty : string.Empty,
                        data.TryGetProperty("parentId", out var parent) && parent.TryGetInt32(out var parentId) ? parentId : 0)
                    : null;
    }

    protected sealed record GeneratedFile(int Id, string Title, int ParentId);

    protected async Task<Reply> SendAsync(HttpMethod method, string path, string? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        }

        using var response = await _aiChatClient.SendAsync(request, TestContext.Current.CancellationToken);

        return new Reply(
            (int)response.StatusCode,
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            response.Content.Headers.ContentType?.MediaType);
    }

    protected Task<Reply> ListAsync() => SendAsync(HttpMethod.Get, ListPath);

    protected Task<Reply> CallRawAsync(string body) => SendAsync(HttpMethod.Post, CallPath, body);

    /// <summary>
    /// Repeats a request that changes nothing until it is refused with 403 or the deadline passes, and returns the last
    /// reply. <see cref="BaseTest.SetPortalAiAccessAsync"/> waits until <c>threads/list</c> sees the switch; the
    /// editor-tools routes read it on their own and may still answer as before for a moment, so an off-state check
    /// polls here instead of taking the first answer. Never pass a call that creates or deletes anything.
    /// </summary>
    protected static async Task<Reply> UntilRefusedAsync(Func<Task<Reply>> request)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (true)
        {
            var reply = await request();

            if (reply.Status == (int)HttpStatusCode.Forbidden || DateTime.UtcNow >= deadline)
            {
                return reply;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Switches AI off and waits until the editor-tools routes see it (<c>list</c> is refused), so the request that
    /// follows meets the off state. The wait polls <c>list</c>, never the call under test, which may create or delete.
    /// </summary>
    protected async Task SwitchAiOffAsync()
    {
        await SetPortalAiAccessAsync(false);

        var list = await UntilRefusedAsync(ListAsync);
        list.Status.Should().Be(403, $"the editor-tools routes must see the switch before the call under test: {list}");
    }

    /// <summary>A call body from JSON fragments; a fragment left null is left out of the body.</summary>
    protected static string BodyJson(string? nameJson, string? argumentsJson = null, string? entityIdJson = null)
    {
        var parts = new List<string>();
        if (nameJson is not null)
        {
            parts.Add($"\"name\":{nameJson}");
        }

        if (argumentsJson is not null)
        {
            parts.Add($"\"arguments\":{argumentsJson}");
        }

        if (entityIdJson is not null)
        {
            parts.Add($"\"entityId\":{entityIdJson}");
        }

        return "{" + string.Join(",", parts) + "}";
    }

    /// <summary>
    /// Calls a tool through the typed client; a non-2xx comes back as a <see cref="Reply"/> with the status and body of
    /// the <see cref="ApiException"/>, and a success as <c>{"result": ...}</c>.
    /// </summary>
    protected async Task<Reply> CallToolAsync(string name, object? arguments = null, string? entityId = null)
    {
        var argumentsJson = arguments is null ? null : JsonSerializer.Serialize(arguments);
        // The typed arguments are a Dictionary<string, object> that the client may drop a null value from, so a null
        // argument (the tests that check it is refused) goes out as the raw body instead.
        if (argumentsJson is not null && HasNullArgument(argumentsJson))
        {
            return await CallRawAsync(BodyJson(
                JsonSerializer.Serialize(name),
                argumentsJson,
                entityId is null ? null : JsonSerializer.Serialize(entityId)));
        }

        var typedArguments = argumentsJson is null
            ? null
            : Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(argumentsJson);

        try
        {
            var response = await SdkEditorTools.AiEditorToolsCallWithHttpInfoAsync(
                new AiEditorToolsCallRequest(name, typedArguments!, entityId!),
                TestContext.Current.CancellationToken);

            return new Reply((int)response.StatusCode, JsonSerializer.Serialize(new { result = response.Data.Result }), "application/json");
        }
        catch (ApiException exception)
        {
            return new Reply(exception.ErrorCode, exception.ErrorContent?.ToString() ?? string.Empty);
        }
    }

    private static bool HasNullArgument(string argumentsJson)
    {
        using var document = JsonDocument.Parse(argumentsJson);

        return document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.EnumerateObject().Any(p => p.Value.ValueKind == JsonValueKind.Null);
    }

    /// <summary>Calls a tool, asserts the route answered 200 and returns what the tool produced.</summary>
    protected async Task<Outcome> RunAsync(string name, object? arguments = null, string? entityId = null)
    {
        var reply = await CallToolAsync(name, arguments, entityId);
        reply.Status.Should().Be(200, $"/call answered {reply}");

        return OutcomeOf(reply);
    }

    /// <summary>
    /// Unwraps <c>result</c>: REST tools answer <c>{"content":[{"text":..}],"isError":..}</c>, a generator
    /// <c>{"data":{id,title,parentId,parentTitle,url}}</c>, and a failure is the plain text <c>Tool "x" failed: ..</c>.
    /// </summary>
    protected static Outcome OutcomeOf(Reply reply)
    {
        var raw = reply.Str("result") ?? string.Empty;
        var outer = TryParse(raw);

        if (outer.ValueKind == JsonValueKind.Object && outer.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            var text = string.Join("\n", content.EnumerateArray().Select(part => part.TryGetProperty("text", out var t) ? t.GetString() : string.Empty));
            var isError = outer.TryGetProperty("isError", out var flag) && flag.ValueKind == JsonValueKind.True;

            return new Outcome(raw, isError, text, TryParse(text));
        }

        if (outer.ValueKind == JsonValueKind.Object)
        {
            return new Outcome(raw, false, raw, outer);
        }

        return new Outcome(raw, raw.StartsWith("Tool \"", StringComparison.Ordinal) && raw.Contains("\" failed:", StringComparison.Ordinal), raw, default);
    }

    private static JsonElement TryParse(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);

            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>The published catalogue, asserted usable.</summary>
    protected async Task<List<Tool>> ToolsAsync()
    {
        var reply = await ListAsync();
        reply.Status.Should().Be(200, $"GET /list answered {reply}");
        reply.Json.TryGetProperty("tools", out var tools).Should().BeTrue(reply.ToString());

        return [.. tools.EnumerateArray().Select(tool => new Tool(
            tool.GetProperty("name").GetString()!,
            tool.GetProperty("description").GetString()!,
            tool.GetProperty("inputSchema").Clone(),
            tool.GetProperty("requireApproval").GetBoolean()))];
    }

    /// <summary>
    /// Skips the test when the portal's quota lacks <c>automationapi</c>, the feature without which ASC.AI publishes no
    /// document generators (the standalone test portals do not have it). The decision is read from the quota, not from
    /// the catalogue the tests then check: on a portal that has the feature, a catalogue without the generators fails.
    /// </summary>
    protected async Task RequireGeneratorsAsync()
    {
        await WebApiHttpClient.Authenticate(Owner);
        var quota = (await QuotaApi.GetPortalQuotaAsync(TestContext.Current.CancellationToken)).Response;
        Assert.SkipUnless(quota.AutomationApi, "The document generators are not offered on this portal: its quota lacks automationapi (EditorToolsFactory returns no tools).");

        var names = (await ToolsAsync()).Select(t => t.Name).ToList();
        names.Should().Contain(GeneratorTools, "the portal quota has automationapi, so the catalogue publishes every generator");
    }

    /// <summary>
    /// Skips the test when the owner's catalogue lacks the DocSpace REST tools, which the onlyoffice-docspace-mcp
    /// container publishes and the integration-test AppHost does not start.
    /// </summary>
    protected async Task RequireRestToolsAsync()
    {
        var names = (await ToolsAsync()).Select(t => t.Name).ToList();

        Assert.SkipWhen(!names.Contains("get_room_types"), "The DocSpace REST tools are not published: the onlyoffice-docspace-mcp container is not part of the integration-test profile.");
    }

    protected async Task<User> ActAsAsync(EmployeeType type)
    {
        var user = type == EmployeeType.Guest
            ? await InviteGuest(TestContext.Current.CancellationToken)
            : await InviteContact(type, TestContext.Current.CancellationToken);

        await _aiChatClient.Authenticate(user);
        await FilesHttpClient.Authenticate(user);

        return user;
    }

    protected async Task ActAsOwnerAsync()
    {
        await _aiChatClient.Authenticate(Owner);
        await FilesHttpClient.Authenticate(Owner);
    }

    /// <summary>The "My documents" folder of whoever the Files client is signed in as.</summary>
    protected async Task<int> MyFolderIdAsync() =>
        (await FoldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;

    /// <summary>Titles of the files in a folder, as whoever the Files client is signed in as.</summary>
    protected async Task<List<string>> TitlesInAsync(int folderId) =>
        [.. (await FoldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken))
            .Response.Files.OfType<FileDto>().Select(f => f.Title)];

    protected async Task<int> CreateFileAsync(string title) =>
        (await FilesApi.CreateFileInMyDocumentsAsync(new CreateFileRequest(title), TestContext.Current.CancellationToken)).Response.Id;

    protected async Task<int> CreateCustomRoomAsync(string title)
    {
        var room = await RoomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title, roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken);

        return room.Response.Id;
    }

    protected static string RandomName(string prefix = "Autotest") => $"{prefix} {Guid.NewGuid().ToString("N")[..8]}";

    /// <summary>Arguments a generator accepts: every property of its schema, with the file name replaced.</summary>
    protected static Dictionary<string, object?> GeneratorArguments(Tool tool, string fileName)
    {
        var arguments = SampleArguments(tool.InputSchema, all: true);
        arguments["fileName"] = fileName;

        return arguments;
    }

    private static string TypeOf(JsonElement schema)
    {
        if (schema.TryGetProperty("type", out var type))
        {
            if (type.ValueKind == JsonValueKind.String)
            {
                return type.GetString()!;
            }

            if (type.ValueKind == JsonValueKind.Array)
            {
                var first = type.EnumerateArray().Select(t => t.GetString()).FirstOrDefault(t => t != "null");
                if (first is not null)
                {
                    return first;
                }
            }
        }

        if (schema.TryGetProperty("properties", out _))
        {
            return "object";
        }

        return schema.TryGetProperty("items", out _) ? "array" : "string";
    }

    private static double? NumberOf(JsonElement schema, string name) =>
        schema.TryGetProperty(name, out var value) && value.TryGetDouble(out var number) ? number : null;

    /// <summary>A value that satisfies <paramref name="schema"/> — plain, built to be accepted, not to fuzz.</summary>
    protected static object? SampleValue(JsonElement schema, string salt = "x")
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return "Autotest";
        }

        if (schema.TryGetProperty("enum", out var values) && values.ValueKind == JsonValueKind.Array && values.GetArrayLength() > 0)
        {
            return values[0].Clone();
        }

        if (schema.TryGetProperty("default", out var fallback))
        {
            return fallback.Clone();
        }

        switch (TypeOf(schema))
        {
            case "string":
                if (schema.TryGetProperty("format", out var format) && format.GetString() == "uuid")
                {
                    return "00000000-0000-0000-0000-000000000000";
                }

                var min = (int)(NumberOf(schema, "minLength") ?? 1);
                var text = $"Autotest {salt}";

                return text.Length >= min ? text : text.PadRight(min, 'x');
            case "integer":
            case "number":
                var low = NumberOf(schema, "minimum") ?? 1;
                var high = NumberOf(schema, "maximum") ?? Math.Max(low, 1);

                return (long)Math.Min(Math.Max(1, low), high);
            case "boolean":
                return true;
            case "array":
                // Empty unless the schema demands items: an item enum the schema does not mention would reject any
                // invented string for a reason the schema cannot explain.
                var count = (int)(NumberOf(schema, "minItems") ?? 0);
                var itemSchema = schema.TryGetProperty("items", out var items) ? items : default;

                return Enumerable.Range(0, count).Select(_ => SampleValue(itemSchema, salt)).ToList();
            case "object":
                return SampleArguments(schema, all: false, salt);
            default:
                return "Autotest";
        }
    }

    /// <summary>Arguments for a tool: only the required properties, or every property.</summary>
    protected static Dictionary<string, object?> SampleArguments(JsonElement schema, bool all = false, string salt = "x")
    {
        var arguments = new Dictionary<string, object?>();
        if (!schema.TryGetProperty("properties", out var properties) || properties.ValueKind != JsonValueKind.Object)
        {
            return arguments;
        }

        var required = schema.TryGetProperty("required", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().Select(e => e.GetString()).ToHashSet()
            : [];

        foreach (var property in properties.EnumerateObject())
        {
            if (all || required.Contains(property.Name))
            {
                arguments[property.Name] = SampleValue(property.Value, $"{salt}-{property.Name}");
            }
        }

        return arguments;
    }

    /// <summary>A value of the wrong JSON type for <paramref name="schema"/>, or null when none is obvious.</summary>
    protected static object? WrongTypeValue(JsonElement schema) => TypeOf(schema) switch
    {
        "string" => 12345,
        "integer" or "number" => "not-a-number",
        "boolean" => "not-a-boolean",
        "array" => "not-an-array",
        "object" => "not-an-object",
        _ => null
    };
}
