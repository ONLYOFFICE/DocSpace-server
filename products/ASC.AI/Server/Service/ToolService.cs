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

namespace ASC.AI.Service;

[Scope]
public class ToolService(IServiceProvider serviceProvider)
{
    public async Task<ToolListResponse> GetToolsAsync(ToolContext context)
    {
        var prompt = new StringBuilder();
        var descriptors = new List<ToolDescriptor>();

        foreach (var factory in serviceProvider.GetServices<IAiToolFactory>())
        {
            var bundle = await factory.BuildAsync(context);
            if (!string.IsNullOrEmpty(bundle.Prompt))
            {
                if (prompt.Length > 0)
                {
                    prompt.Append("\n\n");
                }
                prompt.Append(bundle.Prompt);
            }

            descriptors.AddRange(bundle.Tools.Select(tool =>
                new ToolDescriptor(
                    tool.Name,
                    tool.Function.Description,
                    tool.Function.JsonSchema)));
        }

        return new ToolListResponse(prompt.ToString(), descriptors);
    }

    public async Task<IReadOnlyList<ToolCallResult>> CallAsync(
        IReadOnlyList<ToolCall> calls,
        ToolContext context)
    {
        var factories = serviceProvider.GetServices<IAiToolFactory>().ToArray();
        var targetNames = calls.Select(c => c.Name).ToHashSet(StringComparer.Ordinal);
        var functions = new Dictionary<string, AIFunction>(StringComparer.Ordinal);
        var visited = new HashSet<IAiToolFactory>();

        foreach (var factory in targetNames.Select(
                     name => factories.FirstOrDefault(
                         f => f.Owns(name))).OfType<IAiToolFactory>().Where(visited.Add))
        {
            var bundle = await factory.BuildAsync(context);
            foreach (var tool in bundle.Tools)
            {
                if (targetNames.Contains(tool.Name))
                {
                    functions[tool.Name] = tool.Function;
                }
            }
        }

        var tasks = calls.Select(call => InvokeAsync(call, functions));
        return await Task.WhenAll(tasks);
    }

    private static async Task<ToolCallResult> InvokeAsync(
        ToolCall call,
        Dictionary<string, AIFunction> functions)
    {
        if (!functions.TryGetValue(call.Name, out var function))
        {
            return new ToolCallResult(call.Id, call.Name, Result: null, Error: "tool_not_available");
        }

        try
        {
            var result = await function.InvokeAsync(new AIFunctionArguments(call.Arguments));
            return new ToolCallResult(call.Id, call.Name, result, Error: null);
        }
        catch (Exception e)
        {
            return new ToolCallResult(call.Id, call.Name, Result: null, Error: e.Message);
        }
    }
}
