// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
