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
    VectorizationGlobalSettings vectorizationGlobalSettings)
{
    public async IAsyncEnumerable<AttachmentResult> HandleAsync(IEnumerable<int> filesIds, IEnumerable<string> thirdPartyFilesIds)
    {
        await foreach (var files in HandleAsync(filesIds))
        {
            yield return files;
        }

        await foreach (var files in HandleAsync(thirdPartyFilesIds))
        {
            yield return files;
        }
    }

    private async IAsyncEnumerable<AttachmentResult> HandleAsync<T>(IEnumerable<T> filesIds)
    {
        var fileDao = daoFactory.GetFileDao<T>();

        await foreach (var fileEntry in fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesIds)))
        {
            if (fileEntry is not File<T> file)
            {
                continue;
            }

            var fileType = FileUtility.GetFileTypeByFileName(file.Title);

            if (fileType == FileType.Image)
            {
                await foreach (var result in HandleImageAsync(file, fileDao))
                {
                    yield return result;
                }
            }
            else
            {
                await foreach (var result in HandleTextAsync(file, fileDao))
                {
                    yield return result;
                }
            }
        }
    }

    private async IAsyncEnumerable<AttachmentResult> HandleTextAsync<T>(File<T> file, IFileDao<T> fileDao)
    {
        if (!vectorizationGlobalSettings.IsSupportedContentExtraction(file.Title) ||
            file.ContentLength > vectorizationGlobalSettings.MaxContentLength)
        {
            yield break;
        }

        await using var stream = await fileDao.GetFileStreamAsync(file);

        await using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        var slice = new Memory<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);

        var content = await textExtractor.ExtractAsync(slice);
        if (string.IsNullOrEmpty(content))
        {
            yield return new AttachmentResult
            {
                File = file,
                Success = false
            };

            yield break;
        }

        yield return new AttachmentResult
        {
            File = file,
            Success = true,
            Content = new TextAttachmentMessageContent
            {
                Id = JsonSerializer.SerializeToElement(file.Id),
                Title = file.Title,
                Extension = FileUtility.GetFileExtension(file.Title),
                Content = content
            }
        };
    }

    private async IAsyncEnumerable<AttachmentResult> HandleImageAsync<T>(File<T> file, IFileDao<T> fileDao)
    {
        await using var stream = await fileDao.GetFileStreamAsync(file);

        await using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        var buffer = memoryStream.GetBuffer();
        var byteCount = (int)memoryStream.Length;
        var extension = FileUtility.GetFileExtension(file.Title);
        var mediaType = GetMediaType(extension);

        var base64Length = ((byteCount + 2) / 3) * 4;
        var prefixLength = 5 + mediaType.Length + 8; // "data:" + mediaType + ";base64,"
        var totalLength = prefixLength + base64Length;

        var dataUri = string.Create(totalLength, (buffer, byteCount, mediaType), static (span, state) =>
        {
            var pos = 0;
            "data:".CopyTo(span);
            pos += 5;
            state.mediaType.CopyTo(span[pos..]);
            pos += state.mediaType.Length;
            ";base64,".CopyTo(span[pos..]);
            pos += 8;
            Convert.TryToBase64Chars(new ReadOnlySpan<byte>(state.buffer, 0, state.byteCount), span[pos..], out _);
        });

        yield return new AttachmentResult
        {
            File = file,
            Success = true,
            Content = new DataMessageContent
            {
                Id = JsonSerializer.SerializeToElement(file.Id),
                DataUri = dataUri
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