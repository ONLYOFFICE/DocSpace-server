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
public class McpServerUpdateTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Update_Existing_PersistsNewConfig()
    {
        await CreateMcpServerAsync("server-1", BuildMcpConfig("https://example.com/old"));
        var newConfig = BuildMcpConfig("https://example.com/new");

        using var response = await Ai.PutAsync(
            $"{McpServersPath}/server-1",
            new { config = newConfig },
            TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.Should().BeTrue();
        JsonEquals((await ReadMcpServerAsync("server-1")).Config, newConfig).Should().BeTrue();
    }

    [Fact]
    public async Task Update_WithEntityId_UpdatesOnlyScoped()
    {
        var roomId = await CreateRoomAsync();
        var globalConfig = BuildMcpConfig("https://example.com/global");
        var initialScoped = BuildMcpConfig("https://example.com/scoped-old");
        var newScoped = BuildMcpConfig("https://example.com/scoped-new");

        await CreateMcpServerAsync("server-1", globalConfig);
        await CreateMcpServerAsync("server-1", initialScoped, roomId.ToString());

        using var response = await Ai.PutAsync(
            $"{McpServersPath}/server-1",
            new { config = newScoped, entityId = roomId.ToString() },
            TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.Should().BeTrue();

        JsonEquals((await ReadMcpServerAsync("server-1")).Config, globalConfig).Should().BeTrue();
        JsonEquals((await ReadMcpServerAsync("server-1", roomId.ToString())).Config, newScoped).Should().BeTrue();
    }

    [Fact]
    public async Task Update_NonExistingName_Returns404()
    {
        using var response = await Ai.PutAsync(
            $"{McpServersPath}/missing",
            new { config = BuildMcpConfig() },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_GlobalExists_ButScopedRequest_Returns404()
    {
        var roomId = await CreateRoomAsync();
        await CreateMcpServerAsync("server-1");

        using var response = await Ai.PutAsync(
            $"{McpServersPath}/server-1",
            new { config = BuildMcpConfig(), entityId = roomId.ToString() },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_NonExistentEntityId_Returns404()
    {
        await CreateMcpServerAsync("server-1");

        using var response = await Ai.PutAsync(
            $"{McpServersPath}/server-1",
            new { config = BuildMcpConfig(), entityId = "999999999" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
