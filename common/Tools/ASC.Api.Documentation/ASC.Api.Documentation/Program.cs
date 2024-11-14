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

public class Programm
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Write extension to file: yaml or json");
        var extension = Console.ReadLine();

        if (extension != "yaml" && extension != "json")
        {
            ArgumentNullException.ThrowIfNull(extension);

            Console.WriteLine("Wrong extension");
            return;
        }
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

                var url = $"http://localhost:8092/openapi/{assemblyName}/common.{extension}";
                await GenerateSwaggerFile(url, assemblyName, extension);
            }
        }
    }


    private static async Task GenerateSwaggerFile(string url, string name, string extension)
    {
        var httpClient = new HttpClient();

        try
        {
            var swaggerFile = await httpClient.GetStringAsync(url);
            var fileName = $"{name}.swagger.{extension}";
            var modifiedSwaggerData = SelectExtension(swaggerFile, extension);

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, fileName), modifiedSwaggerData);
            Console.WriteLine($"File created successfully {fileName}");
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
            var swaggerModifiedDoc = JsonNode.Parse(swaggerData) is not JsonObject jsonObject ? swaggerData : ModifyDocumentation(jsonObject);
            return swaggerModifiedDoc;
        }
        else if(extension == "yaml") 
        {
            return swaggerData;
        }
        return swaggerData;
    }

    private static string ModifyDocumentation(JsonObject jsonObject)
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
        return jsonObject.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
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

