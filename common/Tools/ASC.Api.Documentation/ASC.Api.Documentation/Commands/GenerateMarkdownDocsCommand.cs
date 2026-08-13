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

    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        NoArgumentsCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        var (joinedDocument, sourceDocuments) = ReadDocumentPaths(configuration);
        var markdown = ReadMarkdownSettings(configuration);
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

            await ApplyPresentationAsync(document, markdown, cancellationToken);

            var exitCode = await RunGeneratorAsync(
                BuildArguments(document, markdown),
                cancellationToken);

            if (exitCode != 0)
            {
                return exitCode;
            }
        }

        return 0;
    }

    /// <summary>
    /// Writes into the sub-document what the generator reads from the document rather than from
    /// its options: the page heading and the server the URIs are relative to.
    /// </summary>
    /// <remarks>
    /// These cannot travel as `--additional-properties`. openapi-generator-cli is an npm shim
    /// that re-spawns java through a shell, and a value containing spaces arrives there split
    /// into separate arguments - "ONLYOFFICE DocSpace Files API" fails the run outright.
    /// </remarks>
    private static async Task ApplyPresentationAsync(
        OpenapiSplitter.SplitDocument document,
        MarkdownSettings settings,
        CancellationToken cancellationToken)
    {
        var content = await File.ReadAllTextAsync(document.Path, cancellationToken);
        var root = JsonNode.Parse(content)?.AsObject()
            ?? throw new Exception($"Split document is not an object: {document.Path}");

        // The joined document titles every service "Api", and the heading comes from info.title.
        if (root["info"] is JsonObject info)
        {
            info["title"] = settings.TitleFor(document.Name);
        }

        // `baseUrl` is deliberately empty in the document - a DocSpace instance lives wherever it
        // is installed - but an empty default renders as "http://http:" in the page header, so
        // the pages get the placeholder host the handbook already uses.
        if (!string.IsNullOrWhiteSpace(settings.ServerUrl)
            && root["servers"] is JsonArray { Count: > 0 } servers
            && servers[0]?["variables"]?["baseUrl"] is JsonObject baseUrl
            && string.IsNullOrEmpty(baseUrl["default"]?.ToString()))
        {
            baseUrl["default"] = settings.ServerUrl;
        }

        await File.WriteAllTextAsync(document.Path, root.ToJsonString(_writeOptions), cancellationToken);
    }

    /// <summary>
    /// Options for one document. Each value goes over as its own `--additional-properties` flag
    /// rather than as one comma-separated list, because the generator splits that list on commas.
    /// Only values without spaces belong here - see <see cref="ApplyPresentationAsync"/>.
    /// </summary>
    private static List<string> BuildArguments(OpenapiSplitter.SplitDocument document, MarkdownSettings settings)
    {
        var arguments = new List<string> { "-i", document.Path };

        AddProperty(arguments, "documentName", document.Name);
        AddProperty(arguments, "docsUrl", settings.DocsUrl);

        return arguments;
    }

    private static void AddProperty(List<string> arguments, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        arguments.Add("--additional-properties");
        arguments.Add($"{name}={value}");
    }

    /// <summary>
    /// Presentation the generated documents cannot derive themselves: the joined document carries
    /// a single `info.title` ("Api") and an empty server URL, so every service would otherwise be
    /// headed the same and claim to live on localhost.
    /// </summary>
    private static MarkdownSettings ReadMarkdownSettings(IConfiguration configuration)
    {
        var section = configuration.GetSection("markdown");

        var titles = section.GetSection("titles")
            .GetChildren()
            .Where(title => !string.IsNullOrWhiteSpace(title.Value))
            .ToDictionary(title => title.Key, title => title.Value!, StringComparer.OrdinalIgnoreCase);

        return new MarkdownSettings(section["serverUrl"], section["docsUrl"], titles);
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
    private static (string JoinedDocument, IReadOnlyList<string> SourceDocuments) ReadDocumentPaths(
        IConfiguration configuration)
    {
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

    /// <summary>
    /// Titles are keyed by the split document name, so a service added to the join without a title
    /// falls back to that name rather than silently inheriting another service's heading.
    /// </summary>
    private sealed record MarkdownSettings(
        string? ServerUrl,
        string? DocsUrl,
        IReadOnlyDictionary<string, string> Titles)
    {
        public string TitleFor(string documentName) =>
            Titles.TryGetValue(documentName, out var title) ? title : documentName;
    }
}
