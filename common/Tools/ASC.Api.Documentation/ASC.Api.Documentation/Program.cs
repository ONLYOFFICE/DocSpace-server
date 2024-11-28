// (c) Copyright Ascensio System SIA 2009-2024
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

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.Text.Encodings.Web;

public class Programm
{
    public static async Task Main(string[] args)
    {
        var assembliesPath = $"{AppContext.BaseDirectory}";

        if (!Directory.Exists(assembliesPath))
        {
            return;
        }
        var assemblyFiles = Directory.GetFiles(assembliesPath, "*.dll");

        foreach (var assemblyFile in assemblyFiles)
        {
            var assembly = Assembly.LoadFrom(assemblyFile);
            var assemblyName = assembly.GetName().Name?.ToLower();
            if (assemblyName != null && IsApiAssembly(assemblyName))
            {

                var baseUrl = $"http://localhost:8092/openapi/{assemblyName}/common";
                await GenerateSwaggerFile(baseUrl, assemblyName);
            }
        }
    }


    private static async Task GenerateSwaggerFile(string baseUrl, string name)
    {
        var httpClient = new HttpClient();

        try
        {
            var swaggerFileJson = await httpClient.GetStringAsync($"{baseUrl}.json");
            var swaggerFileYaml = await httpClient.GetStringAsync($"{baseUrl}.yaml");
            var modifiedJson = SelectExtension(swaggerFileJson, "json");
            var jsonFileName = $"{name}.swagger.json";
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, jsonFileName), modifiedJson);
            Console.WriteLine($"JSON file created successfully: {jsonFileName}");

            var modifiedYaml = SelectExtension(swaggerFileYaml, "yaml");
            var yamlFileName = $"{name}.swagger.yaml";
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, yamlFileName), modifiedYaml);
            Console.WriteLine($"YAML file created successfully: {yamlFileName}");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static string SelectExtension(string swaggerData, string extension)
    {
        if(extension == "json")
        {
            return JsonNode.Parse(swaggerData) is not JsonObject jsonObject ? swaggerData : ModifyJsonDocumentation(jsonObject);
        }
        else if(extension == "yaml") 
        {
            return ModifyYamlDocumentation(swaggerData);
        }
        return swaggerData;
    }

    private static string ModifyJsonDocumentation(JsonObject jsonObject)
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
                                    ? (JsonNode)$"{summaryText}\n\n**Note**: {existingDescriptionNode}"
                                    : (JsonNode)summaryText;
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
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        return jsonObject.ToJsonString(options);
    }

    private static string ModifyYamlDocumentation(string yamlData)
    {
        var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();

        var yamlObject = deserializer.Deserialize<Dictionary<object, object>>(yamlData);

        if (yamlObject != null && yamlObject.TryGetValue("paths", out var pathsNode) && pathsNode is Dictionary<object, object> paths)
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
                                    ? $"{summaryText}\n\n**Note**: {existingDescription}"
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

    private static bool IsApiAssembly(string name)
    {
        return name switch
        {
            "asc.files" => true,
            "asc.data.backup" => true,
            "asc.people" => true,
            "asc.web.api" => true,
            "asc.apisystem" => true,
            _ => false
        };
    }
}

