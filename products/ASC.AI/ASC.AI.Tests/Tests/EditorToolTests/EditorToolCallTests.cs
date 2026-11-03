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
/// <c>POST /api/2.0/ai/editor-tools/call</c>: running a tool as the caller and the rules for the tool name. The
/// proof of an effect is read back through the Files API, not taken from a 200. The REST tools the TypeScript suite
/// runs here (<c>delete_file</c>, <c>create_folder</c>, ...) need the DocSpace MCP container and live in
/// <see cref="EditorToolRestToolTests"/>; the generators are the tools every portal has.
/// </summary>
[Trait("Category", "Functional")]
[Trait("Feature", "AI/EditorTools")]
public class EditorToolCallTests(AspireAppFixture fixture) : EditorToolTestBase(fixture)
{
    public static TheoryData<string> InvalidNameBodies => new()
    {
        BodyJson("\"\""),
        BodyJson("null"),
        BodyJson(null),
        BodyJson("7"),
        BodyJson("true"),
        BodyJson("""{"a":1}""")
    };

    public static TheoryData<string> SpellingsTheServerRefuses => new() { "no_such_tool_zz", "   ", "ONLYOFFICE_GENERATE_DOCX" };

    [Theory]
    [MemberData(nameof(Generators))]
    public async Task Generator_CreatesADocumentInMyDocuments(string toolName, string extension)
    {
        await RequireGeneratorsAsync();
        await ActAsOwnerAsync();
        var tool = (await ToolsAsync()).Single(t => t.Name == toolName);
        var folderId = await MyFolderIdAsync();
        var fileName = RandomName();

        var outcome = await RunAsync(toolName, GeneratorArguments(tool, fileName));

        outcome.IsError.Should().BeFalse(outcome.Raw);
        var created = outcome.File;
        created.Should().NotBeNull(outcome.Raw);
        created!.Title.Should().Be(fileName + extension);
        created.ParentId.Should().Be(folderId);
        (await TitlesInAsync(folderId)).Should().Contain(fileName + extension);
    }

    [Theory]
    [MemberData(nameof(InvalidNameBodies))]
    public async Task UnusableName_Returns400(string body)
    {
        // Raw: a name that is null, missing or of the wrong JSON type, which the typed request cannot carry.
        var reply = await CallRawAsync(body);

        reply.Error.Should().Be(UnknownToolName);
        reply.Status.Should().Be(400, reply.ToString());
    }

    /// <summary>
    /// The SDK says an unknown or excluded name "is rejected with 400". The control is a real tool under its exact
    /// name, so the refusal is about the spelling and not about the tool being unavailable to this caller.
    /// </summary>
    [Theory]
    [MemberData(nameof(SpellingsTheServerRefuses))]
    [Trait("Bug", "84394")]
    public async Task UnknownName_IsAnswered400AsTheSdkDocuments(string name)
    {
        await RequireGeneratorsAsync();
        var tool = (await ToolsAsync()).Single(t => t.Name == DocxTool);
        var control = await CallToolAsync(DocxTool, GeneratorArguments(tool, RandomName()));
        OutcomeOf(control).Text.Should().NotContain("tool_not_available");

        var reply = await CallToolAsync(name);

        reply.Error.Should().Be(UnknownToolName);
        reply.Status.Should().Be(400, reply.ToString());
    }

