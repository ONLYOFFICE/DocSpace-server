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
public class McpServerDeleteTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Delete_Existing_Removes()
    {
        await CreateMcpServerAsync("server-1");

        using var response = await Ai.DeleteAsync(
            $"{McpServersPath}/server-1",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var readResponse = await Ai.GetAsync(
            $"{McpServersPath}/server-1",
            TestContext.Current.CancellationToken);
        readResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonExisting_NoContent()
    {
        using var response = await Ai.DeleteAsync(
            $"{McpServersPath}/missing",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithEntityId_RemovesOnlyScoped()
    {
        var roomId = await CreateRoomAsync();
        var globalConfig = BuildMcpConfig("https://example.com/global");
        var scopedConfig = BuildMcpConfig("https://example.com/scoped");

        await CreateMcpServerAsync("server-1", globalConfig);
        await CreateMcpServerAsync("server-1", scopedConfig, roomId.ToString());

        using var response = await Ai.DeleteAsync(
            $"{McpServersPath}/server-1?entityId={roomId}",
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        JsonEquals((await ReadMcpServerAsync("server-1")).Config, globalConfig).Should().BeTrue();

        using var scopedRead = await Ai.GetAsync(
            $"{McpServersPath}/server-1?entityId={roomId}",
            TestContext.Current.CancellationToken);
        scopedRead.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Global_DoesNotAffectScoped()
    {
        var roomId = await CreateRoomAsync();
        var globalConfig = BuildMcpConfig("https://example.com/global");
        var scopedConfig = BuildMcpConfig("https://example.com/scoped");

        await CreateMcpServerAsync("server-1", globalConfig);
        await CreateMcpServerAsync("server-1", scopedConfig, roomId.ToString());

        using var response = await Ai.DeleteAsync(
            $"{McpServersPath}/server-1",
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var globalRead = await Ai.GetAsync(
            $"{McpServersPath}/server-1",
            TestContext.Current.CancellationToken);
        globalRead.StatusCode.Should().Be(HttpStatusCode.NotFound);

        JsonEquals((await ReadMcpServerAsync("server-1", roomId.ToString())).Config, scopedConfig).Should().BeTrue();
    }

    [Fact]
    public async Task Delete_NonExistentEntityId_Returns404()
    {
        await CreateMcpServerAsync("server-1");

        using var response = await Ai.DeleteAsync(
            $"{McpServersPath}/server-1?entityId=999999999",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
