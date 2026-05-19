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

namespace ASC.AI.Core.MCP.Data;

public class McpServerConnection
{
    public Guid ServerId { get; init; }
    public int TenantId { get; init; }
    public int RoomId { get; init; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Endpoint { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public ServerType ServerType { get; init; }
    public ConnectionType ConnectionType { get; init; }
    public bool System { get; init; }
    public OauthProvider? OauthProvider { get; init; }
    public McpServerSettings? Settings { get; set; }
    public bool Connected => ConnectionType is ConnectionType.Direct || Settings?.OauthCredentials != null;
    public Icon? Icon { get; set; }

    public bool NeedReset { get; set; }
}

public static class McpRoomServerExtensions
{
    public static async Task<McpServerConnection> ToMcpRoomServerAsync(
        this DbRoomServerUnit item,
        InstanceCrypto crypto, 
        ConsumerFactory consumerFactory,
        McpIconStore iconStore)
    {
        McpServerConnection serverConnection = null!;
        
        if (item.Server != null)
        {
            serverConnection = new McpServerConnection
            {
                ServerId = item.Server.Id,
                TenantId = item.TenantId,
                RoomId = item.RoomId,
                Name = item.Server.Name,
                Description = item.Server.Description,
                Endpoint = item.Server.Endpoint,
                ServerType = ServerType.Custom,
                ConnectionType = item.Server.ConnectionType,
                System = false
            };

            if (item.Server.Headers != null)
            {
                var headersJson = string.Empty;
                try
                {
                    headersJson = await crypto.DecryptAsync(item.Server.Headers);
                }
                catch (CryptographicException)
                {
                    serverConnection.NeedReset = true;
                }

                if (!serverConnection.NeedReset)
                {
                    serverConnection.Headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
                }
            }

            if (item.Server.HasIcon)
            {
                serverConnection.Icon = await iconStore.GetAsync(
                    item.Server.TenantId, item.Server.Id, item.Server.ModifiedOn);
            }
        }
        else if (item.SystemServer != null)
        {
            serverConnection = new McpServerConnection
            {
                ServerId = item.ServerId,
                TenantId = item.TenantId,
                RoomId = item.RoomId,
                Name = item.SystemServer.Name,
                Description = item.SystemServer.Description,
                Endpoint = item.SystemServer.Endpoint,
                ServerType = item.SystemServer.Type,
                ConnectionType = item.SystemServer.ConnectionType,
                System = true,
                OauthProvider = item.SystemServer.LoginProviderSelector?.Invoke(consumerFactory)
            };
        }
        
        if (item.Settings != null)
        {
            serverConnection.Settings = await item.Settings.ToMcpServerSettingsAsync(crypto);
        }

        return serverConnection;
    }
}