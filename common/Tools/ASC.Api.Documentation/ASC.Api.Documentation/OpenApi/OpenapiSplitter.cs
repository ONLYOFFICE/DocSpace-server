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
/// Cuts the joined OpenAPI document into one sub-document per section of the API, so that the
/// Markdown reference is published the way the documentation site presents it.
/// </summary>
/// <remarks>
/// The sections are the ones the documentation site presents, taken from `x-tagGroups` - the same
/// thing its sidebar is built from - rather than from the services the operations are implemented
/// by: a reader looks for rooms under Rooms and has no way of knowing that those endpoints are
/// served by the Files service. Splitting by service put five unrelated sections into one document
/// of fourteen thousand lines and spread Files across two.
/// <para>
/// The split runs on the joined document rather than on the source files: only the joined
/// document has been through <see cref="EnumCleaner"/>, the multipart fixups and the deepObject
/// styling, and rendering from the raw sources would produce different Markdown.
/// </para>
/// </remarks>
internal static class OpenapiSplitter
{
    private static readonly string[] _httpMethods =
        ["get", "put", "post", "delete", "options", "head", "patch", "trace"];

    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// One sub-document per section, named by <paramref name="nameFor"/>: the caller decides what
    /// a section is called on disk, because that name is also the published file name.
    /// </summary>
    public static async Task<IReadOnlyList<SplitDocument>> SplitBySectionAsync(
        string joinedPath,
        string outputDirectory,
        Func<string, string> nameFor,
        CancellationToken cancellationToken = default)
    {
        var joined = LoadObject(joinedPath);
        var joinedPaths = joined["paths"]?.AsObject()
            ?? throw new Exception($"Joined document has no paths: {joinedPath}");
        var joinedSchemas = joined["components"]?["schemas"] as JsonObject;

        Directory.CreateDirectory(outputDirectory);

        var results = new List<SplitDocument>();
        var covered = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (section, wanted) in CollectSections(joined, joinedPaths))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var name = nameFor(section);
            var document = BuildDocument(joined, joinedPaths, joinedSchemas, wanted, covered);
            var outputPath = Path.Combine(outputDirectory, $"{name}.json");

            await File.WriteAllTextAsync(outputPath, document.ToJsonString(_writeOptions), cancellationToken);

