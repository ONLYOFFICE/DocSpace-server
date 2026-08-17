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
/// Cuts a rendered service document into one document per operation, and strips the markers it
/// cut on out of the service document afterwards.
/// </summary>
/// <remarks>
/// The operations are delimited by comments the template emits (`&lt;!--op:id--&gt;`) rather than
/// found by matching heading levels: the slicer would otherwise silently mis-cut the moment the
/// template's headings move, and a documentation page that is quietly wrong is worse than a build
/// that fails. The markers never reach either output.
/// </remarks>
internal static class MarkdownSlicer
{
    private const string OperationMarkerPrefix = "<!--op:";
    private const string OperationMarkerSuffix = "-->";
    private const string OperationsEndMarker = "<!--/ops-->";

    public static async Task<IReadOnlyList<SlicedOperation>> SliceAsync(
        string documentPath,
        string documentName,
        string outputDirectory,
        string modelLinkBase,
        CancellationToken cancellationToken = default)
    {
        var lines = await File.ReadAllLinesAsync(documentPath, cancellationToken);

        Directory.CreateDirectory(outputDirectory);

        var results = new List<SlicedOperation>();
        var reference = $"{modelLinkBase}{documentName}.md";

        string? operationId = null;
        var body = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith(OperationMarkerPrefix, StringComparison.Ordinal))
            {
                await WriteAsync(results, operationId, body, outputDirectory, reference, cancellationToken);

                operationId = line[OperationMarkerPrefix.Length..^OperationMarkerSuffix.Length];
                body.Clear();
                continue;
            }

            if (line.StartsWith(OperationsEndMarker, StringComparison.Ordinal))
            {
                await WriteAsync(results, operationId, body, outputDirectory, reference, cancellationToken);

                operationId = null;
                body.Clear();
                continue;
            }

            if (operationId != null)
            {
                body.Add(line);
            }
        }

        // The markers exist to be cut on, not to be read - the service document is rewritten
        // without them so that neither output carries the scaffolding.
        await File.WriteAllLinesAsync(
            documentPath,
            lines.Where(line => !line.StartsWith(OperationMarkerPrefix, StringComparison.Ordinal)
                && !line.StartsWith(OperationsEndMarker, StringComparison.Ordinal)),
            cancellationToken);

        return results;
    }

    private static async Task WriteAsync(
        List<SlicedOperation> results,
        string? operationId,
        List<string> body,
        string outputDirectory,
        string reference,
        CancellationToken cancellationToken)
    {
        if (operationId == null)
        {
            return;
        }

        var fileName = $"{Slug(operationId)}.md";
        var path = Path.Combine(outputDirectory, fileName);

        var trimmed = Trim(body);

        await File.WriteAllTextAsync(path, BuildDocument(trimmed, reference), cancellationToken);

        results.Add(new SlicedOperation(operationId, fileName, path, Endpoint(trimmed), Summary(trimmed)));
    }

    /// <summary>
    /// The `METHOD /path` line the template prints under the signature, without its backticks.
    /// </summary>
    /// <remarks>
    /// Empty when the line is not where it is expected. What this feeds is an index, and losing a
    /// description there is a blemish - failing the whole build over it would not be.
    /// </remarks>
    private static string Endpoint(List<string> body)
    {
        var line = body.Find(IsEndpoint);

        return line == null ? string.Empty : line.Trim('`');
    }

    /// <summary>
    /// The operation summary: the first prose line after the endpoint.
    /// </summary>
    private static string Summary(List<string> body)
    {
        var endpoint = body.FindIndex(IsEndpoint);

        if (endpoint < 0)
        {
            return string.Empty;
        }

        for (var i = endpoint + 1; i < body.Count; i++)
        {
            var line = body[i].Trim();

            if (line.Length == 0)
            {
                continue;
            }

            // A heading means the operation states no summary at all and the next section already
            // started; taking that would caption the endpoint with the word "Parameters".
            return line.StartsWith('#') || line.StartsWith('>') ? string.Empty : line;
        }

        return string.Empty;
    }

    private static bool IsEndpoint(string line) =>
        line.StartsWith('`') && line.EndsWith('`') && line.Contains(" /", StringComparison.Ordinal);

    private static string BuildDocument(List<string> trimmed, string reference)
    {
        // In the service document an operation is a third-level heading under its class; on its
        // own it is the document, so everything moves up two levels.
        var content = trimmed.Select(Promote).ToList();

        // Models keep living in the service document: inlining them here would repeat the same
        // DTO across hundreds of documents. The in-document fragments they are written as have to
        // become links back to it, or they resolve to nothing.
        var hasModelLinks = content.Any(line => line.Contains("](#", StringComparison.Ordinal));

        content = [.. content.Select(line => line.Replace("](#", $"]({reference}#", StringComparison.Ordinal))];

        if (hasModelLinks)
        {
            content.Insert(1, string.Empty);
            content.Insert(2, $"Referenced types are defined in the [full reference]({reference}).");
        }

        return string.Join(Environment.NewLine, content) + Environment.NewLine;
    }

    private static string Promote(string line)
    {
        if (line.StartsWith("#### ", StringComparison.Ordinal))
        {
            return string.Concat("## ", line.AsSpan("#### ".Length));
        }

        return line.StartsWith("### ", StringComparison.Ordinal)
            ? string.Concat("# ", line.AsSpan("### ".Length))
            : line;
    }

    private static List<string> Trim(List<string> body)
    {
        var first = 0;
        var last = body.Count - 1;

        while (first <= last && string.IsNullOrWhiteSpace(body[first]))
        {
            first++;
        }

        while (last >= first && string.IsNullOrWhiteSpace(body[last]))
        {
            last--;
        }

        return body.GetRange(first, last - first + 1);
    }

    /// <summary>
    /// The file name the operation is published under.
    /// </summary>
    /// <remarks>
    /// This reproduces lodash's `kebabCase`, which is what docusaurus-plugin-openapi-docs names
    /// its pages with: the documents are meant to sit at the page's own URL with `.md` appended,
    /// so any disagreement here puts them somewhere nobody looks. Verified to agree with lodash
    /// on every operation id in the joined document.
    /// </remarks>
    private static string Slug(string operationId)
    {
        var words = new List<string>();
        var word = new StringBuilder();

        void Flush()
        {
            if (word.Length > 0)
            {
                words.Add(word.ToString().ToLowerInvariant());
                word.Clear();
            }
        }

        for (var i = 0; i < operationId.Length; i++)
        {
            var current = operationId[i];

            if (!char.IsAsciiLetterOrDigit(current))
            {
                Flush();
                continue;
            }

            if (word.Length > 0)
            {
                var previous = word[^1];

                // A digit run is a word of its own, an upper case letter opens one after a lower
                // case letter, and an acronym gives its last letter up to the word that follows:
                // `authorizeOAuth` is authorize, o, auth.
                if (char.IsAsciiDigit(current) != char.IsAsciiDigit(previous)
                    || (char.IsAsciiLetterUpper(current) && char.IsAsciiLetterLower(previous))
                    || (char.IsAsciiLetterUpper(current)
                        && char.IsAsciiLetterUpper(previous)
                        && i + 1 < operationId.Length
                        && char.IsAsciiLetterLower(operationId[i + 1])))
                {
                    Flush();
                }
            }

            word.Append(current);
        }

        Flush();

        return string.Join('-', words);
    }

    internal sealed record SlicedOperation(
        string OperationId,
        string FileName,
        string Path,
        string Endpoint,
        string Summary);
}
