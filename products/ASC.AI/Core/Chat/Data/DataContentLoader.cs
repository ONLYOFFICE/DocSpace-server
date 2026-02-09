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

namespace ASC.AI.Core.Chat.Data;

[Scope]
public class DataContentLoader(IFileDao<int> fileDao)
{
    public async Task<bool> TryLoadAsync(DataMessageContent content)
    {
        var file = await fileDao.GetFileAsync(content.Id);
        if (file == null)
        {
            return false;
        }

        var extension = FileUtility.GetFileExtension(file.Title);

        var (memoryOwner, length) = await ReadFileAsync(file);

        content.Data = (memoryOwner, length);
        content.MediaType = GetMediaType(extension);

        return true;
    }

    public async Task<DataMessageContent> CreateAsync(File<int> file, FileType fileType, string extension)
    {
        var (memoryOwner, length) = await ReadFileAsync(file);

        return new DataMessageContent
        {
            Id = file.Id,
            FileType = fileType,
            Data = (memoryOwner, length),
            MediaType = GetMediaType(extension)
        };
    }

    private async Task<(IMemoryOwner<byte> Owner, int Length)> ReadFileAsync(File<int> file)
    {
        await using var stream = await fileDao.GetFileStreamAsync(file);

        var length = (int)file.ContentLength;
        var memoryOwner = MemoryPool<byte>.Shared.Rent(length);
        await stream.ReadExactlyAsync(memoryOwner.Memory[..length]);

        return (memoryOwner, length);
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
