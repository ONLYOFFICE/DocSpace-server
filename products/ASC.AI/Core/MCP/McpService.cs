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

using System.Text.RegularExpressions;

namespace ASC.AI.Core.MCP;

[Scope]
public partial class McpService(
    TenantManager tenantManager,
    AuthContext authContext,
    McpDao mcpDao, 
    IFolderDao<int> folderDao,
    FileSecurity fileSecurity,
    IHttpClientFactory httpClientFactory,
    ILogger<McpService> logger,
    UserManager userManager,
    IDistributedLockProvider distributedLockProvider,
    ClientTransportFactory clientTransportFactory,
    OAuth20TokenHelper oauthTokenHelper,
    IToolPermissionProvider toolPermissionProvider,
    SystemMcpConfig systemMcpConfig,
    SocketManager socketManager)
{
    private const int MaxMcpServersByRoom = 5;
    private static readonly Regex _serverNameRegex = ServerNameRegex();
    
    public async Task<McpServer> AddCustomServerAsync(
        string endpoint, 
        string name,
        string description,
        Dictionary<string, string>? headers,
        string? iconBase64)
    {
        await ThrowIfNotAccessAsync();
        
        ArgumentException.ThrowIfNullOrEmpty(endpoint);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(description);

        var tenantId = tenantManager.GetCurrentTenantId();
        
        await ThrowIfServerNameNotValid(tenantId, name);

        var options = new HttpClientTransportOptions
        {
            Name = name,
            Endpoint = new Uri(endpoint),
            AdditionalHeaders = headers,
            TransportMode = HttpTransportMode.AutoDetect,
            ConnectionTimeout = TimeSpan.FromSeconds(30)
        };
        
        var transport = new HttpClientTransport(options, httpClientFactory.CreateClient());
        
        await ThrowIfNotConnectAsync(transport);
        
        return await mcpDao.AddServerAsync(tenantId, endpoint, name, headers, description, ConnectionType.Direct, iconBase64);
    }

    public async Task<McpServer> UpdateCustomServerAsync(
        Guid serverId, 
        string? url, 
        string? name, 
        Dictionary<string, string>? headers, 
        string? description,
        bool updateIcon,
        string? iconBase64)
    {
        await ThrowIfNotAccessAsync();
        
        var tenantId = tenantManager.GetCurrentTenantId();

        var server = await mcpDao.GetServerAsync(tenantId, serverId);
        if (server == null)
        {
            throw new ItemNotFoundException("MCP Server not found");
        }

        var needConnect = false;
        
        if (!string.IsNullOrEmpty(name) && name != server.Name)
        {
            await ThrowIfServerNameNotValid(tenantId, name, server.Id);
            
            server.Name = name;
        }

        if (!string.IsNullOrEmpty(url))
        {
            var uri = new Uri(url);
            server.Endpoint = uri.ToString();
            needConnect = true;
        }

        if (headers != null)
        {
            server.Headers = headers.Count > 0 ? headers : null;
            needConnect = true;
        }

        if (updateIcon)
        {
            server.HasIcon = iconBase64 != null;
        }

        if (!string.IsNullOrEmpty(description))
        {
            server.Description = description;
        }

        if (!needConnect)
        {
            return await mcpDao.UpdateServerAsync(server, updateIcon, iconBase64);
        }

        var options = new HttpClientTransportOptions
        {
            Name = server.Name,
            Endpoint = new Uri(server.Endpoint),
            AdditionalHeaders = server.Headers,
            TransportMode = HttpTransportMode.AutoDetect,
            ConnectionTimeout = TimeSpan.FromSeconds(30)
        };
            
        var transport = new HttpClientTransport(options, httpClientFactory.CreateClient());
            
        await ThrowIfNotConnectAsync(transport);

        return await mcpDao.UpdateServerAsync(server, updateIcon, iconBase64);
    }
    
    public async Task<McpServer> SetServerStateAsync(Guid serverId, bool enabled)
    {
        await ThrowIfNotAccessAsync();

        var tenantId = tenantManager.GetCurrentTenantId();

        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetLockKey(tenantId)))
        {
            var server = await mcpDao.GetServerAsync(tenantId, serverId);
            if (server == null)
            {
                throw new ItemNotFoundException("MCP Server not found");
            }
        
            await mcpDao.SetServerStateAsync(tenantId, serverId, enabled);
        
            server.Enabled = enabled;

            return server;
        }
    }
    
    public async Task<(List<McpServer> servers, int totalCount)> GetAllServersAsync(int offset, int count)
    {
        await ThrowIfNotAccessAsync();
        
        var tenantId = tenantManager.GetCurrentTenantId();
        
        return await mcpDao.GetServersAsync(tenantId, offset, count);
    }
    
    public async Task<McpServer> GetServerAsync(Guid id)
    {
        await ThrowIfNotAccessAsync();
        
        var tenantId = tenantManager.GetCurrentTenantId();
        
        return await mcpDao.GetServerAsync(tenantId, id) ?? throw new ItemNotFoundException("MCP Server not found");
    }

    public async Task<(List<McpServer> servers, int totalCount)> GetActiveServersAsync(int offset, int count)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        return await mcpDao.GetActiveServersAsync(tenantId, offset, count);
    }

    public async Task DeleteServersAsync(List<Guid> ids)
    {
        await ThrowIfNotAccessAsync();
        
        await mcpDao.DeleteServersAsync(tenantManager.GetCurrentTenantId(), ids);
    }
    
    public async Task<List<McpServerStatus>> AddServersToRoomAsync(int roomId, HashSet<Guid> ids)
    {
        var room = await GetRoomAsync(roomId);
        if (!await fileSecurity.CanEditRoomAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        var notify = false;

        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetLockKey(tenantId)))
        {
            var servers = await mcpDao.GetServersAsync(tenantId, ids);
            if (servers.Count > 0)
            {
                var serversToAdd = servers.Where(x => x.Enabled).Select(x => x.Id).ToList();
        
                var currentServersCount = await mcpDao.GetServersConnectionsCountAsync(tenantId, roomId);
                if (currentServersCount + serversToAdd.Count > MaxMcpServersByRoom)
                {
                    throw new ArgumentOutOfRangeException($"Maximum number of servers per room is {MaxMcpServersByRoom}");
                }
            
                await mcpDao.AddServersConnectionsAsync(tenantId, roomId, serversToAdd);
                notify = true;
            }
        }

        if (notify)
        {
            await socketManager.UpdateFolderAsync(room);
        }
        
        return await GetServerStatusesAsync(roomId, tenantId);
    }

    public async Task DeleteServersFromRoomAsync(int roomId, List<Guid> ids)
    {
        var room = await GetRoomAsync(roomId);
        if (!await fileSecurity.CanEditRoomAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
        
        var tenantId = tenantManager.GetCurrentTenantId();
        
        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetLockKey(tenantId)))
        {
            await mcpDao.DeleteServersConnectionsAsync(tenantId, roomId, ids);
        }
        
        await socketManager.UpdateFolderAsync(room);
    }

    public async Task<List<McpServerStatus>> GetServersStatusesAsync(int roomId)
    {
        await ThrowIfNotAccessUseMcpAsync(roomId);

        var tenantId = tenantManager.GetCurrentTenantId();

        return await GetServerStatusesAsync(roomId, tenantId);
    }

    private async Task<List<McpServerStatus>> GetServerStatusesAsync(int roomId, int tenantId)
    {
        var userId = authContext.CurrentAccount.ID;
        
        var connections = await mcpDao.GetServerConnectionAsync(tenantId, roomId, userId).ToListAsync();

        return connections.Select(connection => connection.ToMcpServerStatus()).ToList();
    }

    public async Task<McpServerStatus> ConnectServerAsync(int roomId, Guid serverId, string code)
    {
        await ThrowIfNotAccessUseMcpAsync(roomId);
        
        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;
        
        var connection = await mcpDao.GetMcpConnectionAsync(tenantId, roomId, userId, serverId);
        if (connection == null)
        {
            throw new ItemNotFoundException("Mcp server not found");
        }
        
        var token = oauthTokenHelper.GetAccessToken(connection.OauthProvider, code);
        if (token == null)
        {
            throw new ArgumentException("Invalid code");
        }

        if (connection.Settings == null)
        {
            connection.Settings = new McpServerSettings
            {
                OauthCredentials = token
            };
        }
        else
        {
            connection.Settings.OauthCredentials = token;
        }

        var transport = await clientTransportFactory.CreateAsync(connection);

        try
        {
            await using var client = await McpClient.CreateAsync(transport);
            await client.PingAsync();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Failed to connect to server", e);
        }

        await mcpDao.SaveSettingsAsync(tenantId, roomId, authContext.CurrentAccount.ID, serverId, connection.Settings);
        
        return connection.ToMcpServerStatus();
    }

    public async Task<McpServerStatus> DisconnectServerAsync(int roomId, Guid serverId)
    {
        await ThrowIfNotAccessUseMcpAsync(roomId);
        
        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;
        
        var connection = await mcpDao.GetMcpConnectionAsync(tenantId, roomId, userId, serverId);
        if (connection == null)
        {
            throw new ItemNotFoundException("Mcp server not found");
        }

        if (connection.Settings?.OauthCredentials == null)
        {
            return connection.ToMcpServerStatus();
        }
        
        connection.Settings.OauthCredentials = null;
        
        await mcpDao.SaveSettingsAsync(tenantId, roomId, authContext.CurrentAccount.ID, serverId, connection.Settings);
        
        return connection.ToMcpServerStatus();
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetToolsAsync(int roomId, Guid serverId)
    {
        await ThrowIfNotAccessUseMcpAsync(roomId);
        
        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;
        
        var connection = await mcpDao.GetMcpConnectionAsync(tenantId, roomId, userId, serverId);
        if (connection == null)
        {
            throw new ItemNotFoundException("Mcp server not found");
        }

        return await GetToolsAsync(connection);
    }
    
    public async Task<IReadOnlyDictionary<string, bool>> SetToolsAsync(int roomId, Guid serverId, List<string> disabledTools)
    {
        await ThrowIfNotAccessUseMcpAsync(roomId);
        
        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;
        
        var connection = await mcpDao.GetMcpConnectionAsync(tenantId, roomId, userId, serverId);
        if (connection == null)
        {
            throw new ItemNotFoundException("Mcp server not found");
        }

        var settings = connection.Settings;
        
        var excludedTools = disabledTools.Where(x => !string.IsNullOrEmpty(x)).ToHashSet();
        
        if (settings == null)
        {
            settings = new McpServerSettings
            {
                ToolsConfiguration = new ToolsConfiguration
                {
                    Excluded = excludedTools,
                    Allowed = []
                }
            };
        }
        else
        {
            if (settings.ToolsConfiguration == null)
            {
                settings.ToolsConfiguration = new ToolsConfiguration
                {
                    Excluded = excludedTools,
                    Allowed = []
                };
            }
            else
            {
                settings.ToolsConfiguration.Excluded = excludedTools;
            }
        }
        
        await mcpDao.SaveSettingsAsync(tenantId, roomId, userId, serverId, settings);
        
        return await GetToolsAsync(connection);
    }
    
    public async Task<ToolHolder> GetToolsAsync(int roomId)
    {
        await ThrowIfNotAccessUseMcpAsync(roomId);
        
        var tenantId = tenantManager.GetCurrentTenantId();

        var connections = mcpDao.GetServerConnectionAsync(tenantId, roomId, authContext.CurrentAccount.ID)
            .Where(x => x.Connected);
        
        var tasks = await connections.Select(ConnectAsync).ToListAsync();
        
        var holder = new ToolHolder();
        
        var result = await Task.WhenAll(tasks);
        foreach (var item in result)
        {
            if (item == null)
            {
                continue;
            }
    
            holder.AddMcpTool(item.Client, item.Tools);
        }
        
        return holder;
    }

    public async Task ProvideMcpToolPermissionAsync(string callId, ToolExecutionDecision decision)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        
        var callData = await toolPermissionProvider.ProvidePermissionAsync(callId, decision);
        if (callData == null || decision is not ToolExecutionDecision.AlwaysAllow)
        {
            return;
        }
        
        var userId = authContext.CurrentAccount.ID;

        var connection = await mcpDao.GetMcpConnectionAsync(tenantId, callData.RoomId, userId, callData.ServerId);
        if (connection == null)
        {
            return;
        }
        
        var settings = connection.Settings;

        if (settings == null)
        {
            settings = new McpServerSettings
            {
                ToolsConfiguration = new ToolsConfiguration { Excluded = [], Allowed = [callData.Name] }
            };
        }
        else if (settings.ToolsConfiguration == null)
        {
            settings.ToolsConfiguration = new ToolsConfiguration { Excluded = [], Allowed = [callData.Name] };
        }
        else
        {
            settings.ToolsConfiguration.Allowed.Add(callData.Name);
        }
        
        await mcpDao.SaveSettingsAsync(tenantId, callData.RoomId, userId, callData.ServerId, settings);
    }

    public IAsyncEnumerable<McpIconState> GetIconStatesAsync(IEnumerable<Guid> servers)
    {
        return mcpDao.GetIconStatesAsync(tenantManager.GetCurrentTenantId(), servers);
    }
    
    private async Task<IReadOnlyDictionary<string, bool>> GetToolsAsync(McpServerConnection connection)
    {
        var transport = await clientTransportFactory.CreateAsync(connection);

        await using var mcpClient = await McpClient.CreateAsync(transport);
        
        var tools = await mcpClient.ListToolsAsync();
        
        if (connection.Settings?.ToolsConfiguration == null)
        {
            return tools.ToDictionary(t => t.Name, _ => true);
        }
        
        var excludedTools = connection.Settings.ToolsConfiguration.Excluded;
        
        return tools.ToDictionary(t => t.Name, t => !excludedTools.Contains(t.Name));
    }
    
    private async Task<Folder<int>> GetRoomAsync(int roomId)
    {
        var room = await folderDao.GetFolderAsync(roomId);
        return room ?? throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
    }
    
    private async Task ThrowIfNotAccessAsync()
    {
        if (!await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException("Access denied");
        }
    }
    
    private async Task ThrowIfNotAccessUseMcpAsync(int roomId)
    {
        var room = await GetRoomAsync(roomId);

        if (!await fileSecurity.CanUseChatAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
    }
    
    private async Task ThrowIfNotConnectAsync(HttpClientTransport transport)
    {
        try
        {
            await using var client = await McpClient.CreateAsync(transport);
            await client.PingAsync();
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            throw new InvalidOperationException("Unable to connect to the mcp server");
        }
    }
    
    private async Task<McpContainer?> ConnectAsync(McpServerConnection connection)
    {
        var transport = await clientTransportFactory.CreateAsync(connection);
        
        try
        {
            var mcpClient = await McpClient.CreateAsync(transport);

            var tools = await mcpClient.ListToolsAsync();

            var wrappers = new List<ToolWrapper>(tools.Count);
            var configuration = connection.Settings?.ToolsConfiguration;
            
            foreach (var tool in tools)
            {
                if (configuration?.Excluded.Contains(tool.Name) == true)
                {
                    continue;
                }

                var wrapper = new ToolWrapper
                {
                    Tool = tool,
                    Context = new ToolContext
                    {
                        McpServerInfo = new McpServerInfo
                        {
                            ServerId = connection.ServerId,
                            ServerName = connection.Name,
                            ServerType = connection.ServerType,
                            Icon = connection.Icon
                        },
                        Name = tool.Name,
                        RoomId = connection.RoomId,
                        AutoInvoke = configuration?.Allowed.Contains(tool.Name) == true
                    }
                };
                
                wrappers.Add(wrapper);
            }
            
            return new McpContainer(mcpClient, wrappers);
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            return null;
        }
    }
    
    private class McpContainer(McpClient client, IEnumerable<ToolWrapper> tools)
    {
        public McpClient Client { get; } = client;
        public IEnumerable<ToolWrapper> Tools { get; } = tools;
    }

    private string GetLockKey(int tenantId)
    {
        return $"mcp_{tenantId}";
    }
    
    [GeneratedRegex("^[a-zA-Z0-9_-]+$")]
    private static partial Regex ServerNameRegex();

    private async Task ThrowIfServerNameNotValid(int tenantId, string name, Guid serverId = default)
    {
        if (!_serverNameRegex.IsMatch(name))
        {
            throw new ArgumentException("Invalid MCP server name. Only letters, numbers, underscores, and hyphens are allowed.");
        }

        if (systemMcpConfig.ReservedServerNames.Contains(name))
        {
            throw new ArgumentException("MCP server name is not allowed.");
        }

        var server = await mcpDao.GetServerByNameAsync(tenantId, name);
        if (server != null && server.Id != serverId)
        {
            throw new ArgumentException("MCP server name is already exists.");
        }
    }
}