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
/// Renders the Markdown API reference: one document per section of the API, one document for
/// every model they exchange, and one document per operation.
/// </summary>
/// <remarks>
/// The joined document is split into per-section sub-documents first (see
/// <see cref="OpenapiSplitter"/>) and the `my-markdown` generator is then run once per
/// sub-document, because a supporting-file template cannot tell which section it is rendering
/// and the generator's `apis` filter does not match tags containing spaces.
/// </remarks>
public class GenerateMarkdownDocsCommand : SdkCommandBase
{
    protected override string Name => "Markdown";

    /// <summary>
    /// The directory the model pages are published in, and the name of the document they are cut
    /// from. Models are shared across sections, so they are published once, beside them all.
    /// </summary>
    private const string ModelsDirectory = "models";

    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        NoArgumentsCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        var joinedDocument = ReadJoinedDocument(configuration);
        var markdown = ReadMarkdownSettings(configuration);
        var splitDirectory = Path.Combine(WorkingDirectory, "json", "split");
        var outputDirectory = ReadOutputDirectory();
        var operationsDirectory = Path.Combine(outputDirectory, markdown.OperationsDirectory);

        var sections = await OpenapiSplitter.SplitBySectionAsync(
            joinedDocument,
            splitDirectory,
            MarkdownSettings.NameFor,
            cancellationToken);

        // Nothing removes what the generator no longer produces, so a section dropped from the
        // API or renamed would leave its document behind looking current - and it would be
        // committed as if it were still generated. The pages are written in full on every run, so
        // the whole tree goes rather than being pruned file by file: a sub-section that lost its
        // last endpoint leaves a directory behind, not a file.
        RemoveStale(outputDirectory, "*.md", []);
        RemoveStale(
            splitDirectory,
            "*.json",
            [.. sections.Select(section => $"{section.Name}.json"), $"{ModelsDirectory}.json"]);

        if (Directory.Exists(operationsDirectory))
        {
            Directory.Delete(operationsDirectory, recursive: true);
        }

        var published = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sectionDocuments = new List<MarkdownBundle.SectionDocument>();
        var operationDocuments = new List<MarkdownBundle.OperationDocument>();

        // Before the sections, because what the endpoints link their types to has to be known by
        // the time those links are written.
        var models = await RenderModelsAsync(
            joinedDocument,
            splitDirectory,
            outputDirectory,
            operationsDirectory,
            markdown,
            cancellationToken);

        if (models == null)
        {
            return 1;
        }

        var modelPages = models.ToDictionary(model => model.Anchor, model => model.Path, StringComparer.Ordinal);

        AnsiConsole.MarkupLine($"Cut [green]{models.Count}[/] model pages");

        foreach (var document in sections)
        {
            AnsiConsole.MarkupLine($"Rendering [green]{Markup.Escape(document.Section)}[/]");

            await ApplyPresentationAsync(document, markdown, cancellationToken);

            var exitCode = await RunGeneratorAsync(BuildArguments(document, markdown), cancellationToken);

            if (exitCode != 0)
            {
                return exitCode;
            }

            // The section's own page is the index of the directory its endpoints are in, which is
            // what makes the directory a section rather than a bag of pages: Docusaurus takes the
            // page named after the folder as that folder's own.
            var sectionDirectory = Path.Combine(operationsDirectory, document.Name);
            var documentPath = Publish(outputDirectory, document.Name, sectionDirectory);

            // Before the document is cut up, so that the section page and every endpoint page cut
            // out of it carry the same escaping.
            await MdxSafe.ApplyAsync(documentPath, cancellationToken);

            var groups = ReadGroups(document);

            var sliced = await MarkdownSlicer.SliceAsync(
                documentPath,
                sectionDirectory,
                modelPages.GetValueOrDefault,
                operationId => MarkdownSettings.NameFor(groups.GetValueOrDefault(operationId, string.Empty)),
                cancellationToken);

            await WriteCategoryAsync(sectionDirectory, document.Section, cancellationToken);

            foreach (var label in groups.Values.Distinct(StringComparer.Ordinal))
            {
                if (label.Length > 0)
                {
                    await WriteCategoryAsync(
                        Path.Combine(sectionDirectory, MarkdownSettings.NameFor(label)),
                        label,
                        cancellationToken);
                }
            }

            sectionDocuments.Add(new MarkdownBundle.SectionDocument(
                document.Name,
                markdown.TitleFor(document.Section),
                $"{document.Name}/{document.Name}.md"));

            foreach (var operation in sliced)
            {
                // Every sub-section publishes into a directory of its own, so a collision here is
                // two operation ids of one sub-section differing only in case or punctuation. The
                // joiner already rejects duplicate ids, and silently overwriting one of these
                // would drop an endpoint from the documentation.
                var key = Path.Combine(document.Name, operation.Group, operation.FileName);

                if (published.TryGetValue(key, out var owner))
                {
                    throw new Exception(
                        $"'{operation.OperationId}' and '{owner}' both publish as {key}");
                }

                published[key] = operation.OperationId;

                operationDocuments.Add(new MarkdownBundle.OperationDocument(
                    document.Name,
                    operation.Group,
                    operation.FileName,
                    operation.OperationId,
                    operation.Endpoint,
                    operation.Summary));
            }

            AnsiConsole.MarkupLine($"  cut into [green]{sliced.Count}[/] endpoint pages");
        }

