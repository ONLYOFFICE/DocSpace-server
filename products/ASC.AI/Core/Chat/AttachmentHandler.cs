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
        var modelSettings = aiConfiguration.GetModel(context.ClientOptions.Provider, context.ClientOptions.ModelId);
        ArgumentNullException.ThrowIfNull(modelSettings);

        var count = 0;

        await foreach (var result in HandleAsync(modelSettings.Multimodal, filesIds, context.ChatId, context.Agent.Id))
        {
            if (count >= MaxAttachmentsCount)
            {
                yield break;
            }

            count++;
            yield return result;
        }

        await foreach (var result in HandleAsync(modelSettings.Multimodal, thirdPartyFilesIds, context.ChatId, context.Agent.Id))
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
        MultimodalSettings? multimodal,
        IEnumerable<T> filesIds,
        Guid chatId,
        int agentId)
    {
        var fileDao = daoFactory.GetFileDao<T>();

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
                if (multimodal?.Image == null)
                {
                    continue;
                }

                if (!multimodal.Image.Formats.Contains(extension))
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
            yield return await HandleTextAsync(fileDao, file, extension);
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

    private async Task<AttachmentResult> HandleTextAsync<T>(IFileDao<T> fileDao, File<T> file, string extension)
    {
        await using var stream = await fileDao.GetFileStreamAsync(file);

        var content = await textExtractor.ExtractAsync(stream, file.ContentLength);
        if (string.IsNullOrEmpty(content))
        {
            return new AttachmentResult
            {
                File = file,
                Success = false
            };
        }

        IReadOnlyList<ToolWrapper>? formTools = null;
        if (file.IsForm)
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
            var tableName = FormFillingReportCreator.GetTableName(intFile.Id, intFile.Version);
            if (!await externalDatabaseClient.TableExistsAsync(tableName))
            {
                return (null, null);
            }

            var rowCount = await externalDatabaseClient.CountAsync(tableName);
            var columns = await formFillingReportCreator.GetColumnDefinitionsAsync(intFile.Id, intFile.Version);
            var columnList = columns.ToList();

            var queryTool = await formDataQueryTool.InitAsync(intFile.Id, tableName, rowCount, columnList);
            var aggregateTool = await aggregateFormDataTool.InitAsync(intFile.Id, tableName, columnList);
            var selfJoinTool = await selfJoinFormDataTool.InitAsync(intFile.Id, tableName, columnList);

            if (queryTool == null && aggregateTool == null && selfJoinTool == null)
            {
                return (null, null);
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

            var contextText = FormatFormDataFromExternalDb(tableName, rowCount, columnList);

            return (contextText, tools);
        }
        catch (Exception e)
        {
            logger.WarnFormDataToolsFailed(e, intFile.Id);
            return (null, null);
        }
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
