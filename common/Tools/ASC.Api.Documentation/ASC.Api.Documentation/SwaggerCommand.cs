// (c) Copyright Ascensio System SIA 2009-2025
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

public class SwaggerCommand : AsyncCommand<Settings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                await Task.WhenAll(settings.Endpoints.Select(async (r, _) =>
                {
                    var p = GetValue(r.Value, ".yaml");
                    p = GetValue(p, ".json");

                    var ctxTask = ctx.AddTask($"Generate [green]{r.Key}[/]");

                    await GenerateSwaggerFiles(ctxTask, $"{settings.BaseUrl}{p}", r.Key, cancellationToken);

                }));
            });
        
        return 0;
    }

    static async Task GenerateSwaggerFiles(ProgressTask task, string baseUrl, string name, CancellationToken cancellationToken)
    {
        var json = GenerateSwaggerFile(task, baseUrl, name, "json", cancellationToken);
        var yaml = GenerateSwaggerFile(task, baseUrl, name, "yaml", cancellationToken);

        await Task.WhenAll(json, yaml);
    }

    static async Task GenerateSwaggerFile(ProgressTask task, string baseUrl, string name, string extension, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient();
            var swaggerFileJson = await httpClient.GetStringAsync($"{baseUrl}.{extension}", cancellationToken);
            task.Increment(25);
            
            var modifiedJson = SelectExtension(swaggerFileJson, extension);
            var jsonFileName = $"{name}.swagger.{extension}";
            await File.WriteAllTextAsync(Path.Combine(AppContext.BaseDirectory, jsonFileName), modifiedJson, cancellationToken);
            task.Increment(25);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    static string SelectExtension(string swaggerData, string extension) => extension switch
    {
        "json" => JsonNode.Parse(swaggerData) is JsonObject jsonObject ? ModifyJsonDocumentation(jsonObject) : swaggerData,
        "yaml" => ModifyYamlDocumentation(swaggerData),
        _ => swaggerData
    };

    static string ModifyJsonDocumentation(JsonObject jsonObject)
    {
        if (jsonObject.TryGetPropertyValue("paths", out var pathsNode) && pathsNode is JsonObject paths)
        {
            foreach (var path in paths)
            {
                if (path.Key.EndsWith("{.format}"))
                {
                    paths.Remove(path.Key);
                    continue;
                }

                if (path.Value is JsonObject methods)
                {
                    foreach (var method in methods)
                    {
                        if (method.Value is JsonObject methodDetails)
                        {
                            if (methodDetails.TryGetPropertyValue("description", out var descriptionNode))
                            {
                                methodDetails["description"] = $"**Note**: {descriptionNode}";
                            }

                            if (methodDetails.TryGetPropertyValue("summary", out var summaryNode) && summaryNode != null)
                            {
                                var summaryText = summaryNode.ToString();

                                methodDetails["description"] = methodDetails.TryGetPropertyValue("description", out var existingDescriptionNode)
                                    ? (JsonNode)$"{summaryText}\n\n {existingDescriptionNode}"
                                    : summaryText;
                            }

                            if (methodDetails.TryGetPropertyValue("x-shortName", out var shortNameNode) && shortNameNode != null)
                            {
                                var shortNameText = shortNameNode.ToString();
                                methodDetails["summary"] = shortNameText;
                                methodDetails.Remove("x-shortName");
                            }
                        }
                    }
                }
            }
        }

        var options = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        return jsonObject.ToJsonString(options);
    }

    static string ModifyYamlDocumentation(string yamlData)
    {
        var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

        var yamlObject = deserializer.Deserialize<Dictionary<object, object>>(yamlData);

        if (yamlObject.TryGetValue("paths", out var pathsNode) && pathsNode is Dictionary<object, object> paths)
        {
            var keysToRemove = new List<object>();

            foreach (var path in paths)
            {
                if (path.Key is string pathKey && pathKey.EndsWith("{.format}"))
                {
                    keysToRemove.Add(pathKey);
                    continue;
                }

                if (path.Value is Dictionary<object, object> methods)
                {
                    foreach (var method in methods)
                    {
                        if (method.Value is Dictionary<object, object> methodDetails)
                        {
                            if (methodDetails.TryGetValue("description", out var description))
                            {
                                methodDetails["description"] = $"**Note**: {description}";
                            }

                            if (methodDetails.TryGetValue("summary", out var summary) && summary is string summaryText)
                            {
                                methodDetails["description"] = methodDetails.TryGetValue("description", out var existingDescription)
                                    ? $"{summaryText}\n\n {existingDescription}"
                                    : (object)summaryText;
                            }

                            if (methodDetails.TryGetValue("x-shortName", out var shortName) && shortName is string shortNameText)
                            {
                                methodDetails["summary"] = shortNameText;
                                methodDetails.Remove("x-shortName");
                            }
                        }
                    }
                }
            }

            foreach (var key in keysToRemove)
            {
                paths.Remove(key);
            }
        }

        var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

        return serializer.Serialize(yamlObject);
    }
    
    string GetValue(string p, string ext)
    {
        var lastIndex = p.LastIndexOf(ext, StringComparison.Ordinal);
        if (lastIndex > 0)
        {
            p = p[..lastIndex];
        }

        return p;
    }
}