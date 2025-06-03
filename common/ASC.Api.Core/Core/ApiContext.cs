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

using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.Api.Core;

[Scope]
public class ApiContext : ICloneable
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public string[] Fields { get; set; }
    public long? TotalCount
    {
        set
        {
            _httpContextAccessor.HttpContext?.Items.TryAdd(nameof(TotalCount),  value);
        }
    }
    
    /// <summary>
    /// Gets count to get item from collection. Request parameter "count"
    /// </summary>
    /// <remarks>
    /// Don't forget to call _context.SetDataPaginated() to prevent SmartList from filtering response if you fetch data from DB with TOP &amp; COUNT
    /// </remarks>
    public long Count { get; init; }

    /// <summary>
    /// Gets field to sort by from request parameter "sortBy"
    /// </summary>
    public string SortBy { get; set; }

    /// <summary>
    /// Gets value to filter from request parameter "filterValue"
    /// </summary>
    public string FilterValue { get; set; }

    /// <summary>
    /// Sort direction. From request parameter "sortOrder" can be "descending" or "ascending"
    /// Like ...&amp;sortOrder=descending&amp;...
    /// </summary>
    public bool SortDescending { get; set; }

    
    public string FilterSeparator { get; set; }

    private static readonly int _maxCount = 1000;

    public ApiContext()
    {
        
    }
    
    public ApiContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        if (httpContextAccessor.HttpContext?.Request == null)
        {
            return;
        }

        Count = _maxCount;

        IQueryCollection query;

        try
        {
            query = _httpContextAccessor.HttpContext.Request?.Query;
        }
        catch (Exception)
        {
            //Access to disposed context
            return;
        }

        if (query == null)
        {
            return;
        }

        //Try parse values
        var count = query.GetRequestValue("count");
        if (!string.IsNullOrEmpty(count) && ulong.TryParse(count, out var countParsed))
        {
            //Count specified and valid
            Count = Math.Min((long)countParsed, _maxCount);
        }
        

        var sortOrder = query.GetRequestValue("sortOrder");
        if ("descending".Equals(sortOrder))
        {
            SortDescending = true;
        }

        SortBy = query.GetRequestValue("sortBy");
        FilterValue = query.GetRequestValue("filterValue");
        Fields = query.GetRequestArray("fields");
        FilterSeparator = query.GetRequestValue("filterSeparator");
    }
    
    public ApiContext SetDataFiltered()
    {
        FilterValue = string.Empty;

        return this;
    }

    public ApiContext SetTotalCount(long totalCollectionCount)
    {
        TotalCount = totalCollectionCount;

        return this;
    }

    public ApiContext SetCount(int count)
    {
        _httpContextAccessor.HttpContext?.Items.TryAdd(nameof(Count), count);

        return this;
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}

public static class QueryExtension
{
    internal static string[] GetRequestArray(this IQueryCollection query, string key)
    {
        if (query != null)
        {
            var values = query[key + "[]"];
            if (values.Count > 0)
            {
                return values;
            }

            values = query[key];
            if (values.Count > 0)
            {
                if (values.Count == 1) //If it's only one element
                {
                    //Try split
                    if (!string.IsNullOrEmpty(values[0]))
                    {
                        return values[0].Split(',');
                    }
                }

                return values;
            }
        }

        return null;
    }

    public static string GetRequestValue(this IQueryCollection query, string key)
    {
        var reqArray = query.GetRequestArray(key);

        return reqArray?.FirstOrDefault();
    }
}

public static class ApiContextExtension
{
    public static bool Check(this ApiContext context, string field)
    {
        return context?.Fields == null
            || (context.Fields != null
            && context.Fields.Contains(field, StringComparer.InvariantCultureIgnoreCase));
    }
}