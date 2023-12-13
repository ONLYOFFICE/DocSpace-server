// (c) Copyright Ascensio System SIA 2010-2023
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

namespace System.Web;

public static class HttpRequestExtensions
{
    public static readonly string RequestTokenHeader = "Request-Token";

    public static Uri Url(this HttpRequest request)
    {
        return request != null ? new Uri(request.GetDisplayUrl()) : null;
    }

    public static Uri PushRewritenUri(this HttpContext context)
    {
        return context != null ? PushRewritenUri(context, context.Request.Url()) : null;
    }

    private static Uri PushRewritenUri(this HttpContext context, Uri rewrittenUri)
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
                                                      new object[] { null });
                    oldUri = url;
                    context.Items["oldUri"] = oldUri;

                }
                catch (Exception) { }
            }
        }

        return oldUri;
    }

    public static Uri PopRewritenUri(this HttpContext context)
    {
        if (context != null && context.Items["oldUri"] != null)
        {
            var rewriteTo = context.Items["oldUri"] as Uri;

            if (rewriteTo != null)
            {
                return PushRewritenUri(context, rewriteTo);
            }
        }

        return null;
    }

    public static bool DesktopApp(this HttpRequest request)
    {
        return request != null
            && (!string.IsNullOrEmpty(request.Query["desktop"])
                || !string.IsNullOrEmpty(request.Headers[HeaderNames.UserAgent]) && request.Headers[HeaderNames.UserAgent].ToString().Contains("AscDesktopEditor"));
    }

    public static bool MobileApp(this HttpRequest request)
    {
        return !string.IsNullOrEmpty(request.Headers[HeaderNames.UserAgent]) && (request.Headers[HeaderNames.UserAgent].Contains("iOS") || request.Headers[HeaderNames.UserAgent].Contains("Android"));
    }
}
