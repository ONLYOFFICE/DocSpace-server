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
    private readonly List<Task> _tasks = new();
    private readonly TaskScheduler _scheduler = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, Limit).ConcurrentScheduler;
    private readonly ConcurrentQueue<int> _queue = new();
    private readonly CancellationTokenSource _cts = new();

    public string Hash { get; private set; }
    public string StoragePath { get; private set; }
    public bool NeedUpload => false;

    public S3TarWriteOperator(CommonChunkedUploadSession chunkedUploadSession, CommonChunkedUploadSessionHolder sessionHolder, TempStream tempStream)
    {
        _chunkedUploadSession = chunkedUploadSession;
        _sessionHolder = sessionHolder;
        _store = _sessionHolder.DataStore as S3Storage;

        _key = _chunkedUploadSession.TempPath;
        _domain = _sessionHolder.TempDomain;
        _tempStream = tempStream;

        for (var i = 1; i <= Limit; i++)
        {
            _queue.Enqueue(i);
        }
    }


    public async Task WriteEntryAsync(string tarKey, string domain, string path, IDataStore store)
    {
        if (_cts.IsCancellationRequested)
        {
            return;
        }
        if (_tasks.Count >= Limit * 3)
        {
            try
            {
                Task.WaitAll(_tasks.ToArray());
                _tasks.Clear();
            }
            catch
            {
                var task = _tasks.First(t => t.Exception != null);
                _cts.Cancel();
                throw task.Exception;
            }

            foreach (var task in _tasks)
            {
                if(task.Exception != null)
                {
                    throw task.Exception;
                }
            }
            _tasks.Clear();
        }
        if (store is S3Storage s3Store) 
        {
            var fullPath = s3Store.MakePath(domain, path);
            var task = new Task(() =>
            {
                if (!_cts.Token.IsCancellationRequested)
                {
                    _store.ConcatFileAsync(fullPath, tarKey, _domain, _key, _queue).Wait();
                }
            });
            _tasks.Add(task);
            task.Start(_scheduler);
        }
        else
        {
            var fileStream = await ActionInvoker.TryAsync(async () => await store.GetReadStreamAsync(domain, path), 5, error => throw error);
            
            if (fileStream != null)
            {
                await WriteEntryAsync(tarKey, fileStream);
                await fileStream.DisposeAsync();
            }
        }
    }

    public async Task WriteEntryAsync(string tarKey, Stream stream)
    {
        if (_cts.IsCancellationRequested)
        {
            return;
        }
        var tStream = _tempStream.Create();
        stream.Position = 0;
        await stream.CopyToAsync(tStream);
        
        var task = new Task(() =>
        {
            if (!_cts.Token.IsCancellationRequested)
            {
                _store.ConcatFileStreamAsync(tStream, tarKey, _domain, _key, _queue).Wait();
            }
        });
        _tasks.Add(task);
        task.Start(_scheduler);
    }

    public async ValueTask DisposeAsync()
    {
        if (_tasks.Count != 0)
        {
            try
            {
                Task.WaitAll(_tasks.ToArray());
                _tasks.Clear();
            }
            catch
            {
                for (var i = 1; i <= Limit; i++)
                {
                    await _store.DeleteAsync(_domain, _key + i);
                }
                var task = _tasks.First(t => t.Exception != null);
                _cts.Cancel();
                throw task.Exception;
            }
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

        var (uploadId, eTags, partNumber) = await _store.InitiateConcatAsync(_domain, _key, lastInit: true);

        _chunkedUploadSession.BytesUploaded = contentLength;
        _chunkedUploadSession.BytesTotal = contentLength;
        _chunkedUploadSession.UploadId = uploadId;
        _chunkedUploadSession.Items["ETag"] = eTags.ToDictionary(e => e.PartNumber, e => e.ETag);
        _chunkedUploadSession.Items["ChunksUploaded"] = (partNumber - 1).ToString();

        StoragePath = await _sessionHolder.FinalizeAsync(_chunkedUploadSession);
        
    }
}
