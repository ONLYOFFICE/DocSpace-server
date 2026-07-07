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

using ASC.AI.Core.Tools;

namespace ASC.AI.Tools;

[Scope(typeof(IAiToolFactory))]
public class EditorToolsFactory(
    TenantManager tenantManager,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    FileStorageService fileStorageService,
    GlobalFolderHelper globalFolderHelper) : IAiToolFactory
{
    private const string GenerateDocxName = "docspace_generate_docx";
    private const string GeneratePresentationName = "docspace_generate_presentation";
    private const string GenerateFormName = "docspace_generate_form";

    private const string GenerateDocxDescription = "Use this function if you are asked to generate a textual document (report, article, letter, etc.) based on a description. Input: Short description of what needs to be generated.";
    private const string GeneratePresentationDescription = "Generates a complete presentation with custom theme, fonts, and streaming content.";
    private const string GenerateFormDescription = "Use this function if you are asked to generate a form or document template (contract, any document for filling) based on a description. Input: a detailed description of the desired form or template.";

    private static readonly HashSet<string> _toolNames = new(StringComparer.Ordinal)
    {
        GenerateDocxName,
        GeneratePresentationName,
        GenerateFormName
    };

    public bool Owns(string toolName)
    {
        return _toolNames.Contains(toolName);
    }

    public async Task<ToolBundle> BuildAsync(ResolvedToolContext context)
    {
        var tenantQuota = await tenantManager.GetCurrentTenantQuotaAsync();
        if (!tenantQuota.AutomationApi)
        {
            return ToolBundle.Empty;
        }

        var target = await ResolveCreateTargetAsync(context.Folder);
        if (target is null)
        {
            return ToolBundle.Empty;
        }

        return new ToolBundle(string.Empty,
        [
            new AiTool(GenerateDocxName, MakeGenerateDocxFunction(target)),
            new AiTool(GeneratePresentationName, MakeGeneratePresentationFunction(target)),
            new AiTool(GenerateFormName, MakeGenerateFormFunction(target))
        ]);
    }

    private async Task<FileCreateTarget?> ResolveCreateTargetAsync(IFolder? folder)
    {
        if (folder is not null)
        {
            return folder switch
            {
                Folder<int> { IsAgent: true } agent => await TryCreateAgentTargetAsync(agent),
                Folder<int> intFolder => await TryCreateFolderTargetAsync(intFolder),
                Folder<string> stringFolder => await TryCreateFolderTargetAsync(stringFolder),
                _ => null
            };
        }

        var myFolder = await daoFactory.GetFolderDao<int>().GetFolderAsync(await globalFolderHelper.FolderMyAsync);
        return myFolder is null ? null : CreateTarget(myFolder);
    }

    private async Task<FileCreateTarget?> TryCreateAgentTargetAsync(Folder<int> agent)
    {
        if (!await fileSecurity.CanUseChatAsync(agent))
        {
            return null;
        }

        var resultStorage = await daoFactory.GetFolderDao<int>()
            .GetFoldersAsync(agent.Id, FolderType.ResultStorage)
            .FirstOrDefaultAsync();

        return resultStorage is null ? null : CreateTarget(resultStorage);
    }

    private async Task<FileCreateTarget?> TryCreateFolderTargetAsync<T>(Folder<T> folder)
        where T : notnull
    {
        return await fileSecurity.CanCreateAsync(folder) ? CreateTarget(folder) : null;
    }

    private FileCreateTarget CreateTarget<T>(Folder<T> folder)
        where T : notnull
    {
        return new FileCreateTarget(
            (fileName, cancellationToken) =>
                CreateFileAsync(folder.Id, folder.Title, fileName, cancellationToken));
    }

    private AIFunction MakeGenerateDocxFunction(FileCreateTarget target)
    {
        return AIFunctionFactory.Create(Function, new AIFunctionFactoryOptions
        {
            Name = GenerateDocxName,
            Description = GenerateDocxDescription
        });

        Task<ToolResponse<GeneratedFileResult>> Function(
            [Description("File name without extension. Use normal spaces, no underscores or special characters")] string fileName,
            [Description("Short description of the document to generate")] string description,
            CancellationToken cancellationToken)
        {
            return target.CreateFile(fileName, cancellationToken);
        }
    }

    private AIFunction MakeGeneratePresentationFunction(FileCreateTarget target)
    {
        return AIFunctionFactory.Create(Function, new AIFunctionFactoryOptions
        {
            Name = GeneratePresentationName,
            Description = GeneratePresentationDescription
        });

        Task<ToolResponse<GeneratedFileResult>> Function(
            [Description("File name without extension. Use normal spaces, no underscores or special characters")] string fileName,
            [Description("Presentation topic")] string? topic,
            [Description("Number of slides")] string? slideCount,
            [Description("Visual style - modern, classic, minimal, corporate")] string? style,
            CancellationToken cancellationToken)
        {
            return target.CreateFile(fileName, cancellationToken);
        }
    }

    private AIFunction MakeGenerateFormFunction(FileCreateTarget target)
    {
        return AIFunctionFactory.Create(Function, new AIFunctionFactoryOptions
        {
            Name = GenerateFormName,
            Description = GenerateFormDescription
        });

        Task<ToolResponse<GeneratedFileResult>> Function(
            [Description("File name without extension. Use normal spaces, no underscores or special characters")] string fileName,
            [Description("Detailed description of the form or template to generate, including purpose, structure")] string description,
            CancellationToken cancellationToken)
        {
            return target.CreateFile(fileName, cancellationToken);
        }
    }

    private async Task<ToolResponse<GeneratedFileResult>> CreateFileAsync<T>(
        T folderId,
        string parentTitle,
        string fileName,
        CancellationToken cancellationToken)
        where T : notnull
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var file = await fileStorageService.CreateNewFileAsync(new FileModel<T, int>
            {
                ParentId = folderId,
                Title = fileName
            }, ignoreTemplates: true);

            return new ToolResponse<GeneratedFileResult>
            {
                Data = new GeneratedFileResult
                {
                    Id = file.Id,
                    Title = file.Title,
                    ParentId = folderId,
                    ParentTitle = parentTitle
                }
            };
        }
        catch (Exception e)
        {
            return new ToolResponse<GeneratedFileResult> { Error = e.Message };
        }
    }

    private sealed record FileCreateTarget(
        Func<string, CancellationToken, Task<ToolResponse<GeneratedFileResult>>> CreateFile);

    private sealed class GeneratedFileResult
    {
        public required object Id { get; init; }

        public required string Title { get; init; }

        public required object ParentId { get; init; }

        public required string ParentTitle { get; init; }
    }
}
