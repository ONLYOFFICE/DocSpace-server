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
/// The custom MCP server registry (<c>tools/add|get|list|update|remove-custom-server</c>): lifecycle, what a stored
/// config keeps, how a name is matched, and the soft/hard refusals of a single server write.
/// </summary>
/// <remarks>
/// The TypeScript suite also measured the registry per agent. Agents are gone and the public registry is one map per
/// portal (the chat service drops a reachable <c>entityId</c>, Bug 82863), so the per-scope variants are not repeated
/// here; <see cref="McpServerEntityScopeTests"/> covers what an <c>entityId</c> does.
/// </remarks>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Mcp")]
public class McpServerRegistryTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    [Fact]
    public async Task Lifecycle_Owner_RoundTripsAServer()
    {
        var config = UrlConfig();
        var updated = UrlConfig("mcp-updated.example.invalid");

        var added = await AddServerAsync("autotest-server", config);
        added.Success.Should().BeTrue();

        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["autotest-server"] = config });
        ShouldBeConfig(await GetServerAsync("autotest-server"), config);

        var changed = await UpdateServerAsync("autotest-server", updated);
        changed.Success.Should().BeTrue();
        ShouldBeConfig(await GetServerAsync("autotest-server"), updated);

        (await RemoveServerAsync("autotest-server")).Success.Should().BeTrue();
        (await ListServersAsync()).Should().NotContainKey("autotest-server");
        (await GetServerAsync("autotest-server")).Should().BeNull();
    }

    [Fact]
    public async Task Lifecycle_AddReplaceAllUpdateRemove_EveryStepReadBack()
    {
        await RegisterAsync("life", UrlConfig("a.example.invalid"));

        var replaced = await ReplaceAllAsync(new Dictionary<string, object> { ["fresh"] = StdioConfig() });
        replaced.Success.Should().BeTrue();
        (await GetServerAsync("life")).Should().BeNull("the added server was replaced away");
        ShouldBeConfig(await GetServerAsync("fresh"), StdioConfig());

        (await ReplaceAllAsync(new Dictionary<string, object> { ["one"] = UrlConfig("one.example.invalid"), ["two"] = UrlConfig("two.example.invalid") })).Success.Should().BeTrue();
        (await UpdateServerAsync("one", StdioConfig())).Success.Should().BeTrue();

        ShouldHold(
            await ListServersAsync(),
            new Dictionary<string, object> { ["one"] = StdioConfig(), ["two"] = UrlConfig("two.example.invalid") });

        (await RemoveServerAsync("one")).Success.Should().BeTrue();
        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["two"] = UrlConfig("two.example.invalid") });
    }

    /// <summary>The store is transport-agnostic: whatever else a server needs rides along untouched.</summary>
    [Fact]
    public async Task Add_ConfigsOfEveryShape_RoundTripVerbatimThroughGetAndList()
    {
        var configs = new Dictionary<string, object>
        {
            ["autotest-stdio"] = new Dictionary<string, object>
            {
                ["command"] = "npx",
                ["args"] = new[] { "-y", "@modelcontextprotocol/server-filesystem", "/tmp" },
                ["env"] = new Dictionary<string, string> { ["NODE_ENV"] = "production" }
            },
            ["autotest-http"] = new Dictionary<string, object>
            {
                ["type"] = "http",
                ["url"] = "https://mcp.example.invalid/mcp",
                ["headers"] = new Dictionary<string, string> { ["Authorization"] = "Bearer AUTOTEST-NOT-A-SECRET", ["X-Trace"] = "1" },
                ["auth"] = new Dictionary<string, string> { ["type"] = "oauth", ["clientId"] = "autotest-client" }
            },
            ["autotest-extras"] = new Dictionary<string, object>
            {
                ["url"] = "https://mcp.example.invalid/sse",
                ["timeout"] = 30,
                ["disabled"] = false,
                ["nested"] = new Dictionary<string, object> { ["retries"] = new object[] { 1, "two", new Dictionary<string, string> { ["backoff"] = "exponential" } } }
            }
        };

        foreach (var (name, config) in configs)
        {
            (await AddServerAsync(name, config)).Success.Should().BeTrue(name);
        }

        ShouldHold(await ListServersAsync(), configs);

        foreach (var (name, config) in configs)
        {
            ShouldBeConfig(await GetServerAsync(name), config, $"for {name}, read twice");
            ShouldBeConfig(await GetServerAsync(name), config, $"for {name}");
        }
    }

    /// <summary>Matters for the credential case: re-saving a server without its headers drops them.</summary>
    [Fact]
    public async Task Update_ReplacesTheConfig_NotMerged()
    {
        await RegisterAsync(
            "autotest-merge",
            new Dictionary<string, object> { ["url"] = "https://mcp.example.invalid/mcp", ["headers"] = new Dictionary<string, string> { ["Authorization"] = "Bearer x" } });

        var updated = new Dictionary<string, object> { ["url"] = "https://mcp-updated.example.invalid/mcp" };
        (await UpdateServerAsync("autotest-merge", updated)).Success.Should().BeTrue();

        ShouldBeConfig(await GetServerAsync("autotest-merge"), updated);
    }

    [Fact]
    public async Task Update_ServerChangesKindAndGainsAndLosesFields()
    {
        await RegisterAsync("morph", UrlConfig());

        var steps = new (string Label, Dictionary<string, object> Config)[]
        {
            ("http to stdio", StdioConfig()),
            ("stdio gains a field", new() { ["command"] = "node", ["cwd"] = "/tmp", ["extra"] = new Dictionary<string, int> { ["a"] = 1 } }),
            ("stdio loses fields", new() { ["command"] = "node" }),
            ("back to http with headers", new() { ["url"] = "https://b.example.invalid/sse", ["headers"] = new Dictionary<string, string> { ["X-A"] = "1" } }),
            ("headers dropped", UrlConfig("b.example.invalid"))
        };

        foreach (var (label, config) in steps)
        {
            (await UpdateServerAsync("morph", config)).Success.Should().BeTrue(label);
            ShouldBeConfig(await GetServerAsync("morph"), config, label);
            ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["morph"] = config }, label);
        }
    }

    [Fact]
    public async Task Update_NameIsCaseInsensitive_KeyKeepsItsCasing_RepeatIsHarmless()
    {
        await RegisterAsync("MixedCase", UrlConfig("a.example.invalid"));
        var other = UrlConfig("b.example.invalid");

        for (var attempt = 0; attempt < 2; attempt++)
        {
            (await UpdateServerAsync("mixedcase", other)).Success.Should().BeTrue($"attempt {attempt}");
        }

        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["MixedCase"] = other });
    }

    [Fact]
    public async Task Update_UnknownServer_SoftFailureAndNothingCreated()
    {
        var result = await UpdateServerAsync("does-not-exist", UrlConfig());

        result.Success.Should().BeFalse();
        result.Error.Message.Should().Be("Server not registered: does-not-exist");
        (await ListServersAsync()).Should().BeEmpty();
    }

    /// <summary>Same rule as room tags: the duplicate check ignores case, the key keeps the original spelling.</summary>
    [Fact]
    public async Task Add_NamesAreCaseInsensitive_AndKeepTheCasingTheyWereRegisteredWith()
    {
        await RegisterAsync("Autotest-Mixed-Case", UrlConfig("a.example.invalid"));

        var duplicate = await AddServerAsync("autotest-mixed-case", UrlConfig("b.example.invalid"));

        duplicate.Success.Should().BeFalse();
        duplicate.Error.Field.Should().Be(AiErrorData.FieldEnum.Name);
        duplicate.Error.Message.Should().Be("Server already registered: autotest-mixed-case");
        (await ListServersAsync()).Keys.Should().BeEquivalentTo(["Autotest-Mixed-Case"], "one entry, in the casing it was created with");
        ShouldBeConfig(await GetServerAsync("autotest-mixed-case"), UrlConfig("a.example.invalid"), "a lower-case lookup finds it");
    }

    [Fact]
    public async Task Add_DuplicateName_KeepsTheOriginalRegistration()
    {
        await RegisterAsync("duplicate", UrlConfig("first.example.invalid"));

        var again = await AddServerAsync("duplicate", UrlConfig("other.example.invalid"));

        again.Success.Should().BeFalse();
        again.Error.Message.Should().Be("Server already registered: duplicate");
        ShouldBeConfig(await GetServerAsync("duplicate"), UrlConfig("first.example.invalid"));
    }

    /// <summary>Not trimmed: "  autotest  " and "autotest" are two different servers.</summary>
    [Fact]
    public async Task Add_SurroundingWhitespace_IsPartOfTheName()
    {
        var padded = UrlConfig("padded.example.invalid");
        var trimmed = UrlConfig("trimmed.example.invalid");

        (await AddServerAsync("  autotest-padded  ", padded)).Success.Should().BeTrue();
        (await AddServerAsync("autotest-padded", trimmed)).Success.Should().BeTrue();

        (await ListServersAsync()).Keys.Should().BeEquivalentTo(["  autotest-padded  ", "autotest-padded"]);
        ShouldBeConfig(await GetServerAsync("  autotest-padded  "), padded);
    }

    /// <summary>A copy of a portal server needs a portal server: without a config the name alone is a hard 400.</summary>
    [Fact]
    public async Task Add_NameWithoutConfig_IsA400NamingThePortalServer()
    {
        var reply = await PostToolsAsync("add-custom-server", new { name = "no-such-portal-server" });

        reply.Code.Should().Be(400, reply.ToString());
        reply.Error.Should().Be(NoPortalServerMessage("no-such-portal-server"));
        (await ListServersAsync()).Should().BeEmpty();
    }

    /// <summary>With the server registered, posting its name alone re-saves it, and that is a duplicate.</summary>
    [Fact]
    public async Task Add_NameOfARegisteredServerWithoutConfig_IsRefusedAsADuplicate()
    {
        await RegisterAsync("portal-template", UrlConfig());

        var reply = await PostToolsAsync("add-custom-server", new { name = "portal-template" });

        reply.Code.Should().Be(200, reply.ToString());
        reply.Success.Should().BeFalse(reply.ToString());
        reply.SoftMessage.Should().Contain("already registered");
        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["portal-template"] = UrlConfig() });
    }

    [Fact]
    public async Task Get_UnregisteredName_AnswersNullNotAnError()
    {
        await RegisterAsync("registered-server", UrlConfig());

        (await GetServerAsync("never-existed")).Should().BeNull();
        ShouldBeConfig(await GetServerAsync("registered-server"), UrlConfig(), "the same call does return a config when there is one");

        var raw = await GetToolsAsync("get-custom-server", "?name=never-existed");
        raw.Code.Should().Be(200);
        raw.Text.Trim().Should().Be("null");
    }

    [Fact]
    public async Task Get_NamesThatExistOnObjectPrototype_ReadAsUnregistered()
    {
        await RegisterAsync("real-server", UrlConfig());

        foreach (var name in new[] { "constructor", "toString", "valueOf", "hasOwnProperty", "isPrototypeOf", "__proto__", "prototype" })
        {
            (await GetServerAsync(name)).Should().BeNull($"{name} was never registered");
        }

        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["real-server"] = UrlConfig() });
    }

    [Theory]
    [InlineData("")]
    [InlineData("?name=")]
    public async Task Get_MissingOrEmptyName_Is400AndAReadChangesNothing(string query)
    {
        await RegisterAsync("CasedServer", UrlConfig());
        var before = await SnapshotAsync();

        var reply = await GetToolsAsync("get-custom-server", query);

        reply.Code.Should().Be(400, reply.ToString());
        foreach (var spelling in new[] { "CasedServer", "casedserver", "CASEDSERVER" })
        {
            ShouldBeConfig(await GetServerAsync(spelling), UrlConfig(), spelling);
        }

        (await SnapshotAsync()).Should().Be(before, "reads changed nothing");
    }

    [Fact]
    public async Task Remove_UnknownServer_IsASilentNoOp()
    {
        await RegisterAsync("survivor", UrlConfig());

        foreach (var name in new[] { "never-registered", "survivor-not", ".." })
        {
            (await RemoveServerAsync(name)).Success.Should().BeTrue(name);
        }

        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["survivor"] = UrlConfig() });
    }

    [Fact]
    public async Task Remove_NoNameOrAnEmptyOne_Is400_AndTheDotIsRefusedByTheRouter()
    {
        await RegisterAsync("survivor", UrlConfig());

        (await DeleteToolsAsync("remove-custom-server", new { })).Code.Should().Be(400);
        var empty = await DeleteToolsAsync("remove-custom-server", new { name = "" });
        empty.Code.Should().Be(400);
        // The router answers before the handler sees a bare dot.
        (await DeleteToolsAsync("remove-custom-server", new { name = "." })).Code.Should().Be(405);

        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["survivor"] = UrlConfig() });
    }

    [Fact]
    public async Task Remove_AnotherCasing_RemovesTheServer_AndARepeatIsANoOp()
    {
        await RegisterAsync("Twin", UrlConfig());
        await RegisterAsync("Sibling", StdioConfig());

        (await RemoveServerAsync("TWIN")).Success.Should().BeTrue();
        (await GetServerAsync("Twin")).Should().BeNull();
        (await RemoveServerAsync("Twin")).Success.Should().BeTrue();

        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["Sibling"] = StdioConfig() });
    }

    /// <summary>
    /// <c>disabled</c> is a field MCP client configs carry as a switch. DocSpace stores the config as an opaque blob:
    /// the flag round-trips and nothing on the tools surface derives from it.
    /// </summary>
    [Fact]
    public async Task Add_DisabledInTheConfig_IsStoredVerbatimAndSwitchesNothingOff()
    {
        var config = new Dictionary<string, object> { ["url"] = "https://mcp.example.invalid/sse", ["disabled"] = true };

        (await AddServerAsync("autotest-switched-off", config)).Success.Should().BeTrue("a server marked disabled is accepted");

        ShouldBeConfig(await GetServerAsync("autotest-switched-off"), config);
        (await ListServersAsync()).Keys.Should().BeEquivalentTo(["autotest-switched-off"], "listed like an enabled one");
        (await DisabledAsync()).Should().BeEmpty("the flag is not a disabled-tools entry");
        (await IsDisabledAsync("autotest-switched-off", StubMcpServer.CalculateTool)).Should().BeFalse();
    }

    /// <summary>
    /// The old POST /ai/servers connected to the endpoint, a live SSRF egress. Registration stores the config as-is:
    /// a server that is up receives nothing when it is added, read, listed, updated or removed.
    /// </summary>
    [Fact]
    public async Task Registration_DoesNotReachOutToTheServer()
    {
        using var mcp = new StubMcpServer();
        var config = new Dictionary<string, object> { ["url"] = mcp.Url };

        await RegisterAsync("attacker-server", config);
        ShouldBeConfig(await GetServerAsync("attacker-server"), config);
        await ListServersAsync();
        (await UpdateServerAsync("attacker-server", config)).Success.Should().BeTrue();
        await RemoveServerAsync("attacker-server");

        mcp.WasDialled.Should().BeFalse("any egress happens at tool-execution time, never at registration");
    }
}
