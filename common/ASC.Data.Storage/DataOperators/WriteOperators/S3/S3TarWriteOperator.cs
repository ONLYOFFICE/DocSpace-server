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

namespace ASC.Data.Storage.DataOperators;
public class S3TarWriteOperator : IDataWriteOperator
{
    private readonly CommonChunkedUploadSession _chunkedUploadSession;
    private readonly CommonChunkedUploadSessionHolder _sessionHolder;
    private readonly TempStream _tempStream;
    private readonly S3Storage _store;
    private readonly string _domain;
    private readonly string _key;
    private const int Limit = 10;
    private readonly List<Task> _tasks = [];
    private readonly TaskScheduler _scheduler = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, Limit).ConcurrentScheduler;
    private readonly ConcurrentQueue<int> _queue = new();
    private readonly IFusionCache _cache;

    private CancellationTokenSource _cts = new();

    public CancellationToken CancellationToken
    {
        get
        {
            return _cts.Token;
        }
        set
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(value);
        }
    }

    public string Hash { get; private set; }
    public string StoragePath { get; private set; }
    public bool NeedUpload => false;

    public S3TarWriteOperator(CommonChunkedUploadSession chunkedUploadSession, CommonChunkedUploadSessionHolder sessionHolder, TempStream tempStream, IFusionCache cache)
    {
        _chunkedUploadSession = chunkedUploadSession;
        _sessionHolder = sessionHolder;
        _store = _sessionHolder.DataStore as S3Storage;

        _key = _chunkedUploadSession.TempPath;
        _domain = _sessionHolder.TempDomain;
        _tempStream = tempStream;
        _cache = cache;

        for (var i = 1; i <= Limit; i++)
        {
            _queue.Enqueue(i);
        }
    }


    public async Task WriteEntryAsync(string tarKey, string domain, string path, IDataStore store, Func<Task> action)
    {
        CancellationToken.ThrowIfCancellationRequested();

        if (store is S3Storage s3Store)
        {
            var task = new Task(() =>
            {
                if (CancellationToken.IsCancellationRequested)
                {
                    return;
                }
                try
                {
                    var fullPath = s3Store.MakePath(domain, path);
                    _store.ConcatFileAsync(fullPath, tarKey, _domain, _key, _queue, CancellationToken).Wait(CancellationToken);
                }
                catch
                {
                    _cts.Cancel();
                    throw;
                }
            });
            _ = task.ContinueWith(async _ => await action(), CancellationToken);
            _tasks.Add(task);
            task.Start(_scheduler);
        }
        else
        {
            await using var fileStream = await store.GetReadStreamAsync(domain, path);

            if (fileStream != null)
            {
                await WriteEntryAsync(tarKey, fileStream, action);
            }
        }
    }

    public async Task WriteEntryAsync(string tarKey, Stream stream, Func<Task> action)
    {
        CancellationToken.ThrowIfCancellationRequested();

        var tStream = _tempStream.Create();
        stream.Position = 0;
        await stream.CopyToAsync(tStream, CancellationToken);

        var task = new Task(() =>
        {
            if (CancellationToken.IsCancellationRequested)
            {
                return;
            }
            try
            {
                _store.ConcatFileStreamAsync(tStream, tarKey, _domain, _key, _queue, CancellationToken).Wait(CancellationToken);
            }
            catch
            {
                _cts.Cancel();
                throw;
            }
        });

        _ = task.ContinueWith(async _ => await action(), CancellationToken);
        _tasks.Add(task);
        task.Start(_scheduler);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Task.WhenAll(_tasks.ToArray());
            _tasks.Clear();
        }
        catch
        {
            for (var i = 1; i <= Limit; i++)
            {
                await _store.DeleteAsync(_domain, _key + i);
            }
            var task = _tasks.First(t => t.Exception != null);
            await _cts.CancelAsync();
            throw task.Exception;
        }

        for (var i = 1; i <= Limit; i++)
        {
            var fullKey = _store.MakePath(_domain, _key + i);
            await _store.AddEndAsync(_domain, _key + i, i == 10);
            await _store.ReloadFileAsync(_domain, _key + i, true);
            await _store.ConcatFileAsync(fullKey, _domain, _key);
            await _store.DeleteAsync(_domain, _key + i);
        }
        await _store.ReloadFileAsync(_domain, _key, true, true);
        await _store.ReloadFileAsync(_domain, _key, false, true);

        var contentLength = await _store.GetFileSizeAsync(_domain, _key);
        Hash = (await _store.GetFileEtagAsync(_domain, _key)).Trim('\"');

        var (uploadId, eTags, _) = await _store.InitiateConcatAsync(_domain, _key, lastInit: true, token: CancellationToken);

        _chunkedUploadSession.BytesTotal = contentLength;
        _chunkedUploadSession.UploadId = uploadId;
        var first = true;
        foreach (var etag in eTags)
        {
            var chunk = new Chunk
            {
                ETag = etag.ETag,
                Length = 0
            };
            if (first)
            {
                chunk.Length = contentLength;
                first = false;
            }
            await _cache.SetAsync($"{_chunkedUploadSession.Id} - {etag.PartNumber}", chunk, TimeSpan.FromHours(12), token: CancellationToken);
        }

        StoragePath = await _sessionHolder.FinalizeAsync(_chunkedUploadSession);

        _cts?.Dispose();
    }
}