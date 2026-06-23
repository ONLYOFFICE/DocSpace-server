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

namespace ASC.Web.Files.Utils;

[Scope]
public class ChunkedUploadSessionHolder(
    IServiceProvider serviceProvider,
    GlobalStore globalStore,
    SetupInfo setupInfo,
    IFusionCache cache)
{

    private CommonChunkedUploadSessionHolder _holder;
    private CommonChunkedUploadSessionHolder _currentHolder;
    public static readonly TimeSpan SlidingExpiration = TimeSpan.FromHours(12);

    public async Task StoreSessionAsync<T>(ChunkedUploadSession<T> s)
    {
        await cache.SetAsync(s.Id, s, SlidingExpiration);
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
        var session = await cache.GetOrDefaultAsync<ChunkedUploadSession<T>>(sessionId);
        session.File.ServiceProvider = serviceProvider;
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
        await MoveAsync(chunkedUploadSession, newPath, Guid.Empty);
    }
    public async Task MoveAsync<T>(ChunkedUploadSession<T> chunkedUploadSession, string newPath, Guid ownerId)
    {
        await (await CommonSessionHolderAsync()).MoveAsync(chunkedUploadSession, newPath, ownerId, chunkedUploadSession.CheckQuota);
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