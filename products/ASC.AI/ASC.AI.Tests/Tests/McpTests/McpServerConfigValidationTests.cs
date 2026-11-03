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
/// What a server config has to be. A config is stored transport-agnostically but not blindly: an object carrying
/// <c>url</c> (HTTP) or <c>command</c> (STDIO), exactly one of them. The refusals split two ways — anything the binder
/// reads as "no config at all" is a hard 400 with the portal-fallback message, a config that is present but unusable
/// is the usual soft <c>{success:false}</c> — and a refused write leaves the registry exactly as it was.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Mcp")]
public class McpServerConfigValidationTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    private const string BothMessage = "Server config must be HTTP (url) or STDIO (command), not both";
    private const string NotAnObjectMessage = "Server config must be an object";

    // A free-form JSON value for the SDK's `Object` config (the client serializes with Newtonsoft).
    private static object Json(string json) => Newtonsoft.Json.Linq.JToken.Parse(json);

    [Theory]
    [InlineData("""{"foo":1}""", UrlRequiredMessage)]
    [InlineData("[1,2]", UrlRequiredMessage)]
    [InlineData("\"https://mcp.example.invalid/sse\"", NotAnObjectMessage)]
    public async Task Add_ConfigThatIsPresentButUnusable_IsASoftRefusal(string config, string message)
    {
        var result = await AddServerAsync("bad-config", Json(config));

        result.Success.Should().BeFalse();
        result.Error.Field.Should().Be(AiErrorData.FieldEnum.Url);
        result.Error.Message.Should().Be(message);
        (await GetServerAsync("bad-config")).Should().BeNull("nothing is registered");
    }

    [Fact]
    public async Task Add_BothUrlAndCommand_IsRefusedAndNothingIsStored()
    {
        var result = await AddServerAsync("both-kinds", new Dictionary<string, object> { ["url"] = "https://a.example.invalid/sse", ["command"] = "node" });

        result.Success.Should().BeFalse();
        result.Error.Field.Should().Be(AiErrorData.FieldEnum.Url);
        result.Error.Message.Should().Be(BothMessage);
        (await GetServerAsync("both-kinds")).Should().BeNull();
        (await ListServersAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Add_UrlOrCommandOfTheWrongType_IsRefusedAndLeavesExistingServersAlone()
    {
        await RegisterAsync("keeper", UrlConfig());

        var bad = new Dictionary<string, string>
        {
            ["url number"] = """{"url":5}""",
            ["url null"] = """{"url":null}""",
            ["url empty"] = """{"url":""}""",
            ["url array"] = """{"url":["https://a.invalid"]}""",
            ["command number"] = """{"command":5}""",
            ["command empty"] = """{"command":""}""",
            ["command object"] = """{"command":{"run":"node"}}"""
        };

        foreach (var (label, config) in bad)
        {
            var result = await AddServerAsync($"bad-{label.Replace(' ', '-')}", Json(config));

            result.Success.Should().BeFalse(label);
            result.Error.Field.Should().Be(AiErrorData.FieldEnum.Url, label);
            result.Error.Message.Should().Be(UrlRequiredMessage, label);
        }

        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["keeper"] = UrlConfig() });
    }

    /// <summary>A config the binder reads as absent is the "copy the portal server of this name" path, and there is none.</summary>
    [Theory]
    [InlineData("{}")]
    [InlineData("42")]
    public async Task Add_EmptyObjectOrNumberAsConfig_IsTreatedAsNoConfigAtAll(string config)
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await AddServerAsync("empty-config", Json(config)));

        exception.ErrorCode.Should().Be(400);
        // The body is JSON, so the quotes around the name are escaped in it.
        exception.ErrorContent?.ToString().Should().Contain("No config provided and no portal-level server named").And.Contain("empty-config");
        (await GetServerAsync("empty-config")).Should().BeNull();
    }

    [Theory]
    [InlineData("""{"name":"no-config"}""")]
    [InlineData("""{"name":"no-config","config":null}""")]
    public async Task Add_MissingOrNullConfig_IsAHard400(string body)
    {
        // Raw: the generated request has a required config and refuses to be built without one.
        var reply = await PostToolsAsync("add-custom-server", body);

        reply.Code.Should().Be(400, reply.ToString());
        reply.Error.Should().Be(NoPortalServerMessage("no-config"));
        (await GetServerAsync("no-config")).Should().BeNull();
    }

    /// <summary>The hard 400s: whatever the binder reads as "no config", or no usable name.</summary>
    [Theory]
    [InlineData("""{"config":{"url":"https://a.example.invalid/sse"}}""", "name is required")]
    [InlineData("""{"name":"","config":{"url":"https://a.example.invalid/sse"}}""", "name is required")]
    [InlineData("""{"name":"   ","config":{"url":"https://a.example.invalid/sse"}}""", "name is required")]
    [InlineData("""{"name":"unregistered-target"}""", "No config provided and no portal-level server named \"unregistered-target\"")]
    [InlineData("""{"name":"unregistered-target","config":null}""", "No config provided and no portal-level server named \"unregistered-target\"")]
    [InlineData("""{"name":"unregistered-target","config":{}}""", "No config provided and no portal-level server named \"unregistered-target\"")]
    public async Task Update_HardRefusals_LeaveTheStoredConfigExactlyAsItWas(string body, string message)
    {
        // Raw: a missing name or config cannot be built from the typed request.
        var original = new Dictionary<string, object> { ["url"] = "https://mcp.example.invalid/sse", ["headers"] = new Dictionary<string, string> { ["X-Keep"] = "yes" } };
        await RegisterAsync("target", original);
        await RegisterAsync("neighbour", UrlConfig("neighbour.example.invalid"));

        var reply = await PutToolsAsync("update-custom-server", body);

        reply.Code.Should().Be(400, reply.ToString());
        reply.Error.Should().Be(message);
        ShouldHold(
            await ListServersAsync(),
            new Dictionary<string, object> { ["target"] = original, ["neighbour"] = UrlConfig("neighbour.example.invalid") });
    }

    [Theory]
    [InlineData("\"not-an-object\"", NotAnObjectMessage)]
    [InlineData("[1]", UrlRequiredMessage)]
    [InlineData("""{"url":5}""", UrlRequiredMessage)]
    [InlineData("""{"command":5}""", UrlRequiredMessage)]
    [InlineData("""{"url":"https://a.invalid","command":"x"}""", BothMessage)]
    public async Task Update_SoftRefusals_LeaveTheStoredConfigExactlyAsItWas(string config, string message)
    {
        var original = new Dictionary<string, object> { ["url"] = "https://mcp.example.invalid/sse", ["headers"] = new Dictionary<string, string> { ["X-Keep"] = "yes" } };
        await RegisterAsync("target", original);

        var result = await UpdateServerAsync("target", Json(config));

        result.Success.Should().BeFalse();
        result.Error.Message.Should().Be(message);
        ShouldBeConfig(await GetServerAsync("target"), original, "a refused update must not touch the stored config");
    }

    /// <summary>A battery of refused writes of every kind leaves the whole tools state identical.</summary>
    [Fact]
    public async Task RefusedWrites_OfEveryKind_LeaveTheWholeToolsStateIdentical()
    {
        await RegisterAsync("srv", UrlConfig());
        await SetDisabledAsync("srv", "d1");
        await SetAllowAlwaysAsync("srv", "a1");
        var before = await SnapshotAsync();

        var refused = await Task.WhenAll(
            PostToolsAsync("add-custom-server", new { name = "srv", config = UrlConfig("other.example.invalid") }),
            PostToolsAsync("add-custom-server", new { name = "new", config = new { url = 5 } }),
            PostToolsAsync("add-custom-server", new { name = "a/b", config = UrlConfig() }),
            PutToolsAsync("update-custom-server", new { name = "srv", config = "x" }),
            PutToolsAsync("update-custom-server", new { name = "ghost", config = UrlConfig() }),
            PutToolsAsync("replace-all-custom-servers", new { map = new { x = new { url = 5 } } }),
            PutToolsAsync("replace-all-custom-servers", new { map = "x" }),
            PutToolsAsync("set-disabled", new { serverType = "ghost", toolNames = new[] { "x" } }),
            PutToolsAsync("set-disabled", new { serverType = "srv", toolNames = new[] { 1 } }),
            PutToolsAsync("set-disabled", new { toolNames = new[] { "x" } }));

        foreach (var reply in refused)
        {
            (reply.Code == 400 || reply.Success == false).Should().BeTrue($"a refused write: {reply}");
        }

        (await SnapshotAsync()).Should().Be(before);
    }
}
