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

namespace ASC.AI.Tests.Tests.McpTests;

/// <summary>
/// <c>tools/list-system-tools</c>: the catalogue of tools. Since the built-in DocSpace tools were hidden it is the
/// system groups the host configures plus the LIVE tools of every registered custom server (a server that cannot be
/// reached is named in <c>errors</c>) — the clause "their tools appear in the common list" has a route behind it now.
/// It is the one tools route that asks for no membership and stays readable with AI access off.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Mcp")]
public class McpToolsCatalogueTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    [Fact]
    public async Task ListSystemTools_NothingRegistered_PublishesNoCustomGroup()
    {
        var catalogue = await _aiTools.AiToolsListSystemToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Whatever groups the host configures are named in `system`; with no server registered there is nothing else.
        catalogue.Groups.Keys.Except(catalogue.System).Should().BeEmpty();
        catalogue.Errors.Keys.Except(catalogue.System).Should().BeEmpty();
    }

    /// <summary>A registered server's tools used to be missing from every listing, so no client could show a common list.</summary>
    [Fact]
    [Trait("Bug", "83163")]
    public async Task ListSystemTools_ARegisteredServersToolsAppearUnderItsName()
    {
        using var mcp = new StubMcpServer();
        await RegisterAsync("calculator", new Dictionary<string, object> { ["url"] = mcp.Url });

        var catalogue = await _aiTools.AiToolsListSystemToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        catalogue.Groups.Keys.Should().Contain("calculator");
        catalogue.Groups["calculator"].Select(t => t.Name).Should().BeEquivalentTo(mcp.ToolNames);
        catalogue.Errors.Should().NotContainKey("calculator", "the server answered");
        catalogue.System.Should().NotContain("calculator", "`system` names the host-configured groups, not the registered servers");
        mcp.ReceivedMethods.Should().Contain(["initialize", "tools/list"]);
    }

    [Fact]
    public async Task ListSystemTools_AnUnreachableServerIsNamedInErrors_AndItsCredentialsAreNotEchoed()
    {
        const string secret = "AUTOTEST-NOT-A-SECRET";
        await RegisterAsync(
            "dead-srv",
            new Dictionary<string, object> { ["url"] = "http://127.0.0.1:1/mcp", ["headers"] = new Dictionary<string, string> { ["Authorization"] = $"Bearer {secret}" } });

        var catalogue = await _aiTools.AiToolsListSystemToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        catalogue.Groups.Keys.Should().Contain("dead-srv");
        catalogue.Groups["dead-srv"].Should().BeEmpty();
        catalogue.Errors.Keys.Should().Contain("dead-srv");
        catalogue.Errors["dead-srv"].Should().NotBeNullOrEmpty();
        JsonSerializer.Serialize(catalogue).Should().NotContain(secret, "the error text must not echo the stored credentials");
    }

    [Fact]
    public async Task ListSystemTools_ListsNoToolNameTwice_AndAServerIsNotASystemGroup()
    {
        using var mcp = new StubMcpServer();
        await RegisterAsync("listed-server", new Dictionary<string, object> { ["url"] = mcp.Url });

        var catalogue = await _aiTools.AiToolsListSystemToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var names = catalogue.Groups.Values.SelectMany(t => t).Select(t => t.Name).ToList();
        names.Should().OnlyHaveUniqueItems();
        catalogue.System.Should().NotContain("listed-server");
    }

    /// <summary>The entity is not looked at: one that names nothing reads the same catalogue.</summary>
    [Theory]
    [InlineData("abc")]
    [InlineData(MissingEntityId)]
    public async Task ListSystemTools_AnEntityThatNamesNothing_ReadsTheSameCatalogue(string entityId)
    {
        using var mcp = new StubMcpServer();
        await RegisterAsync("portal-srv", new Dictionary<string, object> { ["url"] = mcp.Url });

        var reply = await GetToolsAsync("list-system-tools", $"?entityId={entityId}");

        reply.Code.Should().Be(200, reply.ToString());
        var system = reply.Json!.Value.GetProperty("system").EnumerateArray().Select(s => s.GetString()).ToList();
        reply.Json!.Value.GetProperty("groups").EnumerateObject().Select(p => p.Name).Except(system).Should().Equal("portal-srv");
    }

    /// <summary>The one route that asks for no membership: every role gets the catalogue, a Guest included.</summary>
    [Theory]
    [InlineData(McpRole.DocSpaceAdmin)]
    [InlineData(McpRole.RoomAdmin)]
    [InlineData(McpRole.User)]
    [InlineData(McpRole.Guest)]
    public async Task ListSystemTools_EveryRoleReadsTheCatalogue(McpRole role)
    {
        var member = await InviteRoleAsync(role);
        await ActAsAsync(member);

        var reply = await GetToolsAsync("list-system-tools");

        reply.Code.Should().Be(200, reply.ToString());
        reply.Json!.Value.TryGetProperty("groups", out _).Should().BeTrue(reply.ToString());
    }

    [Fact]
    public async Task ListSystemTools_AGuestGetsTheCatalogueAndNothingElseOnThisSurface()
    {
        await RegisterAsync("owners-server");
        var guest = await InviteRoleAsync(McpRole.Guest);
        await ActAsAsync(guest);

        (await GetToolsAsync("list-custom-servers")).Code.Should().Be(403, "the registry stays closed to a Guest");
        (await GetToolsAsync("list-system-tools")).Code.Should().Be(200);
    }
}
