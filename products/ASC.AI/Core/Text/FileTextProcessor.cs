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

using System.Buffers;

namespace ASC.AI.Core.Text;

[Scope]
public class FileTextProcessor(IFileDao<int> fileDao, ITextExtractor textExtractor, ITextSplitter textSplitter)
{
    public async Task<List<string>> GetTextChunksAsync(int fileId, SplitterSettings settings)
    {
        var file = await fileDao.GetFileAsync(fileId);
        if (file == null)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }
        
        return await GetTextChunksAsync(file, settings);
    }

    public async Task<List<string>> GetTextChunksAsync(File<int> file, SplitterSettings settings)
    {
        await using var stream = await fileDao.GetFileStreamAsync(file);

        var buffer = ArrayPool<byte>.Shared.Rent((int)stream.Length);
        
        await using var memoryStream = new MemoryStream(buffer);
        await stream.CopyToAsync(memoryStream);
        
        var memory = new Memory<byte>(buffer, 0, (int)memoryStream.Length);
        
        ArrayPool<byte>.Shared.Return(buffer);
        
        var text = await textExtractor.ExtractAsync(memory);
        
        return string.IsNullOrEmpty(text) 
            ? [] 
            : textSplitter.Split(text, settings.MaxTokensPerChunk, settings.ChunkOverlap);
    }
}