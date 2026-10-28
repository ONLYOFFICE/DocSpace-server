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
/// Renders the Markdown reference of the webhooks contract (<c>docspace-webhooks.yaml</c>): an index page, a page
/// per trigger family listing its triggers, and a page per trigger, laid out like the endpoint pages of the API
/// reference. The models the deliveries carry are rendered by the same generator as the API models, from the
/// document <see cref="WriteModelsDocumentAsync"/> writes.
/// </summary>
/// <remarks>
/// openapi-generator renders <c>paths</c> only, not the <c>webhooks</c> section of an OpenAPI 3.1 document, so the
/// trigger pages are written here. A trigger page states what the contract says of it: the description (with the
/// REST API operations that raise the trigger, linked to their pages in the API reference), the headers, the
/// delivery model and what the receiver's answer means.
/// </remarks>
internal static class WebhookMarkdown
{
    /// <summary>
    /// An endpoint named in code in a description, as the contract lists the operations that raise a trigger:
    /// <c>`PUT api/2.0/files/fileops/move`</c>.
    /// </summary>
    private static readonly Regex _endpoint = new(@"`(GET|POST|PUT|PATCH|DELETE) (api/[^`]+)`", RegexOptions.Compiled);

    private static readonly Regex _lineBreak = new(@"\s*\n\s*", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };

    /// <summary>
    /// The sub-directory of the reference that holds the model pages.
    /// </summary>
    public const string ModelsDirectory = "models";

    /// <summary>
    /// The contract as JSON, read from its YAML. A plain scalar keeps its YAML type (a number, a boolean, null); a
    /// quoted or block one is a string, which is what the contract writer means by choosing that style.
    /// </summary>
    public static JsonObject LoadContract(string path)
    {
        using var reader = new StreamReader(path);

        var stream = new YamlStream();
        stream.Load(reader);

        return ToJson(stream.Documents[0].RootNode) as JsonObject
            ?? throw new Exception($"The webhooks contract is not a mapping: {path}");
    }

    /// <summary>
    /// A document holding only the contract's schemas, for the generator to render the model pages from.
    /// </summary>
    public static async Task<string> WriteModelsDocumentAsync(
        JsonObject contract,
        string directory,
        string name,
        string title,
        CancellationToken cancellationToken)
    {
        var document = new JsonObject
        {
            ["openapi"] = contract["openapi"]?.DeepClone(),
            ["info"] = new JsonObject
            {
                ["title"] = title,
                ["version"] = contract["info"]?["version"]?.DeepClone()
            },
            ["paths"] = new JsonObject(),
            ["components"] = new JsonObject
            {
                ["schemas"] = contract["components"]?["schemas"]?.DeepClone() ?? new JsonObject()
            }
        };

        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"{name}.json");

        await File.WriteAllTextAsync(path, document.ToJsonString(_writeOptions), cancellationToken);

