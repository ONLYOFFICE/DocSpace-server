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
/// Rewrites a rendered Markdown document into the subset of Markdown that Docusaurus can compile
/// as MDX.
/// </summary>
/// <remarks>
/// MDX is not Markdown. Outside code, `{` opens a JavaScript expression and `&lt;` opens a JSX tag,
/// so an OpenAPI path template (`/api/2.0/apps/{id}`), a JSON example in a description or a stray
/// `&lt;br&gt;` inside one does not print - it fails the site build, and it fails it for the whole
/// document rather than for the one endpoint that carries it.
///
/// Escaping here rather than in the descriptions themselves keeps the OpenAPI documents, and the
/// XML comments they come from, free of a downstream renderer's syntax: every other consumer of
/// the same text (the SDKs, the published contract) would otherwise have to undo it.
/// </remarks>
internal static class MdxSafe
{
    private const string LineBreak = "<br/>";

    /// <summary>
    /// Escapes the whole document in place. Safe to run once, and only once: it is applied to the
    /// generator's output before anything is cut out of it, so every document derived from it is
    /// escaped exactly the same way.
    /// </summary>
    public static async Task ApplyAsync(string path, CancellationToken cancellationToken = default)
    {
        var lines = await File.ReadAllLinesAsync(path, cancellationToken);

        var escaped = new string[lines.Length];
        var fenced = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (IsFence(line))
            {
                fenced = !fenced;
                escaped[i] = line;
                continue;
            }

            // The slicer's own markers are HTML comments and are cut out of both outputs before
            // anything compiles them, so they are left exactly as the template wrote them.
            escaped[i] = fenced || IsMarker(line) ? line : Escape(line);
        }

        await TextFile.WriteLinesAsync(path, escaped, cancellationToken);
    }

    /// <summary>
    /// A fenced code block opens and closes on a line of its own, so the block is tracked by
    /// toggling on those lines rather than by matching the fence character and its length: the
    /// template emits no nested or indented fences, and a stricter match would only add a way to
    /// get the state wrong.
    /// </summary>
    private static bool IsFence(string line)
    {
        var trimmed = line.TrimStart();

        return trimmed.StartsWith("```", StringComparison.Ordinal)
            || trimmed.StartsWith("~~~", StringComparison.Ordinal);
    }

    private static bool IsMarker(string line) => line.TrimStart().StartsWith("<!--", StringComparison.Ordinal);

    private static string Escape(string line)
    {
        var escaped = new StringBuilder(line.Length);
        var index = 0;

        while (index < line.Length)
        {
            var character = line[index];

            if (character == '`')
            {
                var opening = BacktickRun(line, index);
                var closing = FindClosingRun(line, index + opening, opening);

                if (closing >= 0)
                {
                    // Inside a code span MDX evaluates nothing, so the span is copied across
                    // untouched - escaping it would print the backslashes.
                    escaped.Append(line, index, closing + opening - index);
                    index = closing + opening;
                    continue;
                }
            }

            switch (character)
            {
                case '<':
                    // The one tag the pages are allowed to carry: the table cells written by the
                    // generator use it where a description has a line break, and a cell cannot
                    // hold a real one.
                    if (line.AsSpan(index).StartsWith(LineBreak, StringComparison.OrdinalIgnoreCase))
                    {
                        escaped.Append(LineBreak);
                        index += LineBreak.Length;
                        continue;
                    }

                    escaped.Append("&lt;");
                    break;

                case '{':
                case '}':
                    if (index == 0 || line[index - 1] != '\\')
                    {
                        escaped.Append('\\');
                    }

                    escaped.Append(character);
                    break;

                default:
                    escaped.Append(character);
                    break;
            }

            index++;
        }

        return escaped.ToString();
    }

    /// <summary>
    /// A code span is closed by a backtick run of the same length as the one that opened it, and
    /// an unclosed run is not a code span at all - the rest of the line is then ordinary text.
    /// </summary>
    private static int FindClosingRun(string line, int start, int length)
    {
        for (var index = start; index < line.Length; index++)
        {
            if (line[index] != '`')
            {
                continue;
            }

            var run = BacktickRun(line, index);

            if (run == length)
            {
                return index;
            }

            index += run - 1;
        }

        return -1;
    }

    private static int BacktickRun(string line, int start)
    {
        var end = start;

        while (end < line.Length && line[end] == '`')
        {
            end++;
        }

        return end - start;
    }
}
