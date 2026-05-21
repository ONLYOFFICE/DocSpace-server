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
