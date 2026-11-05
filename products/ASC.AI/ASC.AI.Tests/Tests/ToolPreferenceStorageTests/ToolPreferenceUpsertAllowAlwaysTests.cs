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
public class ToolPreferenceUpsertAllowAlwaysTests(AspireAppFixture fixture) : McpServerStorageTests.PublicToolsTestBase(fixture)
{
    [Fact]
    public async Task UpsertAllowAlways_Global_McpServer_PersistsValue()
    {
        await AddCustomServerAsync("server-1");

        var result = await _aiTools.AiToolsSetAllowAlwaysAsync(
            new AiToolsSetAllowAlwaysRequest(serverType: "server-1", toolName: "tool-a", value: true),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();

        (await GetAllowAlwaysAsync()).Should().BeEquivalentTo([Token("server-1", "tool-a")]);
        (await _aiTools.AiToolsIsAllowAlwaysAsync("server-1", "tool-a", cancellationToken: TestContext.Current.CancellationToken)).Should().BeTrue();
    }

    [Fact]
    public async Task UpsertAllowAlways_Global_SystemTools_PersistsValue()
    {
        var result = await _aiTools.AiToolsSetAllowAlwaysAsync(
            new AiToolsSetAllowAlwaysRequest(serverType: WebSearchServerType, toolName: "preview", value: true),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();

        (await GetAllowAlwaysAsync()).Should().BeEquivalentTo([Token(WebSearchServerType, "preview")]);
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task UpsertAllowAlways_WithEntityId_McpServer_PersistsValue()
    {
        var roomId = await CreateRoomAsync();
        await CreateMcpServerInternalAsync("server-1", entityId: roomId.ToString());

        await UpsertAllowAlwaysToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                ["server-1"] = ["tool-a"]
            },
            roomId.ToString());

        var stored = await ReadToolPrefsInternalAsync(roomId.ToString());
        stored["server-1"].AllowAlways.Should().BeEquivalentTo(["tool-a"]);
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task UpsertAllowAlways_WithEntityId_SystemTools_PersistsValue()
    {
        var roomId = await CreateRoomAsync();

        await UpsertAllowAlwaysToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["preview", "download"]
            },
            roomId.ToString());

        var stored = await ReadToolPrefsInternalAsync(roomId.ToString());
        stored[SystemToolsServerType].AllowAlways.Should().BeEquivalentTo(["preview", "download"]);
    }

    // The public write toggles one tool per call; revoking one and granting others leaves exactly the new set.
    [Fact]
    public async Task UpsertAllowAlways_Twice_UpdatesValue()
    {
        await SetAllowAlwaysAsync(WebSearchServerType, "preview");

        await SetAllowAlwaysAsync(WebSearchServerType, "preview", false);
        await SetAllowAlwaysAsync(WebSearchServerType, "download");
        await SetAllowAlwaysAsync(WebSearchServerType, "upload");

        (await GetAllowAlwaysAsync()).Should().BeEquivalentTo([Token(WebSearchServerType, "download"), Token(WebSearchServerType, "upload")]);
        (await _aiTools.AiToolsIsAllowAlwaysAsync(WebSearchServerType, "preview", cancellationToken: TestContext.Current.CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task UpsertAllowAlways_DoesNotOverwriteDisabled()
    {
        await SetDisabledAsync(WebSearchServerType, "search");

        await SetAllowAlwaysAsync(WebSearchServerType, "preview");

        (await GetDisabledAsync()).Should().ContainKey(WebSearchServerType).WhoseValue.Should().BeEquivalentTo(["search"]);
        (await GetAllowAlwaysAsync()).Should().BeEquivalentTo([Token(WebSearchServerType, "preview")]);
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task UpsertAllowAlways_Global_DoesNotAffectScoped()
    {
        var roomId = await CreateRoomAsync();
        await UpsertAllowAlwaysToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["scoped-tool"]
            },
            roomId.ToString());

        await UpsertAllowAlwaysToolPrefsInternalAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["global-tool"]
        });

        var scoped = await ReadToolPrefsInternalAsync(roomId.ToString());
        scoped[SystemToolsServerType].AllowAlways.Should().BeEquivalentTo(["scoped-tool"]);
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task UpsertAllowAlways_WithEntityId_DoesNotAffectGlobal()
    {
        var roomId = await CreateRoomAsync();
        await UpsertAllowAlwaysToolPrefsInternalAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["global-tool"]
        });

        await UpsertAllowAlwaysToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["scoped-tool"]
            },
            roomId.ToString());

        var global = await ReadToolPrefsInternalAsync();
        global[SystemToolsServerType].AllowAlways.Should().BeEquivalentTo(["global-tool"]);
    }

    // Internal contract: per-entity tool-pref scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task UpsertAllowAlways_TwoDifferentEntities_AreIsolated()
    {
        var firstRoomId = await CreateRoomAsync();
        var secondRoomId = await CreateRoomAsync();

        await UpsertAllowAlwaysToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["tool-a"]
            },
            firstRoomId.ToString());
        await UpsertAllowAlwaysToolPrefsInternalAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["tool-b"]
            },
            secondRoomId.ToString());

        var first = await ReadToolPrefsInternalAsync(firstRoomId.ToString());
        var second = await ReadToolPrefsInternalAsync(secondRoomId.ToString());

        first[SystemToolsServerType].AllowAlways.Should().BeEquivalentTo(["tool-a"]);
        second[SystemToolsServerType].AllowAlways.Should().BeEquivalentTo(["tool-b"]);
    }

    // The public write takes one server type per call; granting under a second one must keep the first.
    [Fact]
    public async Task UpsertAllowAlways_MultipleServerTypes_AllPersisted()
    {
        await AddCustomServerAsync("server-1");

        await SetAllowAlwaysAsync("server-1", "mcp-tool");
        await SetAllowAlwaysAsync(WebSearchServerType, "preview");

        (await GetAllowAlwaysAsync()).Should().BeEquivalentTo([Token("server-1", "mcp-tool"), Token(WebSearchServerType, "preview")]);
    }

    // Revoking a tool that was never granted changes nothing.
    [Fact]
    public async Task UpsertAllowAlways_RevokeNotGranted_NoOp()
    {
        await SetAllowAlwaysAsync(WebSearchServerType, "preview", false);

        (await GetAllowAlwaysAsync()).Should().BeEmpty();
    }

    // Internal contract: an empty allow-always map has no public route (set-allow-always toggles one tool, and a
    // revoke of an ungranted tool writes nothing).
    [Fact]
    public async Task UpsertAllowAlways_EmptyDictionary_Returns204_NoOp()
    {
        using var response = await _ai.PutAsync(
            $"{ToolPrefsPath}/allow-always",
            new { allowAlways = new Dictionary<string, HashSet<string>>() },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var stored = await ReadToolPrefsInternalAsync();
        stored.Should().BeEmpty();
    }

    // Internal contract: an empty stored set per server type has no public route (set-allow-always toggles one tool);
    // the chat service relies on this to clear a server type it drops from the map.
    [Fact]
    public async Task UpsertAllowAlways_EmptyToolSetForServerType_IsPersisted()
    {
        await UpsertAllowAlwaysToolPrefsInternalAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = []
        });

        var stored = await ReadToolPrefsInternalAsync();
        stored.Should().ContainKey(SystemToolsServerType);
        stored[SystemToolsServerType].AllowAlways.Should().BeEmpty();
    }

    [Fact]
    public async Task UpsertAllowAlways_NonExistentEntityId_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiTools.AiToolsSetAllowAlwaysAsync(
                new AiToolsSetAllowAlwaysRequest(serverType: WebSearchServerType, toolName: "preview", value: true, entityId: MissingEntityId),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);

        (await GetAllowAlwaysAsync()).Should().BeEmpty();
    }
}
