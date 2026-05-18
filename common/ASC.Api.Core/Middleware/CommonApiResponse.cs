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

using Microsoft.AspNetCore.Http.Extensions;

namespace ASC.Api.Core.Middleware;

public abstract class CommonApiResponse
{
    public int Status { get; set; }
    public HttpStatusCode StatusCode { get; set; }

    protected CommonApiResponse()
    {
    }

    protected CommonApiResponse(HttpStatusCode statusCode)
    {
        StatusCode = statusCode;
    }
}

public class ErrorApiResponse : CommonApiResponse
{
    public CommonApiError Error { get; set; }

    public ErrorApiResponse()
    {

    }

    protected internal ErrorApiResponse(HttpStatusCode statusCode, Exception error, string message, bool withStackTrace) : base(statusCode)
    {
        Status = 1;
        Error = CommonApiError.FromException(error, message, withStackTrace);
    }
}

public class SuccessApiResponse : CommonApiResponse
{
    private readonly HttpContext _httpContext;

    public object Response { get; set; }

    public int? Count
    {
        get
        {

            if (_httpContext.Items.TryGetValue("Count", out var count))
            {
                return (int?)count;
            }

            if (Response is List<object> list)
            {
                return list.Count;
            }

            if (Response is IEnumerable<object> collection)
            {
                return collection.Count();
            }

            if (Response == null)
            {
                return 0;
            }

            return 1;
        }
    }

    public long? Total
    {
        get
        {
            if (_httpContext.Items.TryGetValue("TotalCount", out var total))
            {
                return (long?)total;
            }

            return null;
        }
    }

    public List<Link> Links { get; set; }

    public SuccessApiResponse()
    {

    }

    protected internal SuccessApiResponse(HttpContext httpContext, object response) : base((HttpStatusCode)httpContext.Response.StatusCode)
    {
        Status = 0;
        _httpContext = httpContext;
        if (response != null)
        {
            Response = response;
        }

        if (response is ValidationProblemDetails { Status: not null } validationProblemDetails)
        {
            Status = 1;
            StatusCode = (HttpStatusCode)validationProblemDetails.Status;
        }

        Links =
        [
            new Link { Href = httpContext.Request.GetDisplayUrl(), Action = httpContext.Request.Method }
        ];
    }
}

public class CommonApiError
{
    public string Message { get; set; }
    public string Type { get; set; }
    public string Stack { get; set; }
    public int Hresult { get; set; }

    public CommonApiError()
    {

    }

    public static CommonApiError FromException(Exception exception, string message, bool withStackTrace)
    {
        var result = new CommonApiError
        {
            Message = message ?? exception.Message
        };

        if (withStackTrace)
        {
            result.Type = exception.GetType().ToString();
            result.Stack = exception.StackTrace;
            result.Hresult = exception.HResult;
        }

        return result;
    }
}

public class Link
{
    public string Href { get; set; }
    public string Action { get; set; }
}
