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
public class McpServerCreateTests(AspireAppFixture fixture) : PublicToolsTestBase(fixture)
{
    [Fact]
    public async Task Create_Owner_PersistsAndIsReadable()
    {
        var result = await _aiTools.AiToolsAddCustomServerAsync(
            new AiToolsAddCustomServerRequest(name: "server-1", config: McpConfig()),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        result.Error.Should().BeNull();

        ConfigEquals(await GetCustomServerAsync("server-1")).Should().BeTrue();
    }

    // The public registry is one map per portal: an accessible entityId is accepted and ignored (Bug 82863), so a
    // server added from a room is the portal-wide one every listing reads back.
    [Fact]
    public async Task Create_WithAccessibleEntityId_LandsInPortalRegistry()
    {
        var roomId = await CreateRoomAsync();

        await AddCustomServerAsync("server-1", entityId: roomId.ToString());

        ConfigEquals(await GetCustomServerAsync("server-1")).Should().BeTrue();
        (await ListCustomServersAsync()).Should().ContainKey("server-1");
    }

    // Internal contract: per-entity MCP server scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task Create_WithEntityId_PersistsForFolder()
    {
        var roomId = await CreateRoomAsync();
        var config = BuildMcpConfig();

        await CreateMcpServerInternalAsync("server-1", config, roomId.ToString());

        var scoped = await ReadMcpServerInternalAsync("server-1", roomId.ToString());
        scoped.Name.Should().Be("server-1");
        JsonEquals(scoped.Config, config).Should().BeTrue();

        using var globalResponse = await _ai.GetAsync(
            $"{McpServersPath}/server-1",
            TestContext.Current.CancellationToken);
        globalResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Internal contract: per-entity MCP server scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task Create_SameNameInGlobalAndEntity_AreIsolated()
    {
        var roomId = await CreateRoomAsync();
        var globalConfig = BuildMcpConfig("https://example.com/global");
        var scopedConfig = BuildMcpConfig("https://example.com/scoped");

        await CreateMcpServerInternalAsync("server-1", globalConfig);
        await CreateMcpServerInternalAsync("server-1", scopedConfig, roomId.ToString());

        var global = await ReadMcpServerInternalAsync("server-1");
        var scoped = await ReadMcpServerInternalAsync("server-1", roomId.ToString());

        JsonEquals(global.Config, globalConfig).Should().BeTrue();
        JsonEquals(scoped.Config, scopedConfig).Should().BeTrue();
    }

    // A duplicate is a soft failure: status 200 with success=false and the name field named, the first entry kept.
    [Fact]
    public async Task Create_DuplicateName_Fails()
    {
        await AddCustomServerAsync("server-1", "https://example.com/first");

        var result = await _aiTools.AiToolsAddCustomServerAsync(
            new AiToolsAddCustomServerRequest(name: "server-1", config: McpConfig("https://example.com/second")),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error.Field.Should().Be(AiErrorData.FieldEnum.Name);

        ConfigEquals(await GetCustomServerAsync("server-1"), "https://example.com/first").Should().BeTrue();
    }

    // Internal contract: per-entity MCP server scope has no public route (the chat service always stores under entityId null).
    [Fact]
    public async Task Create_DuplicateNameWithSameEntityId_Fails()
    {
        var roomId = await CreateRoomAsync();
        await CreateMcpServerInternalAsync("server-1", entityId: roomId.ToString());

        using var response = await _ai.PostAsync(
            McpServersPath,
            new { name = "server-1", config = BuildMcpConfig(), entityId = roomId.ToString() },
            TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task Create_NonExistentEntityId_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiTools.AiToolsAddCustomServerAsync(
                new AiToolsAddCustomServerRequest(name: "server-1", config: McpConfig(), entityId: MissingEntityId),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);

        (await ListCustomServersAsync()).Should().BeEmpty();
    }

    // Raw HTTP: a body without a config is rejected client-side by the SDK's required constructor parameter.
    [Fact]
    public async Task Create_InvalidJson_Returns400()
    {
        const string invalidJson = """{ "name": "server-1" }""";

        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            $"{ToolsPath}/add-custom-server",
            invalidJson,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
