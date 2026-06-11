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

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/McpServers")]
public class McpServerReplaceTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task ReplaceAll_InsertsNewRows()
    {
        var firstConfig = BuildMcpConfig("https://example.com/first");
        var secondConfig = BuildMcpConfig("https://example.com/second");

        using var response = await Ai.PutAsync(
            McpServersPath,
            new
            {
                servers = new Dictionary<string, string>
                {
                    ["server-1"] = firstConfig,
                    ["server-2"] = secondConfig
                }
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var all = await ReadAllMcpServersAsync();
        all.Should().HaveCount(2);
        JsonEquals(all.Single(s => s.Name == "server-1").Config, firstConfig).Should().BeTrue();
        JsonEquals(all.Single(s => s.Name == "server-2").Config, secondConfig).Should().BeTrue();
    }

    [Fact]
    public async Task ReplaceAll_UpdatesExistingRows()
    {
        var initialConfig = BuildMcpConfig("https://example.com/old");
        var newConfig = BuildMcpConfig("https://example.com/new");
        var addedConfig = BuildMcpConfig("https://example.com/added");

        await CreateMcpServerAsync("server-1", initialConfig);

        using var response = await Ai.PutAsync(
            McpServersPath,
            new
            {
                servers = new Dictionary<string, string>
                {
                    ["server-1"] = newConfig,
                    ["server-2"] = addedConfig
                }
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var all = await ReadAllMcpServersAsync();
        all.Should().HaveCount(2);
        JsonEquals(all.Single(s => s.Name == "server-1").Config, newConfig).Should().BeTrue();
        JsonEquals(all.Single(s => s.Name == "server-2").Config, addedConfig).Should().BeTrue();
    }

    [Fact]
    public async Task ReplaceAll_WithEntityId_IsolatedFromGlobal()
    {
        var roomId = await CreateRoomAsync();
        var globalConfig = BuildMcpConfig("https://example.com/global");
        var scopedConfig = BuildMcpConfig("https://example.com/scoped");

        await CreateMcpServerAsync("server-1", globalConfig);

        using var response = await Ai.PutAsync(
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

        JsonEquals((await ReadMcpServerAsync("server-1")).Config, globalConfig).Should().BeTrue();
        JsonEquals((await ReadMcpServerAsync("server-1", roomId.ToString())).Config, scopedConfig).Should().BeTrue();

        (await ReadAllMcpServersAsync()).Should().ContainSingle().Which.Name.Should().Be("server-1");
        (await ReadAllMcpServersAsync(roomId.ToString())).Should().ContainSingle().Which.Name.Should().Be("server-1");
    }

    [Fact]
    public async Task ReplaceAll_EmptyDict_ClearsAll()
    {
        await CreateMcpServerAsync("server-1");

        using var response = await Ai.PutAsync(
            McpServersPath,
            new { servers = new Dictionary<string, string>() },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        (await ReadAllMcpServersAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ReplaceAll_RemovesServersAbsentFromNewSet()
    {
        var keptConfig = BuildMcpConfig("https://example.com/kept");

        await CreateMcpServerAsync("server-1");
        await CreateMcpServerAsync("server-2");

        using var response = await Ai.PutAsync(
            McpServersPath,
            new
            {
                servers = new Dictionary<string, string>
                {
                    ["server-1"] = keptConfig
                }
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var all = await ReadAllMcpServersAsync();
        all.Should().ContainSingle().Which.Name.Should().Be("server-1");
        JsonEquals(all.Single().Config, keptConfig).Should().BeTrue();
    }

    [Fact]
    public async Task ReplaceAll_NonExistentEntityId_Returns404()
    {
        using var response = await Ai.PutAsync(
            McpServersPath,
            new
            {
                servers = new Dictionary<string, string>
                {
                    ["server-1"] = BuildMcpConfig()
                },
                entityId = "999999999"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
