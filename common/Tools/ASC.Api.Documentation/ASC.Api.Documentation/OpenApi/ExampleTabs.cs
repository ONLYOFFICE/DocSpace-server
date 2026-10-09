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

namespace ASC.Api.Documentation;

/// <summary>
/// Puts the request examples on the endpoint pages: a section with one tab per language, in the
/// place the section template marked for it.
/// </summary>
/// <remarks>
/// The examples go onto the pages once they are cut rather than being written by the section
/// template, because both steps the section document goes through before that would break them:
/// <see cref="MdxSafe"/> escapes the `&lt;` that opens a tab, and <see cref="MarkdownSlicer"/>
/// promotes heading-like lines without telling code from prose, so a `### ` comment inside an
/// example would turn into a heading. Nothing reads a page after this.
/// <para>
/// Each tab reads its examples from a snippet file of its own, which holds the snippets of the
/// whole API between `&lt;!--snippet:operationId--&gt;` markers and ends with `&lt;!--/snippets--&gt;`.
/// They are matched to the pages by operation id, the one name every language shares: the
/// methods the examples call are named differently in each of them.
/// </para>
/// </remarks>
internal static class ExampleTabs
{
    /// <summary>
    /// Where the section template wants an operation's examples: a line of its own, directly
    /// above the heading the examples precede.
    /// </summary>
    private const string ExampleMarker = "<!--example-->";

    private const string SnippetMarkerPrefix = "<!--snippet:";
    private const string SnippetsEndMarker = "<!--/snippets-->";
    private const string MarkerSuffix = "-->";

    /// <summary>
    /// Reads the snippets of one tab, keyed by operation id.
    /// </summary>
    /// <remarks>
    /// A marker with nothing under it is an operation the language has no example for, and the
    /// tab is left off that page rather than shown empty. A file that stops before its end marker
    /// was cut short, and the snippet it stopped in could be cut short with it.
    /// </remarks>
    public static async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ReadSnippetsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var lines = await File.ReadAllLinesAsync(path, cancellationToken);
        var snippets = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        string? operationId = null;
        var body = new List<string>();
        var ended = false;

        foreach (var line in lines)
        {
            if (line.StartsWith(SnippetMarkerPrefix, StringComparison.Ordinal)
                && line.EndsWith(MarkerSuffix, StringComparison.Ordinal))
            {
                Add(snippets, operationId, body, path);

                operationId = line[SnippetMarkerPrefix.Length..^MarkerSuffix.Length];
                body = [];
                continue;
            }

            if (line.StartsWith(SnippetsEndMarker, StringComparison.Ordinal))
            {
                Add(snippets, operationId, body, path);

                ended = true;
                break;
            }

            if (operationId != null)
            {
                body.Add(line);
            }
        }

        if (!ended)
        {
            throw new Exception($"{path} ends without {SnippetsEndMarker}");
        }

        return snippets;
    }

    /// <summary>
    /// Replaces the example marker on every page of a section: with the examples the tabs hold for
    /// that operation, or with nothing when none of them holds one. Returns how many pages got an
    /// example section.
    /// </summary>
    /// <remarks>
    /// The marker goes either way. Like the slicer's own markers, it is bookkeeping between the
    /// template and this tool and never reaches a published page.
    /// <para>
    /// The tabs hold the snippets of this section's operations only. That every snippet found a
    /// page is checked across the whole API once all sections are cut, by the command.
    /// </para>
    /// </remarks>
    public static async Task<int> ApplyAsync(
        IReadOnlyList<MarkdownSlicer.SlicedOperation> operations,
        string groupId,
        IReadOnlyList<TabSnippets> tabs,
        CancellationToken cancellationToken = default)
    {
        var exampled = 0;

        foreach (var operation in operations)
        {
            var lines = new List<string>(await File.ReadAllLinesAsync(operation.Path, cancellationToken));
            var marker = FindMarker(lines, operation);

            lines.RemoveAt(marker);

            var examples = tabs.Where(tab => tab.Snippets.ContainsKey(operation.OperationId)).ToList();

            if (examples.Count > 0)
            {
                lines.InsertRange(marker, Section(groupId, examples, operation.OperationId));
                exampled++;
            }

            await TextFile.WriteLinesAsync(operation.Path, lines, cancellationToken);
        }

        return exampled;
    }

    private static void Add(
        Dictionary<string, IReadOnlyList<string>> snippets,
        string? operationId,
        List<string> body,
        string path)
    {
        if (operationId == null)
        {
            return;
        }

        var snippet = MarkdownSlicer.Trim(body);

        if (snippet.Count == 0)
        {
            return;
        }

        if (!snippets.TryAdd(operationId, snippet))
        {
            throw new Exception($"'{operationId}' has two examples in {path}");
        }
    }

    /// <summary>
    /// The marked line. Exactly one: without it the examples would silently stop being published,
    /// and with two of them they would be printed twice.
    /// </summary>
    private static int FindMarker(List<string> lines, MarkdownSlicer.SlicedOperation operation)
    {
        var index = lines.IndexOf(ExampleMarker);

        if (index < 0 || lines.LastIndexOf(ExampleMarker) != index)
        {
            throw new Exception(
                $"The page of '{operation.OperationId}' does not carry exactly one {ExampleMarker}: {operation.Path}");
        }

        return index;
    }

    /// <summary>
    /// The example section put where the marker was. It ends on a blank line, because the marker
    /// stands directly above the next heading.
    /// </summary>
    /// <remarks>
    /// The imports stay with the tabs rather than heading the page: MDX takes an import anywhere at
    /// the top level, and the Markdown twin of the page, which is what agents read, then still opens
    /// with the endpoint's own heading. Each tag stands on a line of its own and the code is fenced
    /// off by blank lines, which is what makes MDX read the code inside a tab as Markdown.
    /// </remarks>
    private static IEnumerable<string> Section(string groupId, List<TabSnippets> examples, string operationId)
    {
        yield return "## Example";
        yield return string.Empty;
        yield return "import Tabs from '@theme/Tabs';";
        yield return "import TabItem from '@theme/TabItem';";
        yield return string.Empty;
        yield return $"<Tabs groupId=\"{groupId}\" queryString>";

        foreach (var example in examples)
        {
            yield return $"<TabItem value=\"{example.Tab.Value}\" label=\"{example.Tab.Label}\">";
            yield return string.Empty;

            foreach (var line in example.Snippets[operationId])
            {
                yield return line;
            }

            yield return string.Empty;
            yield return "</TabItem>";
        }

        yield return "</Tabs>";
        yield return string.Empty;
    }

    /// <summary>
    /// One tab: <paramref name="Value"/> names it in the page URL and names the snippet file it is
    /// read from, <paramref name="Label"/> is what the reader is shown.
    /// </summary>
    internal sealed record Tab(string Value, string Label);

    /// <summary>
    /// The snippets one tab holds for a section, keyed by operation id.
    /// </summary>
    internal sealed record TabSnippets(Tab Tab, IReadOnlyDictionary<string, IReadOnlyList<string>> Snippets);
}
