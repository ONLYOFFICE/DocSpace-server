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
        var result = request?.Query["request-user-agent"].FirstOrDefault();

        if (result != null)
        {
            return result;
        }

        return request?.Headers[UserAgentHeader].FirstOrDefault();
    }

    public static string GetUAHeader(IDictionary<string, StringValues> headers)
    {
        return headers.TryGetValue(UserAgentHeader, out var header) ? header.FirstOrDefault() : null;
    }

    public static string GetReferer(HttpRequest request)
    {
        return request?.Headers[RefererHeader].FirstOrDefault();
    }

    public static string GetReferer(IDictionary<string, StringValues> headers)
    {
        return headers.TryGetValue(RefererHeader, out var header) ? header.FirstOrDefault() : null;
    }

    public static string GetIP(HttpRequest request)
    {
        var result = request?.Query["request-x-real-ip"].FirstOrDefault();

        if (result != null)
        {
            return result;
        }

        return request?.HttpContext.Connection.RemoteIpAddress?.ToString();
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
