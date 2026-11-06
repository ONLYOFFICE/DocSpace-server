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
/// The arguments of <c>POST /api/2.0/ai/editor-tools/call</c>: the cases are built from the <c>inputSchema</c> that
/// <c>list</c> publishes, so a changed schema or a new tool is covered without touching this file.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/EditorTools")]
public class EditorToolArgumentTests(AspireAppFixture fixture) : EditorToolTestBase(fixture)
{
    [Fact]
    public async Task EmptyArguments_NameEveryMissingRequiredArgument()
    {
        await RequireGeneratorsAsync();
        var tools = (await ToolsAsync()).Where(t => t.TrulyRequired.Any()).ToList();
        tools.Should().NotBeEmpty();

        var offenders = new List<string>();
        foreach (var tool in tools)
        {
            var outcome = OutcomeOf(await CallToolAsync(tool.Name, new { }));
            if (!outcome.IsError)
            {
                offenders.Add($"{tool.Name}: not an error");
            }

            // A generator reports the first missing parameter only.
            var mustName = tool.IsGenerator ? tool.TrulyRequired.Take(1) : tool.TrulyRequired;
            offenders.AddRange(mustName
                .Where(key => !outcome.Text.Contains(key, StringComparison.Ordinal))
                .Select(key => $"{tool.Name}: \"{key}\" is not named in \"{Shorten(outcome.Text)}\""));
        }

        offenders.Should().BeEmpty();
    }

    [Fact]
    public async Task RemovingARequiredArgument_OneAtATime_IsRefusedForEveryTool()
    {
        await RequireGeneratorsAsync();
        var offenders = new List<string>();
        var cases = 0;
        foreach (var tool in await ToolsAsync())
        {
            foreach (var key in tool.TrulyRequired)
            {
                cases++;
                var arguments = SampleArguments(tool.InputSchema);
                arguments.Remove(key);

                var reply = await CallToolAsync(tool.Name, arguments);
                var outcome = OutcomeOf(reply);
                if (reply.Status != 200 || !outcome.IsError || !outcome.Text.Contains(key, StringComparison.Ordinal))
                {
                    offenders.Add($"{tool.Name} without \"{key}\": {reply.Status} isError={outcome.IsError} \"{Shorten(outcome.Text)}\"");
                }
            }
        }

        cases.Should().BeGreaterThan(0);
        offenders.Should().BeEmpty();
    }

    /// <summary>
    /// A generator takes a null file name and writes a document anyway (Bug 84396), so null is left to the
    /// generator tests below and only the wrong type is asserted for it.
    /// </summary>
    [Fact]
    public async Task ARequiredArgumentOfTheWrongType_OrNull_IsRefusedForEveryTool()
    {
        await RequireGeneratorsAsync();
        var offenders = new List<string>();
        var cases = 0;
        foreach (var tool in await ToolsAsync())
        {
            foreach (var key in tool.TrulyRequired)
            {
                var schema = tool.Property(key);
                var nullable = schema.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.Array
                    && type.EnumerateArray().Any(t => t.GetString() == "null");
                var variants = new List<(string Label, object? Value)>();
                var wrong = WrongTypeValue(schema);
                if (wrong is not null)
                {
                    variants.Add(("a wrong type", wrong));
                }

                if (!nullable && !tool.IsGenerator)
                {
                    variants.Add(("null", null));
                }

                foreach (var (label, value) in variants)
                {
                    cases++;
                    var arguments = SampleArguments(tool.InputSchema);
                    arguments[key] = value;

                    var reply = await CallToolAsync(tool.Name, arguments);
                    var outcome = OutcomeOf(reply);
                    if (reply.Status != 200 || !outcome.IsError)
                    {
                        offenders.Add($"{tool.Name}.{key} as {label}: {reply.Status} isError={outcome.IsError} \"{Shorten(outcome.Text)}\"");
                    }
                }
            }
        }

        cases.Should().BeGreaterThan(0);
        offenders.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Generators))]
    [Trait("Bug", "84396")]
    public async Task Generator_WithANullFileName_CreatesNoDocument(string toolName, string extension)
    {
        await RequireGeneratorsAsync();
        await ActAsOwnerAsync();
        var tool = (await ToolsAsync()).Single(t => t.Name == toolName);
        tool.Required.Should().Contain("fileName");
        var folderId = await MyFolderIdAsync();
        var before = await TitlesInAsync(folderId);
        var arguments = GeneratorArguments(tool, string.Empty);
        arguments["fileName"] = null;

        var outcome = await RunAsync(toolName, arguments);

        var created = (await TitlesInAsync(folderId)).Except(before).ToList();
        created.Should().BeEmpty($"a required argument that is null must not produce a {extension} document");
        outcome.IsError.Should().BeTrue(outcome.Raw);
    }

    /// <summary>
    /// Every inputSchema used to say <c>additionalProperties:false</c> while the call accepted an unknown property and
    /// ran. What must hold is that the published schema and the behaviour agree: forbidden means refused, not
    /// forbidden means tolerated.
    /// </summary>
    [Fact]
    [Trait("Bug", "84397")]
    public async Task APropertyThePublishedSchemaForbids_IsRefused_AndOneItAllows_IsAccepted()
    {
        await RequireGeneratorsAsync();
        await ActAsOwnerAsync();
        var tool = (await ToolsAsync()).Single(t => t.Name == DocxTool);
        var forbidsExtras = tool.InputSchema.TryGetProperty("additionalProperties", out var extras) && extras.ValueKind == JsonValueKind.False;
        var folderId = await MyFolderIdAsync();
        var fileName = RandomName("Autotest extra");
        var arguments = GeneratorArguments(tool, fileName);
        arguments["zzNotInTheSchema"] = 1;

        var outcome = await RunAsync(DocxTool, arguments);

        var created = (await TitlesInAsync(folderId)).Contains(fileName + ".docx");
        created.Should().Be(
            !forbidsExtras,
            forbidsExtras ? "the schema forbids extras, so the document must not be created" : "the schema does not forbid extras, so the document is created");
        outcome.IsError.Should().Be(forbidsExtras);
    }

    private static string Shorten(string text) => text.Length <= 120 ? text : text[..120];
}
