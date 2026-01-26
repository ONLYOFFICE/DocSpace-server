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

using ASC.Api.Documentation;

public class RemoveEnumCommand : AsyncCommand<FilePathSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, FilePathSettings settings, CancellationToken cancellationToken)
    {
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"Remove enum [green]{settings.File}[/]");
                await ProcessFileAsync(settings.File, task, cancellationToken);
                task.Value = 100;
            });

        return 0;
    }

    private static async Task ProcessFileAsync(string filePath, ProgressTask task, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(filePath);
        }

        var jsonText = await File.ReadAllTextAsync(filePath, cancellationToken);
        task.Increment(20);

        var root = JsonNode.Parse(jsonText)?.AsObject() ?? throw new InvalidOperationException("Invalid JSON");
        task.Increment(10);

        ProcessNode(root);
        task.Increment(50);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        await File.WriteAllTextAsync(filePath, root.ToJsonString(options), cancellationToken);
        task.Increment(20);
    }

    private static void ProcessNode(JsonNode node, string? preferredEnumType = null, JsonObject? parentNode = null)
    {
        if (node is not JsonObject obj)
        {
            return;
        }

        var keys = obj.Select(kv => kv.Key).ToList();
        foreach (var key in keys)
        {
            var value = obj[key];

            if (key == "summary" && value is JsonValue sv)
            {
                obj[key] = JsonValue.Create(sv.GetValue<string>().Replace("\"", ""));
            }

            if (key == "description" && value is JsonValue dv)
            {
                obj[key] = JsonValue.Create(dv.GetValue<string>().Replace("\"", ""));
            }

            if ((key == "anyOf" || key == "oneOf") && value is JsonArray arr && IsEnumAnyOf(arr))
            {
                var targetType = preferredEnumType ?? obj["x-enum-type"]?.GetValue<string>() ?? "integer";

                var preferred = arr.OfType<JsonObject>().FirstOrDefault(o => (o["x-enum-type"]?.GetValue<string>() ?? o["type"]?.GetValue<string>()) == targetType);

                if (preferred != null)
                {
                    if (preferred["enum"] != null)
                    {
                        obj["enum"] = preferred["enum"]!.DeepClone();
                    }

                    obj["type"] = targetType;

                    if (preferred["example"] != null)
                    {
                        obj["example"] = preferred["example"]!.DeepClone();
                    }

                    if (preferred["x-enum-varnames"] != null)
                    {
                        obj["x-enum-varnames"] = preferred["x-enum-varnames"]!.DeepClone();
                    }

                    obj["description"] = parentNode?["description"]?.DeepClone() ?? preferred["description"]?.DeepClone();

                    obj.Remove(key);
                }

                continue;
            }

            if (value is JsonObject childObj)
            {
                ProcessNode(childObj, preferredEnumType, obj);
            }
            else if (value is JsonArray childArr)
            {
                foreach (var item in childArr)
                {
                    ProcessNode(item, preferredEnumType, obj);
                }
            }
        }
    }

    private static bool IsEnumAnyOf(JsonArray arr)
    {
        return arr.All(item => item is JsonObject o && o["$ref"] == null && (o["type"]?.GetValue<string>() == "string" || o["type"]?.GetValue<string>() == "integer") && o["enum"] is JsonArray);
    }
}
