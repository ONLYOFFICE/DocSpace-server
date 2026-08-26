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
/// Cuts the joined OpenAPI document back into one sub-document per source service, so the
/// Markdown documentation can be grouped the way the `json/*.json` documents are.
/// </summary>
/// <remarks>
/// The split runs on the joined document rather than on the source files directly: only the
/// joined document has been through <see cref="EnumCleaner"/>, the multipart fixups and the
/// deepObject styling, and rendering from the raw sources would produce different Markdown.
/// The source files are read only to learn which operations belong to which service -
/// <see cref="OpenapiJoiner"/> rejects duplicate path+method pairs, so that mapping is
/// unambiguous.
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

    public static async Task<IReadOnlyList<SplitDocument>> SplitAsync(
        string joinedPath,
        IReadOnlyList<string> sourceFiles,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        var joined = LoadObject(joinedPath);
        var joinedPaths = joined["paths"]?.AsObject()
            ?? throw new Exception($"Joined document has no paths: {joinedPath}");
        var joinedSchemas = joined["components"]?["schemas"] as JsonObject;

        Directory.CreateDirectory(outputDirectory);

        var results = new List<SplitDocument>();
        var covered = new HashSet<string>(StringComparer.Ordinal);

        foreach (var sourceFile in sourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var name = GetDocumentName(sourceFile);
            var wanted = CollectOperationKeys(LoadObject(sourceFile));

            var document = BuildDocument(joined, joinedPaths, joinedSchemas, wanted, covered);
            var outputPath = Path.Combine(outputDirectory, $"{name}.json");

            await File.WriteAllTextAsync(outputPath, document.ToJsonString(_writeOptions), cancellationToken);

            results.Add(new SplitDocument(name, outputPath));
        }

        VerifyNothingDropped(joinedPaths, covered);

        return results;
    }

    /// <summary>
    /// `files_2.0.json` becomes `files`, `oauth.json` becomes `oauth`. The name ends up as the
    /// Markdown file name, so it has to stay stable across runs.
    /// </summary>
    private static string GetDocumentName(string sourceFile)
    {
        var name = Path.GetFileNameWithoutExtension(sourceFile);

        var versionSuffix = name.LastIndexOf('_');
        if (versionSuffix > 0)
        {
            name = name[..versionSuffix];
        }

        return name;
    }

    private static HashSet<string> CollectOperationKeys(JsonObject source)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);

        if (source["paths"] is not JsonObject paths)
        {
            return keys;
        }

        foreach (var path in paths)
        {
            if (path.Value is not JsonObject methods)
            {
                continue;
            }

            foreach (var method in methods)
            {
                if (IsHttpMethod(method.Key))
                {
                    keys.Add(OperationKey(path.Key, method.Key));
                }
            }
        }

        return keys;
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

    internal sealed record SplitDocument(string Name, string Path);
}
