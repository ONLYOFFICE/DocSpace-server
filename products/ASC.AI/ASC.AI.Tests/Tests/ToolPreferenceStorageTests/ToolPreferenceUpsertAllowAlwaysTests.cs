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
public class ToolPreferenceUpsertAllowAlwaysTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task UpsertAllowAlways_Global_McpServer_PersistsValue()
    {
        await CreateMcpServerAsync("server-1");

        using var response = await Ai.PutAsync(
            $"{ToolPrefsPath}/allow-always",
            new
            {
                allowAlways = new Dictionary<string, HashSet<string>>
                {
                    ["server-1"] = ["tool-a"]
                }
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var stored = await ReadToolPrefsAsync();
        stored.Should().ContainKey("server-1");
        stored["server-1"].AllowAlways.Should().BeEquivalentTo(["tool-a"]);
    }

    [Fact]
    public async Task UpsertAllowAlways_Global_SystemTools_PersistsValue()
    {
        using var response = await Ai.PutAsync(
            $"{ToolPrefsPath}/allow-always",
            new
            {
                allowAlways = new Dictionary<string, HashSet<string>>
                {
                    [SystemToolsServerType] = ["preview"]
                }
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var stored = await ReadToolPrefsAsync();
        stored[SystemToolsServerType].AllowAlways.Should().BeEquivalentTo(["preview"]);
    }

    [Fact]
    public async Task UpsertAllowAlways_WithEntityId_McpServer_PersistsValue()
    {
        var roomId = await CreateRoomAsync();
        await CreateMcpServerAsync("server-1", entityId: roomId.ToString());

        await UpsertAllowAlwaysToolPrefsAsync(
            new Dictionary<string, HashSet<string>>
            {
                ["server-1"] = ["tool-a"]
            },
            roomId.ToString());

        var stored = await ReadToolPrefsAsync(roomId.ToString());
        stored["server-1"].AllowAlways.Should().BeEquivalentTo(["tool-a"]);
    }

    [Fact]
    public async Task UpsertAllowAlways_WithEntityId_SystemTools_PersistsValue()
    {
        var roomId = await CreateRoomAsync();

        await UpsertAllowAlwaysToolPrefsAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["preview", "download"]
            },
            roomId.ToString());

        var stored = await ReadToolPrefsAsync(roomId.ToString());
        stored[SystemToolsServerType].AllowAlways.Should().BeEquivalentTo(["preview", "download"]);
    }

    [Fact]
    public async Task UpsertAllowAlways_Twice_UpdatesValue()
    {
        await UpsertAllowAlwaysToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["preview"]
        });
        await UpsertAllowAlwaysToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["download", "upload"]
        });

        var stored = await ReadToolPrefsAsync();
        stored[SystemToolsServerType].AllowAlways.Should().BeEquivalentTo(["download", "upload"]);
    }

    [Fact]
    public async Task UpsertAllowAlways_DoesNotOverwriteDisabled()
    {
        await UpsertDisabledToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["search"]
        });

        await UpsertAllowAlwaysToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["preview"]
        });

        var stored = await ReadToolPrefsAsync();
        stored[SystemToolsServerType].Disabled.Should().BeEquivalentTo(["search"]);
        stored[SystemToolsServerType].AllowAlways.Should().BeEquivalentTo(["preview"]);
    }

    [Fact]
    public async Task UpsertAllowAlways_Global_DoesNotAffectScoped()
    {
        var roomId = await CreateRoomAsync();
        await UpsertAllowAlwaysToolPrefsAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["scoped-tool"]
            },
            roomId.ToString());

        await UpsertAllowAlwaysToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["global-tool"]
        });

        var scoped = await ReadToolPrefsAsync(roomId.ToString());
        scoped[SystemToolsServerType].AllowAlways.Should().BeEquivalentTo(["scoped-tool"]);
    }

    [Fact]
    public async Task UpsertAllowAlways_WithEntityId_DoesNotAffectGlobal()
    {
        var roomId = await CreateRoomAsync();
        await UpsertAllowAlwaysToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["global-tool"]
        });

        await UpsertAllowAlwaysToolPrefsAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["scoped-tool"]
            },
            roomId.ToString());

        var global = await ReadToolPrefsAsync();
        global[SystemToolsServerType].AllowAlways.Should().BeEquivalentTo(["global-tool"]);
    }

    [Fact]
    public async Task UpsertAllowAlways_TwoDifferentEntities_AreIsolated()
    {
        var firstRoomId = await CreateRoomAsync();
        var secondRoomId = await CreateRoomAsync();

        await UpsertAllowAlwaysToolPrefsAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["tool-a"]
            },
            firstRoomId.ToString());
        await UpsertAllowAlwaysToolPrefsAsync(
            new Dictionary<string, HashSet<string>>
            {
                [SystemToolsServerType] = ["tool-b"]
            },
            secondRoomId.ToString());

        var first = await ReadToolPrefsAsync(firstRoomId.ToString());
        var second = await ReadToolPrefsAsync(secondRoomId.ToString());

        first[SystemToolsServerType].AllowAlways.Should().BeEquivalentTo(["tool-a"]);
        second[SystemToolsServerType].AllowAlways.Should().BeEquivalentTo(["tool-b"]);
    }

    [Fact]
    public async Task UpsertAllowAlways_MultipleServerTypesInOneCall_AllPersisted()
    {
        await CreateMcpServerAsync("server-1");

        await UpsertAllowAlwaysToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            ["server-1"] = ["mcp-tool"],
            [SystemToolsServerType] = ["preview"]
        });

        var stored = await ReadToolPrefsAsync();
        stored.Should().HaveCount(2);
        stored["server-1"].AllowAlways.Should().BeEquivalentTo(["mcp-tool"]);
        stored[SystemToolsServerType].AllowAlways.Should().BeEquivalentTo(["preview"]);
    }

    [Fact]
    public async Task UpsertAllowAlways_EmptyDictionary_Returns204_NoOp()
    {
        using var response = await Ai.PutAsync(
            $"{ToolPrefsPath}/allow-always",
            new { allowAlways = new Dictionary<string, HashSet<string>>() },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var stored = await ReadToolPrefsAsync();
        stored.Should().BeEmpty();
    }

    [Fact]
    public async Task UpsertAllowAlways_EmptyToolSetForServerType_IsPersisted()
    {
        await UpsertAllowAlwaysToolPrefsAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = []
        });

        var stored = await ReadToolPrefsAsync();
        stored.Should().ContainKey(SystemToolsServerType);
        stored[SystemToolsServerType].AllowAlways.Should().BeEmpty();
    }

    [Fact]
    public async Task UpsertAllowAlways_NonExistentEntityId_Returns404()
    {
        using var response = await Ai.PutAsync(
            $"{ToolPrefsPath}/allow-always",
            new
            {
                allowAlways = new Dictionary<string, HashSet<string>>
                {
                    [SystemToolsServerType] = ["preview"]
                },
                entityId = "999999999"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