        return path;
    }

    /// <summary>
    /// Writes the index, family and trigger pages into <paramref name="directory"/>, whose <c>models</c>
    /// sub-directory holds the model pages, and returns the trigger pages in the order of the contract.
    /// </summary>
    /// <param name="contract">The contract, as <see cref="LoadContract"/> reads it.</param>
    /// <param name="directory">The root of the webhooks reference.</param>
    /// <param name="title">The heading of the index page, and the label of the reference.</param>
    /// <param name="modelFile">The file name of a model's page, by schema name; null for a schema with none.</param>
    /// <param name="apiPage">The page of a REST API operation, by its endpoint (<c>PUT /api/2.0/...</c>), as a path
    /// relative to the directory that holds both references; null for an operation that has none.</param>
    /// <param name="cancellationToken">The token to stop on.</param>
    public static async Task<IReadOnlyList<WebhookPage>> RenderAsync(
        JsonObject contract,
        string directory,
        string title,
        Func<string, string?> modelFile,
        Func<string, string?> apiPage,
        CancellationToken cancellationToken)
    {
        var webhooks = contract["webhooks"] as JsonObject
            ?? throw new Exception("The webhooks contract has no webhooks section");

        var families = (contract["tags"] as JsonArray ?? [])
            .Select(tag => new Family(tag?["name"]?.ToString() ?? string.Empty, tag?["description"]?.ToString()))
            .Where(family => family.Name.Length > 0)
            .ToList();

        var pages = new List<WebhookPage>();

        foreach (var (trigger, item) in webhooks)
        {
            if (item?["post"] is not JsonObject operation)
            {
                continue;
            }

            var family = (operation["tags"] as JsonArray)?.FirstOrDefault()?.ToString() ?? trigger.Split('.')[0];
            var delivery = SchemaName(operation["requestBody"]?["content"]?["application/json"]?["schema"]?["$ref"]?.ToString());

            pages.Add(new WebhookPage(family, $"{Slug(trigger)}.md", trigger, delivery, operation));
        }

        var untagged = pages.Select(page => page.Family).Except(families.Select(family => family.Name)).ToList();
        if (untagged.Count > 0)
        {
            throw new Exception($"Triggers of families the contract has no tag for: {string.Join(", ", untagged)}");
        }

        // The site sorts a directory alphabetically unless told otherwise: the families are listed in the order of
        // the contract instead, after the index page and before the models.
        foreach (var (family, position) in families.Select((family, index) => (family, index + 1)))
        {
            var familyDirectory = Path.Combine(directory, Slug(family.Name));
            var familyPages = pages.Where(page => page.Family == family.Name).ToList();

            await WriteCategoryAsync(familyDirectory, family.Name, position, cancellationToken);
            await WritePageAsync(
                Path.Combine(familyDirectory, $"{Slug(family.Name)}.md"),
                BuildFamily(family, familyPages, modelFile),
                cancellationToken);

            foreach (var page in familyPages)
            {
                await WritePageAsync(
                    Path.Combine(familyDirectory, page.FileName),
                    BuildTrigger(page, modelFile, apiPage),
                    cancellationToken);
            }
        }

        await WriteCategoryAsync(Path.Combine(directory, ModelsDirectory), "Models", families.Count + 1, cancellationToken);
        await WriteCategoryAsync(directory, title, null, cancellationToken);
        await WritePageAsync(Path.Combine(directory, IndexFileName(directory)), BuildIndex(contract, title, families), cancellationToken);

        return pages;
    }

    /// <summary>
    /// The name of the index page of the reference in <paramref name="directory"/>: named after the directory, which
    /// is what makes it the directory's own page on the site, as with the sections of the API reference.
    /// </summary>
    public static string IndexFileName(string directory) => $"{Path.GetFileName(Path.TrimEndingDirectorySeparator(directory))}.md";

    /// <summary>
    /// A name as it is published: lower case, with anything but letters and digits turned into dashes, as the API
    /// reference names its pages.
    /// </summary>
    public static string Slug(string name)
    {
        var slug = new StringBuilder(name.Length);

        foreach (var character in name)
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

    private static string BuildIndex(JsonObject contract, string title, IReadOnlyList<Family> families)
    {
        var page = new StringBuilder();

        // first in the site's sidebar, under a name that does not repeat the heading of the category it opens
        page.AppendLine("---").AppendLine("sidebar_position: 0").AppendLine("sidebar_label: Overview").AppendLine("---").AppendLine();
        page.Append("# ").AppendLine(title).AppendLine();

        if (contract["info"]?["description"]?.ToString() is { Length: > 0 } description)
        {
            page.AppendLine(description.TrimEnd()).AppendLine();
        }

        page.AppendLine("## Trigger families").AppendLine();
        page.AppendLine("| Family | Description |");
        page.AppendLine("|------------- | -------------|");

        foreach (var family in families)
        {
            page.Append("| [**").Append(family.Name).Append("**](").Append(Slug(family.Name)).Append('/').Append(Slug(family.Name))
                .Append(".md) | ").Append(Cell(family.Description)).AppendLine(" |");
        }

        return page.ToString();
    }

    private static string BuildFamily(Family family, IReadOnlyList<WebhookPage> pages, Func<string, string?> modelFile)
    {
        var page = new StringBuilder();

        page.Append("# ").Append(family.Name).AppendLine(" webhooks").AppendLine();

        if (!string.IsNullOrWhiteSpace(family.Description))
        {
            page.AppendLine(family.Description.TrimEnd()).AppendLine();
        }

        page.AppendLine("| Trigger | Request body |");
        page.AppendLine("|------------- | -------------|");

        foreach (var trigger in pages)
        {
            page.Append("| [**").Append(trigger.Trigger).Append("**](").Append(trigger.FileName).Append(") | ")
                .Append(ModelLink(trigger.Delivery, modelFile)).AppendLine(" |");
        }

        return page.ToString();
    }

    private static string BuildTrigger(WebhookPage trigger, Func<string, string?> modelFile, Func<string, string?> apiPage)
    {
        var operation = trigger.Operation;
        var page = new StringBuilder();

        page.Append("# ").AppendLine(trigger.Trigger).AppendLine();
        page.Append("> ").Append(trigger.Delivery ?? "unknown").Append(' ').AppendLine(operation["operationId"]?.ToString()).AppendLine();
        page.AppendLine("`POST` to the URL of the subscription").AppendLine();

        if (operation["description"]?.ToString() is { Length: > 0 } description)
        {
            page.AppendLine(LinkEndpoints(description.TrimEnd(), apiPage)).AppendLine();
        }

        if (operation["parameters"] is JsonArray { Count: > 0 } parameters)
        {
            page.AppendLine("## Request headers").AppendLine();
            page.AppendLine("| Name | Description |");
            page.AppendLine("|------------- | -------------|");

            foreach (var parameter in parameters)
            {
                page.Append("| **").Append(parameter?["name"]).Append("** | ").Append(Cell(parameter?["description"]?.ToString())).AppendLine(" |");
            }

            page.AppendLine();
        }

        page.AppendLine("## Request body").AppendLine();
        page.AppendLine(ModelLink(trigger.Delivery, modelFile)).AppendLine();

        if (operation["responses"] is JsonObject responses)
        {
            page.AppendLine("## Responses").AppendLine();
            page.AppendLine("| Status code | Description |");
            page.AppendLine("|------------- | -------------|");

            foreach (var (code, response) in responses)
            {
                page.Append("| **").Append(code).Append("** | ").Append(Cell(response?["description"]?.ToString())).AppendLine(" |");
            }

            page.AppendLine();
        }

        page.AppendLine("## HTTP request headers").AppendLine();
        page.AppendLine("- **Content-Type**: application/json");

        return page.ToString();
    }

    /// <summary>
    /// Turns every REST API endpoint the description names into a link to its page in the API reference. Both
    /// references sit in one directory, and a trigger page is two levels below it.
    /// </summary>
    private static string LinkEndpoints(string text, Func<string, string?> apiPage)
    {
        return _endpoint.Replace(text, match =>
        {
            var page = apiPage($"{match.Groups[1].Value} /{match.Groups[2].Value}");

            return page is null ? match.Value : $"[{match.Value}](../../{page})";
        });
    }

    private static string ModelLink(string? schema, Func<string, string?> modelFile)
    {
        if (schema is null)
        {
            return "-";
        }

        var file = modelFile(schema);

        return file is null ? $"**{schema}**" : $"[**{schema}**](../models/{file})";
    }

    /// <summary>
    /// Text as one table cell: the line breaks of a multi-line description would end the row.
    /// </summary>
    private static string Cell(string? text)
    {
        return string.IsNullOrWhiteSpace(text) ? string.Empty : _lineBreak.Replace(text.Trim(), " ").Replace("|", "\\|");
    }

    private static string? SchemaName(string? reference)
    {
        return reference?[(reference.LastIndexOf('/') + 1)..];
    }

    private static async Task WritePageAsync(string path, string content, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await TextFile.WriteAsync(path, content, cancellationToken);
        await MdxSafe.ApplyAsync(path, cancellationToken);
    }

    private static async Task WriteCategoryAsync(string directory, string label, int? position, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(directory);

        var category = new JsonObject { ["label"] = label };

        if (position is not null)
        {
            category["position"] = position;
        }

        await TextFile.WriteAsync(Path.Combine(directory, "_category_.json"), category.ToJsonString(_writeOptions), cancellationToken);
    }

    private static JsonNode? ToJson(YamlNode node)
    {
        switch (node)
        {
            case YamlMappingNode mapping:
                var result = new JsonObject();

                foreach (var (key, value) in mapping.Children)
                {
                    result[((YamlScalarNode)key).Value!] = ToJson(value);
                }

                return result;

            case YamlSequenceNode sequence:
                return new JsonArray([.. sequence.Children.Select(ToJson)]);

            case YamlScalarNode { Style: ScalarStyle.Plain, Value: var plain }:
                return plain switch
                {
                    null or "null" or "~" => null,
                    "true" => true,
                    "false" => false,
                    _ when long.TryParse(plain, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer) => integer,
                    _ when double.TryParse(plain, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) => number,
                    _ => plain
                };

            case YamlScalarNode scalar:
                return scalar.Value;

            default:
                return null;
        }
    }

    /// <summary>
    /// A trigger family: its tag in the contract, and what the tag says of it.
    /// </summary>
    private sealed record Family(string Name, string? Description);

    /// <summary>
    /// One published trigger: the family it is filed under, its page, its name, the delivery model it carries and
    /// its entry in the contract.
    /// </summary>
    internal sealed record WebhookPage(string Family, string FileName, string Trigger, string? Delivery, JsonObject Operation);
}
