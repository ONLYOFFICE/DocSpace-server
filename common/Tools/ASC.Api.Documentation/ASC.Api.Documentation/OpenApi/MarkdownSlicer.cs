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
/// Cuts a rendered section document into one document per operation, and leaves the section
/// document holding only what is not on a page of its own: its heading, the tables of endpoints
/// and the authorization schemes.
/// </summary>
/// <remarks>
/// The operations are delimited by comments the template emits (`&lt;!--op:id|anchor--&gt;`) rather
/// than found by matching heading levels: the slicer would otherwise silently mis-cut the moment
/// the template's headings move, and a documentation page that is quietly wrong is worse than a
/// build that fails. The markers never reach either output.
/// </remarks>
internal static class MarkdownSlicer
{
    private const string OperationMarkerPrefix = "<!--op:";
    private const string ModelMarkerPrefix = "<!--model:";
    private const string MarkerSuffix = "-->";
    private const string OperationsEndMarker = "<!--/ops-->";
    private const string ModelsEndMarker = "<!--/models-->";

    /// <summary>
    /// A link to a model. The fragment is prefixed, which is what tells it apart from the
    /// operation and authorization fragments of the same document.
    /// </summary>
    private static readonly Regex _modelFragment = new(@"\]\(#(model-[a-z0-9_-]+)\)", RegexOptions.Compiled);

    /// <summary>
    /// A fragment of the section document that an endpoint's page has to be sent to. Model
    /// fragments are prefixed and are left out: the models are printed on the page itself.
    /// </summary>
    private static readonly Regex _sectionFragment = new(@"\]\(#(?!model-)", RegexOptions.Compiled);

    /// <summary>
    /// Cuts the models document into a page per model. The pages are siblings, so a property that
    /// names another model links straight to it.
    /// </summary>
    /// <remarks>
    /// Every marker is read before anything is written, because a model's page cannot be written
    /// until it is known where the models it points at will be.
    /// </remarks>
    public static async Task<IReadOnlyList<SlicedModel>> SliceModelsAsync(
        string documentPath,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        var lines = await File.ReadAllLinesAsync(documentPath, cancellationToken);

        Directory.CreateDirectory(outputDirectory);

        var cuts = new List<(Marker Marker, List<string> Body)>();
        Marker? marker = null;
        var body = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith(ModelMarkerPrefix, StringComparison.Ordinal))
            {
                if (marker != null)
                {
                    cuts.Add((marker, body));
                }

                marker = Marker.Parse(line, ModelMarkerPrefix);
                body = [];
                continue;
            }

            if (line.StartsWith(ModelsEndMarker, StringComparison.Ordinal))
            {
                if (marker != null)
                {
                    cuts.Add((marker, body));
                }

                marker = null;
                body = [];
                continue;
            }

