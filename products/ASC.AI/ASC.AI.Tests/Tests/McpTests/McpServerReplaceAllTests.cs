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
/// <c>tools/replace-all-custom-servers</c>: the registry always ends up as exactly the map that was sent, and a map
/// that is refused — whole or in part — changes nothing. The bulk route is all-or-nothing and reports per entry
/// (<c>errors[]</c>, not <c>error</c>).
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Mcp")]
public class McpServerReplaceAllTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    private static Dictionary<string, object> Keep => new() { ["autotest-keep-me"] = UrlConfig("keep.example.invalid") };

    private async Task ShouldKeepTheRegistryAsync(string because = "") => ShouldHold(await ListServersAsync(), Keep, because);

    [Fact]
    public async Task ReplaceAll_TheRegistryAlwaysEndsUpAsExactlyTheMapThatWasSent()
    {
        var a = UrlConfig("a.example.invalid");
        var b = StdioConfig();
        var c = UrlConfig("c.example.invalid");

        var maps = new (string Label, Dictionary<string, object> Map)[]
        {
            ("empty to one", new() { ["one"] = a }),
            ("one replaced by another", new() { ["two"] = c }),
            ("several", new() { ["a"] = a, ["b"] = b, ["c"] = c }),
            ("one kept, one changed, one dropped, one new", new() { ["a"] = c, ["b"] = b, ["d"] = a }),
            ("the same map again", new() { ["a"] = c, ["b"] = b, ["d"] = a }),
            ("cleared", new())
        };

        foreach (var (label, map) in maps)
        {
            var result = await ReplaceAllAsync(map);

            result.Success.Should().BeTrue(label);
            ShouldHold(await ListServersAsync(), map, label);

            foreach (var (name, config) in map)
            {
                ShouldBeConfig(await GetServerAsync(name), config, $"{label}: {name}");
            }
        }
    }

    [Fact]
    public async Task ReplaceAll_EmptyMap_ClearsTheRegistry()
    {
        await RegisterAsync("autotest-old", UrlConfig());

        (await ReplaceAllAsync(new Dictionary<string, object>())).Success.Should().BeTrue();

        (await ListServersAsync()).Should().BeEmpty();
    }

    /// <summary>`map` is required and the field name is easy to get wrong; a body without it used to empty the registry silently.</summary>
    [Fact]
    [Trait("Bug", "82864")]
    public async Task ReplaceAll_BodyWithoutMap_IsRejectedAndTheRegistrySurvives()
    {
        await RegisterAsync("autotest-keep-me", UrlConfig("keep.example.invalid"));

        // Raw: the generated request has a required map.
        var reply = await PutToolsAsync("replace-all-custom-servers", new { });

        reply.Code.Should().Be(400, reply.ToString());
        await ShouldKeepTheRegistryAsync("the registered server survives a rejected write");
    }

    /// <summary>A state variable that has not been populated yet serialises to null; it used to empty the registry with `{success:true}`.</summary>
    [Fact]
    [Trait("Bug", "82864")]
    public async Task ReplaceAll_NullMap_IsRejectedAndTheRegistrySurvives()
    {
        await RegisterAsync("autotest-keep-me", UrlConfig("keep.example.invalid"));

        var reply = await PutToolsAsync("replace-all-custom-servers", """{"map":null}""");

        reply.Code.Should().Be(400, reply.ToString());
        await ShouldKeepTheRegistryAsync("the registered server survives a rejected write");
    }

    [Theory]
    [InlineData("\"not-a-map\"")]
    [InlineData("5")]
    [InlineData("true")]
    public async Task ReplaceAll_MapThatIsAScalar_IsAHard400AndTheRegistrySurvives(string map)
    {
        await RegisterAsync("autotest-keep-me", UrlConfig("keep.example.invalid"));

        var reply = await PutToolsAsync("replace-all-custom-servers", $$"""{"map":{{map}}}""");

        reply.Code.Should().Be(400, reply.ToString());
        reply.Error.Should().Be("map is required and must be an object");
        await ShouldKeepTheRegistryAsync();
    }

    /// <summary>The SDK-shaped `[{name, config}]` payload is rejected as the wrong type; either way nothing is written.</summary>
    [Fact]
    public async Task ReplaceAll_ArrayPayload_IsRefusedAndTheRegistrySurvives()
    {
        await RegisterAsync("autotest-keep-me", UrlConfig("keep.example.invalid"));

        var reply = await PutToolsAsync("replace-all-custom-servers", """{"map":[{"name":"autotest-array","config":{"url":"https://a.example.invalid/sse"}}]}""");

        reply.Code.Should().Be(400, reply.ToString());
        await ShouldKeepTheRegistryAsync();
    }

    [Fact]
    public async Task ReplaceAll_OneInvalidConfig_RejectsTheWholeMapAndReportsEachOffender()
    {
        await RegisterAsync("autotest-keep-me", UrlConfig("keep.example.invalid"));

        var result = await ReplaceAllAsync(new Dictionary<string, object>
        {
            ["autotest-valid"] = UrlConfig("valid.example.invalid"),
            ["autotest-no-transport"] = new Dictionary<string, int> { ["foo"] = 1 },
            ["autotest-not-an-object"] = "https://mcp.example.invalid/sse"
        });

        result.Success.Should().BeFalse();
        result.Errors.Select(e => e.Name).Should().Equal("autotest-no-transport", "autotest-not-an-object");
        await ShouldKeepTheRegistryAsync("neither the valid entry nor the wipe went through");
    }

    [Fact]
    public async Task ReplaceAll_SeveralInvalidConfigs_AreAllReportedAndNotOneEntryIsApplied()
    {
        await RegisterAsync("old-one", UrlConfig("one.example.invalid"));
        await RegisterAsync("old-two", StdioConfig());
        var before = await SnapshotAsync();

        var result = await ReplaceAllAsync(new Dictionary<string, object>
        {
            ["fine"] = UrlConfig("fine.example.invalid"),
            ["bad-string"] = "x",
            ["bad-url"] = new Dictionary<string, int> { ["url"] = 5 },
            ["bad-both"] = new Dictionary<string, string> { ["url"] = "https://a.invalid", ["command"] = "x" }
        });

        result.Success.Should().BeFalse();
        result.Errors.Select(e => e.Name).Should().BeEquivalentTo("bad-both", "bad-string", "bad-url");
        result.Errors.Should().OnlyContain(e => e.Error.Field == AiErrorData.FieldEnum.Url && !string.IsNullOrEmpty(e.Error.Message));
        (await SnapshotAsync()).Should().Be(before, "neither the valid entry nor the wipe went through");
    }

    /// <summary>A name the route refuses outright (or a config the binder reads as absent) rejects the whole map with a 400.</summary>
    [Theory]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData(".")]
    [InlineData("a\u0007b")]
    public async Task ReplaceAll_UnroutableNameInTheMap_RejectsTheWholeMap(string name)
    {
        await RegisterAsync("autotest-keep-me", UrlConfig("keep.example.invalid"));

        var reply = await PutToolsAsync("replace-all-custom-servers", new { map = new Dictionary<string, object> { [name] = UrlConfig(), ["ok"] = UrlConfig("ok.example.invalid") } });

        reply.Code.Should().Be(400, reply.ToString());
        await ShouldKeepTheRegistryAsync();
    }

    /// <summary>The name length cap is enforced by model binding, before the per-entry validation that produces errors[].</summary>
    [Fact]
    public async Task ReplaceAll_OneOverLongName_IsAHard400AndTheRegistrySurvives()
    {
        await RegisterAsync("autotest-keep-me", UrlConfig("keep.example.invalid"));

        var reply = await PutToolsAsync(
            "replace-all-custom-servers",
            new { map = new Dictionary<string, object> { ["autotest-valid"] = UrlConfig("valid.example.invalid"), [new string('n', 129)] = UrlConfig() } });

        reply.Code.Should().Be(400, reply.ToString());
        reply.Error.Should().Be("Bad Request");
        await ShouldKeepTheRegistryAsync("neither the valid entry nor the wipe went through");
    }

    [Fact]
    public async Task ReplaceAll_AnEmptyConfigObjectInTheMap_RejectsTheWholeMap()
    {
        await RegisterAsync("autotest-keep-me", UrlConfig("keep.example.invalid"));

        var reply = await PutToolsAsync("replace-all-custom-servers", new { map = new { empty = new { }, ok = UrlConfig() } });

        reply.Code.Should().Be(400, reply.ToString());
        await ShouldKeepTheRegistryAsync();
    }

    /// <summary>add-custom-server refuses a case-variant of an existing name; the bulk route used to apply no such rule.</summary>
    [Fact]
    [Trait("Bug", "82984")]
    public async Task ReplaceAll_TwoNamesDifferingOnlyInCase_AreRefusedLikeOnAdd()
    {
        var result = await ReplaceAllAsync(new Dictionary<string, object>
        {
            ["Autotest-Dup"] = UrlConfig("upper.example.invalid"),
            ["autotest-dup"] = UrlConfig("lower.example.invalid")
        });

        result.Success.Should().BeFalse("the single-server route's rule, applied to the bulk one");
        (await ListServersAsync()).Should().BeEmpty("a refused map writes nothing, so no configuration is stored that one name cannot read back");
    }

    /// <summary>No cap on how many servers one registry may hold: 120 in a single call all land and all read back.</summary>
    [Fact]
    public async Task ReplaceAll_ALargeMap_IsStoredWhole()
    {
        var map = Enumerable.Range(0, 120).ToDictionary(i => $"autotest-bulk-{i}", i => (object)UrlConfig($"mcp-{i}.example.invalid"));

        var result = await ReplaceAllAsync(map);

        var listed = await ListServersAsync();
        listed.Should().HaveCount(120);
        ShouldBeConfig(listed["autotest-bulk-119"], map["autotest-bulk-119"]);
        ShouldBeConfig(await GetServerAsync("autotest-bulk-0"), map["autotest-bulk-0"]);
        result.Success.Should().BeTrue();
    }
}
