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
/// Assembles everything the documentation site needs from this repository into one directory,
/// laid out the way it is served: the directory is published as its own repository and mounted
/// into the site as a submodule, so what is written here is what ends up at those URLs.
/// </summary>
internal static class MarkdownBundle
{
    private const string StaticDirectory = "static";
    private const string IndexFile = "llms.txt";

    public static async Task WriteAsync(
        string bundleDirectory,
        string stagingDirectory,
        string siteUrl,
        string title,
        IReadOnlyList<SectionDocument> sections,
        IReadOnlyList<OperationDocument> operations,
        IReadOnlyList<ModelDocument> models,
        CancellationToken cancellationToken = default)
    {
        // The path under the static root mirrors the site's own URLs: an endpoint page lives at
        // /docspace/api-backend/usage-api/<section>/<sub-section>/<slug>/, so its Markdown is that
        // path with `.md`. Sections and sub-sections are named the way the API states them, which
        // is also what the site's sidebar reads.
        var root = Path.Combine(bundleDirectory, StaticDirectory);
        var aggregateDirectory = Path.Combine(root, "docspace", "api-backend");
        var usageDirectory = Path.Combine(aggregateDirectory, "usage-api");

        // An endpoint dropped from the API has to disappear from the bundle too, or it keeps being
        // served - and read - as though it were still part of the contract. The documents are
        // written in full on every run, so the tree is replaced rather than pruned: a renamed
        // section leaves behind a directory, and a re-filed endpoint a copy in two places.
        if (Directory.Exists(usageDirectory))
        {
            Directory.Delete(usageDirectory, recursive: true);
        }

        Directory.CreateDirectory(aggregateDirectory);

        Copy(stagingDirectory, usageDirectory);

        // Earlier layouts published the section references beside `usage-api` rather than inside
        // it; nothing writes there now, and what is left is served alongside what replaced it.
        Prune(aggregateDirectory);

        await TextFile.WriteAsync(
            Path.Combine(root, IndexFile),
            BuildIndex(siteUrl, title, sections, operations, models),
            cancellationToken);
    }

    /// <summary>
    /// The llms.txt index: the entry point an agent is pointed at, from which every other document
    /// is reachable. Links are absolute because the file is read away from the site that serves it.
    /// </summary>
    private static string BuildIndex(
        string siteUrl,
        string title,
        IReadOnlyList<SectionDocument> sections,
        IReadOnlyList<OperationDocument> operations,
        IReadOnlyList<ModelDocument> models)
    {
        var baseUrl = $"{siteUrl.TrimEnd('/')}/docspace/api-backend/usage-api/";
        var index = new StringBuilder();

        index.Append("# ").AppendLine(title);
        index.AppendLine();
        index.AppendLine(
            "> The ONLYOFFICE DocSpace HTTP API. Every page of the reference is available as Markdown at its own address, with `.md` appended.");
        index.AppendLine();

        index.AppendLine("## Sections");
        index.AppendLine();
        index.AppendLine("One page per section, listing its endpoints. Each endpoint has a page of its own, and every type they exchange has one too.");
        index.AppendLine();

        foreach (var section in sections)
        {
            index
                .Append("- [").Append(section.Title).Append("](")
                .Append(baseUrl).Append(Url(section.RelativePath)).AppendLine(")");
        }

        index.AppendLine();
        index.AppendLine("## Models");
        index.AppendLine();
        index.AppendLine("The types the endpoints take and return.");
        index.AppendLine();

        foreach (var model in models)
        {
            index
                .Append("- [").Append(model.Title).Append("](")
                .Append(baseUrl).Append("models/").Append(model.FileName).AppendLine(")");
        }

        index.AppendLine();
        index.AppendLine("## Endpoints");

        var titles = sections.ToDictionary(
            section => section.Name,
            section => section.Title,
            StringComparer.Ordinal);

        foreach (var section in operations.GroupBy(operation => operation.Section, StringComparer.Ordinal))
        {
            index.AppendLine();
            index.Append("### ").AppendLine(titles.TryGetValue(section.Key, out var heading) ? heading : section.Key);

            foreach (var group in section.GroupBy(operation => operation.Group, StringComparer.Ordinal))
            {
                index.AppendLine();

                if (group.Key.Length > 0)
                {
                    index.Append("#### ").AppendLine(group.Key);
                    index.AppendLine();
                }

                foreach (var operation in group)
                {
                    index
                        .Append("- [").Append(string.IsNullOrEmpty(operation.Summary) ? operation.OperationId : operation.Summary)
                        .Append("](").Append(baseUrl).Append(Url(section.Key)).Append('/');

                    if (group.Key.Length > 0)
                    {
                        index.Append(Url(group.Key)).Append('/');
                    }

                    index.Append(operation.FileName).Append(')');

                    if (!string.IsNullOrEmpty(operation.Endpoint))
                    {
                        index.Append(": ").Append(operation.Endpoint);
                    }

                    index.AppendLine();
                }
            }
        }

        return index.ToString();
    }

    /// <summary>
    /// A path as it reads in a URL. Sections are named the way the API states them, and those
    /// names carry spaces.
    /// </summary>
    private static string Url(string path) =>
        string.Join('/', path.Replace('\\', '/').Split('/').Select(Uri.EscapeDataString));

    private static void Copy(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var path in Directory.EnumerateFiles(source))
        {
            File.Copy(path, Path.Combine(destination, Path.GetFileName(path)));
        }

        foreach (var path in Directory.EnumerateDirectories(source))
        {
            Copy(path, Path.Combine(destination, Path.GetFileName(path)));
        }
    }

    private static void Prune(string directory)
    {
        foreach (var path in Directory.EnumerateFiles(directory, "*.md"))
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// A section's index page: the name it is published under, its heading, and where the page
    /// sits inside `usage-api`.
    /// </summary>
    internal sealed record SectionDocument(string Name, string Title, string RelativePath);

    /// <summary>
    /// One published model: its title, and the page it is documented on.
    /// </summary>
    internal sealed record ModelDocument(string Title, string FileName);

    /// <summary>
    /// One published endpoint. <paramref name="Group"/> is the sub-section it is filed under,
    /// empty when its tag is the section itself.
    /// </summary>
    internal sealed record OperationDocument(
        string Section,
        string Group,
        string FileName,
        string OperationId,
        string Endpoint,
        string Summary);
}
