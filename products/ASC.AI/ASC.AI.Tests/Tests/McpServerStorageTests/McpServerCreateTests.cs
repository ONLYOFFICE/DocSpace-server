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
public class McpServerCreateTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Create_Owner_PersistsAndIsReadable()
    {
        var config = BuildMcpConfig();

        await CreateMcpServerAsync("server-1", config);

        var stored = await ReadMcpServerAsync("server-1");
        stored.Name.Should().Be("server-1");
        JsonEquals(stored.Config, config).Should().BeTrue();
    }

    [Fact]
    public async Task Create_WithEntityId_PersistsForFolder()
    {
        var roomId = await CreateRoomAsync();
        var config = BuildMcpConfig();

        await CreateMcpServerAsync("server-1", config, roomId.ToString());

        var scoped = await ReadMcpServerAsync("server-1", roomId.ToString());
        scoped.Name.Should().Be("server-1");
        JsonEquals(scoped.Config, config).Should().BeTrue();

        using var globalResponse = await Ai.GetAsync(
            $"{McpServersPath}/server-1",
            TestContext.Current.CancellationToken);
        globalResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_SameNameInGlobalAndEntity_AreIsolated()
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
    public async Task Create_DuplicateName_Fails()
    {
        await CreateMcpServerAsync("server-1");

        using var response = await Ai.PostAsync(
            McpServersPath,
            new { name = "server-1", config = BuildMcpConfig() },
            TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task Create_DuplicateNameWithSameEntityId_Fails()
    {
        var roomId = await CreateRoomAsync();
        await CreateMcpServerAsync("server-1", entityId: roomId.ToString());

        using var response = await Ai.PostAsync(
            McpServersPath,
            new { name = "server-1", config = BuildMcpConfig(), entityId = roomId.ToString() },
            TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task Create_NonExistentEntityId_Returns404()
    {
        using var response = await Ai.PostAsync(
            McpServersPath,
            new { name = "server-1", config = BuildMcpConfig(), entityId = "999999999" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_InvalidJson_Returns400()
    {
        const string invalidJson = """{ "name": "server-1" }""";

        using var response = await Ai.PostRawAsync(McpServersPath, invalidJson, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
