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

[Scope]
public class McpDao(IDbContextFactory<AiDbContext> dbContextFactory, InstanceCrypto crypto)
{
    public async Task<McpServerOptions> AddServerAsync(int tenantId, string endpoint, string name, Dictionary<string, string>? headers,
        string? description)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        
        var server = new DbMcpServerOptions
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Endpoint = endpoint,
            Description = description,
            Enabled = true
        };

        if (headers is { Count: > 0 })
        {
            var headersJson = JsonSerializer.Serialize(headers);
            server.Headers = await crypto.EncryptAsync(headersJson);
        }

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await context.McpServers.AddAsync(server);
            await context.SaveChangesAsync();
        });

        return new McpServerOptions
        {
            Id = server.Id,
            TenantId = server.TenantId,
            Name = server.Name,
            Endpoint = new Uri(server.Endpoint),
            Headers = headers
        };
    }
    
    public async Task<McpServerOptions?> GetServerAsync(int tenantId, Guid id)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var server = await dbContext.GetServerAsync(tenantId, id);

        if (server == null)
        {
            return null;
        }

        return await server.ToMcpServerOptions(crypto);
    }
    
    public async Task<List<McpServerOptions>> GetServersAsync(int tenantId, List<Guid> ids)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.GetServersAsync(tenantId, ids)
            .SelectAwait(async x => await x.ToMcpServerOptions(crypto))
            .ToListAsync();
    }

    public async IAsyncEnumerable<McpServerOptions> GetServersAsync(int tenantId, ConnectionStatus? status, int offset, int count)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var query = dbContext.McpServers.Where(x => x.TenantId == tenantId);
        if (status.HasValue)
        {
            query = query.Where(x => x.Enabled == (status == ConnectionStatus.Enabled));
        }
        
        await foreach (var server in query.Skip(offset).Take(count).ToAsyncEnumerable())
        {
            yield return await server.ToMcpServerOptions(crypto);
        }
    }
    
    public async Task<int> GetServersCountAsync(int tenantId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.GetServersCountAsync(tenantId);
    }

    public async Task<McpServerOptions?> UpdateServerAsync(McpServerOptions options)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        var server = await dbContext.GetServerAsync(options.TenantId, options.Id);
        if (server == null)
        {
            return null;
        }
        
        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            var transaction = await context.Database.BeginTransactionAsync();
            
            server.Name = options.Name;
            server.Endpoint = options.Endpoint.ToString();
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
            
            if (server.Enabled != options.Enabled && !options.Enabled)
            {
                server.Enabled = options.Enabled;
                
                await context.DeleteSettingsAsync(options.TenantId, [options.Id]);
                await context.DeleteRoomServersAsync(options.TenantId, [options.Id]);
            }
            
            context.McpServers.Update(server);
            await context.SaveChangesAsync();
            
            await transaction.CommitAsync();
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
            await context.DeleteSettingsAsync(tenantId, ids);
            await context.DeleteRoomServersAsync(tenantId, ids);
            
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
            : await result.Options.ToMcpServerOptions(crypto);
        
        return new McpRoomServer { Id = result.ServerId, Options = options };
    }
    
    public async IAsyncEnumerable<McpRoomServer> GetRoomServersAsync(int tenantId, int roomId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await foreach (var item in dbContext.GetRoomServersAsync(tenantId, roomId))
        {
            if (item.Options == null)
            {
                yield return new McpRoomServer
                {
                    Id = item.ServerId, 
                    Options = null
                };
            }
            else
            {
                yield return new McpRoomServer
                {
                    Id = item.ServerId, 
                    Options = await item.Options.ToMcpServerOptions(crypto)
                };
            }
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

    public async Task<McpSettings> SetToolsSettingsAsync(int tenantId, int roomId, Guid userId, Guid serverId, HashSet<string> disabledTools)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        
        var settings = await dbContext.McpSettings.FindAsync(tenantId, roomId, userId, serverId);

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();

            if (settings == null)
            {
                settings = new McpSettings
                {
                    TenantId = tenantId,
                    RoomId = roomId,
                    UserId = userId,
                    ServerId = serverId,
                    Tools = new Tools { Excluded = disabledTools },
                };
                
                await context.McpSettings.AddAsync(settings);
            }
            else if (disabledTools.Count > 0)
            {
                settings.Tools = new Tools { Excluded = disabledTools };
                context.McpSettings.Update(settings);
            }
            else
            {
                await context.McpSettings.Where(x => 
                        x.TenantId == tenantId && 
                        x.RoomId == roomId && 
                        x.UserId == userId && 
                        x.ServerId == serverId)
                    .ExecuteDeleteAsync();
                
                settings.Tools = new Tools { Excluded = [] };
            }

            await context.SaveChangesAsync();
        });

        return settings!;
    }
    
    public async Task<IReadOnlyDictionary<Guid, McpSettings>> GetToolsSettings(int tenantId, int roomId, Guid userId, 
        IEnumerable<Guid> serversIds)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.GetToolsSettings(tenantId, roomId, userId, serversIds)
            .ToDictionaryAsync(x => x.ServerId, x => x);
    }
    
    public async Task<McpSettings?> GetToolsSettings(int tenantId, int roomId, Guid userId, Guid serverId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.GetToolsSettings(tenantId, roomId, userId, [serverId]).FirstOrDefaultAsync();
    }
}