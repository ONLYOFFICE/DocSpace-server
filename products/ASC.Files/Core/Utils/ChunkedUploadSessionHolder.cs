// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Web.Files.Utils;

[Scope]
public class ChunkedUploadSessionHolder(
    GlobalStore globalStore,
    SetupInfo setupInfo,
    AscDistributedCache cache,
    FileHelper fileHelper)
{
    
    private CommonChunkedUploadSessionHolder _holder;
    private CommonChunkedUploadSessionHolder _currentHolder;
    public static readonly TimeSpan SlidingExpiration = TimeSpan.FromHours(12);

    public async Task StoreSessionAsync<T>(ChunkedUploadSession<T> s)
    {
        await cache.InsertAsync(s.Id, s, SlidingExpiration);
    }

    public async Task RemoveSessionAsync<T>(ChunkedUploadSession<T> s)
    {
        await cache.RemoveAsync(s.Id);

        var count = s.BytesTotal / setupInfo.ChunkUploadSize;
        count += s.BytesTotal % setupInfo.ChunkUploadSize > 0 ? 1L : 0L;
        for (var i = 1; i <= count; i++)
        {
            await cache.RemoveAsync($"{s.Id} - {i}");
        }
    }

    public async Task<Dictionary<int, Chunk>> GetChunksAsync<T>(ChunkedUploadSession<T> s)
    {
        return await (await CommonSessionHolderAsync()).GetChunksAsync(s);
    }
    
    public async Task<ChunkedUploadSession<T>> GetSessionAsync<T>(string sessionId)
    {
        var session = await cache.GetAsync<ChunkedUploadSession<T>>(sessionId);
        session.File.FileHelper = fileHelper;
        session.TransformItems();
        return session;
    }

    public async Task<ChunkedUploadSession<T>> CreateUploadSessionAsync<T>(File<T> file, long contentLength)
    {
        var result = new ChunkedUploadSession<T>(file, contentLength);
        await (await CommonSessionHolderAsync()).InitAsync(result);

        return result;
    }

    public async Task UploadChunkAsync<T>(ChunkedUploadSession<T> uploadSession, Stream stream, long length, int chunkNumber)
    {
        await (await CommonSessionHolderAsync()).UploadChunkAsync(uploadSession, stream, length, chunkNumber);
    }

    public async Task FinalizeUploadSessionAsync<T>(ChunkedUploadSession<T> uploadSession)
    {
        await (await CommonSessionHolderAsync()).FinalizeAsync(uploadSession);
    }

    public async Task MoveAsync<T>(ChunkedUploadSession<T> chunkedUploadSession, string newPath)
    {
        await (await CommonSessionHolderAsync()).MoveAsync(chunkedUploadSession, newPath, chunkedUploadSession.CheckQuota);
    }

    public async Task AbortUploadSessionAsync<T>(ChunkedUploadSession<T> uploadSession)
    {
        await (await CommonSessionHolderAsync()).AbortAsync(uploadSession);
    }

    private async ValueTask<CommonChunkedUploadSessionHolder> CommonSessionHolderAsync(bool currentTenant = true)
    {
        if (currentTenant)
        {
            return _currentHolder ??= new CommonChunkedUploadSessionHolder(await globalStore.GetStoreAsync(), FileConstant.StorageDomainTmp, cache, setupInfo.ChunkUploadSize);
        }

        return _holder ??= new CommonChunkedUploadSessionHolder(await globalStore.GetStoreAsync(false), FileConstant.StorageDomainTmp, cache, setupInfo.ChunkUploadSize);
    }
}
