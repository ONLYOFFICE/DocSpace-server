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

        if (dbMcpUnit.Server.Headers == null)
        {
            return server;
        }

        var headersJson = await crypto.DecryptAsync(dbMcpUnit.Server.Headers);
        server.Headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
        
        if (server.HasIcon)
        {
            server.Icon = await iconStore.GetAsync(
                dbMcpUnit.Server.TenantId, dbMcpUnit.Server.Id, dbMcpUnit.Server.ModifiedOn);
        }

        return server;
    }
}