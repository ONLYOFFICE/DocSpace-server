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
[Transient]
public class FileChecker(
    IDaoFactory daoFactory,
    IConfiguration configuration,
    IFusionCache cache)
{
    public async Task<bool> CheckExtendedPDF<T>(File<T> file)
    {
        const int limit = 300;
        var fileDao = daoFactory.GetFileDao<T>();
        await using var stream = await fileDao.GetFileStreamAsync(file, 0, limit);

        return await CheckExtendedPDFstream(stream);
    }
    public async Task<bool> CheckExtendedPDFstream(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.GetEncoding("iso-8859-1"));
        var message = await reader.ReadToEndAsync();

        var config = configuration.GetSection("files:oform").Get<OFormSettings>();

        return IsExtendedPDFFile(message, config.Signature);
    }
    public async Task<bool> IsFormPDFFile<T>(File<T> file)
    {
        var isFormCache = await cache.TryGetAsync<bool>(FileConstant.IsFormKeyPrefix + file.Id);

        if (isFormCache.HasValue)
        {
            return isFormCache.Value;
        }

        var isForm = await CheckExtendedPDF(file);
        await cache.SetAsync(FileConstant.IsFormKeyPrefix + file.Id, isForm);
        return isForm;
    }
    private static bool IsExtendedPDFFile(string text, string signature)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var indexFirst = text.IndexOf("%\xCD\xCA\xD2\xA9\x0D");

        if (indexFirst == -1)
        {
            return false;
        }

        var pFirst = text.Substring(indexFirst + 6);

        if (!pFirst.StartsWith("1 0 obj\x0A<<\x0A"))
        {
            return false;
        }

        pFirst = pFirst.Substring(11);

        var indexStream = pFirst.IndexOf("stream\x0D\x0A");
        var indexMeta = pFirst.IndexOf(signature);

        if (indexStream == -1 || indexMeta == -1 || indexStream < indexMeta)
        {
            return false;
        }

        var pMeta = pFirst.Substring(indexMeta + signature.Length + 3);

        var indexMetaLast = pMeta.IndexOf(' ');
        if (indexMetaLast == -1)
        {
            return false;
        }

        pMeta = pMeta.Substring(indexMetaLast + 1);
        indexMetaLast = pMeta.IndexOf(' ');
        if (indexMetaLast == -1)
        {
            return false;
        }

        return true;
    }

}
