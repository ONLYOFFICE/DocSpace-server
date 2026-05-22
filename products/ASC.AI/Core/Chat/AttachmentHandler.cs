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

namespace ASC.AI.Core.Chat;

public class AttachmentResult
{
    public required FileEntry File { get; init; }
    public AttachmentMessageContent? Content { get; init; }
    public bool Success { get; init; }
    public IReadOnlyList<ToolWrapper>? DynamicTools { get; init; }
}

[Scope]
public class AttachmentHandler(
    IEventBus eventBus,
    AuthContext authContext,
    TenantManager tenantManager,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    ITextExtractor textExtractor,
    VectorizationGlobalSettings vectorizationGlobalSettings,
    AiConfiguration aiConfiguration,
    DataContentLoader dataContentLoader,
    IConfiguration configuration,
    ILogger<AttachmentHandler> logger,
    FormFillingReportCreator formFillingReportCreator,
    ExternalDatabaseClient externalDatabaseClient,
    FormDataQueryTool formDataQueryTool,
    AggregateFormDataTool aggregateFormDataTool,
    SelfJoinFormDataTool selfJoinFormDataTool)
{
    private static int? _maxAttachmentsCount;
    private int MaxAttachmentsCount => _maxAttachmentsCount ??= configuration.GetValue("ai:maxAttachmentsCount", 5);

    public async IAsyncEnumerable<AttachmentResult> HandleAsync(
        ChatExecutionContext context,
        IEnumerable<int> filesIds,
        IEnumerable<string> thirdPartyFilesIds)
    {
        var count = 0;

        await foreach (var result in HandleAsync(context.ModelSettings, filesIds, context.ChatId, context.Agent.Id))
        {
            if (count >= MaxAttachmentsCount)
            {
                yield break;
            }

            count++;
            yield return result;
        }

        await foreach (var result in HandleAsync(context.ModelSettings, thirdPartyFilesIds, context.ChatId, context.Agent.Id))
        {
            if (count >= MaxAttachmentsCount)
            {
                yield break;
            }

            count++;
            yield return result;
        }
    }

    public async Task CleanupAsync(IEnumerable<AttachmentMessageContent>? attachments)
    {
        if (attachments == null)
        {
            return;
        }

        var fileIds = attachments
            .OfType<DataMessageContent>()
            .Select(attachment => attachment.Id);

        await DeleteInBackgroundAsync(fileIds);
    }

    private async IAsyncEnumerable<AttachmentResult> HandleAsync<T>(
        ModelSettings modelSettings,
        IEnumerable<T> filesIds,
        Guid chatId,
        int agentId)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var hasVision = modelSettings.Capabilities.Vision;

        var textFiles = new List<(File<T> File, string Extension)>();
        var mediaFiles = new List<(File<T> File, FileType FileType, string Extension)>();

        await foreach (var fileEntry in fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesIds)))
        {
            if (fileEntry is not File<T> file)
            {
                continue;
            }

            var extension = FileUtility.GetFileExtension(file.Title);
            var fileType = FileUtility.GetFileTypeByExtention(extension);

            if (fileType == FileType.Image)
            {
                if (!hasVision)
                {
                    continue;
                }

                if (!AiConfiguration.SupportedImageFormats.Contains(extension))
                {
                    continue;
                }

                if (aiConfiguration.MaxImageSize > 0 && file.ContentLength > aiConfiguration.MaxImageSize)
                {
                    continue;
                }

                mediaFiles.Add((file, fileType, extension));
            }
            else
            {
                if (!vectorizationGlobalSettings.IsSupportedContentExtraction(file.Title) ||
                    file.ContentLength > vectorizationGlobalSettings.MaxContentLength)
                {
                    continue;
                }

                textFiles.Add((file, extension));
            }
        }

        var copiedFiles = await CopyMediaFilesAsync(fileDao, mediaFiles, chatId, agentId);

        foreach (var (copiedFile, fileType, extension) in copiedFiles)
        {
            yield return await HandleMediaAsync(copiedFile, fileType, extension);
        }

        foreach (var (file, extension) in textFiles)
        {
            yield return await HandleTextAsync(modelSettings, fileDao, file, extension);
        }
    }

    private async Task<List<(File<int> CopiedFile, FileType FileType, string Extension)>> CopyMediaFilesAsync<T>(
        IFileDao<T> fileDao,
        List<(File<T> File, FileType FileType, string Extension)> mediaFiles,
        Guid chatId,
        int agentId)
    {
        var copiedFiles = new List<(File<int> CopiedFile, FileType FileType, string Extension)>();

        try
        {
            foreach (var (file, fileType, extension) in mediaFiles)
            {
                var copiedFile = await fileDao.CopyFileAsync(file.Id, agentId, chatId);
                copiedFiles.Add((copiedFile, fileType, extension));
            }

            return copiedFiles;
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);

            await DeleteInBackgroundAsync(copiedFiles.Select(item => item.CopiedFile.Id));

            throw;
        }
    }

    private async Task DeleteInBackgroundAsync(IEnumerable<int> fileIds)
    {
        var ids = fileIds
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return;
        }

        try
        {
            await eventBus.PublishAsync(new ChatDeletionIntegrationEvent(
                authContext.IsAuthenticated ? authContext.CurrentAccount.ID : Guid.Empty,
                tenantManager.GetCurrentTenantId())
            {
                FileIds = ids
            });
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
        }
    }

    private async Task<AttachmentResult> HandleTextAsync<T>(
        ModelSettings modelSettings,
        IFileDao<T> fileDao,
        File<T> file,
        string extension)
    {
        await using var stream = await fileDao.GetFileStreamAsync(file);

        var length = (int)file.ContentLength;
        var buffer = new byte[length];
        await stream.ReadExactlyAsync(buffer);

        var content = await textExtractor.ExtractAsync(buffer);
        if (string.IsNullOrEmpty(content))
        {
            return new AttachmentResult
            {
                File = file,
                Success = false
            };
        }

        IReadOnlyList<ToolWrapper>? formTools = null;
        if (file.IsForm && modelSettings.Capabilities.ToolCalling)
        {
            var (formData, tools) = await TryGetFormDataAsync(file);
            formTools = tools;
            if (formData != null)
            {
                content += formData;
            }
        }

        return new AttachmentResult
        {
            File = file,
            Success = true,
            DynamicTools = formTools,
            Content = new TextAttachmentMessageContent
            {
                Id = JsonSerializer.SerializeToElement(file.Id),
                Title = file.Title,
                Extension = extension,
                Content = content
            }
        };
    }

    private async Task<AttachmentResult> HandleMediaAsync(
        File<int> file,
        FileType fileType,
        string extension)
    {
        return new AttachmentResult
        {
            File = file,
            Success = true,
            Content = await dataContentLoader.CreateAsync(file, fileType, extension)
        };
    }

    public async Task<(IReadOnlyList<ToolWrapper> Tools, string SchemaContext)?> GetFormDataToolsAsync(int fileId)
    {
        if (!externalDatabaseClient.IsEnabled())
        {
            return null;
        }

        var fileDao = daoFactory.GetFileDao<int>();
        var file = await fileDao.GetFileAsync(fileId);
        if (file == null)
        {
            return null;
        }

        var properties = await fileDao.GetProperties(fileId);
        var formFilling = properties?.FormFilling;

        if (formFilling?.StartFilling != true || formFilling.OriginalFormId != fileId)
        {
            return null;
        }

        try
        {
            return await BuildFormDataToolsAsync(fileId, file.Version);
        }
        catch (Exception e)
        {
            logger.WarnFormDataToolsFailed(e, fileId);
            return null;
        }
    }

    private async Task<(string? contextText, IReadOnlyList<ToolWrapper>? tools)> TryGetFormDataAsync<T>(File<T> file)
    {
        if (file is not File<int> intFile)
        {
            return (null, null);
        }

        var fileDao = daoFactory.GetFileDao<int>();
        var properties = await fileDao.GetProperties(intFile.Id);
        var formFilling = properties?.FormFilling;

        if (formFilling?.StartFilling != true || formFilling.OriginalFormId != intFile.Id)
        {
            return (null, null);
        }

        if (!externalDatabaseClient.IsEnabled())
        {
            return (null, null);
        }

        try
        {
            var result = await BuildFormDataToolsAsync(intFile.Id, intFile.Version);
            if (result == null)
            {
                return (null, null);
            }

            return (result.Value.SchemaContext, result.Value.Tools);
        }
        catch (Exception e)
        {
            logger.WarnFormDataToolsFailed(e, intFile.Id);
            return (null, null);
        }
    }

    private async Task<(IReadOnlyList<ToolWrapper> Tools, string SchemaContext)?> BuildFormDataToolsAsync(int fileId, int version)
    {
        var tableName = FormFillingReportCreator.GetTableName(fileId, version);
        if (!await externalDatabaseClient.TableExistsAsync(tableName))
        {
            return null;
        }

        var rowCount = await externalDatabaseClient.CountAsync(tableName);
        var columns = await formFillingReportCreator.GetColumnDefinitionsAsync(fileId, version);
        var columnList = columns.ToList();

        var queryTool = await formDataQueryTool.InitAsync(fileId, tableName, rowCount, columnList);
        var aggregateTool = await aggregateFormDataTool.InitAsync(fileId, tableName, columnList);
        var selfJoinTool = await selfJoinFormDataTool.InitAsync(fileId, tableName, columnList);

        if (queryTool == null && aggregateTool == null && selfJoinTool == null)
        {
            return null;
        }

        var tools = new List<ToolWrapper>(3);
        if (queryTool != null)
        {
            tools.Add(new ToolWrapper { Tool = queryTool, Context = new ToolContext { Name = queryTool.Name, AutoInvoke = true } });
        }
        if (aggregateTool != null)
        {
            tools.Add(new ToolWrapper { Tool = aggregateTool, Context = new ToolContext { Name = aggregateTool.Name, AutoInvoke = true } });
        }
        if (selfJoinTool != null)
        {
            tools.Add(new ToolWrapper { Tool = selfJoinTool, Context = new ToolContext { Name = selfJoinTool.Name, AutoInvoke = true } });
        }

        return (tools, FormatFormDataFromExternalDb(tableName, rowCount, columnList));
    }

    private static string FormatFormDataFromExternalDb(
        string tableName,
        long rowCount,
        IEnumerable<DbColumnDefinition> columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"\n\n## Form Submissions ({rowCount} total, stored in external database)");
        sb.AppendLine($"Table: {tableName}");
        sb.AppendLine("Schema:");
        foreach (var col in columns)
        {
            sb.Append($"- {col.Name} ({col.Type})");
            if (col.EnumValues?.Count > 0)
            {
                sb.Append($": {string.Join(", ", col.EnumValues)}");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"\nUse '{AggregateFormDataTool.Name}' for counts, distributions, and statistics.");
        sb.AppendLine($"Use '{FormDataQueryTool.Name}' to retrieve specific rows with filters.");
        sb.AppendLine($"Use '{SelfJoinFormDataTool.Name}' to compare pairs of records (overlapping dates, concurrent events).");
        return sb.ToString();
    }
}

internal static partial class AttachmentHandlerLogger
{
    [LoggerMessage(LogLevel.Warning, "Failed to initialize form data tools for file {FileId}")]
    public static partial void WarnFormDataToolsFailed(this ILogger logger, Exception exception, int fileId);
}
