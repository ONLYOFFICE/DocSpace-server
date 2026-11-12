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

namespace ASC.AI.Tests.Tests.McpServerStorageTests;

/// <summary>
/// Helpers for the public tools surface (<c>/api/2.0/ai/tools/*</c>, served by the Node chat service), shared by the
/// MCP server and the tool preference suites.
/// </summary>
/// <remarks>
/// Two facts of the public surface shape these suites. The custom MCP server registry and the tool preferences are
/// one map per portal (per user for the preferences): a supplied <c>entityId</c> must name a reachable folder
/// (Bug 82975) but is otherwise ignored (Bug 82863), so the per-entity scoping of the internal storage has no public
/// route. And the set-disabled write only accepts a server type the round's tool filter can match (Bug 83013), so the
/// legacy system-tools GUID of the internal tests is replaced by the web-search group here.
/// </remarks>
public abstract class PublicToolsTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const string ToolsPath = "/api/2.0/ai/tools";

    /// <summary>A built-in server type the set-disabled write always accepts.</summary>
    protected const string WebSearchServerType = "web-search";

    /// <summary>A second built-in server type, for cases that need two of them.</summary>
    protected const string ImageGenerationServerType = "image-generation";

    /// <summary>A folder id no portal has; the public writes answer 404 for it.</summary>
    protected const string MissingEntityId = "999999999";

    /// <summary>The DocSpace user list of disabled tools, the one the chat reads (no tenant list on a fresh portal).</summary>
    protected const string UserScope = "user";

    protected static string DefaultMcpUrl(string? url) => url ?? "https://example.com/mcp";

    /// <summary>The HTTP transport config a custom server is registered with; nothing dials it on add.</summary>
    protected static Dictionary<string, object> McpConfig(string? url = null) =>
        new()
        {
            ["transport"] = "http",
            ["url"] = DefaultMcpUrl(url)
        };

    /// <summary>
    /// Compares a config the SDK returned with the config built for <paramref name="url"/>. The SDK types a server
    /// config as <c>Object</c> (a free-form JSON object, a Newtonsoft token at runtime), so it is compared as JSON text.
    /// </summary>
    protected static bool ConfigEquals(object? config, string? url = null) =>
        config is not null && JsonEquals(config.ToString(), BuildMcpConfig(DefaultMcpUrl(url)));

    protected async Task AddCustomServerAsync(string name, string? url = null, string? entityId = null)
    {
        var result = await _aiTools.AiToolsAddCustomServerAsync(
            new AiToolsAddCustomServerRequest(name: name, config: McpConfig(url), entityId: entityId!),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue($"add-custom-server rejected {name}: {result.Error?.Message}");
    }

    protected Task<object> GetCustomServerAsync(string name) =>
        _aiTools.AiToolsGetCustomServerAsync(name, cancellationToken: TestContext.Current.CancellationToken);

    protected Task<Dictionary<string, object>> ListCustomServersAsync() =>
        _aiTools.AiToolsListCustomServersAsync(cancellationToken: TestContext.Current.CancellationToken);

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

    protected Task<Dictionary<string, List<string>>> GetDisabledAsync() =>
        _aiTools.AiToolsGetDisabledAsync(UserScope, cancellationToken: TestContext.Current.CancellationToken);

    protected Task<List<string>> GetAllowAlwaysAsync() =>
        _aiTools.AiToolsGetAllowAlwaysAsync(cancellationToken: TestContext.Current.CancellationToken);

    /// <summary>The allow-always token the public read returns for one tool: <c>{serverType}_{toolName}</c>.</summary>
    protected static string Token(string serverType, string toolName) => $"{serverType}_{toolName}";
}
