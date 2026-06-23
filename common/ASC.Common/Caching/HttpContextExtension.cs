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

namespace ASC.Common.Caching;

public static class HttpContextExtension
{
    extension(HttpContext httpContext)
    {
        public DateTime? GetIfModifiedSince()
        {
            DateTime? lastModified = null;
            if (DateTime.TryParse(httpContext.Request.Headers.IfModifiedSince, CultureInfo.InvariantCulture, out var parsedLastModified))
            {
                lastModified = parsedLastModified;
                lastModified = DateTime.SpecifyKind(lastModified.Value, DateTimeKind.Local);
            }
            return lastModified;
        }

        public bool TryGetFromCache(DateTime lastModified)
        {
            if (lastModified != DateTime.MinValue)
            {
                var lastModifiedStr = lastModified.ToString(CultureInfo.InvariantCulture);
                if (lastModifiedStr == httpContext.Request.Headers.IfModifiedSince)
                {
                    httpContext.Response.StatusCode = 304;
                    return true;
                }

                httpContext.Response.Headers.LastModified = lastModifiedStr;
                httpContext.Response.Headers.CacheControl = "private, no-cache";
            }

            return false;
        }

        public bool TryGetFromCache(string etag)
        {
            var etagFromRequest = httpContext.Request.Headers.IfNoneMatch;

            etag = "W/" + etag;
            if (etag == etagFromRequest)
            {
                httpContext.Response.StatusCode = 304;
                return true;
            }

            httpContext.Response.Headers.ETag = etag;
            httpContext.Response.Headers.CacheControl = "private, no-cache";

            return false;
        }
    }

    public static async Task<string> CalculateEtagAsync(IEnumerable<string> data)
    {
        using var md5 = MD5.Create();
        using var memoryStream = new MemoryStream();

        foreach (var d in data)
        {
            await memoryStream.WriteAsync(Encoding.UTF8.GetBytes(d).AsMemory(0, Encoding.UTF8.GetByteCount(d)));
        }

        var hash = await md5.ComputeHashAsync(memoryStream);
        var hex = BitConverter.ToString(hash);
        return hex.Replace("-", "");
    }
}