            results.Add(new SplitDocument(name, section, outputPath));
        }

        VerifyNothingDropped(joinedPaths, covered);

        return results;
    }

    /// <summary>
    /// The whole joined document, rebuilt the way a section is.
    /// </summary>
    /// <remarks>
    /// The models document is rendered from this rather than from the joined file itself: a
    /// schema is only told apart from an inline one openapi-generator promoted once
    /// <see cref="BuildDocument"/> has marked it as one the document declares.
    /// </remarks>
    public static async Task<SplitDocument> WholeAsync(
        string joinedPath,
        string outputDirectory,
        string name,
        CancellationToken cancellationToken = default)
    {
        var joined = LoadObject(joinedPath);
        var joinedPaths = joined["paths"]?.AsObject()
            ?? throw new Exception($"Joined document has no paths: {joinedPath}");
        var joinedSchemas = joined["components"]?["schemas"] as JsonObject;

        Directory.CreateDirectory(outputDirectory);

        var wanted = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (_, keys) in CollectSections(joined, joinedPaths))
        {
            wanted.UnionWith(keys);
        }

        var document = BuildDocument(joined, joinedPaths, joinedSchemas, wanted, []);
        var outputPath = Path.Combine(outputDirectory, $"{name}.json");

        await File.WriteAllTextAsync(outputPath, document.ToJsonString(_writeOptions), cancellationToken);

        return new SplitDocument(name, string.Empty, outputPath);
    }

    /// <summary>
    /// The operations of the joined document grouped by the section each belongs to, ordered by
    /// section name - which is the order they are published and listed in.
    /// </summary>
    private static SortedDictionary<string, HashSet<string>> CollectSections(
        JsonObject joined,
        JsonObject joinedPaths)
    {
        var sections = new SortedDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var groups = ReadTagGroups(joined);

        foreach (var path in joinedPaths)
        {
            if (path.Value is not JsonObject methods)
            {
                continue;
            }

            foreach (var method in methods)
            {
                if (!IsHttpMethod(method.Key))
                {
                    continue;
                }

                var section = SectionOf(method.Value, groups, path.Key, method.Key);

                if (!sections.TryGetValue(section, out var keys))
                {
                    keys = new HashSet<string>(StringComparer.Ordinal);
                    sections[section] = keys;
                }

                keys.Add(OperationKey(path.Key, method.Key));
            }
        }

        return sections;
    }

    /// <summary>
    /// Which tag belongs to which section, as `x-tagGroups` states it. That is what the site
    /// builds its sidebar from, so reading it here is what keeps the two in step.
    /// </summary>
    private static Dictionary<string, string> ReadTagGroups(JsonObject joined)
    {
        var groups = new Dictionary<string, string>(StringComparer.Ordinal);

        if (joined["x-tagGroups"] is not JsonArray tagGroups)
        {
            return groups;
        }

        foreach (var groupNode in tagGroups)
        {
            if (groupNode is not JsonObject group || group["tags"] is not JsonArray tags)
            {
                continue;
            }

            var name = group["name"]?.ToString();

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            foreach (var tag in tags)
            {
                var tagName = tag?.ToString();

                if (!string.IsNullOrEmpty(tagName))
                {
                    groups[tagName] = name;
                }
            }
        }

        return groups;
    }

    /// <summary>
    /// The section an operation belongs to: the group its first tag is in, or - for a tag no
    /// group claims - what the tag states before the separator, so that `Files / Folders` and
    /// `Files / Sharing` still end up together.
    /// </summary>
    /// <remarks>
    /// An untagged operation is refused rather than swept into a catch-all section: a tag is what
    /// puts an endpoint in front of a reader, on the site and here alike, and an endpoint filed
    /// under "Other" is one nobody finds.
    /// </remarks>
    private static string SectionOf(
        JsonNode? operation,
        IReadOnlyDictionary<string, string> groups,
        string path,
        string method)
    {
        var tag = (operation?["tags"] as JsonArray)?.FirstOrDefault()?.ToString();

        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new Exception($"{method.ToUpperInvariant()} {path} states no tag, so it belongs to no section");
        }

        if (groups.TryGetValue(tag, out var group))
        {
            return group;
        }

        var separator = tag.IndexOf('/', StringComparison.Ordinal);

        return (separator < 0 ? tag : tag[..separator]).Trim();
    }

    private static JsonObject BuildDocument(
        JsonObject joined,
        JsonObject joinedPaths,
        JsonObject? joinedSchemas,
        HashSet<string> wanted,
        HashSet<string> covered)
    {
        var document = new JsonObject();

        foreach (var property in joined)
        {
            if (property.Key is "paths" or "components" or "tags" or "x-tagGroups")
            {
                continue;
            }

            document[property.Key] = property.Value?.DeepClone();
        }

        var paths = new JsonObject();
        var usedTags = new HashSet<string>(StringComparer.Ordinal);
        var usedSchemas = new HashSet<string>(StringComparer.Ordinal);

        foreach (var path in joinedPaths)
        {
            if (path.Value is not JsonObject methods)
            {
                continue;
            }

            JsonObject? kept = null;

            foreach (var method in methods)
            {
                if (!IsHttpMethod(method.Key))
                {
                    continue;
                }

                var key = OperationKey(path.Key, method.Key);
                if (!wanted.Contains(key))
                {
                    continue;
                }

                covered.Add(key);

                kept ??= [];
                var operation = method.Value!.DeepClone();
                kept[method.Key] = operation;

                CollectTags(operation, usedTags);
                CollectSchemaRefs(operation, joinedSchemas, usedSchemas);
            }

            if (kept != null)
            {
                paths[path.Key] = kept;
            }
        }

        document["paths"] = paths;
        document["components"] = BuildComponents(joined, joinedSchemas, usedSchemas);

        var tags = FilterTags(joined, usedTags);
        if (tags != null)
        {
            document["tags"] = tags;
        }

        var tagGroups = FilterTagGroups(joined, usedTags);
        if (tagGroups != null)
        {
            document["x-tagGroups"] = tagGroups;
        }

        return document;
    }

    private static JsonObject BuildComponents(JsonObject joined, JsonObject? joinedSchemas, HashSet<string> usedSchemas)
    {
        var components = new JsonObject();

        if (joined["components"] is not JsonObject joinedComponents)
        {
            return components;
        }

        // The sections carried over whole may themselves point at schemas - a reusable response
        // names the error model it returns - and those refs reach no operation, so nothing has
        // collected them yet. Walk them before pruning, or the pruned `schemas` section ends up
        // missing what the copied sections reference and the document no longer resolves.
        foreach (var section in joinedComponents)
        {
            if (section.Key != "schemas")
            {
                CollectSchemaRefs(section.Value, joinedSchemas, usedSchemas);
            }
        }

        foreach (var section in joinedComponents)
        {
            // Security schemes and the like are small and referenced by name from the root
            // `security` block, so they are carried over whole; only schemas are pruned.
            if (section.Key != "schemas")
            {
                components[section.Key] = section.Value?.DeepClone();
                continue;
            }

            if (joinedSchemas == null)
            {
                continue;
            }

            var schemas = new JsonObject();

            foreach (var schema in joinedSchemas)
            {
                if (!usedSchemas.Contains(schema.Key))
                {
                    continue;
                }

                var clone = schema.Value?.DeepClone();

                // Marks the schema as one the document actually declares. openapi-generator
                // promotes inline schemas to models under synthesized names, and by the time the
                // generator sees them nothing tells the two apart - this extension does, and it
                // survives into the codegen model.
                if (clone is JsonObject cloneObject)
                {
                    cloneObject["x-declared-schema"] = true;
                }

                schemas[schema.Key] = clone;
            }

            components["schemas"] = schemas;
        }

        return components;
    }

    private static JsonArray? FilterTags(JsonObject joined, HashSet<string> usedTags)
    {
        if (joined["tags"] is not JsonArray joinedTags)
        {
            return null;
        }

        var tags = new JsonArray();

        foreach (var tag in joinedTags)
        {
            var name = tag?["name"]?.ToString();
            if (name != null && usedTags.Contains(name))
            {
                tags.Add(tag!.DeepClone());
            }
        }

        return tags;
    }

    private static JsonArray? FilterTagGroups(JsonObject joined, HashSet<string> usedTags)
    {
        if (joined["x-tagGroups"] is not JsonArray joinedGroups)
        {
            return null;
        }

        var groups = new JsonArray();

        foreach (var groupNode in joinedGroups)
        {
            if (groupNode is not JsonObject group || group["tags"] is not JsonArray groupTags)
            {
                continue;
            }

            var tags = new JsonArray();

            foreach (var tag in groupTags)
            {
                var name = tag?.ToString();
                if (name != null && usedTags.Contains(name))
                {
                    tags.Add(name);
                }
            }

            if (tags.Count == 0)
            {
                continue;
            }

            groups.Add(new JsonObject { ["name"] = group["name"]?.DeepClone(), ["tags"] = tags });
        }

        return groups;
    }

    private static void CollectTags(JsonNode operation, HashSet<string> usedTags)
    {
        if (operation["tags"] is not JsonArray tags)
        {
            return;
        }

        foreach (var tag in tags)
        {
            var name = tag?.ToString();
            if (!string.IsNullOrEmpty(name))
            {
                usedTags.Add(name);
            }
        }
    }

    private static void CollectSchemaRefs(JsonNode? node, JsonObject? schemas, HashSet<string> usedSchemas)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj)
                {
                    if (property.Key == "$ref" && property.Value?.GetValueKind() == JsonValueKind.String)
                    {
                        AddSchemaRef(property.Value.ToString(), schemas, usedSchemas);
                        continue;
                    }

                    CollectSchemaRefs(property.Value, schemas, usedSchemas);
                }

                break;

            case JsonArray array:
                foreach (var item in array)
                {
                    CollectSchemaRefs(item, schemas, usedSchemas);
                }

                break;
        }
    }

    private static void AddSchemaRef(string reference, JsonObject? schemas, HashSet<string> usedSchemas)
    {
        const string prefix = "#/components/schemas/";

        if (!reference.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        var name = reference[prefix.Length..];

        if (!usedSchemas.Add(name) || schemas == null)
        {
            return;
        }

        if (schemas.TryGetPropertyValue(name, out var schema))
        {
            CollectSchemaRefs(schema, schemas, usedSchemas);
        }
    }

    /// <summary>
    /// Every operation of the joined document has to land in exactly one sub-document.
    /// A silently dropped operation would look identical to one that simply has no docs.
    /// </summary>
    private static void VerifyNothingDropped(JsonObject joinedPaths, HashSet<string> covered)
    {
        var missing = new List<string>();

        foreach (var path in joinedPaths)
        {
            if (path.Value is not JsonObject methods)
            {
                continue;
            }

            foreach (var method in methods)
            {
                if (!IsHttpMethod(method.Key))
                {
                    continue;
                }

                var key = OperationKey(path.Key, method.Key);
                if (!covered.Contains(key))
                {
                    missing.Add(key);
                }
            }
        }

        if (missing.Count > 0)
        {
            throw new Exception(
                $"{missing.Count} operation(s) of the joined document belong to no source file, " +
                $"so they would get no documentation: {string.Join(", ", missing.Take(10))}" +
                (missing.Count > 10 ? ", ..." : string.Empty));
        }
    }

    private static bool IsHttpMethod(string key)
    {
        return Array.Exists(_httpMethods, method => method.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    private static string OperationKey(string path, string method)
    {
        return $"{method.ToLowerInvariant()} {path}";
    }

    private static JsonObject LoadObject(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Openapi file not found: {path}");
        }

        try
        {
            return JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        }
        catch (Exception ex)
        {
            throw new Exception($"Invalid JSON in file: {path}\n{ex.Message}");
        }
    }

    /// <summary>
    /// A sub-document on disk: the name it is published under, the section it holds (empty for
    /// the models document, which stands for all of them) and where it was written.
    /// </summary>
    internal sealed record SplitDocument(string Name, string Section, string Path);
}
