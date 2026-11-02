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
/// <c>tools/set-allow-always</c>, <c>get-allow-always</c>, <c>is-allow-always</c> as stored state: the list is flat
/// <c>{serverType}_{toolName}</c> strings, a decision about one server type never answers for another, and the write
/// refuses a body the library would otherwise coerce.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Mcp")]
public class McpAllowAlwaysTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    [Fact]
    public async Task SetAllowAlways_MarksAToolPreApproved()
    {
        (await AllowedAsync()).Should().BeEmpty();

        await SetAllowAlwaysAsync("docspace", "delete_file");

        (await AllowedAsync()).Should().Contain("docspace_delete_file");
        (await IsAllowedAsync("docspace", "delete_file")).Should().BeTrue();
    }

    [Fact]
    public async Task SetAllowAlways_False_RevokesThePreApproval()
    {
        await SetAllowAlwaysAsync("docspace", "delete_file");
        await SetAllowAlwaysAsync("docspace", "delete_file", false);

        (await IsAllowedAsync("docspace", "delete_file")).Should().BeFalse();
        (await AllowedAsync()).Should().NotContain("docspace_delete_file");
    }

    [Fact]
    public async Task SetAllowAlways_ListIsFlatTokens_SetIsIdempotent_AndRevokingOneEntryTouchesNoOther()
    {
        await RegisterAsync("alpha");
        await RegisterAsync("beta", UrlConfig("beta.example.invalid"));

        await SetAllowAlwaysAsync("alpha", "t1");
        await SetAllowAlwaysAsync("alpha", "t1");
        await SetAllowAlwaysAsync("alpha", "t2");
        await SetAllowAlwaysAsync("beta", "t1");

        (await AllowedAsync()).Order().Should().Equal("alpha_t1", "alpha_t2", "beta_t1");
        (await IsAllowedAsync("alpha", "t1")).Should().BeTrue();
        (await IsAllowedAsync("alpha", "unlisted")).Should().BeFalse();

        await SetAllowAlwaysAsync("alpha", "t1", false);
        await SetAllowAlwaysAsync("alpha", "t1", false);

        (await AllowedAsync()).Order().Should().Equal("alpha_t2", "beta_t1");
        (await IsAllowedAsync("alpha", "t1")).Should().BeFalse();
        (await IsAllowedAsync("alpha", "t2")).Should().BeTrue();
        (await IsAllowedAsync("beta", "t1")).Should().BeTrue("same tool name, other server");
    }

    /// <summary>
    /// A client tool called <c>delete_file</c> may be offered by any editor and a user would approve it without much
    /// thought: it must not pre-approve <c>docspace_delete_file</c>. The tool name is the destructive DocSpace one.
    /// </summary>
    [Fact]
    [Trait("Bug", "83161")]
    public async Task IsAllowAlways_APreApprovalGrantedForOneServerType_DoesNotAnswerForAnyOtherType()
    {
        await SetAllowAlwaysAsync(HostServer, "delete_file");

        (await IsAllowedAsync(HostServer, "delete_file")).Should().BeTrue("the type it was written under");
        (await AllowedAsync()).Should().Contain(Token(HostServer, "delete_file"), "the read-back list prefixes each entry with its server type");

        (await IsAllowedAsync("docspace", "delete_file")).Should().BeFalse("a host tool's pre-approval must not cover the DocSpace tool of that name");
        (await IsAllowedAsync("not-a-server-type", "delete_file")).Should().BeFalse("an unknown server type has nothing");
    }

    /// <summary>The other direction of Bug 83161.</summary>
    [Fact]
    [Trait("Bug", "83161")]
    public async Task IsAllowAlways_APreApprovalForADocSpaceTool_DoesNotCoverTheHostToolOfThatName()
    {
        await SetAllowAlwaysAsync("docspace", "delete_file");

        (await IsAllowedAsync("docspace", "delete_file")).Should().BeTrue("control: the pair that was written");
        foreach (var serverType in new[] { HostServer, WebSearch, "image-generation" })
        {
            (await IsAllowedAsync(serverType, "delete_file")).Should().BeFalse($"{serverType}/delete_file");
        }

        (await AllowedAsync()).Should().Equal("docspace_delete_file");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("null")]
    [InlineData(null)]
    public async Task SetAllowAlways_FalsyValue_Is400AndGrantsNothing(string? value)
    {
        await RegisterAsync("srv");
        var valueField = value is null ? string.Empty : $",\"value\":{value}";

        // Raw: the value is a non-nullable bool in the typed request.
        var reply = await PutToolsAsync("set-allow-always", $$"""{"serverType":"srv","toolName":"tool"{{valueField}}}""");

        reply.Code.Should().Be(400, reply.ToString());
        reply.Error.Should().Be("value is required and must be a boolean");
        (await IsAllowedAsync("srv", "tool")).Should().BeFalse();
        (await AllowedAsync()).Should().BeEmpty();
    }

    /// <summary>The string "false" used to be coerced to true — a permanent approval nobody asked for.</summary>
    [Fact]
    [Trait("Bug", "84404")]
    public async Task SetAllowAlways_TheStringFalse_DoesNotGrantAPermanentApproval()
    {
        await RegisterAsync("srv");

        await PutToolsAsync("set-allow-always", """{"serverType":"srv","toolName":"risky","value":"false"}""");

        (await IsAllowedAsync("srv", "risky")).Should().BeFalse("the string \"false\" must not approve the tool");
    }

    /// <summary>A body without a toolName used to be a 500.</summary>
    [Theory]
    [Trait("Bug", "84405")]
    [InlineData("""{"serverType":"srv","value":true}""")]
    [InlineData("""{"serverType":"srv","toolName":null,"value":true}""")]
    public async Task SetAllowAlways_NoToolNameOrNull_IsA400AndNotA500(string body)
    {
        await RegisterAsync("srv");
        await SetAllowAlwaysAsync("srv", "kept");

        var reply = await PutToolsAsync("set-allow-always", body);

        (await AllowedAsync()).Should().Equal("srv_kept");
        reply.Code.Should().Be(400, reply.ToString());
    }

    [Theory]
    [InlineData("?toolName=kept")]
    [InlineData("?serverType=docspace")]
    [InlineData("?serverType=&toolName=")]
    [InlineData("")]
    public async Task IsAllowAlways_MissingParameters_Is400_AndALeftOverListIsUntouched(string query)
    {
        await SetAllowAlwaysAsync("docspace", "kept");

        var reply = await GetToolsAsync("is-allow-always", query);

        reply.Code.Should().Be(400, reply.ToString());
        reply.Error.Should().Be("serverType and toolName required");
        (await AllowedAsync()).Should().Equal("docspace_kept");
    }

    /// <summary>set-allow-always (unlike set-disabled) does not validate the server type: it is stored as given.</summary>
    [Fact]
    public async Task SetAllowAlways_AnInventedServerType_IsStoredAsGiven()
    {
        await SetAllowAlwaysAsync("docspace", "kept");
        await SetAllowAlwaysAsync("invented-type", "t");

        (await IsAllowedAsync("invented-type", "t")).Should().BeTrue();
        (await AllowedAsync()).Order().Should().Equal("docspace_kept", "invented-type_t");
    }
}
