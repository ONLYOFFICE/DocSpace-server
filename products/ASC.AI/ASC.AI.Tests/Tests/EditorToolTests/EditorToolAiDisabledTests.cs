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

namespace ASC.AI.Tests.Tests.EditorToolTests;

/// <summary>
/// The portal AI switch as it applies to the editor tools: <c>list</c> and every <c>call</c> that gets past the body
/// check are refused with 403, and a body without a usable name is still a 400 (validation runs before the switch).
/// Each test puts the portal in the state it claims to start from and reads the switch back through a gated route.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/EditorTools")]
public class EditorToolAiDisabledTests(AspireAppFixture fixture) : EditorToolTestBase(fixture)
{
    public static TheoryData<string> BodiesWithoutAUsableName => new()
    {
        "{}",
        BodyJson("null"),
        BodyJson("7")
    };

    [Fact]
    public async Task List_Returns403WhenTheSwitchIsOff()
    {
        var before = await ListAsync();
        await SetPortalAiAccessAsync(false);

        var reply = await UntilRefusedAsync(ListAsync);

        before.Status.Should().Be(200, "the list works while the switch is on");
        reply.Error.Should().Be("Forbidden");
        reply.Status.Should().Be(403, reply.ToString());
    }

    [Fact]
    public async Task Generator_Returns403AndCreatesNothingWhenTheSwitchIsOff()
    {
        await RequireGeneratorsAsync();
        await ActAsOwnerAsync();
        var tool = (await ToolsAsync()).Single(t => t.Name == DocxTool);
        var folderId = await MyFolderIdAsync();
        var onTitle = RandomName("Autotest on");
        var on = await RunAsync(DocxTool, GeneratorArguments(tool, onTitle));
        await SwitchAiOffAsync();
        var offTitle = RandomName("Autotest off");

        var reply = await CallToolAsync(DocxTool, GeneratorArguments(tool, offTitle));

        var titles = await TitlesInAsync(folderId);
        on.IsError.Should().BeFalse(on.Raw);
        titles.Should().Contain(onTitle + ".docx");
        titles.Should().NotContain(offTitle + ".docx");
        reply.Error.Should().Be("Forbidden");
        reply.Status.Should().Be(403, reply.ToString());
    }

    [Fact]
    public async Task UnknownToolName_Returns403WhenTheSwitchIsOff()
    {
        await SetPortalAiAccessAsync(false);

        var reply = await UntilRefusedAsync(() => CallToolAsync("no_such_tool_zz"));

        reply.Error.Should().Be("Forbidden");
        reply.Status.Should().Be(403, reply.ToString());
    }

    /// <summary>
    /// The trap for any off-state test: a body without a usable name is a 400 even with the switch off, so such a test
    /// never sees the 403 it claims to assert.
    /// </summary>
    [Theory]
    [MemberData(nameof(BodiesWithoutAUsableName))]
    public async Task BodyValidation_RunsBeforeTheSwitchIsChecked(string body)
    {
        await SwitchAiOffAsync();

        // Raw: bodies with a missing or wrong-typed name, which the typed request cannot carry.
        var reply = await CallRawAsync(body);

        reply.Error.Should().Be(UnknownToolName);
        reply.Status.Should().Be(400, reply.ToString());
    }

    [Fact]
    public async Task EmptyName_IsValidatedBeforeTheSwitchIsChecked()
    {
        await SwitchAiOffAsync();

        var reply = await CallToolAsync(string.Empty);

        reply.Error.Should().Be(UnknownToolName);
        reply.Status.Should().Be(400, reply.ToString());
    }
}
