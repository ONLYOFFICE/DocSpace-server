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


public class ZipWriteOperator : IDataWriteOperator
{
    private readonly TarOutputStream _tarOutputStream;
    private readonly TempStream _tempStream;

    public bool NeedUpload => true;

    public CancellationToken CancellationToken { get; set; }

    public string Hash => throw new NotImplementedException();

    public string StoragePath => throw new NotImplementedException();

    public ZipWriteOperator(TempStream tempStream, string targetFile)
    {
        _tempStream = tempStream;
        var file = new FileStream(targetFile, FileMode.Create);
        var gZipOutputStream = new GZipOutputStream(file);
        _tarOutputStream = new TarOutputStream(gZipOutputStream, Encoding.UTF8);
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

        var (buffered, isNew) = await _tempStream.TryGetBufferedAsync(stream);
        try
        {
            var entry = TarEntry.CreateTarEntry(tarKey);
            entry.Size = buffered.Length;
            await _tarOutputStream.PutNextEntryAsync(entry, CancellationToken.None);
            buffered.Position = 0;
            await buffered.CopyToAsync(_tarOutputStream, CancellationToken);
            await _tarOutputStream.CloseEntryAsync(CancellationToken).ContinueWith(async _ => await action(), CancellationToken);
        }
        finally
        {
            if (isNew)
            {
                await buffered.DisposeAsync();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _tarOutputStream.Close();
        await _tarOutputStream.DisposeAsync();
    }
}