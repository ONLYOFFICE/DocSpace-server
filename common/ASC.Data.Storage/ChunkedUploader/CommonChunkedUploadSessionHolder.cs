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

namespace ASC.Core.ChunkedUploader;

public class CommonChunkedUploadSessionHolder(
    IDataStore dataStore,
    string domain,
    AscDistributedCache cache,
    long maxChunkUploadSize = 10 * 1024 * 1024)
{
    public IDataStore DataStore { get; set; } = dataStore;

    public static readonly TimeSpan SlidingExpiration = TimeSpan.FromHours(12);
    public long MaxChunkUploadSize = maxChunkUploadSize;
    public string TempDomain;

    public const string StoragePath = "sessions";

    public async ValueTask InitAsync(CommonChunkedUploadSession chunkedUploadSession)
    {
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
            dict.Add(i, await cache.GetAsync<Chunk>($"{uploadSession.Id} - {i}"));
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
        else
        {
            var path = uploadSession.TempPath;
            var uploadId = uploadSession.UploadId;
            var eTags = chunks.ToDictionary(c => c.Key, c => c.Value.ETag);

            await DataStore.FinalizeChunkedUploadAsync(domain, path, uploadId, eTags);
            return Path.GetFileName(path);
        }
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

    public virtual async Task StoreChunkAsync(CommonChunkedUploadSession uploadSession, int chunkNumber, string eTag, long length)
    {
        var chunk = new Chunk
        {
            ETag = eTag,
            Length = length
        };

        await cache.InsertAsync($"{uploadSession.Id} - {chunkNumber}", chunk, TimeSpan.FromHours(12));
    }
}