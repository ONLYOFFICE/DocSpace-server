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
public class ToolPreferenceUpsertDisabledTests(AspireAppFixture fixture) : McpServerStorageTests.PublicToolsTestBase(fixture)
{
    [Fact]
    public async Task UpsertDisabled_Global_McpServer_PersistsValue()
    {
        await AddCustomServerAsync("server-1");

        var result = await _aiTools.AiToolsSetDisabledAsync(
            new AiToolsSetDisabledRequest(serverType: "server-1", toolNames: ["tool-a", "tool-b"]),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();

        (await GetDisabledAsync()).Should().ContainKey("server-1").WhoseValue.Should().BeEquivalentTo(["tool-a", "tool-b"]);
    }

    [Fact]
    public async Task UpsertDisabled_Global_SystemTools_PersistsValue()
    {
        var result = await _aiTools.AiToolsSetDisabledAsync(
            new AiToolsSetDisabledRequest(serverType: WebSearchServerType, toolNames: ["search"]),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();

        (await GetDisabledAsync()).Should().ContainKey(WebSearchServerType).WhoseValue.Should().BeEquivalentTo(["search"]);
    }

    // A server type the round's tool filter cannot match is refused rather than stored inert (Bug 83013), the
    // legacy system-tools GUID of the internal storage tests among them.
    [Fact]
    public async Task UpsertDisabled_UnknownServerType_Returns400()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiTools.AiToolsSetDisabledAsync(
                new AiToolsSetDisabledRequest(serverType: SystemToolsServerType, toolNames: ["search"]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);

        (await GetDisabledAsync()).Should().BeEmpty();
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task UpsertDisabled_WithEntityId_McpServer_PersistsValue()
    {
        var roomId = await CreateRoomAsync();
        await CreateMcpServerInternalAsync("server-1", entityId: roomId.ToString());

        await UpsertDisabledToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                ["server-1"] = ["tool-a"]
            },
            roomId.ToString());

        var stored = await ReadToolPrefsInternalAsync(roomId.ToString());
        stored["server-1"].Disabled.Should().BeEquivalentTo(["tool-a"]);
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task UpsertDisabled_WithEntityId_SystemTools_PersistsValue()
    {
        var roomId = await CreateRoomAsync();

        await UpsertDisabledToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["search", "preview"]
            },
            roomId.ToString());

        var stored = await ReadToolPrefsInternalAsync(roomId.ToString());
        stored[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["search", "preview"]);
    }

    [Fact]
    public async Task UpsertDisabled_Twice_UpdatesValue()
    {
        await SetDisabledAsync(WebSearchServerType, "search");
        await SetDisabledAsync(WebSearchServerType, "upload", "delete");

        (await GetDisabledAsync()).Should().ContainKey(WebSearchServerType).WhoseValue.Should().BeEquivalentTo(["upload", "delete"]);
    }

    [Fact]
    public async Task UpsertDisabled_DoesNotOverwriteAllowAlways()
    {
        await SetAllowAlwaysAsync(WebSearchServerType, "preview");

        await SetDisabledAsync(WebSearchServerType, "search");

        (await GetDisabledAsync()).Should().ContainKey(WebSearchServerType).WhoseValue.Should().BeEquivalentTo(["search"]);
        (await GetAllowAlwaysAsync()).Should().BeEquivalentTo([Token(WebSearchServerType, "preview")]);
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task UpsertDisabled_Global_DoesNotAffectScoped()
    {
        var roomId = await CreateRoomAsync();
        await UpsertDisabledToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["scoped-tool"]
            },
            roomId.ToString());

        await UpsertDisabledToolPrefsInternalAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["global-tool"]
        });

        var scoped = await ReadToolPrefsInternalAsync(roomId.ToString());
        scoped[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["scoped-tool"]);
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task UpsertDisabled_WithEntityId_DoesNotAffectGlobal()
    {
        var roomId = await CreateRoomAsync();
        await UpsertDisabledToolPrefsInternalAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["global-tool"]
        });

        await UpsertDisabledToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["scoped-tool"]
            },
            roomId.ToString());

        var global = await ReadToolPrefsInternalAsync();
        global[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["global-tool"]);
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task UpsertDisabled_TwoDifferentEntities_AreIsolated()
    {
        var firstRoomId = await CreateRoomAsync();
        var secondRoomId = await CreateRoomAsync();

        await UpsertDisabledToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["tool-a"]
            },
            firstRoomId.ToString());
        await UpsertDisabledToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["tool-b"]
            },
            secondRoomId.ToString());

        var first = await ReadToolPrefsInternalAsync(firstRoomId.ToString());
        var second = await ReadToolPrefsInternalAsync(secondRoomId.ToString());

        first[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["tool-a"]);
        second[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["tool-b"]);
    }

    // The public write takes one server type per call; writing a second one must keep the first.
    [Fact]
    public async Task UpsertDisabled_MultipleServerTypes_AllPersisted()
    {
        await AddCustomServerAsync("server-1");

        await SetDisabledAsync("server-1", "mcp-tool");
        await SetDisabledAsync(WebSearchServerType, "search");

        var disabled = await GetDisabledAsync();
        disabled.Should().HaveCount(2);
        disabled["server-1"].Should().BeEquivalentTo(["mcp-tool"]);
        disabled[WebSearchServerType].Should().BeEquivalentTo(["search"]);
    }

    [Fact]
    public async Task UpsertDisabled_EmptyList_NothingStored_NoOp()
    {
        await SetDisabledAsync(WebSearchServerType);

        (await GetDisabledAsync()).Should().BeEmpty();
    }

    // An explicit empty list is the documented "switch everything back on": the server type leaves the map, the
    // other server types stay.
    [Fact]
    public async Task UpsertDisabled_EmptyList_ClearsServerType()
    {
        await SetDisabledAsync(WebSearchServerType, "search");
        await SetDisabledAsync(ImageGenerationServerType, "generate");

        await SetDisabledAsync(WebSearchServerType);

        var disabled = await GetDisabledAsync();
        disabled.Should().NotContainKey(WebSearchServerType);
        disabled.Should().ContainKey(ImageGenerationServerType).WhoseValue.Should().BeEquivalentTo(["generate"]);
    }

    // Internal contract: an empty stored set per server type has no public route (an empty public list removes the
    // server type); the chat service relies on this to clear a server type it drops from the map.
    [Fact]
    public async Task UpsertDisabled_EmptyToolSetForServerType_IsPersisted()
    {
        await UpsertDisabledToolPrefsInternalAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = []
        });

        var stored = await ReadToolPrefsInternalAsync();
        stored.Should().ContainKey(SystemToolsServerType);
        stored[SystemToolsServerType].Disabled.Should().BeEmpty();
    }

    [Fact]
    public async Task UpsertDisabled_NonExistentEntityId_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiTools.AiToolsSetDisabledAsync(
                new AiToolsSetDisabledRequest(serverType: WebSearchServerType, toolNames: ["search"], entityId: MissingEntityId),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);

        (await GetDisabledAsync()).Should().BeEmpty();
    }
}
