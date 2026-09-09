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
        string siteUrl,
        string title,
        IReadOnlyList<AggregateDocument> aggregates,
        IReadOnlyList<OperationDocument> operations,
        CancellationToken cancellationToken = default)
    {
        // The path under the static root mirrors the site's own URLs: an operation page lives at
        // /docspace/api-backend/usage-api/<slug>/, so its Markdown is that path with `.md`.
        var root = Path.Combine(bundleDirectory, StaticDirectory);
        var aggregateDirectory = Path.Combine(root, "docspace", "api-backend");
        var operationDirectory = Path.Combine(aggregateDirectory, "usage-api");

        Directory.CreateDirectory(operationDirectory);

        foreach (var aggregate in aggregates)
        {
            File.Copy(aggregate.Path, Path.Combine(aggregateDirectory, aggregate.FileName), overwrite: true);
        }

        foreach (var operation in operations)
        {
            File.Copy(operation.Path, Path.Combine(operationDirectory, operation.FileName), overwrite: true);
        }

        // An endpoint dropped from the API has to disappear from the bundle too, or it keeps being
        // served - and read - as though it were still part of the contract.
        Prune(aggregateDirectory, aggregates.Select(aggregate => aggregate.FileName));
        Prune(operationDirectory, operations.Select(operation => operation.FileName));

        await File.WriteAllTextAsync(
            Path.Combine(root, IndexFile),
            BuildIndex(siteUrl, title, aggregates, operations),
            cancellationToken);
    }

    /// <summary>
    /// The llms.txt index: the entry point an agent is pointed at, from which every other document
    /// is reachable. Links are absolute because the file is read away from the site that serves it.
    /// </summary>
    private static string BuildIndex(
        string siteUrl,
        string title,
        IReadOnlyList<AggregateDocument> aggregates,
        IReadOnlyList<OperationDocument> operations)
    {
        var baseUrl = siteUrl.TrimEnd('/');
        var index = new StringBuilder();

        index.Append("# ").AppendLine(title);
        index.AppendLine();
        index.AppendLine(
            "> The ONLYOFFICE DocSpace HTTP API. Every page of the reference is available as Markdown at its own address, with `.md` appended.");
        index.AppendLine();

        index.AppendLine("## Full references");
        index.AppendLine();
        index.AppendLine("One document per service, each carrying every endpoint of that service and the models it uses.");
        index.AppendLine();

        foreach (var aggregate in aggregates)
        {
            index
                .Append("- [").Append(aggregate.Title).Append("](")
                .Append(baseUrl).Append("/docspace/api-backend/").Append(aggregate.FileName).AppendLine(")");
        }

        index.AppendLine();
        index.AppendLine("## Endpoints");
        index.AppendLine();

        foreach (var operation in operations)
        {
            index
                .Append("- [").Append(string.IsNullOrEmpty(operation.Summary) ? operation.OperationId : operation.Summary)
                .Append("](").Append(baseUrl).Append("/docspace/api-backend/usage-api/").Append(operation.FileName).Append(')');

            if (!string.IsNullOrEmpty(operation.Endpoint))
            {
                index.Append(": ").Append(operation.Endpoint);
            }

            index.AppendLine();
        }

        return index.ToString();
    }

    private static void Prune(string directory, IEnumerable<string> expected)
    {
        var keep = new HashSet<string>(expected, StringComparer.OrdinalIgnoreCase);

        foreach (var path in Directory.EnumerateFiles(directory, "*.md"))
        {
            if (!keep.Contains(Path.GetFileName(path)))
            {
                File.Delete(path);
            }
        }
    }

    internal sealed record AggregateDocument(string FileName, string Title, string Path);

    internal sealed record OperationDocument(string FileName, string OperationId, string Endpoint, string Summary, string Path);
}
