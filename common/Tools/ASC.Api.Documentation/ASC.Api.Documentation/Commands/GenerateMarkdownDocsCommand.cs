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

namespace ASC.Api.Documentation.Commands;

/// <summary>
/// Renders the Markdown API reference next to the per-service `json/*.json` documents, one
/// self-contained document per service rather than a page per tag and per model.
/// </summary>
/// <remarks>
/// The joined document is split back into per-service sub-documents first (see
/// <see cref="OpenapiSplitter"/>) and the `my-markdown` generator is then run once per
/// sub-document, because a supporting-file template cannot tell which service it is
/// rendering and the generator's `apis` filter does not match tags containing spaces.
/// </remarks>
public class GenerateMarkdownDocsCommand : SdkCommandBase
{
    protected override string Name => "Markdown";

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        NoArgumentsCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var (joinedDocument, sourceDocuments) = ReadDocumentPaths();
        var splitDirectory = Path.Combine(WorkingDirectory, "json", "split");

        var documents = await OpenapiSplitter.SplitAsync(
            joinedDocument,
            sourceDocuments,
            splitDirectory,
            cancellationToken);

        // Nothing removes what the generator no longer produces, so a service dropped from the
        // join or renamed would leave its document behind looking current - and it would be
        // committed as if it were still generated.
        RemoveStale(ReadOutputDirectory(), "*.md", documents.Select(document => $"{document.Name}.md"));
        RemoveStale(splitDirectory, "*.json", documents.Select(document => $"{document.Name}.json"));

        foreach (var document in documents)
        {
            AnsiConsole.MarkupLine($"Rendering [green]{Markup.Escape(document.Name)}.md[/]");

            var exitCode = await RunGeneratorAsync(
                ["-i", document.Path, "--additional-properties", $"documentName={document.Name}"],
                cancellationToken);

            if (exitCode != 0)
            {
                return exitCode;
            }
        }

        return 0;
    }

    /// <summary>
    /// Deletes files the generator is no longer going to write. The expected set is taken from the
    /// split result, so removing a service from the join is enough to retire its document.
    /// </summary>
    private static void RemoveStale(string directory, string searchPattern, IEnumerable<string> expectedFiles)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        var expected = new HashSet<string>(expectedFiles, StringComparer.OrdinalIgnoreCase);

        foreach (var path in Directory.EnumerateFiles(directory, searchPattern))
        {
            var fileName = Path.GetFileName(path);
            if (expected.Contains(fileName))
            {
                continue;
            }

            AnsiConsole.MarkupLine($"Removing stale [yellow]{Markup.Escape(fileName)}[/]");
            File.Delete(path);
        }
    }

    /// <summary>
    /// Where the generator writes, read from its own config so the two cannot drift apart.
    /// </summary>
    private string ReadOutputDirectory()
    {
        var configPath = Path.Combine(WorkingDirectory, "tools", $"tools{Name}.json");
        var outputDirectory = JsonNode.Parse(File.ReadAllText(configPath))?["outputDir"]?.ToString();

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new Exception($"'outputDir' is not specified in {configPath}");
        }

        return Path.GetFullPath(Path.Combine(WorkingDirectory, outputDirectory));
    }

    /// <summary>
    /// Reads the same paths the joiner works with: the joined document it produces and the
    /// per-service documents it consumes. Relative paths resolve against the current directory,
    /// matching <see cref="JoinSettings"/>.
    /// </summary>
    private static (string JoinedDocument, IReadOnlyList<string> SourceDocuments) ReadDocumentPaths()
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        var joinedParts = configuration.GetSection("pathToFile").Get<string[]>();
        if (joinedParts == null || joinedParts.Length == 0)
        {
            throw new Exception("File path not specified. Configure 'pathToFile' in appsettings.json");
        }

        var sourceDocuments = new List<string>();

        foreach (var child in configuration.GetSection("join").GetChildren())
        {
            var parts = child.Get<string[]>();
            if (parts is { Length: > 0 })
            {
                sourceDocuments.Add(Path.GetFullPath(Path.Combine(parts)));
            }
        }

        if (sourceDocuments.Count == 0)
        {
            throw new Exception("No source documents specified. Configure 'join' in appsettings.json");
        }

        return (Path.GetFullPath(Path.Combine(joinedParts)), sourceDocuments);
    }
}
