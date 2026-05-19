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

public class S3ZipWriteOperator : IDataWriteOperator
{
    private readonly TarOutputStream _tarOutputStream;
    private readonly GZipOutputStream _gZipOutputStream;
    private readonly CommonChunkedUploadSession _chunkedUploadSession;
    private readonly CommonChunkedUploadSessionHolder _sessionHolder;
    private readonly SHA256 _sha;
    private Stream _fileStream;
    private const int TasksLimit = 10;
    private readonly List<Task> _tasks = new(TasksLimit);
    private readonly List<Stream> _streams = new(TasksLimit);
    private readonly TempStream _tempStream;
    private int _chunkNumber = 1;
    private readonly Lock _locker = new();

    public string Hash { get; private set; }
    public CancellationToken CancellationToken { get; set; }
    public string StoragePath { get; private set; }
    public bool NeedUpload => false;

    public S3ZipWriteOperator(TempStream tempStream,
        CommonChunkedUploadSession chunkedUploadSession,
        CommonChunkedUploadSessionHolder sessionHolder)
    {
        _tempStream = tempStream;
        _chunkedUploadSession = chunkedUploadSession;
        _sessionHolder = sessionHolder;
        _sessionHolder.MaxChunkUploadSize *= 5;

        _fileStream = _tempStream.Create();
        _gZipOutputStream = new GZipOutputStream(_fileStream)
        {
            IsStreamOwner = false
        };
        _tarOutputStream = new TarOutputStream(_gZipOutputStream, Encoding.UTF8);
        _sha = SHA256.Create();
    }

    public async Task WriteEntryAsync(string tarKey, string domain, string path, IDataStore store, Func<Task> action)
    {
        CancellationToken.ThrowIfCancellationRequested();

        await using var fileStream = await ActionInvoker.TryAsync(async () => await store.GetReadStreamAsync(domain, path), 5, error => throw error);

        if (fileStream != null)
        {
            await WriteEntryAsync(tarKey, fileStream, action);
        }
    }

    public async Task WriteEntryAsync(string tarKey, Stream stream, Func<Task> action)
    {
        CancellationToken.ThrowIfCancellationRequested();

        if (_fileStream == null)
        {
            _fileStream = _tempStream.Create();
            _gZipOutputStream.baseOutputStream_ = _fileStream;
        }

        var (buffered, isNew) = await _tempStream.TryGetBufferedAsync(stream);
        try
        {
            var entry = TarEntry.CreateTarEntry(tarKey);
            entry.Size = buffered.Length;
            await _tarOutputStream.PutNextEntryAsync(entry, CancellationToken);
            buffered.Position = 0;
            await buffered.CopyToAsync(_tarOutputStream, CancellationToken);
            await _tarOutputStream.FlushAsync(CancellationToken);
            await _tarOutputStream.CloseEntryAsync(CancellationToken).ContinueWith(async _ => await action(), CancellationToken);
        }
        finally
        {
            if (isNew)
            {
                await buffered.DisposeAsync();
            }
        }

        if (_fileStream.Length > _sessionHolder.MaxChunkUploadSize)
        {
            var fs = _fileStream;
            _fileStream = null;

            try
            {
                await SplitAndUploadAsync(fs);
            }
            finally
            {
                await fs.DisposeAsync();
            }
        }
    }

    private async Task SplitAndUploadAsync(Stream stream, bool last = false)
    {
        stream.Position = 0;
        var buffer = new byte[_sessionHolder.MaxChunkUploadSize];

        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, (int)_sessionHolder.MaxChunkUploadSize), CancellationToken)) > 0)
        {
            var tempStream = _tempStream.Create();

            await tempStream.WriteAsync(buffer.AsMemory(0, bytesRead), CancellationToken);

            if (CancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (tempStream.Length == _sessionHolder.MaxChunkUploadSize)
            {
                tempStream.Position = 0;
                await ComputeHashAsync(tempStream, false);
                Upload(tempStream);
            }
            else
            {
                if (last)
                {
                    await ComputeHashAsync(tempStream, true);
                    Upload(tempStream);
                }
                else
                {
                    tempStream.Position = tempStream.Length;
                    _fileStream = tempStream;
                    _gZipOutputStream.baseOutputStream_ = _fileStream;
                }
            }
        }

        CancellationToken.ThrowIfCancellationRequested();
    }

    private async Task ComputeHashAsync(Stream stream, bool last)
    {
        stream.Position = 0;
        var buffer = new byte[_sessionHolder.MaxChunkUploadSize];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, (int)_sessionHolder.MaxChunkUploadSize), CancellationToken)) > 0)
        {
            _sha.TransformBlock(buffer, 0, bytesRead, buffer, 0);
        }
        if (last)
        {
            _sha.TransformFinalBlock(buffer, 0, 0);
        }
    }

    private void Upload(Stream stream)
    {
        stream.Position = 0;
        if (_tasks.Count == TasksLimit)
        {
            Task.WaitAny(_tasks.ToArray());
            for (var i = 0; i < _tasks.Count; i++)
            {
                if (_tasks[i].IsCompleted)
                {
                    _tasks.RemoveAt(i);
                    _streams[i].Dispose();
                    _streams.RemoveAt(i);
                }
            }
        }
        _streams.Add(stream);
        _tasks.Add(InternalUploadAsync(_chunkedUploadSession, stream, stream.Length, _chunkNumber++));
    }

    private async Task InternalUploadAsync(CommonChunkedUploadSession uploadSession, Stream stream, long length, int number)
    {
        await _sessionHolder.UploadChunkAsync(uploadSession, stream, length, number);

        lock (_locker)
        {
            uploadSession.BytesTotal += length;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _tarOutputStream.Close();
        await _tarOutputStream.DisposeAsync();

        try
        {
            await SplitAndUploadAsync(_fileStream, true);
        }
        finally
        {
            await _fileStream.DisposeAsync();
        }

        Task.WaitAll(_tasks.ToArray(), CancellationToken);

        _chunkedUploadSession.BytesTotal++;
        StoragePath = await _sessionHolder.FinalizeAsync(_chunkedUploadSession);

        Hash = BitConverter.ToString(_sha.Hash).Replace("-", string.Empty);
        _sha.Dispose();

        _streams.ForEach(s => s.Dispose());
    }
}