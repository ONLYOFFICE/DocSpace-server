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

namespace ASC.MessagingSystem;

public class MessageSettings
{
    private const string UserAgentHeader = "User-Agent";
    private const string RefererHeader = "Referer";
    private const string EditorsUAHeader = "AscDesktopEditor";
    private const string XRemoteIpAddress = "X-Remote-Ip-Address"; // Custom (fake) header for storage client remote ip address
    private const string EditorsName = "Desktop Editors";
    private const string ZoomAppsUAHeader = "ZoomApps";
    private const string ZoomBrowserUAHeader = "ZoomWebKit";

    static MessageSettings()
    {
        Parser = Parser.GetDefault();
    }

    private static Parser Parser { get; }

    public static ClientInfo GetClientInfo(string uaHeader)
    {
        return Parser.Parse(uaHeader);
    }

    public static IDictionary<string, StringValues> GetHttpHeaders(HttpRequest request)
    {
        if (request == null)
        {
            return null;
        }

        var headers = request.Headers.ToDictionary(k => k.Key, v => v.Value);

        if (!headers.TryGetValue(XRemoteIpAddress, out _))
        {
            var remoteIpAddress = GetIP(request);

            if (!string.IsNullOrEmpty(remoteIpAddress))
            {
                headers.Add(XRemoteIpAddress, remoteIpAddress);
            }
        }

        return headers;
    }

    public static string GetUAHeader(HttpRequest request)
    {
        string result = request?.Query["request-user-agent"];

        return !string.IsNullOrEmpty(result) ? result : request?.Headers.UserAgent.FirstOrDefault();
    }

    public static string GetUAHeader(IDictionary<string, StringValues> headers)
    {
        return headers.TryGetValue(UserAgentHeader, out var header) ? header.FirstOrDefault() : null;
    }

    public static string GetReferer(HttpRequest request)
    {
        return request?.Headers.Referer.FirstOrDefault();
    }

    public static string GetRequestPath(HttpRequest request)
    {
        return request == null ? null : $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
    }

    public static string GetReferer(IDictionary<string, StringValues> headers)
    {
        return headers.TryGetValue(RefererHeader, out var header) ? (string)header : null;
    }

    public static string GetIP(HttpRequest request)
    {
        string result = request?.Query["request-x-real-ip"];

        return !string.IsNullOrEmpty(result) ? result : request?.HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    public static string GetIP(IDictionary<string, StringValues> headers)
    {
        return headers.TryGetValue(XRemoteIpAddress, out var header) ? header.FirstOrDefault() : null;
    }

    public static void AddInfoMessage(EventMessage message, Dictionary<string, ClientInfo> dict = null)
    {
        ClientInfo clientInfo;
        if (dict != null)
        {
            if (!dict.TryGetValue(message.UaHeader, out clientInfo))
            {
                clientInfo = GetClientInfo(message.UaHeader);
                dict.Add(message.UaHeader, clientInfo);
            }
        }
        else
        {
            clientInfo = GetClientInfo(message.UaHeader);
        }
        if (clientInfo != null)
        {
            message.Browser = GetBrowser(clientInfo);
            message.Platform = GetPlatformAndDevice(clientInfo);
        }
    }

    public static string GetBrowser(ClientInfo clientInfo)
    {
        if (clientInfo == null)
        {
            return null;
        }

        if (TryGetCustomUAData(clientInfo.String, EditorsUAHeader, EditorsName, out var customBrowser))
        {
            return customBrowser;
        }

        if (TryGetCustomUAData(clientInfo.String, ZoomBrowserUAHeader, ZoomBrowserUAHeader, out customBrowser))
        {
            return customBrowser;
        }

        return $"{clientInfo.UA.Family} {clientInfo.UA.Major}".Trim();
    }

    public static string GetPlatformAndDevice(ClientInfo clientInfo)
    {
        if (clientInfo == null)
        {
            return null;
        }

        if (TryGetCustomUAData(clientInfo.String, ZoomAppsUAHeader, ZoomAppsUAHeader, out var customDevice))
        {
            return customDevice;
        }

        return $"{clientInfo.OS.Family} {clientInfo.OS.Major} {clientInfo.Device.Brand} {clientInfo.Device.Model}".Trim();
    }

    private static bool TryGetCustomUAData(string ua, string pattern, string displayName, out string result)
    {
        result = null;
        if (!ua.Contains(pattern))
        {
            return false;
        }

        var data = ua.Split(" ").FirstOrDefault(r => r.StartsWith(pattern));
        if (data == null)
        {
            return false;
        }

        var parts = data.Split("/");
        var version = parts.Length > 1 ? parts[1] : null;
        result = $"{displayName} {version}";
        return true;
    }
}