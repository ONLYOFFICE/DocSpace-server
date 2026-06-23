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

public class ChunkZipWriteOperator : IDataWriteOperator
{
    private readonly TarOutputStream _tarOutputStream;
    private readonly GZipOutputStream _gZipOutputStream;
    private readonly CommonChunkedUploadSession _chunkedUploadSession;
    private readonly CommonChunkedUploadSessionHolder _sessionHolder;
    private readonly SHA256 _sha;
    private Stream _fileStream;
    private readonly TempStream _tempStream;
    private int _chunkNumber = 1;


    public CancellationToken CancellationToken { get; set; }
    public string Hash { get; private set; }
    public string StoragePath { get; private set; }
    public bool NeedUpload => false;

    public ChunkZipWriteOperator(TempStream tempStream,
        CommonChunkedUploadSession chunkedUploadSession,
        CommonChunkedUploadSessionHolder sessionHolder)
    {
        _tempStream = tempStream;
        _chunkedUploadSession = chunkedUploadSession;
        _sessionHolder = sessionHolder;

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
            await UploadAsync(false);
        }
    }

    private async Task UploadAsync(bool last)
    {
        var chunkUploadSize = _sessionHolder.MaxChunkUploadSize;

        var buffer = new byte[chunkUploadSize];
        int bytesRead;
        _fileStream.Position = 0;
        while ((bytesRead = await _fileStream.ReadAsync(buffer.AsMemory(0, (int)chunkUploadSize), CancellationToken)) > 0)
        {
            using var theMemStream = new MemoryStream();
            await theMemStream.WriteAsync(buffer.AsMemory(0, bytesRead), CancellationToken);

            if (CancellationToken.IsCancellationRequested)
            {
                break;
            }

            theMemStream.Position = 0;
            if (bytesRead == chunkUploadSize || last)
            {
                if (last)
                {
                    _chunkedUploadSession.Items["lastChunk"] = "true";
                }

                theMemStream.Position = 0;
                var length = theMemStream.Length;
                await _sessionHolder.UploadChunkAsync(_chunkedUploadSession, theMemStream, length, _chunkNumber++);
                _chunkedUploadSession.BytesTotal += length;
                _sha.TransformBlock(buffer, 0, bytesRead, buffer, 0);
            }
            else
            {
                await _fileStream.DisposeAsync();
                _fileStream = _tempStream.Create();
                _gZipOutputStream.baseOutputStream_ = _fileStream;

                await theMemStream.CopyToAsync(_fileStream, CancellationToken);
                await _fileStream.FlushAsync(CancellationToken);
            }
        }

        CancellationToken.ThrowIfCancellationRequested();

        if (last)
        {
            _chunkedUploadSession.BytesTotal++;
            StoragePath = await _sessionHolder.FinalizeAsync(_chunkedUploadSession);
            _sha.TransformFinalBlock(buffer, 0, 0);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _tarOutputStream.Close();
        await _tarOutputStream.DisposeAsync();

        await UploadAsync(true);
        await _fileStream.DisposeAsync();

        Hash = BitConverter.ToString(_sha.Hash).Replace("-", string.Empty);
        _sha.Dispose();
    }
}