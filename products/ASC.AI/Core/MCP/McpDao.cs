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
public class McpDao(
    IDbContextFactory<AiDbContext> dbContextFactory,
    SystemMcpConfig systemMcpConfig,
    InstanceCrypto crypto,
    ConsumerFactory consumerFactory,
    McpIconStore iconStore)
{
    public async Task<McpServer> AddServerAsync(
        int tenantId, 
        string endpoint, 
        string name, 
        Dictionary<string, string>? headers,
        string description,
        ConnectionType connectionType,
        string? iconBase64)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        
        var server = new DbMcpServer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Endpoint = endpoint,
            Description = description,
            ConnectionType = connectionType,
            HasIcon = !string.IsNullOrEmpty(iconBase64),
            ModifiedOn = DateTime.UtcNow
        };

        if (headers is { Count: > 0 })
        {
            var headersJson = JsonSerializer.Serialize(headers);
            server.Headers = await crypto.EncryptAsync(headersJson);
        }

        var state = new DbMcpServerState
        {
            TenantId = tenantId,
            ServerId = server.Id,
            Enabled = true
        };

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            var transaction = await context.Database.BeginTransactionAsync();
            
            await context.McpServers.AddAsync(server);
            await context.McpServerStates.AddAsync(state);
            
            if (server.HasIcon)
            {
                await iconStore.SaveAsync(tenantId, server.Id, iconBase64!);
            }
            
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        return new McpServer
        {
            Id = server.Id,
            TenantId = server.TenantId,
            Name = server.Name,
            Description = server.Description,
            Endpoint = server.Endpoint,
            Headers = headers,
            Enabled = true,
            HasIcon = server.HasIcon,
            Icon = server.HasIcon ? await iconStore.GetAsync(tenantId, server.Id, server.ModifiedOn) : null,
            ModifiedOn = server.ModifiedOn
        };
    }
    
    public async Task<McpServer?> GetServerAsync(int tenantId, Guid id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        if (systemMcpConfig.Servers.TryGetValue(id, out var systemServer))
        {
            var state = await dbContext.GetServerStateAsync(tenantId, id);
            
            return systemServer.ToMcpServer(tenantId, state);
        }
        
        var server = await dbContext.GetServerAsync(tenantId, id);
        if (server == null)
        {
            return null;
        }

        return await server.ToMcpServerAsync(crypto, iconStore);
    }
    
    public async Task<List<McpServer>> GetServersAsync(int tenantId, HashSet<Guid> ids)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var servers = new List<McpServer>();

        var foundedSystemServers = systemMcpConfig.Servers.Values
            .Where(x => ids.Contains(x.Id))
            .ToList();
        
        if (foundedSystemServers.Count > 0)
        {
            var states = await dbContext.GetServersStatesAsync(
                    tenantId, 
                    foundedSystemServers.Select(x => x.Id))
                .ToDictionaryAsync(x => x.ServerId);

            var systemServers =
                foundedSystemServers.Select(x => x.ToMcpServer(tenantId, states.GetValueOrDefault(x.Id)));
            
            servers.AddRange(systemServers);
        }

        var dbServers = await dbContext.GetServersAsync(tenantId, ids)
            .SelectAwait(async x => await x.ToMcpServerAsync(crypto, iconStore))
            .ToListAsync();
        
        servers.AddRange(dbServers);
        
        return servers;
    }
    
    public async Task<(List<McpServer> servers, int total)> GetActiveServersAsync(int tenantId, int offset, int count)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var servers = new List<McpServer>();
        
        var dbTotalCount = await dbContext.GetActiveServersTotalCountAsync(tenantId);
        
        var dbServers = await dbContext.GetActiveServersAsync(tenantId, offset, count)
            .SelectAwait(async x => await x.ToMcpServerAsync(crypto, iconStore))
            .ToListAsync();
        
        servers.AddRange(dbServers);
        
        offset = Math.Max(0, offset - dbTotalCount);
        count = Math.Max(0, count - dbServers.Count);
        
        var systemServers = new List<McpServer>();
        
        if (systemMcpConfig.Servers.Count > 0)
        {
            var states = await dbContext.GetServersStatesAsync(
                tenantId,
                systemMcpConfig.Servers.Keys
            ).ToDictionaryAsync(x => x.ServerId);
            
            foreach (var systemServer in systemMcpConfig.Servers.Values)
            {
                var state = states.GetValueOrDefault(systemServer.Id);
                if (state is { Enabled: false })
                {
                    continue;
                }
                    
                systemServers.Add(systemServer.ToMcpServer(tenantId, state));
            }
            
            if (count > 0)
            {
                servers.AddRange(systemServers.Skip(offset).Take(count));
            }
        }
        
        var total = dbTotalCount + systemServers.Count;
        
        return (servers, total);
    }
    
    public async Task<(List<McpServer> servers, int total)> GetServersAsync(int tenantId, int offset, int count)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var servers = new List<McpServer>();
        
        var dbTotalCount = await dbContext.GetServersCountAsync(tenantId);
        
        var dbServers = await dbContext.GetServersAsync(tenantId, offset, count)
            .SelectAwait(async x => await x.ToMcpServerAsync(crypto, iconStore))
            .ToListAsync();
        
        servers.AddRange(dbServers);
        
        offset = Math.Max(0, offset - dbTotalCount);
        count = Math.Max(0, count - dbServers.Count);
        
        if (count > 0)
        {
            var filteredSystemServers = systemMcpConfig.Servers.Values.Skip(offset).Take(count).ToList();

            if (filteredSystemServers.Count > 0)
            {
                var states = await dbContext.GetServersStatesAsync(
                    tenantId, 
                    filteredSystemServers.Select(x => x.Id)
                ).ToDictionaryAsync(x => x.ServerId);

                var systemServers = filteredSystemServers.Select(x => 
                    x.ToMcpServer(tenantId, states.GetValueOrDefault(x.Id))).ToList();
                
                servers.AddRange(systemServers);
            }
        }
        
        var total = dbTotalCount + systemMcpConfig.Servers.Count;
        
        return (servers, total);
    }

    public async Task SetServerStateAsync(int tenantId, Guid id, bool enabled)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            var transaction = await context.Database.BeginTransactionAsync();
            
            var state = new DbMcpServerState
            {
                TenantId = tenantId,
                ServerId = id,
                Enabled = enabled
            };

            await context.McpServerStates.AddOrUpdateAsync(state);

            if (!enabled)
            {
                await context.DeleteSettingsAsync(tenantId, [id]);
                await context.DeleteRoomServersAsync(tenantId, [id]);
            }
            
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    public async Task<McpServer> UpdateServerAsync(McpServer server, bool updateIcon, string? iconBase64)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        var serverUnit = await dbContext.GetServerAsync(server.TenantId, server.Id);
        if (serverUnit == null)
        {
            return server;
        }

        var dbServer = serverUnit.Server;
        
        server.ModifiedOn = DateTime.UtcNow;

        if (updateIcon)
        {
            switch (server.HasIcon)
            {
                case true when !string.IsNullOrEmpty(iconBase64):
                    await iconStore.SaveAsync(server.TenantId, server.Id, iconBase64);
                    break;
                case false:
                    await iconStore.DeleteAsync(dbServer.TenantId, dbServer.Id);
                    server.Icon = null;
                    break;
            }
        }
        
        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            
            dbServer.Name = server.Name;
            dbServer.Endpoint = server.Endpoint;
            dbServer.Description = server.Description;
            dbServer.HasIcon = server.HasIcon;
            dbServer.ModifiedOn = server.ModifiedOn;
        
            if (server.Headers is { Count: > 0 })
            {
                var headersJson = JsonSerializer.Serialize(server.Headers);
                dbServer.Headers = await crypto.EncryptAsync(headersJson);
            }
            else
            {
                dbServer.Headers = null;
            }
            
            context.McpServers.Update(dbServer);
            
            await context.SaveChangesAsync();
        });

        if (server.HasIcon)
        {
            server.Icon = await iconStore.GetAsync(dbServer.TenantId, dbServer.Id, server.ModifiedOn);
        }

        return server;
    }
    
    public async Task DeleteServersAsync(int tenantId, List<Guid> ids)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        
        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            var transaction = await context.Database.BeginTransactionAsync();
            
            await context.DeleteServersAsync(tenantId, ids);
            await context.DeleteRoomServersAsync(tenantId, ids);
            await context.DeleteSettingsAsync(tenantId, ids);
            await context.DeleteServersStatesAsync(tenantId, ids);
            
            foreach (var id in ids)
            {
                await iconStore.DeleteAsync(tenantId, id);
            }
            
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }
    
    public async Task AddServersConnectionsAsync(int tenantId, int roomId, IEnumerable<Guid> ids)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        var maps = ids.Select(x => 
            new DbRoomMcpServer
            {
                TenantId = tenantId, 
                RoomId = roomId, 
                ServerId = x
            });
        
        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await context.AddRangeAsync(maps);
            await context.SaveChangesAsync();
        });
    }

    public async Task<McpServerConnection?> GetMcpConnectionAsync(int tenantId, int roomId, Guid userId, Guid serverId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var item = await dbContext.GetRoomServerAsync(tenantId, roomId, userId, serverId);
        if (item == null)
        {
            return null;
        }

        if (item.Server == null)
        {
            if (!systemMcpConfig.Servers.TryGetValue(item.ServerId, out var systemServer))
            {
                return null;
            }
                
            item.SystemServer = systemServer;
        }
            
        return await item.ToMcpRoomServerAsync(crypto, consumerFactory, iconStore);
    }
    
    public async IAsyncEnumerable<McpServerConnection> GetServerConnectionAsync(int tenantId, int roomId, Guid userId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await foreach (var item in dbContext.GetRoomServersAsync(tenantId, roomId, userId))
        {
            if (item.Server == null)
            {
                if (!systemMcpConfig.Servers.TryGetValue(item.ServerId, out var systemServer))
                {
                    continue;
                }
                
                item.SystemServer = systemServer;
            }
            
            yield return await item.ToMcpRoomServerAsync(crypto, consumerFactory, iconStore);
        }
    }
    
    public async Task<int> GetServersConnectionsCountAsync(int tenantId, int roomId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.GetRoomServersCountAsync(tenantId, roomId);
    }
    
    public async Task DeleteServersConnectionsAsync(int tenantId, int roomId, List<Guid> serversIds)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        
        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await context.DeleteRoomServersAsync(tenantId, roomId, serversIds);
            await context.DeleteSettingsAsync(tenantId, roomId, serversIds);
            await context.SaveChangesAsync();
        });
    }

    public async Task SaveSettingsAsync(int tenantId,
        int roomId,
        Guid userId,
        Guid serverId,
        McpServerSettings mcpServerSettings)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        
        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            
            var dbSettings = new DbMcpServerSettings
            {
                TenantId = tenantId, 
                RoomId = roomId,
                UserId = userId,
                ServerId = serverId,
                ToolsConfiguration = mcpServerSettings.ToolsConfiguration
            };

            if (mcpServerSettings.OauthCredentials != null)
            {
                dbSettings.OauthCredentials = await crypto.EncryptAsync(mcpServerSettings.OauthCredentials.ToJson());
            }
            
            await context.RoomMcpServerSettings.AddOrUpdateAsync(dbSettings);
            await context.SaveChangesAsync();
        });
    }

    public async Task UpdateOauthCredentialsAsync(int tenantId, int roomId, Guid userId, Guid serverId, OAuth20Token token)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            
            var tokenJson = token.ToJson();
            var encryptedToken = await crypto.EncryptAsync(tokenJson);
            
            await context.UpdateOauthCredentialsAsync(tenantId, roomId, userId, serverId, encryptedToken);
        });
    }

    public async Task<McpServerShort?> GetServerByNameAsync(int tenantId, string name)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        return await dbContext.GetServerByNameAsync(tenantId, name);
    }
    
    public async IAsyncEnumerable<McpIconState> GetIconStatesAsync(int tenantId, IEnumerable<Guid> ids)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        await foreach (var iconState in dbContext.GetIconStatesAsync(tenantId, ids))
        {
            yield return iconState;
        }
    }
}