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
