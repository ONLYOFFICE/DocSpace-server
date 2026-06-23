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

public static class ContentDispositionUtil
{
    public static string GetHeaderValue(string fileName, bool inline = false, bool withoutBase = false)
    {
        // If fileName contains any Unicode characters, encode according
        // to RFC 2231 (with clarifications from RFC 5987)
        if (fileName.Any(c => c > 127))
        {
            //.netcore
            var str = withoutBase
                ? "{0}; filename*=UTF-8''{2}"
                : "{0}; filename=\"{2}\"; filename*=UTF-8''{2}";

            return string.Format(str,
                                 inline ? "inline" : "attachment",
                                 fileName,
                                 CreateRfc2231HeaderValue(fileName));
        }

        // Knowing there are no Unicode characters in this fileName, rely on
        // ContentDisposition.ToString() to encode properly.
        // In .Net 4.0, ContentDisposition.ToString() throws FormatException if
        // the file name contains Unicode characters.
        // In .Net 4.5, ContentDisposition.ToString() no longer throws FormatException
        // if it contains Unicode, and it will not encode Unicode as we require here.
        // The Unicode test above is identical to the 4.0 FormatException test,
        // allowing this helper to give the same results in 4.0 and 4.5.         
        var disposition = new ContentDisposition { FileName = fileName, Inline = inline };
        return disposition.ToString();
    }

    private static string CreateRfc2231HeaderValue(string filename)
    {
        var builder = new StringBuilder();

        var filenameBytes = Encoding.UTF8.GetBytes(filename);
        foreach (var b in filenameBytes)
        {
            if (IsByteValidHeaderValueCharacter(b))
            {
                builder.Append((char)b);
            }
            else
            {
                AddByteToStringBuilder(b, builder);
            }
        }

        return builder.ToString();
    }

    // Application of RFC 2231 Encoding to Hypertext Transfer Protocol (HTTP) Header Fields, sec. 3.2
    // http://greenbytes.de/tech/webdav/draft-reschke-rfc2231-in-http-latest.html
    private static bool IsByteValidHeaderValueCharacter(byte b)
    {
        if (b is >= (byte)'0' and <= (byte)'9')
        {
            return true; // is digit
        }
        if (b is >= (byte)'a' and <= (byte)'z')
        {
            return true; // lowercase letter
        }
        if (b is >= (byte)'A' and <= (byte)'Z')
        {
            return true; // uppercase letter
        }

        return b switch
        {
            (byte)'-' or
            (byte)'.' or
            (byte)'_' or
            (byte)'~' or
            (byte)':' or
            (byte)'!' or
            (byte)'$' or
            (byte)'&' or
            (byte)'+' => true,
            _ => false
        };
    }

    private static void AddByteToStringBuilder(byte b, StringBuilder builder)
    {
        builder.Append('%');

        int i = b;
        AddHexDigitToStringBuilder(i >> 4, builder);
        AddHexDigitToStringBuilder(i % 16, builder);
    }

    private const string HexDigits = "0123456789ABCDEF";

    private static void AddHexDigitToStringBuilder(int digit, StringBuilder builder)
    {
        builder.Append(HexDigits[digit]);
    }
}