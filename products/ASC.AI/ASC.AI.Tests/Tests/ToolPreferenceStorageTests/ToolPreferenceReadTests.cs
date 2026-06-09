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

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/ToolPreferences")]
public class ToolPreferenceReadTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Read_Global_NoneStored_ReturnsEmpty()
    {
        var prefs = await ReadToolPrefsAsync();

        prefs.Should().BeEmpty();
    }

    [Fact]
    public async Task Read_WithEntityId_NoneStored_ReturnsEmpty()
    {
        var roomId = await CreateRoomAsync();

        var prefs = await ReadToolPrefsAsync(roomId.ToString());

        prefs.Should().BeEmpty();
    }

    [Fact]
    public async Task Read_Global_McpServerType_ReturnsValue()
    {
        await CreateMcpServerAsync("server-1");
        await UpsertDisabledToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            ["server-1"] = ["tool-a", "tool-b"]
        });

        var prefs = await ReadToolPrefsAsync();

        prefs.Should().ContainKey("server-1");
        prefs["server-1"].Disabled.Should().BeEquivalentTo(["tool-a", "tool-b"]);
        prefs["server-1"].AllowAlways.Should().BeNull();
    }

    [Fact]
    public async Task Read_Global_SystemToolsServerType_ReturnsValue()
    {
        await UpsertDisabledToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["search", "preview"]
        });

        var prefs = await ReadToolPrefsAsync();

        prefs.Should().ContainKey(SystemToolsServerType);
        prefs[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["search", "preview"]);
    }

    [Fact]
    public async Task Read_WithEntityId_McpServerType_ReturnsScopedValue()
    {
        var roomId = await CreateRoomAsync();
        await CreateMcpServerAsync("server-1", entityId: roomId.ToString());
        await UpsertDisabledToolPrefsAsync(
            new Dictionary<string, HashSet<string>>
            {
                ["server-1"] = ["tool-a"]
            },
            roomId.ToString());

        var prefs = await ReadToolPrefsAsync(roomId.ToString());

        prefs.Should().ContainKey("server-1");
        prefs["server-1"].Disabled.Should().BeEquivalentTo(["tool-a"]);
    }

    [Fact]
    public async Task Read_WithEntityId_SystemToolsServerType_ReturnsScopedValue()
    {
        var roomId = await CreateRoomAsync();
        await UpsertDisabledToolPrefsAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["search"]
            },
            roomId.ToString());

        var prefs = await ReadToolPrefsAsync(roomId.ToString());

        prefs.Should().ContainKey(SystemToolsServerType);
        prefs[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["search"]);
    }

    [Fact]
    public async Task Read_Global_NotAffectedByScoped()
    {
        var roomId = await CreateRoomAsync();
        await UpsertDisabledToolPrefsAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["search"]
            },
            roomId.ToString());

        var global = await ReadToolPrefsAsync();

        global.Should().BeEmpty();
    }

    [Fact]
    public async Task Read_WithEntityId_NotAffectedByGlobal()
    {
        var roomId = await CreateRoomAsync();
        await UpsertDisabledToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["search"]
        });

        var scoped = await ReadToolPrefsAsync(roomId.ToString());

        scoped.Should().BeEmpty();
    }

    [Fact]
    public async Task Read_GlobalAndScoped_StoreDifferentValues()
    {
        var roomId = await CreateRoomAsync();

        await UpsertDisabledToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["search", "preview"]
        });
        await UpsertDisabledToolPrefsAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["upload"]
            },
            roomId.ToString());

        var global = await ReadToolPrefsAsync();
        var scoped = await ReadToolPrefsAsync(roomId.ToString());

        global[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["search", "preview"]);
        scoped[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["upload"]);
    }

    [Fact]
    public async Task Read_Global_McpAndSystemToolsTogether()
    {
        await CreateMcpServerAsync("server-1");
        await UpsertDisabledToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            ["server-1"] = ["mcp-tool"],
            [SystemToolsServerType] = ["search"]
        });

        var prefs = await ReadToolPrefsAsync();

        prefs.Should().HaveCount(2);
        prefs["server-1"].Disabled.Should().BeEquivalentTo(["mcp-tool"]);
        prefs[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["search"]);
    }

    [Fact]
    public async Task Read_WithEntityId_McpAndSystemToolsTogether()
    {
        var roomId = await CreateRoomAsync();
        await CreateMcpServerAsync("server-1", entityId: roomId.ToString());

        await UpsertDisabledToolPrefsAsync(
            new Dictionary<string, HashSet<string>>
            {
                ["server-1"] = ["mcp-tool"],
                [SystemToolsServerType] = ["search"]
            },
            roomId.ToString());

        var prefs = await ReadToolPrefsAsync(roomId.ToString());

        prefs.Should().HaveCount(2);
        prefs["server-1"].Disabled.Should().BeEquivalentTo(["mcp-tool"]);
        prefs[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["search"]);
    }

    [Fact]
    public async Task Read_DisabledAndAllowAlwaysForSameServerType_BothReturned()
    {
        await UpsertDisabledToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["search"]
        });
        await UpsertAllowAlwaysToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["preview"]
        });

        var prefs = await ReadToolPrefsAsync();

        prefs.Should().ContainKey(SystemToolsServerType);
        prefs[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["search"]);
        prefs[SystemToolsServerType].AllowAlways.Should().BeEquivalentTo(["preview"]);
    }

    [Fact]
    public async Task Read_NonExistentEntityId_Returns404()
    {
        using var response = await Ai.GetAsync(
            $"{ToolPrefsPath}?entityId=999999999",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
