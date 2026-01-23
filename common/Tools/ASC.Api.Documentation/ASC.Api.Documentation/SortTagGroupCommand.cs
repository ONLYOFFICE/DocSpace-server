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

public class SortTagGroupsCommand : AsyncCommand<FilePathSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, FilePathSettings settings, CancellationToken cancellationToken)
    {
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"Sort tags [green]{settings.File}[/]");
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

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        task.Increment(30);

        var root = JsonNode.Parse(json)?.AsObject() ?? throw new InvalidOperationException("Invalid JSON");

        BuildTagGroups(root);
        task.Increment(40);

        await File.WriteAllTextAsync(filePath,
            root.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }), cancellationToken);

        task.Increment(30);
    }

    private static void BuildTagGroups(JsonObject root)
    {
        if (root["tags"] is not JsonArray tags)
        {
            return;
        }

        var groups = new SortedDictionary<string, SortedSet<string>>();

        foreach (var tag in tags.OfType<JsonObject>())
        {
            var name = tag["name"]?.GetValue<string>();
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            var group = name.Split(" / ")[0];

            if (!groups.TryGetValue(group, out var set))
            {
                groups[group] = set = [];
            }

            set.Add(name);
        }

        root["x-tagGroups"] = new JsonArray(
            groups.Select(g =>
                new JsonObject
                {
                    ["name"] = g.Key,
                    ["tags"] = new JsonArray(
                        g.Value.Select(t => JsonValue.Create(t)).ToArray()
                    )
                }
            ).ToArray()
        );
    }

}

