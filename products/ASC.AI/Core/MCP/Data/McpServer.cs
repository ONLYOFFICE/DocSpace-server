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

public class McpServer
{
    public Guid Id { get; init; }
    public int TenantId { get; init; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Endpoint { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public ServerType ServerType { get; init; }
    public ConnectionType ConnectionType { get; init; }
    public bool Enabled { get; set; }
    public bool HasIcon { get; set; }
    public Icon? Icon { get; set; }
    public DateTime ModifiedOn { get; set; }

    public bool NeedReset { get; set; }
}

public static class McpServerExtensions
{
    public static async Task<McpServer> ToMcpServerAsync(
        this DbMcpServerUnit dbMcpUnit, 
        InstanceCrypto crypto,
        McpIconStore iconStore)
    {
        var server = new McpServer
        {
            Id = dbMcpUnit.Server.Id, 
            TenantId = dbMcpUnit.Server.TenantId, 
            Name = dbMcpUnit.Server.Name,
            Description = dbMcpUnit.Server.Description,
            Endpoint = dbMcpUnit.Server.Endpoint,
            ConnectionType = dbMcpUnit.Server.ConnectionType,
            Enabled = dbMcpUnit.State?.Enabled ?? false,
            HasIcon = dbMcpUnit.Server.HasIcon,
            ModifiedOn = dbMcpUnit.Server.ModifiedOn
        };
        
        if (server.HasIcon)
        {
            server.Icon = await iconStore.GetAsync(
                dbMcpUnit.Server.TenantId, dbMcpUnit.Server.Id, dbMcpUnit.Server.ModifiedOn);
        }

        if (dbMcpUnit.Server.Headers == null)
        {
            return server;
        }

        var headersJson = string.Empty;
        try
        {
            headersJson = await crypto.DecryptAsync(dbMcpUnit.Server.Headers);
        }
        catch (CryptographicException)
        {
            server.NeedReset = true;
        }

        if (!server.NeedReset)
        {
            server.Headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
        }

        return server;
    }
}