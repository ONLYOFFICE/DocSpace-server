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

using System.Buffers;

using ASC.Web.Core.Files;

namespace ASC.AI.Core.Chat;

public class AttachmentResult
{
    public required FileEntry File { get; init; }
    public AttachmentMessageContent? Content { get; init; }
    public bool Success { get; init; }
}

[Scope]
public class AttachmentHandler(
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    ITextExtractor textExtractor,
    VectorizationGlobalSettings vectorizationGlobalSettings,
    ProviderSettings providerSettings,
    ILogger<AttachmentHandler> logger)
{
    public async IAsyncEnumerable<AttachmentResult> HandleAsync(
        ChatExecutionContext context, 
        IEnumerable<int> filesIds, 
        IEnumerable<string> thirdPartyFilesIds)
    {
        var modelSettings = providerSettings.GetModel(context.ClientOptions.Provider, context.ClientOptions.ModelId);
        ArgumentNullException.ThrowIfNull(modelSettings);
        
        await foreach (var files in HandleAsync(modelSettings.Multimodal, filesIds, context.Agent.Id))
        {
            yield return files;
        }

        await foreach (var files in HandleAsync(modelSettings.Multimodal, thirdPartyFilesIds, context.Agent.Id))
        {
            yield return files;
        }
    }
    
    public async Task CleanupAsync(IEnumerable<AttachmentMessageContent>? attachments)
    {
        if (attachments == null)
        {
            return;
        }

        var internalDao = daoFactory.GetFileDao<int>();

        foreach (var attachment in attachments)
        {
            if (attachment is not DataMessageContent dataContent)
            {
                continue;
            }

            try
            {
                dataContent.Dispose();
                await internalDao.DeleteFileAsync(dataContent.Id);
            }
            catch (Exception e)
            {
                logger.ErrorWithException(e);
            }
        }
    }

    private async IAsyncEnumerable<AttachmentResult> HandleAsync<T>(
        MultimodalSettings? multimodal,
        IEnumerable<T> filesIds,
        int agentId)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var internalDao = daoFactory.GetFileDao<int>();

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

        var copiedFiles = await CopyMediaFilesAsync(fileDao, internalDao, mediaFiles, agentId);

        foreach (var (copiedFile, fileType, extension) in copiedFiles)
        {
            yield return await HandleMediaAsync(internalDao, copiedFile, fileType, extension);
        }

        foreach (var (file, extension) in textFiles)
        {
            yield return await HandleTextAsync(fileDao, file, extension);
        }
    }

    private async Task<List<(File<int> CopiedFile, FileType FileType, string Extension)>> CopyMediaFilesAsync<T>(
        IFileDao<T> fileDao,
        IFileDao<int> internalDao,
        List<(File<T> File, FileType FileType, string Extension)> mediaFiles,
        int agentId)
    {
        var copiedFiles = new List<(File<int> CopiedFile, FileType FileType, string Extension)>();

        try
        {
            foreach (var (file, fileType, extension) in mediaFiles)
            {
                var copiedFile = await fileDao.CopyFileAsync(file.Id, agentId);
                copiedFiles.Add((copiedFile, fileType, extension));
            }

            return copiedFiles;
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            
            foreach (var (copiedFile, _, _) in copiedFiles)
            {
                await internalDao.DeleteFileAsync(copiedFile.Id);
            }

            throw;
        }
    }

    private async Task<AttachmentResult> HandleTextAsync<T>(IFileDao<T> fileDao, File<T> file, string extension)
    {
        await using var stream = await fileDao.GetFileStreamAsync(file);

        var length = (int)file.ContentLength;
        var buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            var memory = buffer.AsMemory(0, length);
            await stream.ReadExactlyAsync(memory);

            var content = await textExtractor.ExtractAsync(memory);
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
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
    
    private static async Task<AttachmentResult> HandleMediaAsync(
        IFileDao<int> internalDao,
        File<int> file,
        FileType fileType,
        string extension)
    {
        await using var stream = await internalDao.GetFileStreamAsync(file);

        var length = (int)file.ContentLength;
        var memoryOwner = MemoryPool<byte>.Shared.Rent(length);
        await stream.ReadExactlyAsync(memoryOwner.Memory[..length]);

        return new AttachmentResult
        {
            File = file,
            Success = true,
            Content = new DataMessageContent
            {
                Id = file.Id,
                FileType = fileType,
                Data = (memoryOwner, length),
                MediaType = GetMediaType(extension)
            }
        };
    }

    private static string GetMediaType(string extension) => extension.ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" or ".jpe" or ".jfif" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => throw new ArgumentOutOfRangeException(nameof(extension), extension, null)
    };
}