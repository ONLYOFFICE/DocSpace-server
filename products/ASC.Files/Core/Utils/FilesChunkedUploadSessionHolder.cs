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

public class FilesChunkedUploadSessionHolder : CommonChunkedUploadSessionHolder
{
    private readonly IDaoFactory _daoFactory;

    public FilesChunkedUploadSessionHolder(IDaoFactory daoFactory, IDataStore dataStore, string domain, IFusionCache cache, long maxChunkUploadSize = 10485760)
        : base(dataStore, domain, cache, maxChunkUploadSize)
    {
        _daoFactory = daoFactory;
        TempDomain = FileConstant.StorageDomainTmp;
    }

    public override async Task<(string, string)> UploadChunkAsync(CommonChunkedUploadSession uploadSession, Stream stream, long length, int chunkNumber)
    {
        if (uploadSession is ChunkedUploadSession<int>)
        {
            return ((await InternalUploadChunkAsync<int>(uploadSession, stream, length)).ToString(), null);
        }

        return (await InternalUploadChunkAsync<string>(uploadSession, stream, length), null);
    }

    private async Task<T> InternalUploadChunkAsync<T>(CommonChunkedUploadSession uploadSession, Stream stream, long length)
    {
        var chunkedUploadSession = uploadSession as ChunkedUploadSession<T>;
        chunkedUploadSession.File.ContentLength += stream.Length;
        var fileDao = GetFileDao<T>();
        var file = await fileDao.UploadChunkAsync(chunkedUploadSession, stream, length);
        return file.Id;
    }

    public override async Task<string> FinalizeAsync(CommonChunkedUploadSession uploadSession)
    {
        if (uploadSession is ChunkedUploadSession<int>)
        {
            return (await InternalFinalizeAsync<int>(uploadSession)).ToString();
        }

        return await InternalFinalizeAsync<string>(uploadSession);
    }

    private async Task<T> InternalFinalizeAsync<T>(CommonChunkedUploadSession commonChunkedUploadSession)
    {
        var chunkedUploadSession = commonChunkedUploadSession as ChunkedUploadSession<T>;
        var fileDao = GetFileDao<T>();
        var file = await fileDao.FinalizeUploadSessionAsync(chunkedUploadSession);
        return file.Id;
    }

    private IFileDao<T> GetFileDao<T>()
    {
        return _daoFactory.GetFileDao<T>();
    }
}