        if (!string.IsNullOrWhiteSpace(markdown.BundleDirectory))
        {
            await MarkdownBundle.WriteAsync(
                markdown.BundleDirectory,
                operationsDirectory,
                markdown.SiteUrl ?? string.Empty,
                markdown.IndexTitle,
                sectionDocuments,
                operationDocuments,
                [.. models.Select(model => new MarkdownBundle.ModelDocument(model.Title, model.FileName))],
                cancellationToken);

            AnsiConsole.MarkupLine(
                $"Bundled [green]{sectionDocuments.Count}[/] sections, [green]{operationDocuments.Count}[/] endpoints and [green]{models.Count}[/] models for publishing");

            // Only once the bundle holds a complete copy: until then these directories are the
            // only place the work exists, and a failed run is far easier to diagnose with them
            // still on disk.
            RemoveStaging(outputDirectory, operationsDirectory, splitDirectory);
        }

        return 0;
    }

    /// <summary>
    /// Deletes what the run needed on the way to the bundle and nothing else needs afterwards:
    /// the rendered documents the bundle now carries, the sub-documents they were rendered from,
    /// and openapi-generator's bookkeeping.
    /// </summary>
    /// <remarks>
    /// The alternative - leaving them behind and ignoring them - keeps a second copy of every
    /// published document in the working tree, where it goes stale and gets read as though it
    /// were current.
    /// </remarks>
    private static void RemoveStaging(string outputDirectory, string operationsDirectory, string splitDirectory)
    {
        foreach (var path in Directory.EnumerateFiles(outputDirectory, "*.md"))
        {
            File.Delete(path);
        }

        File.Delete(Path.Combine(outputDirectory, ".openapi-generator-ignore"));

        foreach (var directory in new[]
                 {
                     operationsDirectory,
                     splitDirectory,
                     Path.Combine(outputDirectory, ".openapi-generator")
                 })
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
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
            info["title"] = settings.TitleFor(document.Section);
        }

        OpenApiServer.ApplyBaseUrlDefault(root, settings.ServerUrl);

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

        return new MarkdownSettings(
            configuration["serverUrl"],
            section["docsUrl"],
            section["operationsDirectory"] ?? "operations",
            ReadBundleDirectory(section),
            section["siteUrl"],
            section["indexTitle"] ?? "ONLYOFFICE DocSpace API",
            titles);
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
    /// The joined document the joiner produced. Relative paths resolve against the current
    /// directory, matching <see cref="JoinSettings"/>.
    /// </summary>
    private static string ReadJoinedDocument(IConfiguration configuration)
    {
        var parts = configuration.GetSection("pathToFile").Get<string[]>();

        if (parts == null || parts.Length == 0)
        {
            throw new Exception("File path not specified. Configure 'pathToFile' in appsettings.json");
        }

        return Path.GetFullPath(Path.Combine(parts));
    }

    /// <summary>
    /// Renders every model of the API and cuts it into a page per model. Null when the generator
    /// refused the document, which the caller reports as the failure it is.
    /// </summary>
    private async Task<IReadOnlyList<MarkdownSlicer.SlicedModel>?> RenderModelsAsync(
        string joinedDocument,
        string splitDirectory,
        string outputDirectory,
        string operationsDirectory,
        MarkdownSettings markdown,
        CancellationToken cancellationToken)
    {
        var document = await OpenapiSplitter.WholeAsync(
            joinedDocument,
            splitDirectory,
            ModelsDirectory,
            cancellationToken);

        AnsiConsole.MarkupLine($"Rendering [green]{ModelsDirectory}[/]");

        await ApplyPresentationAsync(document, markdown, cancellationToken);

        var arguments = BuildArguments(document, markdown);
        arguments.Add("--additional-properties");
        arguments.Add("modelsOnly=true");

        if (await RunGeneratorAsync(arguments, cancellationToken) != 0)
        {
            return null;
        }

        var documentPath = Path.Combine(outputDirectory, $"{ModelsDirectory}.md");

        await MdxSafe.ApplyAsync(documentPath, cancellationToken);

        var directory = Path.Combine(operationsDirectory, ModelsDirectory);
        var models = await MarkdownSlicer.SliceModelsAsync(documentPath, directory, cancellationToken);

        await WriteCategoryAsync(directory, "Models", cancellationToken);

        // The document exists to be cut up; what is left of it is the heading and the markers.
        File.Delete(documentPath);

        return models;
    }

    /// <summary>
    /// Moves the rendered document to where it is published, under the name it is published as.
    /// </summary>
    /// <remarks>
    /// The generator names its output after `documentName`, which travels as a command-line
    /// option: openapi-generator-cli is an npm shim that re-spawns java through a shell, and a
    /// value containing a space arrives there as two arguments - so it is rendered under a name
    /// without spaces and moved here, where nothing is parsing a command line.
    /// </remarks>
    private static string Publish(string renderedIn, string rendered, string directory)
    {
        Directory.CreateDirectory(directory);

        var from = Path.Combine(renderedIn, $"{rendered}.md");
        var to = Path.Combine(directory, $"{rendered}.md");

        File.Move(from, to, overwrite: true);

        return to;
    }

    /// <summary>
    /// Writes the label a directory is listed under.
    /// </summary>
    /// <remarks>
    /// The directories are named for the URL they become, so their names are not what a reader
    /// should be shown: `third-party-integration` is a path, "Third-party integration" is what the
    /// API calls that group of endpoints. Docusaurus reads the label from this file; anything else
    /// reading the tree gets the same answer from it.
    /// </remarks>
    private static async Task WriteCategoryAsync(string directory, string label, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directory);

        var category = new JsonObject { ["label"] = label };

        await TextFile.WriteAsync(
            Path.Combine(directory, "_category_.json"),
            category.ToJsonString(_writeOptions),
            cancellationToken);
    }

    /// <summary>
    /// The sub-section each operation of a document belongs to, as its tag's `x-displayName`
    /// states it - empty when the tag is the section itself and there is no sub-section to file
    /// the operation under.
    /// </summary>
    private static Dictionary<string, string> ReadGroups(OpenapiSplitter.SplitDocument document)
    {
        var groups = new Dictionary<string, string>(StringComparer.Ordinal);
        var root = JsonNode.Parse(File.ReadAllText(document.Path))?.AsObject();

        if (root?["paths"] is not JsonObject paths)
        {
            return groups;
        }

        var displayNames = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var tag in root["tags"] as JsonArray ?? [])
        {
            var name = tag?["name"]?.ToString();
            var displayName = tag?["x-displayName"]?.ToString();

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrWhiteSpace(displayName))
            {
                displayNames[name] = displayName;
            }
        }

        foreach (var path in paths)
        {
            if (path.Value is not JsonObject methods)
            {
                continue;
            }

            foreach (var method in methods)
            {
                var operationId = method.Value?["operationId"]?.ToString();
                var tag = (method.Value?["tags"] as JsonArray)?.FirstOrDefault()?.ToString();

                if (string.IsNullOrEmpty(operationId) || string.IsNullOrEmpty(tag))
                {
                    continue;
                }

                // A tag that is the section itself has no sub-section to name, and a directory
                // repeating the section it is already inside would only add a level to click
                // through.
                groups[operationId] = string.Equals(tag, document.Section, StringComparison.Ordinal)
                    ? string.Empty
                    : displayNames.GetValueOrDefault(tag, Subsection(tag));
            }
        }

        return groups;
    }

    /// <summary>
    /// What a tag states after the separator, for a document that names no `x-displayName`.
    /// </summary>
    private static string Subsection(string tag)
    {
        var separator = tag.IndexOf('/', StringComparison.Ordinal);

        return separator < 0 ? tag.Trim() : tag[(separator + 1)..].Trim();
    }

    /// <summary>
    /// Where the publishable bundle is assembled. Relative to the current directory, matching the
    /// other paths in the configuration.
    /// </summary>
    private static string? ReadBundleDirectory(IConfiguration section)
    {
        var parts = section.GetSection("bundle").Get<string[]>();

        return parts is { Length: > 0 } ? Path.GetFullPath(Path.Combine(parts)) : null;
    }

    private sealed record MarkdownSettings(
        string? ServerUrl,
        string? DocsUrl,
        string OperationsDirectory,
        string? BundleDirectory,
        string? SiteUrl,
        string IndexTitle,
        IReadOnlyDictionary<string, string> Titles)
    {
        /// <summary>
        /// Titles are keyed by the published document name, so a section added to the API without
        /// a title is headed by its own tag rather than silently inheriting another's heading.
        /// </summary>
        public string TitleFor(string section) => Titles.TryGetValue(section, out var title) ? title : section;

        /// <summary>
        /// What a section is rendered as before it is published under its own name. Only the
        /// generator ever sees this, and it reaches the generator through a command line, so it
        /// carries neither spaces nor anything else a shell would take apart.
        /// </summary>
        public static string NameFor(string section) => Slugify(section);

        private static string Slugify(string section)
        {
            var slug = new StringBuilder(section.Length);

            foreach (var character in section)
            {
                if (char.IsAsciiLetterOrDigit(character))
                {
                    slug.Append(char.ToLowerInvariant(character));
                }
                else if (slug.Length > 0 && slug[^1] != '-')
                {
                    slug.Append('-');
                }
            }

            return slug.ToString().TrimEnd('-');
        }
    }
}
