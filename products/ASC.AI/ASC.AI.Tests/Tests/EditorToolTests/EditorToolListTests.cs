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
/// <c>GET /api/2.0/ai/editor-tools/list</c>: the catalogue the editor's AI panel offers. The checks walk whatever the
/// catalogue holds, so a tool added later is covered without touching this file.
/// </summary>
[Trait("Category", "Functional")]
[Trait("Feature", "AI/EditorTools")]
public class EditorToolListTests(AspireAppFixture fixture) : EditorToolTestBase(fixture)
{
    private static readonly string[] _toolKeys = ["name", "description", "inputSchema", "requireApproval"];

    private static readonly System.Text.RegularExpressions.Regex _readTool = new("^(get|download)_");
    private static readonly System.Text.RegularExpressions.Regex _writeTool = new("^(delete|update|create|upload|move|copy|rename|archive|set|onlyoffice_generate)_");

    [Fact]
    public async Task Owner_GetsThePublishedTools()
    {
        await RequireGeneratorsAsync();
        var reply = await ListAsync();
        var typed = await SdkEditorTools.AiEditorToolsListAsync(TestContext.Current.CancellationToken);

        reply.Status.Should().Be(200, reply.ToString());
        reply.ContentType.Should().Be("application/json");
        reply.Json.EnumerateObject().Select(p => p.Name).Should().Equal("tools");
        typed.Tools.Should().NotBeEmpty();
    }

    [Fact]
    public async Task EveryTool_MatchesTheSdkShape()
    {
        var reply = await ListAsync();
        reply.Status.Should().Be(200, reply.ToString());

        var offenders = new List<string>();
        foreach (var tool in reply.Json.GetProperty("tools").EnumerateArray())
        {
            var name = tool.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null;
            var label = string.IsNullOrWhiteSpace(name) ? "<unnamed>" : name;
            if (string.IsNullOrWhiteSpace(name))
            {
                offenders.Add($"{label}: name is empty");
            }

            if (!tool.TryGetProperty("description", out var description) || description.ValueKind != JsonValueKind.String)
            {
                offenders.Add($"{label}: description is not a string");
            }

            if (!tool.TryGetProperty("requireApproval", out var approval) || approval.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                offenders.Add($"{label}: requireApproval is not a boolean");
            }

            if (!tool.TryGetProperty("inputSchema", out var schema) || schema.ValueKind != JsonValueKind.Object)
            {
                offenders.Add($"{label}: inputSchema is not an object");
            }

            offenders.AddRange(tool.EnumerateObject().Where(p => !_toolKeys.Contains(p.Name)).Select(p => $"{label}: unexpected key {p.Name}"));
        }

        offenders.Should().BeEmpty();
    }

    [Fact]
    public async Task ToolNames_AreUnique()
    {
        var names = (await ToolsAsync()).Select(t => t.Name).ToList();

        names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).Should().BeEmpty();
    }

    [Fact]
    public async Task EveryInputSchema_IsAnObjectSchemaWhoseRequiredArgumentsAreDeclared()
    {
        var offenders = new List<string>();
        foreach (var tool in await ToolsAsync())
        {
            var schema = tool.InputSchema;
            var type = schema.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
            if (type != "object")
            {
                offenders.Add($"{tool.Name}: type is {type}");
            }

            var declared = schema.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object
                ? properties.EnumerateObject().Select(p => p.Name).ToList()
                : null;
            if (declared is null)
            {
                offenders.Add($"{tool.Name}: properties is missing");
            }

            var required = tool.Required.ToList();
            offenders.AddRange(required.Where(key => declared?.Contains(key) != true).Select(key => $"{tool.Name}: required \"{key}\" is not a declared property"));
            if (required.Distinct().Count() != required.Count)
            {
                offenders.Add($"{tool.Name}: required is not a list of distinct names");
            }

            if (properties.ValueKind == JsonValueKind.Object)
            {
                offenders.AddRange(properties.EnumerateObject().Where(p => p.Value.ValueKind != JsonValueKind.Object).Select(p => $"{tool.Name}: property \"{p.Name}\" is not a schema object"));
            }
        }

        offenders.Should().BeEmpty();
    }

    [Fact]
    public async Task RepeatedReads_ReturnTheSameCatalogue()
    {
        var first = await ListAsync();
        var second = await ListAsync();
        var third = await ListAsync();

        first.Status.Should().Be(200, first.ToString());
        second.Text.Should().Be(first.Text);
        third.Text.Should().Be(first.Text);
    }

    /// <summary>
    /// A tool the pattern does not know is listed instead of skipped, so a new tool forces a decision about which
    /// side it is on.
    /// </summary>
    [Fact]
    public async Task RequireApproval_IsOffForReadToolsAndOnForTheOnesThatChangeSomething()
    {
        var tools = await ToolsAsync();

        var wrong = tools
            .Where(t => (_readTool.IsMatch(t.Name) && t.RequireApproval) || (_writeTool.IsMatch(t.Name) && !t.RequireApproval))
            .Select(t => $"{t.Name}: requireApproval={t.RequireApproval}");
        var unclassified = tools.Where(t => !_readTool.IsMatch(t.Name) && !_writeTool.IsMatch(t.Name)).Select(t => t.Name);

        unclassified.Should().BeEmpty();
        wrong.Should().BeEmpty();
    }

    /// <summary>
    /// <c>create_room</c> used to list <c>roomType</c> as required while giving it a default and running without it.
    /// No tool may publish a property as both required and defaulted; the contradiction is what the generic
    /// "remove a required argument" checks have to step around.
    /// </summary>
    [Fact]
    [Trait("Bug", "84395")]
    public async Task NoToolPublishesAPropertyAsBothRequiredAndDefaulted()
    {
        var tools = await ToolsAsync();

        var contradictions = tools
            .SelectMany(t => t.Required.Where(key => t.Property(key).ValueKind == JsonValueKind.Object && t.Property(key).TryGetProperty("default", out _)).Select(key => $"{t.Name}.{key}"));

        contradictions.Should().BeEmpty("required properties that also have a default");
    }
}
