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

namespace ASC.AI.Service;

[Scope]
public class ToolService(
    IServiceProvider serviceProvider,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity)
{
    public async Task<ToolListResponse> GetToolsAsync(ToolContext context)
    {
        var prompt = new StringBuilder();
        var descriptors = new List<ToolDescriptor>();

        var resolvedContext = await ResolveContext(context);

        foreach (var factory in serviceProvider.GetServices<IAiToolFactory>())
        {
            var bundle = await factory.BuildAsync(resolvedContext);
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
        var resolvedContext = await ResolveContext(context);

        var factories = serviceProvider.GetServices<IAiToolFactory>().ToArray();
        var targetNames = calls.Select(c => c.Name).ToHashSet(StringComparer.Ordinal);
        var functions = new Dictionary<string, AIFunction>(StringComparer.Ordinal);
        var visited = new HashSet<IAiToolFactory>();

        foreach (var factory in targetNames.Select(
                     name => factories.FirstOrDefault(
                         f => f.Owns(name))).OfType<IAiToolFactory>().Where(visited.Add))
        {
            var bundle = await factory.BuildAsync(resolvedContext);
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

    private async Task<ResolvedToolContext> ResolveContext(ToolContext context)
    {
        var folderTask = ResolveFolderAsync(context.FolderId);
        var formTask = ResolveFormAsync(context.FormId);

        await Task.WhenAll(folderTask, formTask);

        return new ResolvedToolContext
        {
            Folder = await folderTask,
            Form = await formTask
        };
    }

    private async Task<IFolder?> ResolveFolderAsync(JsonElement? folderId)
    {
        switch (folderId)
        {
            case { ValueKind: JsonValueKind.Number } value:
                var folder = await daoFactory.GetFolderDao<int>().GetFolderAsync(value.GetInt32());
                return folder is null
                    ? null
                    : await fileSecurity.SetSecurity(new[] { folder }.ToAsyncEnumerable()).FirstAsync() as IFolder;

            case { ValueKind: JsonValueKind.String } value :
                var stringFolder = await daoFactory.GetFolderDao<string>().GetFolderAsync(value.GetString()!);
                return stringFolder is null
                    ? null
                    : await fileSecurity.SetSecurity(new[] { stringFolder }.ToAsyncEnumerable()).FirstAsync() as IFolder;

            default:
                return null;
        }
    }

    private async Task<File<int>?> ResolveFormAsync(int formId)
    {
        if (formId <= 0)
        {
            return null;
        }

        var form = await daoFactory.GetFileDao<int>().GetFileAsync(formId);

        return form is null
            ? null
            : await fileSecurity.SetSecurity(new[] { form }.ToAsyncEnumerable()).FirstAsync() as File<int>;
    }
}
