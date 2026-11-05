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

/// <summary>
/// A live MCP server (JSON-RPC 2.0 over streamable HTTP, stateless) run in the test process. The TypeScript suite
/// registered the QA team's calculator server for these cases; this stands in for it so a registered server can be
/// dialled for real: the Node chat service reaches the loopback address because the integration-test profile sets
/// <c>AI__ALLOW_PRIVATE_BASE_URL</c> and the MCP url is not otherwise guarded.
/// </summary>
/// <remarks>
/// It advertises <see cref="CalculateTool"/> and <see cref="GetTimeTool"/>, answers <c>initialize</c>, the
/// <c>notifications/initialized</c> handshake, <c>tools/list</c> and <c>tools/call</c>, and records every JSON-RPC
/// method it receives so a test can tell "the server was dialled" from "it was only registered".
/// </remarks>
public sealed class StubMcpServer : IDisposable
{
    public const string CalculateTool = "calculate";
    public const string GetTimeTool = "get_time";
    public const string FixedTime = "2026-01-02T03:04:05Z";

    private readonly WireMockServer _server = WireMockServer.Start();
    private readonly ConcurrentQueue<(string Method, string? Tool)> _received = new();

    public StubMcpServer()
    {
        _server
            .Given(Request.Create().WithPath("/mcp").UsingPost())
            .RespondWith(Response.Create().WithCallback(request => Handle(request.Body ?? string.Empty)));

        // Session termination is best effort on the client side: a server may answer 405.
        _server
            .Given(Request.Create().WithPath("/mcp").UsingDelete())
            .RespondWith(Response.Create().WithStatusCode(405));
    }

    /// <summary>The <c>url</c> a server config is registered with.</summary>
    public string Url => $"http://127.0.0.1:{_server.Port}/mcp";

    /// <summary>The tools the server advertises, in the order it lists them.</summary>
    public IReadOnlyList<string> ToolNames { get; } = [CalculateTool, GetTimeTool];

    /// <summary>Every JSON-RPC method received so far, oldest first.</summary>
    public IReadOnlyList<string> ReceivedMethods => [.. _received.Select(r => r.Method)];

    /// <summary>How many times <paramref name="tool"/> was actually called (<c>tools/call</c>).</summary>
    public int CallsOf(string tool) => _received.Count(r => r.Method == "tools/call" && r.Tool == tool);

    /// <summary>Whether the server received any request at all.</summary>
    public bool WasDialled => _server.LogEntries.Any();

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }

    private ResponseMessage Handle(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var method = root.TryGetProperty("method", out var m) ? m.GetString() ?? string.Empty : string.Empty;
        var parameters = root.TryGetProperty("params", out var p) ? p : default;
        string? tool = null;

        if (method == "tools/call" && parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("name", out var name))
        {
            tool = name.GetString();
        }

        _received.Enqueue((method, tool));

        // A notification carries no id and gets no body.
        if (!root.TryGetProperty("id", out var id))
        {
            return Reply(202, string.Empty);
        }

        object result = method switch
        {
            "initialize" => new
            {
                protocolVersion = parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("protocolVersion", out var v)
                    ? v.GetString()
                    : "2024-11-05",
                capabilities = new { tools = new { } },
                serverInfo = new { name = "stub-mcp", version = "1.0" }
            },
            "tools/list" => new
            {
                tools = new object[]
                {
                    new
                    {
                        name = CalculateTool,
                        description = "Multiply or add two numbers.",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new Dictionary<string, object>
                            {
                                ["operation"] = new { type = "string" },
                                ["a"] = new { type = "number" },
                                ["b"] = new { type = "number" }
                            }
                        }
                    },
                    new
                    {
                        name = GetTimeTool,
                        description = "The current time.",
                        inputSchema = new { type = "object", properties = new { } }
                    }
                }
            },
            "tools/call" => new
            {
                content = new[] { new { type = "text", text = CallResult(tool, parameters) } },
                isError = false
            },
            _ => new { }
        };

        return Reply(200, JsonSerializer.Serialize(new { jsonrpc = "2.0", id = id.Clone(), result }));
    }

    private static string CallResult(string? tool, JsonElement parameters)
    {
        if (tool == GetTimeTool)
        {
            return FixedTime;
        }

        if (parameters.ValueKind == JsonValueKind.Object
            && parameters.TryGetProperty("arguments", out var arguments)
            && arguments.ValueKind == JsonValueKind.Object
            && arguments.TryGetProperty("a", out var a)
            && arguments.TryGetProperty("b", out var b)
            && a.ValueKind == JsonValueKind.Number
            && b.ValueKind == JsonValueKind.Number)
        {
            var add = arguments.TryGetProperty("operation", out var op) && op.GetString() == "add";

            return (add ? a.GetDouble() + b.GetDouble() : a.GetDouble() * b.GetDouble()).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return "0";
    }

    private static ResponseMessage Reply(int statusCode, string body)
    {
        var message = new ResponseMessage
        {
            StatusCode = statusCode,
            BodyData = new BodyData { BodyAsString = body, DetectedBodyType = BodyType.String }
        };
        message.AddHeader("Content-Type", "application/json");

        return message;
    }
}
