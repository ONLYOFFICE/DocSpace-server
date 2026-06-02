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

public static class SecureHelper
{
    public static bool IsSecure(HttpContext httpContext, ILoggerFactory loggerFactory)
    {
        try
        {
            return httpContext != null && Uri.UriSchemeHttps.Equals(httpContext.Request.Url().Scheme, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception err)
        {
            loggerFactory.CreateLogger("ASC.Data.Storage.SecureHelper").ErrorIsSecure(err);

            return false;
        }
    }

    public static string GenerateSecureKeyHeader(string path, EmailValidationKeyProvider keyProvider)
    {
        var ticks = DateTime.UtcNow.Ticks;
        var data = path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar) + '.' + ticks;
        var key = keyProvider.GetEmailKey(data);

        return Constants.SecureKeyHeader + ':' + ticks + '-' + key;
    }

    public static bool CheckSecureKeyHeader(string queryHeaders, string path, EmailValidationKeyProvider keyProvider)
    {
        if (string.IsNullOrEmpty(queryHeaders))
        {
            return false;
        }

        var headers = queryHeaders.Length > 0 ? queryHeaders.Split('&').Select(HttpUtility.UrlDecode) : [];

        var headerKey = headers.FirstOrDefault(h => h.StartsWith(Constants.SecureKeyHeader))?.
            Replace(Constants.SecureKeyHeader + ':', string.Empty);

        if (string.IsNullOrEmpty(headerKey))
        {
            return false;
        }

        var separatorPosition = headerKey.IndexOf('-');
        var ticks = headerKey[..separatorPosition];
        var key = headerKey[(separatorPosition + 1)..];

        var result = keyProvider.ValidateEmailKey(path + '.' + ticks, key);

        return result == EmailValidationKeyProvider.ValidationResult.Ok;
    }
}