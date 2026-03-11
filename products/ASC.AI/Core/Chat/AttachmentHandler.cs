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
    ILogger<AttachmentHandler> logger)
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

        return new AttachmentResult
        {
            File = file,
            Success = true,
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
}
