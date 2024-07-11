// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Web.Files.Utils;
[Transient]
public class FileСhecker(
    IDaoFactory daoFactory,
    IConfiguration configuration)
{
    public async Task<bool> CheckExtendedPDF<T>(File<T> file)
    {
        const int limit = 300;
        var fileDao = daoFactory.GetFileDao<T>();
        var stream = await fileDao.GetFileStreamAsync(file, 0, limit);

        using var memStream = new MemoryStream();
        await stream.CopyToAsync(memStream);
        memStream.Seek(0, SeekOrigin.Begin);

        return await CheckExtendedPDFstream(memStream);
    }
    public async Task<bool> CheckExtendedPDFstream(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.GetEncoding("iso-8859-1"));
        var message = await reader.ReadToEndAsync();

        var config = configuration.GetSection("files:oform").Get<OFormSettings>();

        return IsExtendedPDFFile(message, config.Signature);
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