            if (marker != null)
            {
                body.Add(line);
            }
        }

        var results = new List<SlicedModel>();
        var pages = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (found, _) in cuts)
        {
            var fileName = $"{Slug(found.Name)}.md";
            var path = Path.Combine(outputDirectory, fileName);

            // Two titles that differ only in case or punctuation would overwrite each other, and
            // the page that survived would be documenting the wrong type.
            if (pages.TryGetValue(found.Anchor, out var owner) || results.Any(model => model.FileName == fileName))
            {
                throw new Exception($"'{found.Name}' and '{owner}' both publish as {fileName}");
            }

            pages[found.Anchor] = path;
            results.Add(new SlicedModel(found.Anchor, found.Name, fileName, path));
        }

        foreach (var ((found, content), model) in cuts.Zip(results))
        {
            await TextFile.WriteAsync(
                model.Path,
                BuildDocument(Trim(content), model.Path, model.Path, pages.GetValueOrDefault),
                cancellationToken);
        }

        return results;
    }

    public static async Task<IReadOnlyList<SlicedOperation>> SliceAsync(
        string documentPath,
        string outputDirectory,
        Func<string, string?> modelPage,
        Func<string, string> groupOf,
        CancellationToken cancellationToken = default)
    {
        var lines = await File.ReadAllLinesAsync(documentPath, cancellationToken);

        Directory.CreateDirectory(outputDirectory);

        var results = new List<SlicedOperation>();
        var index = new List<string>();

        Marker? marker = null;
        var body = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith(OperationMarkerPrefix, StringComparison.Ordinal))
            {
                await WriteAsync(
                    results,
                    marker,
                    body,
                    outputDirectory,
                    documentPath,
                    modelPage,
                    groupOf,
                    cancellationToken);

                marker = Marker.Parse(line, OperationMarkerPrefix);
                body.Clear();
                continue;
            }

            if (line.StartsWith(OperationsEndMarker, StringComparison.Ordinal))
            {
                await WriteAsync(
                    results,
                    marker,
                    body,
                    outputDirectory,
                    documentPath,
                    modelPage,
                    groupOf,
                    cancellationToken);

                marker = null;
                body.Clear();
                continue;
            }

            if (marker != null)
            {
                body.Add(line);
            }
            else
            {
                index.Add(line);
            }
        }

        // What is left is the section index: the endpoints are on pages of their own now, so the
        // fragments the tables point at are no longer in this document and have to be sent to
        // those pages instead.
        await TextFile.WriteLinesAsync(
            documentPath,
            Link(index, results, documentPath, modelPage),
            cancellationToken);

        return results;
    }

    /// <summary>
    /// Sends the index's endpoint links to the pages the endpoints were cut into. Anchors that
    /// belong to no endpoint - the authorization schemes - are left as the in-document fragments
    /// they still are.
    /// </summary>
    private static IEnumerable<string> Link(
        List<string> index,
        List<SlicedOperation> operations,
        string documentPath,
        Func<string, string?> modelPage)
    {
        var pages = operations.ToDictionary(
            operation => $"](#{operation.Anchor})",
            operation => $"]({Relative(documentPath, operation.Path)})",
            StringComparer.Ordinal);

        var blank = false;

        foreach (var line in index)
        {
            var linked = line;

            if (linked.Contains("](#", StringComparison.Ordinal))
            {
                foreach (var (fragment, page) in pages)
                {
                    linked = linked.Replace(fragment, page, StringComparison.Ordinal);
                }

                linked = LinkModels(linked, documentPath, modelPage);
            }

            // Cutting the endpoints out leaves the blank lines that separated them behind, one
            // run of them per endpoint.
            if (linked.Length == 0)
            {
                if (blank)
                {
                    continue;
                }

                blank = true;
            }
            else
            {
                blank = false;
            }

            yield return linked;
        }
    }

    private static async Task WriteAsync(
        List<SlicedOperation> results,
        Marker? marker,
        List<string> body,
        string outputDirectory,
        string documentPath,
        Func<string, string?> modelPage,
        Func<string, string> groupOf,
        CancellationToken cancellationToken)
    {
        if (marker == null)
        {
            return;
        }

        var group = groupOf(marker.Name);
        var directory = group.Length > 0 ? Path.Combine(outputDirectory, group) : outputDirectory;

        Directory.CreateDirectory(directory);

        var fileName = $"{Slug(marker.Name)}.md";
        var path = Path.Combine(directory, fileName);

        var trimmed = Trim(body);

        await TextFile.WriteAsync(
            path,
            BuildDocument(trimmed, path, documentPath, modelPage),
            cancellationToken);

        results.Add(new SlicedOperation(
            marker.Name,
            marker.Anchor,
            group,
            fileName,
            path,
            Endpoint(trimmed),
            Summary(trimmed)));
    }

    /// <summary>
    /// How one document reaches another from where it is written. Measured rather than assembled
    /// from a configured prefix: an operation of a section with sub-sections sits one level deeper
    /// than one without, and the staging tree is laid out like the published one, so what is
    /// measured here holds after the bundle is assembled.
    /// </summary>
    private static string Relative(string from, string to) =>
        Path.GetRelativePath(Path.GetDirectoryName(from)!, to).Replace('\\', '/');

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

    /// <summary>
    /// In the section document an operation is a third-level heading under its sub-section; on its
    /// own it is the document, so everything moves up two levels.
    /// </summary>
    /// <remarks>
    /// The models the endpoint exchanges are printed on the page itself, so their fragments stay
    /// as they are. The authorization schemes are not - they are the same handful for the whole
    /// section and are documented once, on the section's own page.
    /// </remarks>
    private static string BuildDocument(
        List<string> trimmed,
        string path,
        string documentPath,
        Func<string, string?> modelPage)
    {
        var section = Relative(path, documentPath);

        var content = trimmed.Select(line => _sectionFragment.Replace(
            LinkModels(Promote(line), path, modelPage),
            $"]({section}#"));

        return string.Join('\n', content) + '\n';
    }

    /// <summary>
    /// Sends the links to the types a page names to the pages those types are documented on.
    /// </summary>
    /// <remarks>
    /// A fragment nothing claims is left alone rather than pointed at a page that does not exist:
    /// it then fails the site build as the broken anchor it is, which is where such a thing
    /// belongs.
    /// </remarks>
    private static string LinkModels(string line, string path, Func<string, string?> modelPage) =>
        _modelFragment.Replace(line, match =>
        {
            var page = modelPage(match.Groups[1].Value);

            return page == null ? match.Value : $"]({Relative(path, page)})";
        });

    private static string Promote(string line)
    {
        if (line.StartsWith("##### ", StringComparison.Ordinal))
        {
            return string.Concat("### ", line.AsSpan("##### ".Length));
        }

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

    /// <summary>
    /// What the template states about an operation where it cut: its id, and the fragment the
    /// section's tables link to it by.
    /// </summary>
    private sealed record Marker(string Name, string Anchor)
    {
        public static Marker Parse(string line, string prefix)
        {
            var content = line[prefix.Length..^MarkerSuffix.Length];
            var separator = content.IndexOf('|', StringComparison.Ordinal);

            return separator < 0
                ? new Marker(content, string.Empty)
                : new Marker(content[..separator], content[(separator + 1)..]);
        }
    }

    /// <summary>
    /// One published model: the fragment the rest of the documentation links it by, its title,
    /// and where its page was written.
    /// </summary>
    internal sealed record SlicedModel(string Anchor, string Title, string FileName, string Path);

    /// <summary>
    /// One published operation. <paramref name="Group"/> is the sub-section it was filed under,
    /// empty when its tag is the section itself.
    /// </summary>
    internal sealed record SlicedOperation(
        string OperationId,
        string Anchor,
        string Group,
        string FileName,
        string Path,
        string Endpoint,
        string Summary);
}
