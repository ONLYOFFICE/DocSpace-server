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

using System.Web;

using ASC.AI.Core.MCP.Builder;
using ASC.Core.Common.Configuration;

namespace ASC.AI.Core.MCP;

[Scope]
public class McpService(
    TenantManager tenantManager,
    AuthContext authContext,
    McpDao mcpDao, 
    IFolderDao<int> folderDao,
    FileSecurity fileSecurity,
    IHttpClientFactory httpClientFactory,
    SystemMcpConfig systemMcpConfig,
    ILogger<McpService> logger,
    UserManager userManager,
    IDistributedLockProvider distributedLockProvider,
    ConsumerFactory consumerFactory,
    HttpTransportFactory httpTransportFactory)
{
    private const int MaxMcpServersByRoom = 5;
    
    public async Task<McpServer> AddCustomServerAsync(
        string endpoint, 
        string name,
        string description,
        Dictionary<string, string>? headers)
    {
        await ThrowIfNotAccessAsync();
        
        ArgumentException.ThrowIfNullOrEmpty(endpoint);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(description);

        if (headers is { Count: > 0 })
        {
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
        }
        
        return await mcpDao.AddServerAsync(tenantManager.GetCurrentTenantId(), endpoint, name, headers, description);
    }

    public async Task<McpServer> UpdateCustomServerAsync(
        Guid serverId, 
        string? url, 
        string? name, 
        Dictionary<string, string>? headers, 
        string? description, 
        bool? enabled)
    {
        await ThrowIfNotAccessAsync();

        var server = await mcpDao.GetServerAsync(tenantManager.GetCurrentTenantId(), serverId);
        if (server == null)
        {
            throw new ItemNotFoundException("MCP Server not found");
        }
        
        if (!string.IsNullOrEmpty(name))
        {
            server.Name = name;
        }

        if (!string.IsNullOrEmpty(url))
        {
            var uri = new Uri(url);
            server.Endpoint = uri.ToString();
        }

        if (headers != null)
        {
            server.Headers = headers.Count > 0 ? headers : null;
        }

        if (!string.IsNullOrEmpty(description))
        {
            server.Description = description;
        }

        if (enabled.HasValue)
        {
            server.Enabled = enabled.Value;
        }
        
        return await mcpDao.UpdateServerAsync(server);
    }
    
    public async Task<McpServer> SetServerStateAsync(Guid serverId, bool enabled)
    {
        await ThrowIfNotAccessAsync();

        var tenantId = tenantManager.GetCurrentTenantId();

        var server = await GetServerInternalAsync(tenantId, serverId);
        if (server == null)
        {
            throw new ItemNotFoundException("MCP Server not found");
        }
        
        await mcpDao.SetServerStateAsync(tenantId, serverId, enabled);
        server.Enabled = enabled;

        return server;
    }
    
    public async Task<(List<McpServer> servers, int totalCount)> GetServersAsync(int offset, int count)
    {
        await ThrowIfNotAccessAsync();
        
        var tenantId = tenantManager.GetCurrentTenantId();
        var servers = new List<McpServer>();

        var systemServers = new List<SystemMcpServer>();

        systemServers.AddRange(systemMcpConfig.Servers.Values.Skip(offset).Take(count));
        offset = Math.Max(0, offset - systemMcpConfig.Servers.Count);
        count = Math.Max(0, count - systemServers.Count);

        if (systemServers.Count > 0)
        {
            var states = await mcpDao.GetServersStatesAsync(
                    tenantId, 
                    systemServers.Select(x => x.Id)
                    ).ToDictionaryAsync(x => x.ServerId);

            foreach (var systemServer in systemServers)
            {
                var server = new McpServer
                {
                    Id = systemServer.Id,
                    Name = systemServer.Name,
                    Description = systemServer.Description,
                    Endpoint = systemServer.Endpoint,
                    Headers = systemServer.Headers,
                    Type = systemServer.Type,
                    ConnectionType = systemServer.ConnectionType,
                    Enabled = states.TryGetValue(systemServer.Id, out var state) && state.Enabled
                };
                
                servers.Add(server);
            }
        }
        
        var totalTask = mcpDao.GetServersCountAsync(tenantId);

        if (count > 0)
        {
            var dbServers = await mcpDao.GetServersAsync(tenantId, offset, count).ToListAsync();
            servers.AddRange(dbServers);
        }
        
        var totalCount = await totalTask;
        
        return (servers, totalCount + systemMcpConfig.Servers.Count);
    }

    public async Task DeleteServersAsync(List<Guid> ids)
    {
        await ThrowIfNotAccessAsync();
        
        await mcpDao.DeleteServersAsync(tenantManager.GetCurrentTenantId(), ids);
    }
    
    public async Task AddServersToRoomAsync(int roomId, List<Guid> ids)
    {
        var room = await GetRoomAsync(roomId);

        if (!await fileSecurity.CanEditRoomAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var idsToAdd = new List<Guid>();

        var tenantId = tenantManager.GetCurrentTenantId();

        var systemServersIds = systemMcpConfig.Servers
            .Where(x => ids.Contains(x.Key))
            .Select(x => x.Key)
            .ToList();

        if (systemServersIds.Count > 0)
        {
            var states = await mcpDao.GetServersStatesAsync(tenantId, systemServersIds)
                .ToDictionaryAsync(x => x.ServerId);

            foreach (var systemServer in systemServersIds)
            {
                if (states.TryGetValue(systemServer, out var state) && state.Enabled)
                {
                    idsToAdd.Add(systemServer);
                }
            }
        }

        var dbServers = await mcpDao.GetServersAsync(tenantId, ids);
        idsToAdd.AddRange(dbServers.Where(x => x.Enabled).Select(x => x.Id));

        if (ids.Count == 0)
        {
            throw new ItemNotFoundException("MCP Servers not found");
        }

        await using (await distributedLockProvider.TryAcquireFairLockAsync($"mcp_room_{roomId}"))
        {
            var currentServersCount = await mcpDao.GetRoomServersCountAsync(tenantId, roomId);
            if (currentServersCount + idsToAdd.Count > MaxMcpServersByRoom)
            {
                throw new ArgumentOutOfRangeException($"Maximum number of servers per room is {MaxMcpServersByRoom}");
            }
            
            await mcpDao.AddRoomServersAsync(tenantId, roomId, idsToAdd);
        }
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

    public async Task<List<McpRoomServerInfo>> GetServersAsync(int roomId)
    {
        var room = await GetRoomAsync(roomId);
        
        if (!await fileSecurity.CanUseChatsAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
        
        var tenantId = tenantManager.GetCurrentTenantId();
        var servers = new List<McpRoomServerInfo>();
        
        var roomServers = await mcpDao.GetRoomServersAsync(tenantId, roomId).ToListAsync();
        foreach (var roomServer in roomServers)
        {
            if (roomServer.Server == null)
            {
                var systemServer = systemMcpConfig.Servers.GetValueOrDefault(roomServer.Id);
                if (systemServer == null)
                {
                    continue;
                }

                var server = new McpRoomServerInfo
                {
                    Id = systemServer.Id,
                    Name = systemServer.Name,
                    ServerType = systemServer.Type,
                    Connected = systemServer.ConnectionType == ConnectionType.Direct || roomServer.Settings?.OauthCredential != null
                };

                if (systemServer.ConnectionType is ConnectionType.OAuth)
                {
                    var provider = systemServer.LoginProviderSelector?.Invoke(consumerFactory);
                    if (provider != null)
                    {
                        var builder = new UriBuilder(provider.CodeUrl);
                        
                        var queryString = HttpUtility.ParseQueryString(string.Empty);
                        queryString.Add("client_id", provider.ClientID);
                        queryString.Add("redirect_uri", provider.RedirectUri);
                        queryString.Add("response_type", "code");
                        
                        builder.Query = queryString.ToString();
                        
                        server.AuthorizationEndpoint = builder.ToString();
                    }
                }
                
                servers.Add(server);
            }
            else
            {
                var server = new McpRoomServerInfo
                {
                    Id = roomServer.Id, 
                    Name = roomServer.Server.Name,
                    Connected = true,
                    ServerType = roomServer.Server.Type
                };
                
                servers.Add(server);
            }
        }

        return servers;
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetToolsAsync(int roomId, Guid serverId)
    {
        var room = await GetRoomAsync(roomId);

        if (!await fileSecurity.CanUseChatsAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
        
        var executionOptions = await GetExecutionOptions(roomId, serverId);
        if (executionOptions == null)
        {
            throw new ItemNotFoundException("Mcp server not found");
        }

        return await GetToolsAsync(executionOptions);
    }
    
    public async Task<IReadOnlyDictionary<string, bool>> SetToolsAsync(int roomId, Guid serverId, List<string> disabledTools)
    {
        var room = await GetRoomAsync(roomId);

        if (!await fileSecurity.CanUseChatsAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
        
        var executionOptions = await GetExecutionOptions(roomId, serverId);
        if (executionOptions == null)
        {
            throw new ItemNotFoundException("Mcp server not found");
        }

        var settings = executionOptions.Settings;
        
        var excludedTools = disabledTools.Where(x => !string.IsNullOrEmpty(x)).ToHashSet();
        
        if (settings == null)
        {
            settings = new McpServerSettings
            {
                ToolsConfiguration = new ToolsConfiguration
                {
                    Excluded = excludedTools
                }
            };
        }
        else
        {
            if (settings.ToolsConfiguration == null)
            {
                settings.ToolsConfiguration = new ToolsConfiguration
                {
                    Excluded = excludedTools
                };
            }
            else
            {
                settings.ToolsConfiguration.Excluded = excludedTools;
            }
        }
        
        await mcpDao.SaveSettingsAsync(
            tenantManager.GetCurrentTenantId(), roomId, authContext.CurrentAccount.ID, serverId, settings);
        
        return await GetToolsAsync(executionOptions);
    }
    
    public async Task<ToolHolder> GetToolsAsync(int roomId)
    {
        var room = await GetRoomAsync(roomId);
        
        if (!await fileSecurity.CanUseChatsAsync(room))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
        
        var executionOptions = new List<McpExecutionOptions>();
        
        var tenantId = tenantManager.GetCurrentTenantId();
        
        await foreach (var roomServer in mcpDao.GetRoomServersAsync(tenantId, roomId))
        {
            if (roomServer.Server != null)
            {
                executionOptions.Add(new McpExecutionOptions
                {
                    Name = roomServer.Server.Name,
                    Endpoint = roomServer.Server.Endpoint,
                    Headers = roomServer.Server.Headers,
                    ServerType = roomServer.Server.Type,
                    ConnectionType = roomServer.Server.ConnectionType,
                    Settings = roomServer.Settings
                });
            }

            var systemServer = systemMcpConfig.Servers.GetValueOrDefault(roomServer.Id);
            if (systemServer == null)
            {
                continue;
            }
            
            executionOptions.Add(new McpExecutionOptions
            {
                Name = systemServer.Name,
                Endpoint = systemServer.Endpoint,
                Headers = systemServer.Headers,
                ServerType = systemServer.Type,
                ConnectionType = systemServer.ConnectionType,
                OauthProvider = systemServer.LoginProviderSelector?.Invoke(consumerFactory),
                Settings = roomServer.Settings
            });
        }
        
        var tasks = executionOptions.Select(ConnectAsync);
        
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
    
    private async Task<IReadOnlyDictionary<string, bool>> GetToolsAsync(McpExecutionOptions executionOptions)
    {
        var transport = await httpTransportFactory.CreateAsync(executionOptions);

        await using var mcpClient = await McpClientFactory.CreateAsync(transport);
        
        var tools = await mcpClient.ListToolsAsync();
        
        if (executionOptions.Settings?.ToolsConfiguration == null)
        {
            return tools.ToDictionary(t => t.Name, _ => true);
        }
            
        var excludedTools = executionOptions.Settings.ToolsConfiguration.Excluded;
        
        return tools.ToDictionary(t => t.Name, t => !excludedTools.Contains(t.Name));
    }
    
    private async Task<McpExecutionOptions?> GetExecutionOptions(int roomId, Guid serverId)
    {
        var roomServer = await mcpDao.GetRoomServerAsync(tenantManager.GetCurrentTenantId(), roomId, serverId);
        if (roomServer == null)
        {
            throw new ItemNotFoundException("MCP Server not found");
        }
        
        if (roomServer.Server != null)
        {
            return new McpExecutionOptions
            {
                Name = roomServer.Server.Name,
                Endpoint = roomServer.Server.Endpoint,
                Headers = roomServer.Server.Headers,
                ServerType = roomServer.Server.Type,
                ConnectionType = roomServer.Server.ConnectionType,
                Settings = roomServer.Settings
            };
        }

        var systemServer = systemMcpConfig.Servers.GetValueOrDefault(roomServer.Id);
        if (systemServer == null)
        {
            return null;
        }

        return new McpExecutionOptions
        {
            Name = systemServer.Name,
            Endpoint = systemServer.Endpoint,
            Headers = systemServer.Headers,
            ServerType = systemServer.Type,
            ConnectionType = systemServer.ConnectionType,
            OauthProvider = systemServer.LoginProviderSelector?.Invoke(consumerFactory),
            Settings = roomServer.Settings
        };
    }
    
    private async Task<Folder<int>> GetRoomAsync(int roomId)
    {
        var room = await folderDao.GetFolderAsync(roomId);
        return room ?? throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
    }
    
    private async Task<McpServer?> GetServerInternalAsync(int tenantId, Guid id)
    {
        if (systemMcpConfig.Servers.TryGetValue(id, out var systemServer))
        {
            return new McpServer
            {
                Id = systemServer.Id,
                Name = systemServer.Name,
                Description = systemServer.Description,
                Endpoint = systemServer.Endpoint,
                Headers = systemServer.Headers,
                Type = systemServer.Type,
                ConnectionType = systemServer.ConnectionType,
                Enabled = true
            };
        }
        
        var server = await mcpDao.GetServerAsync(tenantId, id);
        return server;
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
    
    private async Task<McpContainer?> ConnectAsync(McpExecutionOptions executionOptions)
    {
        var transport = await httpTransportFactory.CreateAsync(executionOptions);
        
        try
        {
            var mcpClient = await McpClientFactory.CreateAsync(transport);

            var tools = await mcpClient.ListToolsAsync();

            if (executionOptions.Settings?.ToolsConfiguration == null)
            {
                return new McpContainer(mcpClient, tools);
            }
            
            var excludedTools = executionOptions.Settings.ToolsConfiguration.Excluded;
            return new McpContainer(mcpClient, tools.Where(x => !excludedTools.Contains(x.Name)).ToList());
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