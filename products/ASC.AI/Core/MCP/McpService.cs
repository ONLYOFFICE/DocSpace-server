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

namespace ASC.AI.Core.MCP;

[Scope]
public class McpService(
    TenantManager tenantManager,
    AuthContext authContext,
    McpDao mcpDao, 
    IFolderDao<int> folderDao,
    FileSecurity fileSecurity,
    IHttpClientFactory httpClientFactory,
    ConfigMcpSource configMcpSource,
    ILogger<McpService> logger,
    IServiceProvider serviceProvider,
    UserManager userManager,
    IDistributedLockProvider distributedLockProvider)
{
    private const int MaxMcpServersByRoom = 5;
    
    public async Task<McpServerOptions> AddServerAsync(string endpoint, string name, Dictionary<string, string>? headers,
        string? description)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(name);
        
        await ThrowIfNotAccessAsync();
        
        var options = new SseClientTransportOptions
        {
            Name = name,
            Endpoint = new Uri(endpoint),
            AdditionalHeaders = headers,
            TransportMode = HttpTransportMode.AutoDetect,
            ConnectionTimeout = TimeSpan.FromSeconds(15)
        };
        
        var transport = new SseClientTransport(options, httpClientFactory.CreateClient());
        
        await ThrowIfNotConnectAsync(transport);
        
        return await mcpDao.AddServerAsync(tenantManager.GetCurrentTenantId(), endpoint, name, headers, description);
    }

    public async Task<McpServerOptions> UpdateServerAsync(Guid serverId, string? url, string? name, 
        Dictionary<string, string>? headers, string? description, bool? enabled)
    {
        await ThrowIfNotAccessAsync();

        var server = await mcpDao.GetServerAsync(tenantManager.GetCurrentTenantId(), serverId);
        if (server == null)
        {
            throw new ItemNotFoundException("MCP Server not found");
        }

        var needConnect = false;
        
        if (!string.IsNullOrEmpty(name))
        {
            server.Name = name;
        }

        if (!string.IsNullOrEmpty(url))
        {
            server.Endpoint = new Uri(url);
            needConnect = true;
        }

        if (headers != null)
        {
            server.Headers = headers.Count > 0 ? headers : null;
            needConnect = true;
        }

        if (!string.IsNullOrEmpty(description))
        {
            server.Description = description;
        }

        if (enabled.HasValue)
        {
            server.Enabled = enabled.Value;
        }

        if (needConnect)
        {
            var transportOptions = server.ToTransportOptions();
            var transport = new SseClientTransport(transportOptions, httpClientFactory.CreateClient());
        
            await ThrowIfNotConnectAsync(transport);
        }
        
        var updatedServer = await mcpDao.UpdateServerAsync(server);
        if (updatedServer == null)
        {
            throw new ItemNotFoundException("MCP Server not found");
        }

        return updatedServer;
    }
    
    public async Task<(List<McpServer> servers, int totalCount)> GetServersAsync(
        ConnectionStatus? status, int offset, int count)
    {
        await ThrowIfNotAccessAsync();
        
        var tenantId = tenantManager.GetCurrentTenantId();

        var servers = new List<McpServer>();

        if (status is null or ConnectionStatus.Enabled)
        {
            servers.AddRange(configMcpSource.Servers.Skip(offset).Take(count));
            offset = Math.Max(0, offset - configMcpSource.Servers.Count);
            count = Math.Max(0, count - servers.Count);
        }
        
        var totalTask = mcpDao.GetServersCountAsync(tenantId);

        if (count > 0)
        {
            var dbServers = await mcpDao.GetServersAsync(tenantId, status, offset, count)
                .Select(x => new McpServer
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Enabled = x.Enabled,
                    ServerType = ServerType.Custom
                }).ToListAsync();
            
            servers.AddRange(dbServers);
        }
        
        var totalCount = await totalTask;
        
        return (servers, totalCount + configMcpSource.Servers.Count);
    }

    public async Task<List<McpServerOptions>> GetServersAsync(int roomId)
    {
        var room = await GetRoomAsync(roomId);

        if (!await fileSecurity.CanUseChatsAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
        
        var tenantId = tenantManager.GetCurrentTenantId();

        var servers = new List<McpServerOptions>();
        
        var roomServers = await mcpDao.GetRoomServersAsync(tenantId, roomId).ToListAsync();
        foreach (var roomServer in roomServers)
        {
            if (roomServer.Options == null)
            {
                var builder = configMcpSource.GetServerOptionsBuilder(roomServer.Id);
                if (builder == null)
                {
                    continue;
                }

                var server = builder.Build(serviceProvider);
                servers.Add(server);
                continue;
            }
            
            servers.Add(roomServer.Options);
        }
        
        return servers;
    }

    public async Task DeleteServersAsync(List<Guid> ids)
    {
        await ThrowIfNotAccessAsync();

        await mcpDao.DeleteServersAsync(tenantManager.GetCurrentTenantId(), ids);
    }

    public async Task<List<McpServerOptions>> AddServersToRoomAsync(int roomId, List<Guid> ids)
    {
        var room = await GetRoomAsync(roomId);

        if (!await fileSecurity.CanEditRoomAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var tenantId = tenantManager.GetCurrentTenantId();
        
        var servers = ids.Select(configMcpSource.GetServerOptionsBuilder)
            .OfType<IMcpServerOptionsBuilder>()
            .Select(builder => builder.Build(serviceProvider))
            .ToList();

        var dbServers = await mcpDao.GetServersAsync(tenantId, ids);
        servers.AddRange(dbServers);
        
        if (servers.Count == 0)
        {
            throw new ItemNotFoundException("MCP Servers not found");
        }
        
        await using (await distributedLockProvider.TryAcquireFairLockAsync($"mcp_room_{roomId}"))
        {
            var currentServersCount = await mcpDao.GetRoomServersCountAsync(tenantId, roomId);
            if (currentServersCount + servers.Count > MaxMcpServersByRoom)
            {
                throw new ArgumentOutOfRangeException($"Maximum number of servers per room is {MaxMcpServersByRoom}");
            }
            
            await mcpDao.AddRoomServersAsync(tenantId, roomId, ids);
        }

        return servers;
    }
    
    public async Task DeleteServersFromRoomAsync(int roomId, List<Guid> ids)
    {
        var room = await GetRoomAsync(roomId);

        if (!await fileSecurity.CanEditRoomAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
        
        await using (await distributedLockProvider.TryAcquireFairLockAsync($"mcp_room_{roomId}"))
        {
            await mcpDao.DeleteServersFromRoomAsync(tenantManager.GetCurrentTenantId(), roomId, ids);
        }
    }

    public async Task<IReadOnlyDictionary<string, bool>> SetToolsSettingsAsync(int roomId, Guid serverId, List<string> disabledTools)
    {
        var room = await GetRoomAsync(roomId);

        if (!await fileSecurity.CanUseChatsAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
        
        var server = await GetServerAsync(roomId, serverId);

        var tools = disabledTools.Where(x => !string.IsNullOrEmpty(x)).ToHashSet();

        var settings = await mcpDao.SetToolsSettingsAsync(
            tenantManager.GetCurrentTenantId(), roomId, authContext.CurrentAccount.ID, serverId, tools);
        
        return await GetToolsAsync(server, settings);
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetToolsAsync(int roomId, Guid serverId)
    {
        var room = await GetRoomAsync(roomId);

        if (!await fileSecurity.CanUseChatsAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
        
        var server = await GetServerAsync(roomId, serverId);
        
        var settings = await mcpDao.GetToolsSettings(
            tenantManager.GetCurrentTenantId(), roomId, authContext.CurrentAccount.ID, serverId);
        
        return await GetToolsAsync(server, settings);
    }
    
    public async Task<ToolHolder> GetToolsAsync(int roomId)
    {
        var httpClient = httpClientFactory.CreateClient();

        var servers = await GetServersAsync(roomId);
        
        var settingsMap = await mcpDao.GetToolsSettings(
            tenantManager.GetCurrentTenantId(), roomId, authContext.CurrentAccount.ID, servers.Select(data => data.Id));
        
        var tasks = servers.Select(data => 
            ConnectAsync(data, settingsMap.GetValueOrDefault(data.Id), httpClient));

        var clients = new List<IMcpClient>();
        var tools = new List<AITool>();
        
        var result = await Task.WhenAll(tasks);
        foreach (var item in result)
        {
            if (item == null)
            {
                continue;
            }

            clients.Add(item.Client);
            tools.AddRange(item.Tools);
        }
        
        return new ToolHolder(clients, tools);
    }
    
    private async Task<McpServerOptions> GetServerAsync(int roomId, Guid serverId)
    {
        McpServerOptions? server;
        
        var roomServer = await mcpDao.GetRoomServerAsync(tenantManager.GetCurrentTenantId(), roomId, serverId);
        if (roomServer == null)
        {
            throw new ItemNotFoundException("MCP Server not found");
        }
        
        if (roomServer.Options != null)
        {
            server = roomServer.Options;
        }
        else
        {
            var builder = configMcpSource.GetServerOptionsBuilder(serverId);
            server = builder?.Build(serviceProvider);
        }
        
        if (server == null)
        {
            throw new ItemNotFoundException("MCP Server not found");
        }

        return server;
    }
    
    private async Task<IReadOnlyDictionary<string, bool>> GetToolsAsync(McpServerOptions server, McpSettings? settings)
    {
        var transport = new SseClientTransport(server.ToTransportOptions(), httpClientFactory.CreateClient());

        await using var mcpClient = await McpClientFactory.CreateAsync(transport);
        
        var tools = await mcpClient.ListToolsAsync();
        
        return settings?.Tools == null 
            ? tools.ToDictionary(t => t.Name, _ => true) 
            : tools.ToDictionary(t => t.Name, t => !settings.Tools.Excluded.Contains(t.Name));
    }
    
    private async Task ThrowIfNotAccessAsync()
    {
        if (!await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException("Access denied");
        }
    }
    
    private async Task ThrowIfNotConnectAsync(SseClientTransport transport)
    {
        try
        {
            await using var client = await McpClientFactory.CreateAsync(transport);
            await client.PingAsync();
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            throw new InvalidOperationException("Unable to connect to the mcp server");
        }
    }
    
    private async Task<Folder<int>> GetRoomAsync(int roomId)
    {
        var room = await folderDao.GetFolderAsync(roomId);
        if (room == null)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        return room;
    }

    private async Task<McpContainer?> ConnectAsync(
        McpServerOptions options, McpSettings? settings, HttpClient httpClient)
    {
        var transportOptions = new SseClientTransportOptions
        {
            Name = options.Name,
            Endpoint = options.Endpoint,
            AdditionalHeaders = options.Headers,
            TransportMode = HttpTransportMode.AutoDetect,
            ConnectionTimeout = TimeSpan.FromSeconds(5)
        };
        var transport = new SseClientTransport(transportOptions, httpClient);
        
        try
        {
            var mcpClient = await McpClientFactory.CreateAsync(transport);

            var tools = await mcpClient.ListToolsAsync();
            
            return settings?.Tools == null ? 
                new McpContainer(mcpClient, tools) : 
                new McpContainer(mcpClient, tools.Where(t => !settings.Tools.Excluded.Contains(t.Name)).ToList());
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            return null;
        }
    }

    private class McpContainer(IMcpClient client, IEnumerable<AITool> tools)
    {
        public IMcpClient Client { get; } = client;
        public IEnumerable<AITool> Tools { get; } = tools;
    }
}