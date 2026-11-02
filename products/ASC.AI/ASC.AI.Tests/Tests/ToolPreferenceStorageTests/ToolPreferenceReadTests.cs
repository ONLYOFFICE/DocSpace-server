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

namespace ASC.AI.Tests.Tests.ToolPreferenceStorageTests;

[Trait("Category", "CRUD")]
[Trait("Feature", "AI/ToolPreferences")]
public class ToolPreferenceReadTests(AspireAppFixture fixture) : McpServerStorageTests.PublicToolsTestBase(fixture)
{
    [Fact]
    public async Task Read_Global_NoneStored_ReturnsEmpty()
    {
        (await GetDisabledAsync()).Should().BeEmpty();
        (await GetAllowAlwaysAsync()).Should().BeEmpty();
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task Read_WithEntityId_NoneStored_ReturnsEmpty()
    {
        var roomId = await CreateRoomAsync();

        var prefs = await ReadToolPrefsInternalAsync(roomId.ToString());

        prefs.Should().BeEmpty();
    }

    // The public reads accept an entityId and ignore it (Bug 82863): from a room the caller sees the same list.
    [Fact]
    public async Task Read_WithAccessibleEntityId_ReturnsUserList()
    {
        var roomId = await CreateRoomAsync();
        await SetDisabledAsync(WebSearchServerType, "search");
        await SetAllowAlwaysAsync(WebSearchServerType, "preview");

        var disabled = await _aiTools.AiToolsGetDisabledAsync(UserScope, roomId.ToString(), TestContext.Current.CancellationToken);
        var allowAlways = await _aiTools.AiToolsGetAllowAlwaysAsync(roomId.ToString(), TestContext.Current.CancellationToken);

        disabled.Should().ContainKey(WebSearchServerType).WhoseValue.Should().BeEquivalentTo(["search"]);
        allowAlways.Should().BeEquivalentTo([Token(WebSearchServerType, "preview")]);
    }

    [Fact]
    public async Task Read_Global_McpServerType_ReturnsValue()
    {
        await AddCustomServerAsync("server-1");
        await SetDisabledAsync("server-1", "tool-a", "tool-b");

        var disabled = await GetDisabledAsync();

        disabled.Should().ContainKey("server-1").WhoseValue.Should().BeEquivalentTo(["tool-a", "tool-b"]);
        (await GetAllowAlwaysAsync()).Should().NotContain(t => t.StartsWith("server-1_", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Read_Global_SystemToolsServerType_ReturnsValue()
    {
        await SetDisabledAsync(WebSearchServerType, "search", "preview");

        var disabled = await GetDisabledAsync();

        disabled.Should().ContainKey(WebSearchServerType).WhoseValue.Should().BeEquivalentTo(["search", "preview"]);
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task Read_WithEntityId_McpServerType_ReturnsScopedValue()
    {
        var roomId = await CreateRoomAsync();
        await CreateMcpServerInternalAsync("server-1", entityId: roomId.ToString());
        await UpsertDisabledToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                ["server-1"] = ["tool-a"]
            },
            roomId.ToString());

        var prefs = await ReadToolPrefsInternalAsync(roomId.ToString());

        prefs.Should().ContainKey("server-1");
        prefs["server-1"].Disabled.Should().BeEquivalentTo(["tool-a"]);
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task Read_WithEntityId_SystemToolsServerType_ReturnsScopedValue()
    {
        var roomId = await CreateRoomAsync();
        await UpsertDisabledToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["search"]
            },
            roomId.ToString());

        var prefs = await ReadToolPrefsInternalAsync(roomId.ToString());

        prefs.Should().ContainKey(SystemToolsServerType);
        prefs[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["search"]);
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task Read_Global_NotAffectedByScoped()
    {
        var roomId = await CreateRoomAsync();
        await UpsertDisabledToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["search"]
            },
            roomId.ToString());

        var global = await ReadToolPrefsInternalAsync();

        global.Should().BeEmpty();
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task Read_WithEntityId_NotAffectedByGlobal()
    {
        var roomId = await CreateRoomAsync();
        await UpsertDisabledToolPrefsInternalAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["search"]
        });

        var scoped = await ReadToolPrefsInternalAsync(roomId.ToString());

        scoped.Should().BeEmpty();
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task Read_GlobalAndScoped_StoreDifferentValues()
    {
        var roomId = await CreateRoomAsync();

        await UpsertDisabledToolPrefsInternalAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["search", "preview"]
        });
        await UpsertDisabledToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["upload"]
            },
            roomId.ToString());

        var global = await ReadToolPrefsInternalAsync();
        var scoped = await ReadToolPrefsInternalAsync(roomId.ToString());

        global[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["search", "preview"]);
        scoped[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["upload"]);
    }

    [Fact]
    public async Task Read_Global_McpAndSystemToolsTogether()
    {
        await AddCustomServerAsync("server-1");
        await SetDisabledAsync("server-1", "mcp-tool");
        await SetDisabledAsync(WebSearchServerType, "search");

        var disabled = await GetDisabledAsync();

        disabled.Should().HaveCount(2);
        disabled["server-1"].Should().BeEquivalentTo(["mcp-tool"]);
        disabled[WebSearchServerType].Should().BeEquivalentTo(["search"]);
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task Read_WithEntityId_McpAndSystemToolsTogether()
    {
        var roomId = await CreateRoomAsync();
        await CreateMcpServerInternalAsync("server-1", entityId: roomId.ToString());

        await UpsertDisabledToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                ["server-1"] = ["mcp-tool"],
                [SystemToolsServerType] = ["search"]
            },
            roomId.ToString());

        var prefs = await ReadToolPrefsInternalAsync(roomId.ToString());

        prefs.Should().HaveCount(2);
        prefs["server-1"].Disabled.Should().BeEquivalentTo(["mcp-tool"]);
        prefs[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["search"]);
    }

    [Fact]
    public async Task Read_DisabledAndAllowAlwaysForSameServerType_BothReturned()
    {
        await SetDisabledAsync(WebSearchServerType, "search");
        await SetAllowAlwaysAsync(WebSearchServerType, "preview");

        (await GetDisabledAsync()).Should().ContainKey(WebSearchServerType).WhoseValue.Should().BeEquivalentTo(["search"]);
        (await GetAllowAlwaysAsync()).Should().BeEquivalentTo([Token(WebSearchServerType, "preview")]);

        var isDisabled = await _aiTools.AiToolsIsToolDisabledAsync(WebSearchServerType, "search", cancellationToken: TestContext.Current.CancellationToken);
        var isAllowed = await _aiTools.AiToolsIsAllowAlwaysAsync(WebSearchServerType, "preview", cancellationToken: TestContext.Current.CancellationToken);

        isDisabled.Should().BeTrue();
        isAllowed.Should().BeTrue();
    }

    // Internal contract: the 404 for an unknown entityId on a read is the AI service's; the public reads ignore entityId (Bug 82863) and check it on writes only.
    [Fact]
    public async Task Read_NonExistentEntityId_Returns404()
    {
        using var response = await _ai.GetAsync(
            $"{ToolPrefsPath}?entityId=999999999",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
