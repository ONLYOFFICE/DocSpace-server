// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Common.Caching;

public static class HttpContextExtension
{
    public static DateTime? GetIfModifiedSince(this HttpContext httpContext)
    {
        DateTime? lastModified = null;
        if (DateTime.TryParse(httpContext.Request.Headers.IfModifiedSince, CultureInfo.InvariantCulture, out var parsedLastModified))
        {
            lastModified = parsedLastModified;
            lastModified = DateTime.SpecifyKind(lastModified.Value, DateTimeKind.Local);
        }
        return lastModified;
    }
    
    public static bool TryGetFromCache(this HttpContext httpContext, DateTime lastModified)
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

    public static async Task SetOutputCacheAsync(this HttpContext httpContext, IFusionCache cache, string key, List<string> tags)
    {
        var lastModified = DateTime.Now;
        httpContext.Response.Headers.LastModified = lastModified.ToString(CultureInfo.InvariantCulture);
        await cache.SetAsync(key,
            new CacheEntry() { LastModified = lastModified },
            CacheExtention.OutputDuration,
            tags.ToArray());
    }

    public static bool TryGetFromCache(this HttpContext httpContext, string etag)
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