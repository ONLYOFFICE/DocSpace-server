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

namespace ASC.Core.ChunkedUploader;

public class CommonChunkedUploadSessionHolder(
    IDataStore dataStore,
    string domain,
    IFusionCache cache,
    long maxChunkUploadSize = 10 * 1024 * 1024)
{
    public IDataStore DataStore { get; set; } = dataStore;

    public static readonly TimeSpan SlidingExpiration = TimeSpan.FromHours(12);
    public long MaxChunkUploadSize = maxChunkUploadSize;
    public string TempDomain;

    public const string StoragePath = "sessions";

    public async ValueTask InitAsync(CommonChunkedUploadSession chunkedUploadSession)
    {
        //TODO:fix
        if (chunkedUploadSession.BytesTotal < MaxChunkUploadSize && chunkedUploadSession.BytesTotal != -1)
        {
            chunkedUploadSession.UseChunks = false;
            return;
        }

        var path = Guid.NewGuid().ToString();
        var uploadId = await DataStore.InitiateChunkedUploadAsync(domain, path);

        chunkedUploadSession.TempPath = path;
        chunkedUploadSession.UploadId = uploadId;
    }

    public async Task<Dictionary<int, Chunk>> GetChunksAsync(CommonChunkedUploadSession uploadSession)
    {
        var count = uploadSession.BytesTotal / MaxChunkUploadSize;
        count += uploadSession.BytesTotal % MaxChunkUploadSize > 0 ? 1L : 0L;

        var dict = new Dictionary<int, Chunk>();
        for (var i = 1; i <= count; i++)
        {
            var chunk = await cache.GetOrDefaultAsync<Chunk>($"{uploadSession.Id} - {i}");
            if (chunk == null)
            {
                break;
            }
            dict.Add(i, chunk);
        }

        return dict;
    }

    public virtual async Task<string> FinalizeAsync(CommonChunkedUploadSession uploadSession)
    {
        var chunks = await GetChunksAsync(uploadSession);
        var uploadSize = chunks.Sum(c => c.Value?.Length ?? 0);
        if (uploadSize != uploadSession.BytesTotal)
        {
            throw new ArgumentException("uploadSize != bytesTotal");
        }

        var path = uploadSession.TempPath;
        var uploadId = uploadSession.UploadId;
        var eTags = chunks.ToDictionary(c => c.Key, c => c.Value.ETag);

        await DataStore.FinalizeChunkedUploadAsync(domain, path, uploadId, eTags);
        return Path.GetFileName(path);
    }

    public async Task MoveAsync(CommonChunkedUploadSession chunkedUploadSession, string newPath,
        bool quotaCheckFileSize = true)
    {
        await MoveAsync(chunkedUploadSession, newPath, Guid.Empty, quotaCheckFileSize);
    }
    public async Task MoveAsync(CommonChunkedUploadSession chunkedUploadSession, string newPath, Guid ownerId, bool quotaCheckFileSize = true)
    {
        await DataStore.MoveAsync(domain, chunkedUploadSession.TempPath, string.Empty, newPath, ownerId, quotaCheckFileSize);
    }

    public async Task AbortAsync(CommonChunkedUploadSession uploadSession)
    {
        if (uploadSession.UseChunks)
        {
            var tempPath = uploadSession.TempPath;
            var uploadId = uploadSession.UploadId;

            await DataStore.AbortChunkedUploadAsync(domain, tempPath, uploadId);
        }
        else if (!string.IsNullOrEmpty(uploadSession.ChunksBuffer))
        {
            File.Delete(uploadSession.ChunksBuffer);
        }
    }

    public virtual async Task<(string, string)> UploadChunkAsync(CommonChunkedUploadSession uploadSession, Stream stream, long length, int chunkNumber)
    {
        var path = uploadSession.TempPath;
        var uploadId = uploadSession.UploadId;

        var eTag = await DataStore.UploadChunkAsync(domain, path, uploadId, stream, MaxChunkUploadSize, chunkNumber, length);
        await StoreChunkAsync(uploadSession, chunkNumber, eTag, length);
        return (Path.GetFileName(path), eTag);
    }

    public async Task StoreChunkAsync(CommonChunkedUploadSession uploadSession, int chunkNumber, string eTag, long length)
    {
        var chunk = new Chunk
        {
            ETag = eTag,
            Length = length
        };

        await cache.SetAsync($"{uploadSession.Id} - {chunkNumber}", chunk, TimeSpan.FromHours(12));
    }
}