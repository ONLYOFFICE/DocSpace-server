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

namespace ASC.Common.Utils;

public static class MimeHeaderUtils
{
    public static string EncodeMime(string mimeHeaderValue)
    {
        return EncodeMime(mimeHeaderValue, Encoding.UTF8, false);
    }

    public static string EncodeMime(string mimeHeaderValue, Encoding charset, bool split)
    {
        if (MustEncode(mimeHeaderValue))
        {
            var result = new StringBuilder();
            var data = charset.GetBytes(mimeHeaderValue);
            var maxEncodedTextSize = split ? 75 - ("=?" + charset.WebName + "?" + "B"/*Base64 encode*/ + "?" + "?=").Length : int.MaxValue;

            result.Append("=?" + charset.WebName + "?B?");
            var stored = 0;
            var base64 = Convert.ToBase64String(data);

            for (var i = 0; i < base64.Length; i += 4)
            {
                // Encoding buffer full, create new encoded-word.
                if (stored + 4 > maxEncodedTextSize)
                {
                    result.Append("?=\r\n =?" + charset.WebName + "?B?");
                    stored = 0;
                }

                result.Append(base64, i, 4);
                stored += 4;
            }

            result.Append("?=");

            return result.ToString();
        }

        return mimeHeaderValue;
    }

    public static bool MustEncode(string text)
    {
        return !string.IsNullOrEmpty(text) && text.Any(c => c > 127);
    }
}