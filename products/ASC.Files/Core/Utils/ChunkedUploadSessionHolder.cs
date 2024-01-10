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
public class ChunkedUploadSessionHolder(GlobalStore globalStore,
    SetupInfo setupInfo,
    TempPath tempPath,
    FileHelper fileHelper,
    ICache cache)
{
    
    private CommonChunkedUploadSessionHolder _holder;
    private CommonChunkedUploadSessionHolder _currentHolder;
    public static readonly TimeSpan SlidingExpiration = TimeSpan.FromHours(12);

    public void StoreSession<T>(ChunkedUploadSession<T> s)
    {
        cache.Insert(s.Id, s, SlidingExpiration);
    }

    public void RemoveSession<T>(ChunkedUploadSession<T> s)
    {
        cache.Remove(s.Id);

        var count = s.BytesTotal / setupInfo.ChunkUploadSize;
        count += s.BytesTotal % setupInfo.ChunkUploadSize > 0 ? 1L : 0L;
        for (var i = 1; i <= count; i++)
        {
            cache.Remove($"{s.Id} - {i}");
        }
    }

    public async Task<Dictionary<int, Chunk>> GetChunksAsync<T>(ChunkedUploadSession<T> s)
    {
        return (await CommonSessionHolderAsync()).GetChunks(s);
    }
    
    public ChunkedUploadSession<T> GetSession<T>(string sessionId)
    {
        return cache.Get<ChunkedUploadSession<T>>(sessionId);
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

    public async Task<Stream> UploadSingleChunkAsync<T>(ChunkedUploadSession<T> uploadSession, Stream stream, long chunkLength)
    {
        return await (await CommonSessionHolderAsync()).UploadSingleChunkAsync(uploadSession, stream, chunkLength);
    }

    private async ValueTask<CommonChunkedUploadSessionHolder> CommonSessionHolderAsync(bool currentTenant = true)
    {
        if (currentTenant)
        {
            if (_currentHolder == null)
            {
                _currentHolder = new CommonChunkedUploadSessionHolder(tempPath,
                    await globalStore.GetStoreAsync(), FileConstant.StorageDomainTmp, cache,
                    setupInfo.ChunkUploadSize);
            }

            return _currentHolder;
        }
        else
        {
            if (_holder == null)
            {
                _holder = new CommonChunkedUploadSessionHolder(tempPath, await globalStore.GetStoreAsync(false),
                    FileConstant.StorageDomainTmp, cache, setupInfo.ChunkUploadSize);
            }

            return _holder;
        }
    }
}
