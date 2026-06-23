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

namespace ASC.Data.Storage;

public class CrossModuleTransferUtility(ILogger option,
    TempStream tempStream,
    IDataStore source,
    IDataStore destination,
    IFusionCache cache)
{
    private readonly IDataStore _source = source ?? throw new ArgumentNullException(nameof(source));
    private readonly IDataStore _destination = destination ?? throw new ArgumentNullException(nameof(destination));
    private readonly long _maxChunkUploadSize = 10 * 1024 * 1024;
    private readonly int _chunkSize = 5 * 1024 * 1024;

    public async ValueTask CopyFileAsync(string srcDomain, string srcPath, string destDomain, string destPath)
    {
        ArgumentNullException.ThrowIfNull(srcDomain);
        ArgumentNullException.ThrowIfNull(srcPath);
        ArgumentNullException.ThrowIfNull(destDomain);
        ArgumentNullException.ThrowIfNull(destPath);

        await using var stream = await _source.GetReadStreamAsync(srcDomain, srcPath);
        if (stream.Length < _maxChunkUploadSize)
        {
            await _destination.SaveAsync(destDomain, destPath, stream);
        }
        else
        {
            var session = new CommonChunkedUploadSession(stream.Length);
            var holder = new CommonChunkedUploadSessionHolder(_destination, destDomain, cache);
            await holder.InitAsync(session);
            try
            {
                Stream memstream = null;
                var i = 1;
                try
                {
                    while (GetStream(stream, out memstream))
                    {
                        memstream.Seek(0, SeekOrigin.Begin);
                        await holder.UploadChunkAsync(session, memstream, _chunkSize, i++);
                        await memstream.DisposeAsync();
                    }
                }
                finally
                {
                    if (memstream != null)
                    {
                        await memstream.DisposeAsync();
                    }
                }

                await holder.FinalizeAsync(session);
                await _destination.MoveAsync(destDomain, session.TempPath, destDomain, destPath);
            }
            catch (Exception ex)
            {
                option.ErrorCopyFile(ex);
                await holder.AbortAsync(session);
            }
        }
    }

    private bool GetStream(Stream stream, out Stream memstream)
    {
        memstream = tempStream.Create();
        var total = 0;
        int readed;
        const int portion = 2048;
        var buffer = new byte[portion];

        while ((readed = stream.Read(buffer, 0, portion)) > 0)
        {
            memstream.Write(buffer, 0, readed);
            total += readed;
            if (total >= _chunkSize)
            {
                break;
            }
        }

        return total > 0;
    }
}