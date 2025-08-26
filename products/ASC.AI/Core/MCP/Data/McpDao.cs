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

using ASC.FederatedLogin;

namespace ASC.AI.Core.MCP.Data;

[Scope]
public class McpDao(IDbContextFactory<AiDbContext> dbContextFactory, InstanceCrypto crypto, IMapper mapper)
{
    public async Task<McpServer> AddServerAsync(
        int tenantId, 
        string endpoint, 
        string name, 
        Dictionary<string, string>? headers,
        string description)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        
        var server = new DbMcpServer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Endpoint = endpoint,
            Description = description
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
            Enabled = true
        };
    }
    
    public async Task<McpServer?> GetServerAsync(int tenantId, Guid id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var server = await dbContext.GetServerAsync(tenantId, id);
        if (server == null)
        {
            return null;
        }

        return await server.ToMcpServerAsync(crypto);
    }
    
    public async Task<List<McpServer>> GetServersAsync(int tenantId, List<Guid> ids)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.GetServersAsync(tenantId, ids)
            .SelectAwait(async x => await x.ToMcpServerAsync(crypto))
            .ToListAsync();
    }

    public async IAsyncEnumerable<McpServer> GetServersAsync(int tenantId, int offset, int count)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var query = dbContext.McpServers.Where(x => x.TenantId == tenantId);
        
        await foreach (var server in query.Skip(offset).Take(count).ToAsyncEnumerable())
        {
            yield return await server.ToMcpServerAsync(crypto);
        }
    }

    public async IAsyncEnumerable<McpServerState> GetServersStatesAsync(int tenantId, IEnumerable<Guid> serversIds)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var query = dbContext.McpServerStates
            .Where(x => x.TenantId == tenantId && serversIds.Contains(x.ServerId));

        await foreach (var state in query.ToAsyncEnumerable())
        {
            yield return mapper.Map<DbMcpServerState, McpServerState>(state);
        }
    }

    public async IAsyncEnumerable<McpServerStateUnion> GetStateServersAsync(int tenantId, int offset, int count)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var query = dbContext.McpServerStates
            .Where(x => x.TenantId == tenantId && x.Enabled)
            .GroupJoin(
                dbContext.McpServers,
                state => state.ServerId,
                server => server.Id,
                (state, servers) => new { state, servers })
            .SelectMany(
                x => x.servers.DefaultIfEmpty(),
                (x, server) => new McpServerStateUnion
                {
                    State = x.state,
                    Server = server
                });
        
        await foreach (var server in query.Skip(offset).Take(count).ToAsyncEnumerable())
        {
            yield return server;
        }
    }

    public class McpServerStateUnion
    {
        public required DbMcpServerState State { get; init; }
        public DbMcpServer? Server { get; init; }
    }
    
    public async Task<int> GetServersCountAsync(int tenantId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.GetServersCountAsync(tenantId);
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

    public async Task<McpServer> UpdateServerAsync(McpServer options)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        var server = await dbContext.GetServerAsync(options.TenantId, options.Id);
        if (server == null)
        {
            return options;
        }
        
        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            
            server.Name = options.Name;
            server.Endpoint = options.Endpoint;
            server.Description = options.Description;
        
            if (options.Headers is { Count: > 0 })
            {
                var headersJson = JsonSerializer.Serialize(options.Headers);
                server.Headers = await crypto.EncryptAsync(headersJson);
            }
            else
            {
                server.Headers = null;
            }
            
            context.McpServers.Update(server);
            await context.SaveChangesAsync();
        });

        return options;
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
            
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }
    
    public async Task AddRoomServersAsync(int tenantId, int roomId, IEnumerable<Guid> ids)
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

    public async Task<McpRoomServer?> GetRoomServerAsync(int tenantId, int roomId, Guid serverId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var result = await dbContext.GetRoomServerAsync(tenantId, roomId, serverId);
        if (result == null)
        {
            return null;
        }
        
        var options = result.Options == null 
            ? null 
            : await result.Options.ToMcpServerAsync(crypto);
        
        var settings = result.Settings == null ?
            null :
            await result.Settings.ToMcpServerSettingsAsync(crypto);
        
        return new McpRoomServer { Id = result.ServerId, Server = options, Settings = settings };
    }
    
    public async IAsyncEnumerable<McpRoomServer> GetRoomServersAsync(int tenantId, int roomId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await foreach (var item in dbContext.GetRoomServersAsync(tenantId, roomId))
        {
            var roomServer = new McpRoomServer { Id = item.ServerId };
            
            if (item.Options != null)
            { 
                roomServer.Server = await item.Options.ToMcpServerAsync(crypto);
            }

            if (item.Settings != null)
            { 
                roomServer.Settings = await item.Settings.ToMcpServerSettingsAsync(crypto);
            }
            
            yield return roomServer;
        }
    }
    
    public async Task<int> GetRoomServersCountAsync(int tenantId, int roomId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.GetRoomServersCountAsync(tenantId, roomId);
    }
    
    public async Task DeleteServersFromRoomAsync(int tenantId, int roomId, List<Guid> serversIds)
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

    public async Task<McpServerSettings> SaveSettingsAsync(
        int tenantId, 
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

            if (mcpServerSettings.OauthCredential != null)
            {
                dbSettings.OauthCredential = await crypto.EncryptAsync(mcpServerSettings.OauthCredential.ToJson());
            }
            
            await context.RoomMcpServerSettings.AddOrUpdateAsync(dbSettings);
            await context.SaveChangesAsync();
        });
        
        return mcpServerSettings;
    }

    public async Task<McpServerSettings?> GetServerSettingsAsync(int tenantId, int roomId, Guid userId, Guid serverId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var dbSettings = await dbContext.RoomMcpServerSettings.Where(x =>
                x.TenantId == tenantId &&
                x.RoomId == roomId &&
                x.UserId == userId &&
                x.ServerId == serverId)
            .Include(dbMcpSettings => dbMcpSettings.ToolsConfiguration)
            .FirstOrDefaultAsync();

        if (dbSettings == null)
        {
            return null;
        }

        return await dbSettings.ToMcpServerSettingsAsync(crypto);
    }

    public async IAsyncEnumerable<McpServerSettings> GetServersSettings(int tenantId, int roomId, Guid userId, IEnumerable<Guid> serversIds)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var query = dbContext.RoomMcpServerSettings
            .Where(x => 
                x.TenantId == tenantId &&
                x.RoomId == roomId &&
                x.UserId == userId && 
                serversIds.Contains(x.ServerId))
            .Include(dbMcpSettings => dbMcpSettings.ToolsConfiguration);

        await foreach (var dbSettings in query.AsAsyncEnumerable())
        {
            yield return await dbSettings.ToMcpServerSettingsAsync(crypto);
        }
    }
}