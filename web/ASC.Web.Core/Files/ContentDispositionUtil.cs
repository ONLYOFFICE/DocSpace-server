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

namespace ASC.Web.Core.Files;

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
