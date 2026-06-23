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

namespace ASC.Common;

[Singleton]
public class TempStream(TempPath tempPath)
{
    public async Task<(Stream, bool)> TryGetBufferedAsync(Stream srcStream)
    {
        ArgumentNullException.ThrowIfNull(srcStream);
        if (!srcStream.CanSeek || srcStream.CanTimeout)
        {
            //Buffer it
            var memStream = Create();
            await srcStream.CopyToAsync(memStream);
            memStream.Position = 0;

            return (memStream, true);
        }

        return (srcStream, false);
    }

    public async Task<MemoryStream> CloneMemoryStream(MemoryStream originalStream, int limit = -1)
    {
        var cloneStream = new MemoryStream();

        var originalPosition = originalStream.Position;

        originalStream.Position = 0;

        if (limit > 0)
        {
            await originalStream.CopyToAsync(cloneStream, limit);
        }
        else
        {
            await originalStream.CopyToAsync(cloneStream);
        }


        originalStream.Position = originalPosition;

        cloneStream.Position = 0;

        return cloneStream;
    }

    public Stream Create()
    {
        var path = tempPath.GetTempFileName();
        return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.DeleteOnClose);
    }
}