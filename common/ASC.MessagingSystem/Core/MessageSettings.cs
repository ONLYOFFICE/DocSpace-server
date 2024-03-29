﻿// (c) Copyright Ascensio System SIA 2010-2023
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
    private const string EditorsName = "Desktop Editors";

    static MessageSettings()
    {
        Parser = Parser.GetDefault();
    }

    private static Parser Parser { get; }

    public static ClientInfo GetClientInfo(string uaHeader)
    {
        return Parser.Parse(uaHeader);
    }

    public static string GetUAHeader(HttpRequest request)
    {
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
        return request?.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }

    public static void AddInfoMessage(EventMessage message, Dictionary<string, ClientInfo> dict = null)
    {
        ClientInfo clientInfo;
        if (dict != null)
        {
            if (!dict.TryGetValue(message.UAHeader, out clientInfo))
            {
                clientInfo = GetClientInfo(message.UAHeader);
                dict.Add(message.UAHeader, clientInfo);
            }
        }
        else
        {
            clientInfo = GetClientInfo(message.UAHeader);
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

        if (clientInfo.String.Contains(EditorsUAHeader))
        {
            var data = clientInfo.String.Split(" ").FirstOrDefault(r=> r.StartsWith(EditorsUAHeader));
            if (data != null)
            {
                var parts = data.Split("/");
                return $"{EditorsName} {parts[1]}";
            }
        }
        
        return $"{clientInfo.UA.Family} {clientInfo.UA.Major}".Trim();
    }

    public static string GetPlatformAndDevice(ClientInfo clientInfo)
    {
        if (clientInfo == null)
        {
            return null;
        }
        
        return $"{clientInfo.OS.Family} {clientInfo.OS.Major} {clientInfo.Device.Brand} {clientInfo.Device.Model}".Trim();
    }
}