    /// <summary>
    /// The name is looked up after trimming it, so padding reaches the real tool while a different case does not.
    /// Both halves are read from the folder, not from the status alone.
    /// </summary>
    [Fact]
    public async Task WrongCaseRunsNothing_SurroundingWhitespaceIsTrimmedAndRunsTheTool()
    {
        await RequireGeneratorsAsync();
        await ActAsOwnerAsync();
        var tool = (await ToolsAsync()).Single(t => t.Name == DocxTool);
        var folderId = await MyFolderIdAsync();
        var wrongCase = RandomName("Autotest wrong case");
        var padded = RandomName("Autotest padded");
        var exact = RandomName("Autotest exact");

        var refused = await CallToolAsync("ONLYOFFICE_GENERATE_DOCX", GeneratorArguments(tool, wrongCase));
        var trimmed = await CallToolAsync(" onlyoffice_generate_docx ", GeneratorArguments(tool, padded));
        var exactReply = await RunAsync(DocxTool, GeneratorArguments(tool, exact));

        refused.Status.Should().Be(400, "a different case is not the tool's name");
        refused.Error.Should().Be(UnknownToolName);
        trimmed.Status.Should().Be(200, "padding is trimmed and the tool runs: " + trimmed);
        exactReply.IsError.Should().BeFalse(exactReply.Raw);
        var titles = await TitlesInAsync(folderId);
        titles.Should().Contain(exact + ".docx", "the exact name created its file");
        titles.Should().Contain(padded + ".docx", "and so did the padded spelling");
        titles.Should().NotContain(wrongCase + ".docx", "the wrong case left nothing behind");
    }

    /// <summary>
    /// SDK: "Treated as empty when it is not an object". The reference is the answer to an explicit {} — the same
    /// refusal naming the same missing field.
    /// </summary>
    [Fact]
    public async Task ArgumentsThatAreNotAnObject_AreTreatedAsEmpty()
    {
        await RequireGeneratorsAsync();
        var reference = OutcomeOf(await CallToolAsync(DocxTool, new { }));
        reference.IsError.Should().BeTrue(reference.Raw);
        reference.Text.Should().Contain("fileName");

        foreach (var (label, argumentsJson) in new (string, string?)[] { ("missing", null), ("null", "null"), ("a string", "\"abc\""), ("an array", "[1,2]"), ("a number", "5") })
        {
            // Raw: arguments that are not an object (or are missing), which the typed request cannot carry.
            var outcome = OutcomeOf(await CallRawAsync(BodyJson($"\"{DocxTool}\"", argumentsJson)));

            outcome.Text.Should().Be(reference.Text, $"arguments as {label}");
            outcome.IsError.Should().BeTrue($"arguments as {label}");
        }
    }

    [Fact]
    public async Task EveryPublishedTool_IsDispatched_NoneIsNotAvailable()
    {
        // An empty catalogue would make the loop below assert nothing.
        await RequireGeneratorsAsync();
        var offenders = new List<string>();
        foreach (var tool in await ToolsAsync())
        {
            var reply = await CallToolAsync(tool.Name, new { });
            if (reply.Status != 200 || OutcomeOf(reply).Text.Contains("tool_not_available", StringComparison.Ordinal))
            {
                offenders.Add($"{tool.Name}: {reply.Status} {Shorten(reply.Text)}");
            }
        }

        offenders.Should().BeEmpty();
    }

    [Fact]
    public async Task NoArgumentShape_TurnsIntoAServerError()
    {
        // An empty catalogue would make the loop below assert nothing.
        await RequireGeneratorsAsync();
        var offenders = new List<string>();
        foreach (var tool in await ToolsAsync())
        {
            var shapes = new List<string> { "{}", "null", "\"x\"", "[1]", """{"zz":{"nested":[null]}}""" };
            if (tool.InputSchema.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object)
            {
                shapes.AddRange(properties.EnumerateObject().Select(p => $"{{{JsonSerializer.Serialize(p.Name)}:{{\"a\":[1]}}}}"));
            }

            foreach (var shape in shapes)
            {
                // Raw: argument shapes that are not an object, which the typed request cannot carry.
                var reply = await CallRawAsync(BodyJson(JsonSerializer.Serialize(tool.Name), shape));
                if (reply.Status >= 500)
                {
                    offenders.Add($"{tool.Name} {shape}: {reply.Status}");
                }
            }
        }

        offenders.Should().BeEmpty();
    }

    private static string Shorten(string text) => text.Length <= 80 ? text : text[..80];
}
