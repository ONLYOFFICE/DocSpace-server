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
/// <c>tools/set-disabled</c>, <c>get-disabled</c>, <c>is-tool-disabled</c>: the per-user list of tools switched off for
/// a server type. The write addresses a (serverType, toolNames) pair and the list is a FULL replacement — "turn this
/// server off" is "send every tool it has", "turn it back on" is "send a shorter list". The serverType must be a
/// built-in group or a registered custom server.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Mcp")]
public class McpDisabledToolsTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    private const string Target = "autotest-disable-target";

    [Fact]
    public async Task SetDisabled_ATool_ThenReenable()
    {
        await RegisterAsync(Target);

        await SetDisabledAsync(Target, "calculate");

        (await DisabledAsync())[Target].Should().Equal("calculate");
        (await IsDisabledAsync(Target, "calculate")).Should().BeTrue();

        // Re-enable by writing an empty list back.
        await SetDisabledAsync(Target);
        (await IsDisabledAsync(Target, "calculate")).Should().BeFalse();
    }

    [Fact]
    public async Task SetDisabled_AWholeServerInOneCall_ThenAShorterListEnablesWhatItOmits()
    {
        await RegisterAsync(Target);
        string[] all = ["calculate", "get_time", "convert_units"];

        await SetDisabledAsync(Target, all);

        // Every name is stored, in order.
        (await DisabledAsync())[Target].Should().Equal(all);
        (await IsDisabledAsync(Target, all[0])).Should().BeTrue();
        (await IsDisabledAsync(Target, all[^1])).Should().BeTrue();

        // A full replacement, not a merge.
        await SetDisabledAsync(Target, all[0]);

        (await DisabledAsync())[Target].Should().Equal(all[0]);
        (await IsDisabledAsync(Target, all[0])).Should().BeTrue($"{all[0]} stayed off");
        (await IsDisabledAsync(Target, all[1])).Should().BeFalse($"{all[1]} came back on");
    }

    /// <summary>
    /// Every tool a live server advertises can be switched off without unregistering it: "off" and "removed" are
    /// different states, and only the second one is <c>remove-custom-server</c>.
    /// </summary>
    [Fact]
    public async Task SetDisabled_EveryToolALiveServerAdvertises_KeepsTheServerRegistered()
    {
        using var mcp = new StubMcpServer();
        var config = new Dictionary<string, object> { ["url"] = mcp.Url };
        await RegisterAsync("calculator", config);

        await SetDisabledAsync("calculator", [.. mcp.ToolNames]);

        // The server's own tool names are stored.
        (await DisabledAsync())["calculator"].Should().Equal(mcp.ToolNames);
        foreach (var tool in mcp.ToolNames)
        {
            (await IsDisabledAsync("calculator", tool)).Should().BeTrue($"calculator_{tool} is off");
        }

        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["calculator"] = config });
        ShouldBeConfig(await GetServerAsync("calculator"), config, "the config survives switching the tools off");
    }

    [Fact]
    public async Task SetDisabled_DuplicatesCollapse_UnknownNamesAreKept_AnEmptyListClears_EveryReadAgrees()
    {
        await RegisterAsync("srv");

        await SetDisabledAsync("srv", "a", "a", "b");
        (await DisabledAsync()).Should().BeEquivalentTo(new Dictionary<string, List<string>> { ["srv"] = ["a", "b"] });
        (await IsDisabledAsync("srv", "a")).Should().BeTrue();
        (await IsDisabledAsync("srv", "b")).Should().BeTrue();
        (await IsDisabledAsync("srv", "c")).Should().BeFalse("a tool nobody disabled");

        await SetDisabledAsync("srv", "a", "a", "b");
        (await DisabledAsync())["srv"].Should().Equal("a", "b");

        await SetDisabledAsync("srv", "b", "never-advertised");
        (await DisabledAsync())["srv"].Should().Equal("b", "never-advertised");
        (await IsDisabledAsync("srv", "a")).Should().BeFalse("dropped from the list, so enabled");
        (await IsDisabledAsync("srv", "never-advertised")).Should().BeTrue();

        await SetDisabledAsync("srv");
        (await DisabledAsync()).Should().BeEmpty();
        (await IsDisabledAsync("srv", "b")).Should().BeFalse();
    }

    [Fact]
    public async Task SetDisabled_TheListBelongsToItsServerType()
    {
        await RegisterAsync("alpha");
        await RegisterAsync("beta", UrlConfig("beta.example.invalid"));

        await SetDisabledAsync("alpha", "shared", "only-alpha");
        await SetDisabledAsync(WebSearch, "lookup");

        (await DisabledAsync()).Should().BeEquivalentTo(new Dictionary<string, List<string>>
        {
            ["alpha"] = ["shared", "only-alpha"],
            [WebSearch] = ["lookup"]
        });
        (await IsDisabledAsync("alpha", "shared")).Should().BeTrue();
        (await IsDisabledAsync("beta", "shared")).Should().BeFalse("same name, other server");
        (await IsDisabledAsync(WebSearch, "shared")).Should().BeFalse();

        await SetDisabledAsync("beta", "shared");
        await SetDisabledAsync("alpha");

        (await DisabledAsync()).Should().BeEquivalentTo(
            new Dictionary<string, List<string>> { ["beta"] = ["shared"], [WebSearch] = ["lookup"] },
            "clearing alpha left beta and web-search alone");
    }

    /// <summary>
    /// <c>serverType</c> used to accept any string and echo it back without ever disabling anything. It names a built-in
    /// group or a registered custom server, and is a 400 otherwise.
    /// </summary>
    [Fact]
    [Trait("Bug", "83013")]
    public async Task SetDisabled_ServerTypeMustBeABuiltInOrARegisteredServer()
    {
        foreach (var serverType in new[] { "editor", "document", "form", "presentation", "autotest-not-a-server-type" })
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _aiTools.AiToolsSetDisabledAsync(
                    new AiToolsSetDisabledRequest(serverType: serverType, toolNames: ["autotest_tool"]),
                    TestContext.Current.CancellationToken));

            exception.ErrorCode.Should().Be(400, $"set-disabled for {serverType}");
        }

        (await DisabledAsync()).Should().BeEmpty("none of the rejected writes landed");

        // Positive control: a registered server's own name is not unknown, so the 400s above are the rule at work.
        await RegisterAsync("autotest-valid-server-type");
        await SetDisabledAsync("autotest-valid-server-type", "autotest_tool");
        (await DisabledAsync())["autotest-valid-server-type"].Should().Equal("autotest_tool");
    }

    [Fact]
    [Trait("Bug", "83013")]
    public async Task SetDisabled_UnknownServerType_NamesTheValidValues()
    {
        await RegisterAsync("srv");

        var reply = await PutToolsAsync("set-disabled", new { serverType = "ghost", toolNames = new[] { "x" } });

        reply.Code.Should().Be(400, reply.ToString());
        reply.Error.Should().Contain("unknown serverType \"ghost\"").And.Contain("srv").And.Contain(WebSearch);
    }

    [Theory]
    [InlineData("""{"toolNames":["x"]}""")]
    [InlineData("""{"serverType":"","toolNames":["x"]}""")]
    [InlineData("""{"serverType":null,"toolNames":["x"]}""")]
    [InlineData("""{"serverType":"srv","toolNames":[1,2]}""")]
    public async Task SetDisabled_BadServerTypeOrNonStringToolNames_Is400AndTheStoredListIsUntouched(string body)
    {
        await RegisterAsync("srv");
        await SetDisabledAsync("srv", "keep");

        // Raw: a missing or wrong-typed field cannot be built from the typed request.
        var reply = await PutToolsAsync("set-disabled", body);

        reply.Code.Should().Be(400, reply.ToString());
        reply.Error.Should().NotBeNullOrEmpty();
        (await DisabledAsync()).Should().BeEquivalentTo(new Dictionary<string, List<string>> { ["srv"] = ["keep"] });
    }

    /// <summary>A string toolNames used to be spread into single-letter tool names.</summary>
    [Fact]
    [Trait("Bug", "84402")]
    public async Task SetDisabled_StringToolNames_IsRejectedInsteadOfSplitIntoLetters()
    {
        await RegisterAsync("srv");
        await SetDisabledAsync("srv", "keep");

        var reply = await PutToolsAsync("set-disabled", """{"serverType":"srv","toolNames":"abc"}""");

        (await DisabledAsync()).Should().BeEquivalentTo(new Dictionary<string, List<string>> { ["srv"] = ["keep"] });
        reply.Code.Should().Be(400, reply.ToString());
    }

    /// <summary>An absent toolNames used to read as "clear the list" and wipe it.</summary>
    [Theory]
    [Trait("Bug", "84403")]
    [InlineData("""{"serverType":"srv"}""")]
    [InlineData("""{"serverType":"srv","toolNames":null}""")]
    public async Task SetDisabled_NoToolNamesOrNull_IsRejectedInsteadOfWipingTheList(string body)
    {
        await RegisterAsync("srv");
        await SetDisabledAsync("srv", "keep");

        var reply = await PutToolsAsync("set-disabled", body);

        (await DisabledAsync()).Should().BeEquivalentTo(new Dictionary<string, List<string>> { ["srv"] = ["keep"] });
        reply.Code.Should().Be(400, reply.ToString());
    }

    [Theory]
    [InlineData("?toolName=p1")]
    [InlineData("?serverType=portal-srv")]
    [InlineData("?serverType=&toolName=")]
    [InlineData("")]
    public async Task IsToolDisabled_MissingOrEmptyParameters_Is400(string query)
    {
        var reply = await GetToolsAsync("is-tool-disabled", query);

        reply.Code.Should().Be(400, reply.ToString());
        reply.Error.Should().Be("serverType and toolName required");
    }

    [Fact]
    public async Task IsToolDisabled_UnknownServerType_IsFalse_AndAnEntityThatNamesNothingReadsTheSameList()
    {
        await RegisterAsync("portal-srv");
        await SetDisabledAsync("portal-srv", "p1");

        (await IsDisabledAsync("no-such-server", "p1")).Should().BeFalse();

        var viaMissingEntity = await GetToolsAsync("is-tool-disabled", $"?serverType=portal-srv&toolName=p1&entityId={MissingEntityId}");
        viaMissingEntity.Code.Should().Be(200, viaMissingEntity.ToString());
        viaMissingEntity.Text.Trim().Should().Be("true", "the list is the user's own whatever entity is named");
    }
}
