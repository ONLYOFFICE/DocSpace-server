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

[Trait("Category", "CRUD")]
[Trait("Feature", "AI/McpServers")]
public class McpServerReplaceTests(AspireAppFixture fixture) : PublicToolsTestBase(fixture)
{
    private async Task ReplaceAllAsync(Dictionary<string, object> map, string? entityId = null)
    {
        var result = await _aiTools.AiToolsReplaceAllCustomServersAsync(
            new AiToolsReplaceAllCustomServersRequest(map: map, entityId: entityId!),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue($"replace-all-custom-servers rejected the map: {string.Join("; ", result.Errors?.Select(e => $"{e.Name}: {e.Error?.Message}") ?? [])}");
    }

    [Fact]
    public async Task ReplaceAll_InsertsNewRows()
    {
        await ReplaceAllAsync(new Dictionary<string, object>
        {
            ["server-1"] = McpConfig("https://example.com/first"),
            ["server-2"] = McpConfig("https://example.com/second")
        });

        var all = await ListCustomServersAsync();
        all.Should().HaveCount(2);
        ConfigEquals(all["server-1"], "https://example.com/first").Should().BeTrue();
        ConfigEquals(all["server-2"], "https://example.com/second").Should().BeTrue();
    }

    [Fact]
    public async Task ReplaceAll_UpdatesExistingRows()
    {
        await AddCustomServerAsync("server-1", "https://example.com/old");

        await ReplaceAllAsync(new Dictionary<string, object>
        {
            ["server-1"] = McpConfig("https://example.com/new"),
            ["server-2"] = McpConfig("https://example.com/added")
        });

        var all = await ListCustomServersAsync();
        all.Should().HaveCount(2);
        ConfigEquals(all["server-1"], "https://example.com/new").Should().BeTrue();
        ConfigEquals(all["server-2"], "https://example.com/added").Should().BeTrue();
    }

    // Internal contract: per-entity MCP server scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task ReplaceAll_WithEntityId_IsolatedFromGlobal()
    {
        var roomId = await CreateRoomAsync();
        var globalConfig = BuildMcpConfig("https://example.com/global");
        var scopedConfig = BuildMcpConfig("https://example.com/scoped");

        await CreateMcpServerInternalAsync("server-1", globalConfig);

        using var response = await _ai.PutAsync(
            McpServersPath,
            new
            {
                servers = new Dictionary<string, string>
                {
                    ["server-1"] = scopedConfig
                },
                entityId = roomId.ToString()
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        JsonEquals((await ReadMcpServerInternalAsync("server-1")).Config, globalConfig).Should().BeTrue();
        JsonEquals((await ReadMcpServerInternalAsync("server-1", roomId.ToString())).Config, scopedConfig).Should().BeTrue();

        (await ReadAllMcpServersInternalAsync()).Should().ContainSingle().Which.Name.Should().Be("server-1");
        (await ReadAllMcpServersInternalAsync(roomId.ToString())).Should().ContainSingle().Which.Name.Should().Be("server-1");
    }

    [Fact]
    public async Task ReplaceAll_EmptyDict_ClearsAll()
    {
        await AddCustomServerAsync("server-1");

        await ReplaceAllAsync(new Dictionary<string, object>());

        (await ListCustomServersAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ReplaceAll_RemovesServersAbsentFromNewSet()
    {
        await AddCustomServerAsync("server-1");
        await AddCustomServerAsync("server-2");

        await ReplaceAllAsync(new Dictionary<string, object>
        {
            ["server-1"] = McpConfig("https://example.com/kept")
        });

        var all = await ListCustomServersAsync();
        all.Should().ContainSingle().Which.Key.Should().Be("server-1");
        ConfigEquals(all["server-1"], "https://example.com/kept").Should().BeTrue();
    }

    [Fact]
    [Trait("Bug", "84406")]
    public async Task ReplaceAll_RemovesToolPrefsOfDroppedServers()
    {
        await AddCustomServerAsync("kept");
        await AddCustomServerAsync("dropped");

        await SetDisabledAsync("kept", "t");
        await SetDisabledAsync("dropped", "t");
        await SetDisabledAsync(WebSearchServerType, "search");

        await SetAllowAlwaysAsync("kept", "t");
        await SetAllowAlwaysAsync("dropped", "t");
        await SetAllowAlwaysAsync(WebSearchServerType, "preview");

        await ReplaceAllAsync(new Dictionary<string, object>
        {
            ["kept"] = McpConfig()
        });

        var disabled = await GetDisabledAsync();
        disabled.Should().NotContainKey("dropped");
        disabled["kept"].Should().BeEquivalentTo(["t"]);
        disabled[WebSearchServerType].Should().BeEquivalentTo(["search"]);

        (await GetAllowAlwaysAsync()).Should().BeEquivalentTo([Token("kept", "t"), Token(WebSearchServerType, "preview")]);

        // A server registered again under the dropped name starts without the old preferences.
        await AddCustomServerAsync("dropped");

        (await GetDisabledAsync()).Should().NotContainKey("dropped");
        (await GetAllowAlwaysAsync()).Should().NotContain(Token("dropped", "t"));
    }

    // Internal contract: per-entity MCP server and tool-pref scopes have no public route (the chat service always stores under entityId null).
    [Fact]
    [Trait("Bug", "84406")]
    public async Task ReplaceAll_WithEntityId_RemovesOnlyScopedToolPrefsOfDroppedServers()
    {
        var roomId = await CreateRoomAsync();
        var entityId = roomId.ToString();

        await CreateMcpServerInternalAsync("dropped");
        await UpsertAllowAlwaysToolPrefsInternalAsync(new Dictionary<string, HashSet<string>>
        {
            ["dropped"] = ["global-tool"]
        });

        await CreateMcpServerInternalAsync("kept", entityId: entityId);
        await CreateMcpServerInternalAsync("dropped", entityId: entityId);
        await UpsertDisabledToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                ["kept"] = ["t"],
                ["dropped"] = ["t"]
            },
            entityId);
        await UpsertAllowAlwaysToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                ["kept"] = ["t"],
                ["dropped"] = ["t"]
            },
            entityId);

        using var response = await _ai.PutAsync(
            McpServersPath,
            new
            {
                servers = new Dictionary<string, string>
                {
                    ["kept"] = BuildMcpConfig()
                },
                entityId
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var scoped = await ReadToolPrefsInternalAsync(entityId);
        scoped.Should().NotContainKey("dropped");
        scoped["kept"].Disabled.Should().BeEquivalentTo(["t"]);
        scoped["kept"].AllowAlways.Should().BeEquivalentTo(["t"]);

        var global = await ReadToolPrefsInternalAsync();
        global["dropped"].AllowAlways.Should().BeEquivalentTo(["global-tool"]);

        await CreateMcpServerInternalAsync("dropped", entityId: entityId);

        (await ReadToolPrefsInternalAsync(entityId)).Should().NotContainKey("dropped");
    }

    [Fact]
    public async Task ReplaceAll_NonExistentEntityId_Returns404()
    {
        await AddCustomServerAsync("server-1");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiTools.AiToolsReplaceAllCustomServersAsync(
                new AiToolsReplaceAllCustomServersRequest(
                    map: new Dictionary<string, object> { ["server-2"] = McpConfig() },
                    entityId: MissingEntityId),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);

        // Bug 82975: an unreachable entityId must not fall back to the portal map and replace it.
        (await ListCustomServersAsync()).Should().ContainSingle().Which.Key.Should().Be("server-1");
    }
}
