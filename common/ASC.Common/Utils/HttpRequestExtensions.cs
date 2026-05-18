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

namespace System.Web;

public static class HttpRequestExtensions
{
    public const string RequestTokenHeader = "Request-Token";

    extension(HttpContext context)
    {
        public Uri PushRewritenUri()
        {
            return context != null ? PushRewritenUri(context, context.Request.Url()) : null;
        }

        private Uri PushRewritenUri(Uri rewrittenUri)
        {
            Uri oldUri = null;

            if (context != null)
            {
                var request = context.Request;

                var url = new Uri(request.GetDisplayUrl());

                if (url != rewrittenUri)
                {
                    try
                    {
                        //Push it
                        request.Headers.SetCommaSeparatedValues("HTTPS", rewrittenUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "on" : "off");
                        request.Headers.SetCommaSeparatedValues("SERVER_NAME", rewrittenUri.Host);
                        request.Headers.SetCommaSeparatedValues("SERVER_PORT",
                            rewrittenUri.Port.ToString(CultureInfo.InvariantCulture));

                        if (rewrittenUri.IsDefaultPort)
                        {
                            request.Headers.SetCommaSeparatedValues("HTTP_HOST",
                                rewrittenUri.Host);
                        }
                        else
                        {
                            request.Headers.SetCommaSeparatedValues("HTTP_HOST",
                                rewrittenUri.Host + ":" + url.Port);
                        }
                        //Hack:
                        typeof(HttpRequest).InvokeMember("_url",
                            BindingFlags.NonPublic | BindingFlags.SetField |
                            BindingFlags.Instance,
                            null, request,
                            [null]);
                        oldUri = url;
                        context.Items["oldUri"] = oldUri;

                    }
                    catch (Exception) { }
                }
            }

            return oldUri;
        }

        public Uri PopRewritenUri()
        {
            if (context?.Items["oldUri"] != null)
            {
                var rewriteTo = context.Items["oldUri"] as Uri;

                if (rewriteTo != null)
                {
                    return PushRewritenUri(context, rewriteTo);
                }
            }

            return null;
        }
    }

    extension(HttpRequest request)
    {
        public bool DesktopApp()
        {
            return request != null
                   && (!string.IsNullOrEmpty(request.Query["desktop"])
                       || !string.IsNullOrEmpty(request.Headers[HeaderNames.UserAgent]) && request.Headers[HeaderNames.UserAgent].ToString().Contains("AscDesktopEditor"));
        }

        public bool MobileApp()
        {
            return !string.IsNullOrEmpty(request.Headers[HeaderNames.UserAgent]) && (request.Headers[HeaderNames.UserAgent].ToString().Contains("iPhone") || request.Headers[HeaderNames.UserAgent].ToString().Contains("iOS") || request.Headers[HeaderNames.UserAgent].ToString().Contains("Android"));
        }

        public Uri Url()
        {
            var url = request != null ? new Uri(request.GetDisplayUrl()) : null;

            if (!string.IsNullOrEmpty(url?.Query))
            {
                var queryParams = HttpUtility.ParseQueryString(url.Query);
                var origin = queryParams[HeaderNames.Origin.ToLower()];
                if (Uri.TryCreate(origin, UriKind.Absolute, out var urlOrigin))
                {
                    var result = new UriBuilder(url)
                    {
                        Scheme = urlOrigin.Scheme,
                        Host = urlOrigin.Host,
                        Port = urlOrigin.Port
                    };
                    return result.Uri;
                }
            }

            return url;
        }
    }
}