// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Api.Documentation;

public class OpenapiJoiner : AsyncCommand<JoinSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, JoinSettings settings, CancellationToken cancellationToken)
    {
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("Joining [green]openapi files[/]");

                await JoinAsync(settings.Output, [.. settings.Files!], progress: percent => task.Value = percent, cancellationToken);

                task.Value = 100;
            });

        return 0;
    }
    public static async Task JoinAsync(string outputPath, string[] inputFiles, Action<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (inputFiles == null || inputFiles.Length == 0)
        {
            throw new ArgumentException("No openapi files provided.");
        }

        JsonObject? result = null;
        var usedOperationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        double totalSteps = inputFiles.Length + 1; 
        double currentStep = 0;

        foreach (var file in inputFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"Openapi file not found: {file}");
            }

            var openapi = LoadJson(file);

            if (result == null)
            {
                result = openapi;
                CollectOperationIds(result, usedOperationIds);
            }
            else
            {
                MergeOpenapiFile(result, openapi, usedOperationIds, file);
            }

            currentStep++;
            progress?.Invoke(currentStep / totalSteps * 100);
        }

        if (result == null)
        {
            throw new Exception("Nothing to merge.");
        }

        SortTagGroups(result);
        EnumCleaner.Clean(result);

        ApplyDeepObjectStyle(result);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        await File.WriteAllTextAsync(outputPath, result.ToJsonString(options), cancellationToken);

        progress?.Invoke(100);
    }

    static JsonObject LoadJson(string path)
    {
        try
        {
            return JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        }
        catch (Exception ex)
        {
            throw new Exception($"Invalid JSON in file: {path}\n{ex.Message}");
        }
    }

    static void MergeOpenapiFile( JsonObject target, JsonObject source, HashSet<string> usedOperationIds, string fileName)
    {
        MergePaths(target, source, usedOperationIds, fileName);
        MergeTags(target, source);
        MergeComponents(target, source, fileName);
        MergeServers(target, source);
        MergeXTagGroups(target, source);
    }

    static void MergePaths(JsonObject target, JsonObject source, HashSet<string> usedOperationIds, string fileName)
    {
        if (!source.TryGetPropertyValue("paths", out var sourcePathsNode))
        {
            return;
        }

        if (!target.TryGetPropertyValue("paths", out var targetPathsNode))
        {
            target["paths"] = sourcePathsNode!.DeepClone();
            CollectOperationIds(target, usedOperationIds);
            return;
        }

        var sourcePaths = sourcePathsNode!.AsObject();
        var targetPaths = targetPathsNode!.AsObject();

        foreach (var pathEntry in sourcePaths)
        {
            var pathKey = pathEntry.Key;

            if (!targetPaths.ContainsKey(pathKey))
            {
                targetPaths[pathKey] = pathEntry.Value!.DeepClone();
                CollectOperationIdsFromPath(pathEntry.Value!.AsObject(), usedOperationIds, fileName, pathKey);
                continue;
            }

            var trgMethods = targetPaths[pathKey]!.AsObject();
            var srcMethods = pathEntry.Value!.AsObject();

            foreach (var method in srcMethods)
            {
                if (trgMethods.ContainsKey(method.Key))
                {
                    throw new Exception($"Duplicate path and method detected: {pathKey} [{method.Key}] in {fileName}");
                }

                CheckOperationId(method.Value!.AsObject(), usedOperationIds, fileName, pathKey);

                trgMethods[method.Key] = method.Value!.DeepClone();
            }
        }
    }

    static void CollectOperationIds(JsonObject pathObj, HashSet<string> set)
    {
        if (!pathObj.TryGetPropertyValue("paths", out var pathsNode))
        {
            return;
        }

        foreach (var path in pathsNode!.AsObject())
        {
            CollectOperationIdsFromPath(path.Value!.AsObject(), set, "initial", path.Key);
        }
    }

    static void CollectOperationIdsFromPath(JsonObject pathObj, HashSet<string> set, string file, string path)
    {
        foreach (var method in pathObj)
        {
            if (method.Value is not JsonObject methodObj)
            {
                continue;
            }

            if (!methodObj.TryGetPropertyValue("operationId", out var opNode))
            {
                continue;
            }

            var opId = opNode!.ToString();

            if (string.IsNullOrWhiteSpace(opId))
            {
                throw new Exception($"Empty operationId in {file}, path {path}");
            }

            if (!set.Add(opId))
            {
                throw new Exception($"Duplicate operationId '{opId}' in {file}, path {path}");
            }
        }
    }

    static void CheckOperationId(JsonObject methodObj, HashSet<string> set, string fileName, string path)
    {
        if (!methodObj.TryGetPropertyValue("operationId", out var opNode))
        {
            return;
        }

        var opId = opNode!.ToString();

        if (string.IsNullOrWhiteSpace(opId))
        {
            throw new Exception($"Empty operationId in {fileName}, path {path}");
        }

        if (set.Contains(opId))
        {
            throw new Exception($"Duplicate operationId '{opId}' in file {fileName}, path {path}");
        }

        set.Add(opId);
    }

    static void MergeTags(JsonObject target, JsonObject source)
    {
        if (!source.TryGetPropertyValue("tags", out var srcTagsNode))
        {
            return;
        }

        if (!target.TryGetPropertyValue("tags", out var trgTagsNode))
        {
            target["tags"] = srcTagsNode!.DeepClone();
            return;
        }

        var trgTags = trgTagsNode!.AsArray();
        var existing = new HashSet<string>();

        foreach (var tag in trgTags)
        {
            var name = tag!["name"]!.ToString();
            existing.Add(name);
        }

        foreach (var tag in srcTagsNode!.AsArray())
        {
            var name = tag!["name"]!.ToString();
            if (!existing.Contains(name))
            {
                trgTags.Add(tag!.DeepClone());
            }
        }
    }

    static void MergeXTagGroups(JsonObject target, JsonObject source)
    {
        if (!source.TryGetPropertyValue("x-tagGroups", out var sourceNode))
        {
            return;
        }

        if (!target.TryGetPropertyValue("x-tagGroups", out var targetNode))
        {
            target["x-tagGroups"] = sourceNode!.DeepClone();
            return;
        }

        var targetArr = targetNode!.AsArray();
        var existing = new Dictionary<string, JsonObject>();

        foreach (var item in targetArr)
        {
            var obj = item!.AsObject();
            var name = obj["name"]!.ToString();
            existing[name] = obj;
        }

        foreach (var item in sourceNode!.AsArray())
        {
            var sourceObj = item!.AsObject();
            var name = sourceObj["name"]!.ToString();

            if (!existing.TryGetValue(name, out var trgObj))
            {
                targetArr.Add(sourceObj.DeepClone());
                continue;
            }

            var targetTags = trgObj["tags"]!.AsArray();
            var set = new HashSet<string>(targetTags.Select(t => t!.ToString()));

            foreach (var tag in sourceObj["tags"]!.AsArray())
            {
                var t = tag!.ToString();
                if (set.Add(t))
                {
                    targetTags.Add(t);
                }
            }
        }
    }

    static void SortTagGroups(JsonObject root)
    {
        if (!root.TryGetPropertyValue("x-tagGroups", out var node))
        {
            return;
        }

        foreach (var groupNode in node!.AsArray())
        {
            var group = groupNode!.AsObject();

            if (!group.TryGetPropertyValue("tags", out var tagsNode))
            {
                continue;
            }

            foreach (var tag in tagsNode!.AsArray())
            {
                var t = tag!.ToString();

                if (!string.IsNullOrEmpty(t) && char.IsLower(t[0]))
                {
                    throw new Exception($"Tag starts with lowercase: '{t}'");
                }
            }

            var sorted = tagsNode!.AsArray().Select(t => t!.ToString()).OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToArray();

            var newArr = new JsonArray();
            foreach (var tag in sorted)
            {
                newArr.Add(tag);
            }

            group["tags"] = newArr;
        }
    }

    static void MergeComponents(JsonObject target, JsonObject source, string fileName)
    {
        if (!source.TryGetPropertyValue("components", out var srcCompNode))
        {
            return;
        }

        if (!target.TryGetPropertyValue("components", out var trgCompNode))
        {
            target["components"] = srcCompNode!.DeepClone();
            return;
        }

        var srcComp = srcCompNode!.AsObject();
        var trgComp = trgCompNode!.AsObject();

        foreach (var section in srcComp)
        {
            if (!trgComp.ContainsKey(section.Key))
            {
                trgComp[section.Key] = section.Value!.DeepClone();
                continue;
            }

            var srcSection = section.Value!.AsObject();
            var trgSection = trgComp[section.Key]!.AsObject();

            foreach (var item in srcSection)
            {
                if (!trgSection.ContainsKey(item.Key))
                {
                    trgSection[item.Key] = item.Value!.DeepClone();
                    continue;
                }

                var existing = trgSection[item.Key]!;

                if(!JsonDeepEquals(existing, item.Value))
                {
                    throw new Exception($"Component conflict in '{section.Key}/{item.Key}' in {fileName}");
                }
            }
        }
    }

    static void MergeServers(JsonObject target, JsonObject source)
    {
        if (!source.TryGetPropertyValue("servers", out var srcNode))
        {
            return;
        }

        if (!target.TryGetPropertyValue("servers", out var trgNode))
        {
            target["servers"] = srcNode!.DeepClone();
            return;
        }

        var trgArr = trgNode!.AsArray();
        var existingUrls = new HashSet<string>();

        foreach (var s in trgArr)
        {
            var url = s!["url"]?.ToString();
            if (!string.IsNullOrWhiteSpace(url))
            {
                existingUrls.Add(url);
            }
        }

        foreach (var server in srcNode!.AsArray())
        {
            var url = server!["url"]?.ToString();

            if (string.IsNullOrWhiteSpace(url) || existingUrls.Contains(url))
            {
                continue;
            }

            trgArr.Add(server.DeepClone());
            existingUrls.Add(url);
        }
    }

    static void ApplyDeepObjectStyle(JsonObject root)
    {
        if (!root.TryGetPropertyValue("paths", out var pathsNode))
        {
            return;
        }

        var components = root["components"]?["schemas"] as JsonObject;

        foreach (var path in pathsNode!.AsObject())
        {
            var methods = path.Value!.AsObject();

            foreach (var method in methods)
            {
                if (method.Value is not JsonObject methodObj)
                {
                    continue;
                }

                if (!methodObj.TryGetPropertyValue("parameters", out var paramsNode))
                {
                    continue;
                }

                foreach (var paramNode in paramsNode!.AsArray())
                {
                    if (paramNode is not JsonObject param)
                    {
                        continue;
                    }

                    if (param["in"]?.ToString() != "query")
                    {
                        continue;
                    }

                    if (!param.TryGetPropertyValue("schema", out var schemaNode))
                    {
                        continue;
                    }

                    if (IsObjectSchema(schemaNode!, components))
                    {
                        param["style"] = "deepObject";
                    }
                }
            }
        }
    }

    static bool IsObjectSchema(JsonNode schemaNode, JsonObject? components)
    {
        if (schemaNode is not JsonObject schemaObj)
        {
            return false;
        }

        if (schemaObj.TryGetPropertyValue("$ref", out var refNode))
        {
            var refValue = refNode!.ToString();

            const string prefix = "#/components/schemas/";
            if (refValue.StartsWith(prefix) && components != null)
            {
                var name = refValue.Substring(prefix.Length);

                if (components.TryGetPropertyValue(name, out var compSchema))
                {
                    var compObj = compSchema as JsonObject;
                    if (compObj?["type"]?.ToString() == "object")
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    static bool JsonDeepEquals(JsonNode a, JsonNode b)
    {
        return a.ToJsonString() == b.ToJsonString();
    }
}

