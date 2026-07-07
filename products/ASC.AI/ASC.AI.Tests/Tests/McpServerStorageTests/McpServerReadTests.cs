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
public class McpServerReadTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task ReadByName_Existing_ReturnsServer()
    {
        var config = BuildMcpConfig();
        await CreateMcpServerAsync("server-1", config);

        var stored = await ReadMcpServerAsync("server-1");

        stored.Name.Should().Be("server-1");
        JsonEquals(stored.Config, config).Should().BeTrue();
    }

    [Fact]
    public async Task ReadByName_NonExisting_Returns404()
    {
        using var response = await Ai.GetAsync(
            $"{McpServersPath}/missing",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadByName_WithEntityId_ReturnsScopedValue()
    {
        var roomId = await CreateRoomAsync();
        var globalConfig = BuildMcpConfig("https://example.com/global");
        var scopedConfig = BuildMcpConfig("https://example.com/scoped");

        await CreateMcpServerAsync("server-1", globalConfig);
        await CreateMcpServerAsync("server-1", scopedConfig, roomId.ToString());

        var global = await ReadMcpServerAsync("server-1");
        var scoped = await ReadMcpServerAsync("server-1", roomId.ToString());

        JsonEquals(global.Config, globalConfig).Should().BeTrue();
        JsonEquals(scoped.Config, scopedConfig).Should().BeTrue();
    }

    [Fact]
    public async Task ReadByName_GlobalNotVisibleFromEntityScope()
    {
        var roomId = await CreateRoomAsync();
        await CreateMcpServerAsync("server-1");

        using var response = await Ai.GetAsync(
            $"{McpServersPath}/server-1?entityId={roomId}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadByName_NonExistentEntityId_Returns404()
    {
        using var response = await Ai.GetAsync(
            $"{McpServersPath}/server-1?entityId=999999999",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadAll_Owner_ReturnsAllCreated()
    {
        await CreateMcpServerAsync("server-1");
        await CreateMcpServerAsync("server-2");

        var all = await ReadAllMcpServersAsync();

        all.Should().HaveCount(2);
        all.Should().Contain(s => s.Name == "server-1");
        all.Should().Contain(s => s.Name == "server-2");
    }

    [Fact]
    public async Task ReadAll_Empty_ReturnsEmpty()
    {
        var all = await ReadAllMcpServersAsync();

        all.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAll_WithEntityId_ReturnsOnlyScopedServers()
    {
        var roomId = await CreateRoomAsync();

        await CreateMcpServerAsync("global-server");
        await CreateMcpServerAsync("scoped-server", entityId: roomId.ToString());

        var global = await ReadAllMcpServersAsync();
        var scoped = await ReadAllMcpServersAsync(roomId.ToString());

        global.Should().ContainSingle(s => s.Name == "global-server");
        global.Should().NotContain(s => s.Name == "scoped-server");

        scoped.Should().ContainSingle(s => s.Name == "scoped-server");
        scoped.Should().NotContain(s => s.Name == "global-server");
    }

    [Fact]
    public async Task ReadAll_NonExistentEntityId_Returns404()
    {
        using var response = await Ai.GetAsync(
            $"{McpServersPath}?entityId=999999999",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
