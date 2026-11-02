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
/// Server names as map keys: the names that are refused up front, the ones that must keep round-tripping, and the
/// sharp edges of a name that is a key of a plain object.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Mcp")]
public class McpServerNameTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    private const string UnroutableMessage = "name must not be \".\", \"..\", or contain a path separator or control character";
    private const string UnderscoreMessage = "Server name cannot contain '_' — reserved for tool-token format";

    /// <summary>
    /// A name that could never be addressed again (a path separator, a control character, a dot segment) is refused
    /// on add, update and the bulk replace alike. These names used to register and then be unaddressable: get answered
    /// null, update reported "not registered" and the entry survived every delete.
    /// </summary>
    [Theory]
    [Trait("Bug", "82985")]
    [InlineData("autotest/nested-name", 200)]
    [InlineData("autotest\\backslash", 200)]
    [InlineData("autotest\u0001ctl", 200)]
    [InlineData(".", 405)]
    [InlineData("..", 200)]
    public async Task Write_UnroutableName_IsA400OnAddUpdateAndReplaceAll(string name, int removeStatus)
    {
        await RegisterAsync("autotest-legal", UrlConfig());

        var added = await PostToolsAsync("add-custom-server", new { name, config = UrlConfig() });
        var updated = await PutToolsAsync("update-custom-server", new { name, config = UrlConfig("other.example.invalid") });
        var replaced = await PutToolsAsync("replace-all-custom-servers", new { map = new Dictionary<string, object> { [name] = UrlConfig() } });
        var removed = await DeleteToolsAsync("remove-custom-server", new { name });

        (await ListServersAsync()).Keys.Should().BeEquivalentTo(["autotest-legal"], "the refused name was never stored and the legal one survived");
        added.Code.Should().Be(400, "add: " + added);
        added.Error.Should().Be(UnroutableMessage, "add");
        updated.Code.Should().Be(400, "update: " + updated);
        updated.Error.Should().Be(UnroutableMessage, "update");
        replaced.Code.Should().Be(400, "replace-all: " + replaced);
        replaced.Error.Should().Be(UnroutableMessage, "replace-all");
        // remove-custom-server validates nothing; a bare dot never reaches its handler.
        removed.Code.Should().Be(removeStatus, "remove: " + removed);
    }

    [Theory]
    [InlineData("""{"name":"","config":{"url":"https://a.example.invalid/sse"}}""")]
    [InlineData("""{"config":{"url":"https://a.example.invalid/sse"}}""")]
    [InlineData("""{"name":"   ","config":{"url":"https://a.example.invalid/sse"}}""")]
    public async Task Add_BlankOrMissingName_IsAHard400NamingTheRule(string body)
    {
        // Raw: a missing name cannot be built from the typed request, and the three spellings must all answer alike.
        var reply = await PostToolsAsync("add-custom-server", body);

        reply.Code.Should().Be(400, reply.ToString());
        reply.Error.Should().Be("name is required");
        (await ListServersAsync()).Should().BeEmpty("nothing was stored");
    }

    [Fact]
    public async Task Add_NullOrNumericNameOrObject_IsAHard400_AndAOneCharacterNameIsFine()
    {
        // Raw: a wrong-typed name is a body the typed request cannot carry.
        foreach (var literal in new[] { "null", "5", "true", "{}" })
        {
            var reply = await PostToolsAsync("add-custom-server", $$$"""{"name":{{{literal}}},"config":{"url":"https://a.example.invalid/sse"}}""");

            reply.Code.Should().Be(400, $"{literal}: {reply}");
            reply.Error.Should().Be("name is required", literal);
        }

        (await ListServersAsync()).Should().BeEmpty();

        await RegisterAsync("a", UrlConfig());
        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["a"] = UrlConfig() });
    }

    /// <summary>The counterweight to the refusals: punctuation that merely looks dangerous survives the whole cycle.</summary>
    [Fact]
    public async Task Add_UrlAndShellPunctuationInAName_RoundTripsThroughTheWholeCycle()
    {
        var first = UrlConfig("a.example.invalid");
        var second = UrlConfig("b.example.invalid");

        foreach (var name in new[] { "%2F", "autotest#fragment", "autotest?query=1", "autotest%percent", "autotest server", "autotest.dotted", ".autotest", "autotest." })
        {
            (await AddServerAsync(name, first)).Success.Should().BeTrue(name);
            ShouldBeConfig(await GetServerAsync(name), first, name);

            (await UpdateServerAsync(name, second)).Success.Should().BeTrue(name);
            ShouldBeConfig(await GetServerAsync(name), second, name);

            (await RemoveServerAsync(name)).Success.Should().BeTrue(name);
            (await ListServersAsync()).Should().BeEmpty(name);
        }
    }

    /// <summary>The engine addresses a tool as <c>server_tool</c>, so the underscore is reserved, on add and on the bulk route.</summary>
    [Theory]
    [InlineData("my_server")]
    [InlineData("_leading")]
    [InlineData("trailing_")]
    [InlineData("a_b_c")]
    public async Task Add_UnderscoreInTheName_IsRefused(string name)
    {
        var added = await AddServerAsync(name, UrlConfig());

        added.Success.Should().BeFalse();
        added.Error.Field.Should().Be(AiErrorData.FieldEnum.Name);
        added.Error.Message.Should().Be(UnderscoreMessage);
        (await ListServersAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ReplaceAll_UnderscoreInTheName_IsReportedPerEntry()
    {
        var replaced = await ReplaceAllAsync(new Dictionary<string, object> { ["my_server"] = UrlConfig() });

        replaced.Success.Should().BeFalse();
        replaced.Errors.Should().ContainSingle();
        replaced.Errors[0].Name.Should().Be("my_server");
        replaced.Errors[0].Error.Field.Should().Be(AiErrorData.FieldEnum.Name);
        replaced.Errors[0].Error.Message.Should().Be(UnderscoreMessage);
        (await ListServersAsync()).Should().BeEmpty("nothing was stored");
    }

    /// <summary>
    /// <c>constructor</c> and <c>toString</c> are legal names by every rule the API states, and the config sent with
    /// them is the same valid <c>{url}</c>. They used to come back "Server config must be an object": the store looked
    /// the name up on a plain JS object, walked into Object.prototype and called a function a bad config.
    /// </summary>
    [Fact]
    [Trait("Bug", "82986")]
    public async Task Add_NamedAfterAnObjectPrototypeMember_IsNotRejectedAsIfTheConfigWereBroken()
    {
        // Control: an ordinary-but-suspicious name that is not an Object member.
        await RegisterAsync("prototype", UrlConfig());
        ShouldBeConfig(await GetServerAsync("prototype"), UrlConfig());

        var constructor = await AddServerAsync("constructor", UrlConfig());
        var toString = await AddServerAsync("toString", UrlConfig());

        constructor.Error?.Message.Should().NotBe("Server config must be an object", "the config is valid, the name is what the API dislikes");
        toString.Error?.Message.Should().NotBe("Server config must be an object");
    }

    [Fact]
    public async Task ReplaceAll_ObjectPrototypeNames_RoundTripThroughGetListAndRemove()
    {
        var names = new[] { "constructor", "toString", "valueOf", "hasOwnProperty" };
        var map = names.Select((n, i) => (n, config: UrlConfig($"proto-{i}.example.invalid"))).ToDictionary(x => x.n, x => (object)x.config);

        (await ReplaceAllAsync(map)).Success.Should().BeTrue();

        ShouldHold(await ListServersAsync(), map);
        foreach (var name in names)
        {
            ShouldBeConfig(await GetServerAsync(name), map[name], name);
        }

        (await RemoveServerAsync("constructor")).Success.Should().BeTrue();
        map.Remove("constructor");
        ShouldHold(await ListServersAsync(), map);
    }

    /// <summary>"café" typed as NFC (U+00E9) and as NFD (e + U+0301) look identical in the UI but are two names.</summary>
    [Fact]
    public async Task Add_TheTwoUnicodeSpellingsOfOneName_AreTwoServers()
    {
        var nfc = "autotest-café";
        var nfd = "autotest-café";
        nfc.Normalize(System.Text.NormalizationForm.FormD).Should().Be(nfd);

        var first = UrlConfig("a.example.invalid");
        var second = UrlConfig("b.example.invalid");
        (await AddServerAsync(nfc, first)).Success.Should().BeTrue();
        (await AddServerAsync(nfd, second)).Success.Should().BeTrue();

        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { [nfc] = first, [nfd] = second });
    }

    /// <summary>A non-BMP name used to be the one input that answered an unhandled 500, on the add and on every later read of it.</summary>
    [Fact]
    [Trait("Bug", "82987")]
    public async Task Add_EmojiInTheName_RoundTripsThroughTheWholeCycle()
    {
        const string name = "autotest-🙂";
        var first = UrlConfig("a.example.invalid");
        var second = UrlConfig("b.example.invalid");

        (await AddServerAsync(name, first)).Success.Should().BeTrue("an emoji name is accepted");
        (await ListServersAsync()).Keys.Should().BeEquivalentTo([name], "listed under the key as sent");
        ShouldBeConfig(await GetServerAsync(name), first, "readable by name");

        (await UpdateServerAsync(name, second)).Success.Should().BeTrue("editable by name");
        ShouldBeConfig(await GetServerAsync(name), second);

        (await RemoveServerAsync(name)).Success.Should().BeTrue("removable by name");
        (await ListServersAsync()).Should().BeEmpty();
    }

    /// <summary>128 is accepted, 129 is a hard 400 carrying only a generic "Bad Request" — a model-binding refusal.</summary>
    [Fact]
    public async Task Add_ANameOf128Characters_IsTheLimit()
    {
        var atLimit = new string('n', 128);

        (await AddServerAsync(atLimit, UrlConfig())).Success.Should().BeTrue();

        var refused = await PostToolsAsync("add-custom-server", new { name = new string('n', 129), config = UrlConfig() });
        refused.Code.Should().Be(400, refused.ToString());
        refused.Error.Should().Be("Bad Request");
        (await ListServersAsync()).Keys.Should().BeEquivalentTo([atLimit]);
    }
}
