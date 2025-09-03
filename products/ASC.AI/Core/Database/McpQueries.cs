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

namespace ASC.AI.Core.Database;

public partial class AiDbContext
{
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<DbMcpServer?> GetServerAsync(int tenantId, Guid id)
    {
        return McpQueries.GetServerAsync(this, tenantId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<DbMcpServerUnit> GetServersAsync(int tenantId, IEnumerable<Guid> ids)
    {
        return McpQueries.GetServersByIdsAsync(this, tenantId, ids);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> GetRoomServersCountAsync(int tenantId, int roomId)
    {
        return McpQueries.GetRoomServersCount(this, tenantId, roomId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultGuid])]
    public Task<DbRoomServerUnit?> GetRoomServerAsync(int tenantId, int roomId, Guid userId, Guid serverId)
    {
        return McpQueries.GetRoomServerAsync(this, tenantId, roomId, userId, serverId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public IAsyncEnumerable<DbRoomServerUnit> GetRoomServersAsync(int tenantId, int roomId, Guid userId)
    {
        return McpQueries.GetRoomServersAsync(this, tenantId, roomId, userId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteServersAsync(int tenantId, IEnumerable<Guid> ids)
    {
        return McpQueries.DeleteServersAsync(this, tenantId, ids);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteSettingsAsync(int tenantId, IEnumerable<Guid> ids)
    {
        return McpQueries.DeleteSettingsAsync(this, tenantId, ids);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteSettingsAsync(int tenantId, int roomId, IEnumerable<Guid> ids)
    {
        return McpQueries.DeleteSettingsByRoomAsync(this, tenantId, roomId, ids);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteRoomServersAsync(int tenantId, IEnumerable<Guid> ids)
    {
        return McpQueries.DeleteRoomServersAsync(this, tenantId, ids);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteRoomServersAsync(int tenantId, int roomId, IEnumerable<Guid> ids)
    {
        return McpQueries.DeleteRoomServersByRoomAsync(this, tenantId, roomId, ids);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteServersStatesAsync(int tenantId, IEnumerable<Guid> ids)
    {
        return McpQueries.DeleteServersStatesAsync(this, tenantId, ids);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbMcpServerUnit> GetServersAsync(int tenantId, int offset, int count)
    {
        return McpQueries.GetServersAsync(this, tenantId, offset, count);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public Task<int> GetServersCountAsync(int tenantId)
    {
        return McpQueries.GetServersCountAsync(this, tenantId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbMcpServerUnit> GetActiveServersAsync(int tenantId, int offset, int count)
    {
        return McpQueries.GetActiveServersAsync(this, tenantId, offset, count);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public Task<int> GetActiveServersTotalCountAsync(int tenantId)
    {
        return McpQueries.GetActiveServersTotalCountAsync(this, tenantId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<DbMcpServerState?> GetServerStateAsync(int tenantId, Guid id)
    {
        return McpQueries.GetServerStateAsync(this, tenantId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<DbMcpServerState> GetServersStatesAsync(int tenantId, IEnumerable<Guid> ids)
    {
        return McpQueries.GetServersStatesAsync(this, tenantId, ids);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultGuid, null])]
    public Task<int> UpdateOauthCredentials(int tenantId, int roomId, Guid userId, Guid serverId, string token)
    {
        return McpQueries.UpdateOauthCredentials(this, tenantId, roomId, userId, serverId, token);
    }
}

static file class McpQueries
{
    public static readonly Func<AiDbContext, int, Guid, Task<DbMcpServer?>> GetServerAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, Guid id) =>
            ctx.McpServers.FirstOrDefault(x => x.TenantId == tenantId && x.Id == id));

    public static readonly Func<AiDbContext, int, int, Guid, Guid, Task<DbRoomServerUnit?>> GetRoomServerAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, int roomId, Guid userId, Guid serverId) =>
            ctx.RoomMcpServers
                .Where(x => x.TenantId == tenantId && x.RoomId == roomId && x.ServerId == serverId)
                .Select(x =>
                    new DbRoomServerUnit
                    {
                        ServerId = x.ServerId,
                        TenantId = x.TenantId,
                        RoomId = x.RoomId,
                        Server = ctx.McpServers.FirstOrDefault(y => y.TenantId == tenantId && y.Id == x.ServerId),
                        Settings = ctx.RoomMcpServerSettings.FirstOrDefault(y =>
                            y.TenantId == tenantId && y.RoomId == x.RoomId && y.UserId == userId && y.ServerId == x.ServerId)
                    })
                .FirstOrDefault());

    public static readonly Func<AiDbContext, int, IEnumerable<Guid>, IAsyncEnumerable<DbMcpServerUnit>> GetServersByIdsAsync = 
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, IEnumerable<Guid> ids) => 
            ctx.McpServers
                .Where(x => x.TenantId == tenantId && ids.Contains(x.Id))
                .GroupJoin(
                    ctx.McpServerStates,
                    server => new { TenantId = tenantId, server.Id },
                    state => new { TenantId = tenantId, Id = state.ServerId },
                    (server, states) => new { server, states })
                .SelectMany(
                    x => x.states.DefaultIfEmpty(),
                    (x, state) =>
                        new DbMcpServerUnit { Server = x.server, State = state })
        );

    public static readonly Func<AiDbContext, int, int, int, IAsyncEnumerable<DbMcpServerUnit>> GetServersAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, int offset, int count) =>
            ctx.McpServers
                .GroupJoin(
                    ctx.McpServerStates,
                    server => new { TenantId = tenantId, server.Id },
                    state => new { TenantId = tenantId, Id = state.ServerId },
                    (server, states) => new { server, states })
                .SelectMany(
                    x => x.states.DefaultIfEmpty(),
                    (x, state) =>
                        new DbMcpServerUnit { Server = x.server, State = state })
                .OrderBy(x => x.Server.Id)
                .Skip(offset)
                .Take(count)
        );

    public static readonly Func<AiDbContext, int, Task<int>> GetServersCountAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId) =>
            ctx.McpServers.Count(x => x.TenantId == tenantId));

    public static readonly Func<AiDbContext, int, int, int, IAsyncEnumerable<DbMcpServerUnit>> GetActiveServersAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, int offset, int count) =>
            ctx.McpServers
                .Join(
                    ctx.McpServerStates,
                    server => new { TenantId = tenantId, server.Id },
                    state => new { TenantId = tenantId, Id = state.ServerId },
                    (server, state) =>
                        new DbMcpServerUnit { Server = server, State = state })
                .OrderBy(x => x.Server.Id)
                .Skip(offset)
                .Take(count)
        );

    public static readonly Func<AiDbContext, int, Task<int>> GetActiveServersTotalCountAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId) =>
            ctx.McpServers
                .Join(
                    ctx.McpServerStates,
                    server => new { TenantId = tenantId, server.Id },
                    state => new { TenantId = tenantId, Id = state.ServerId },
                    (server, state) => server)
                .Count());

    public static readonly Func<AiDbContext, int, int, Task<int>> GetRoomServersCount =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, int roomId) =>
            ctx.RoomMcpServers
                .Count(x => x.TenantId == tenantId && x.RoomId == roomId));

    public static readonly Func<AiDbContext, int, int, Guid, IAsyncEnumerable<DbRoomServerUnit>> GetRoomServersAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, int roomId, Guid userId) =>
            ctx.RoomMcpServers.Where(x => x.TenantId == tenantId && x.RoomId == roomId)
                .GroupJoin(
                    ctx.McpServers,
                    m => new { tenantId = m.TenantId, id = m.ServerId },
                    s => new { tenantId = s.TenantId, id = s.Id },
                    (m, group) => new { map = m, servers = group })
                .SelectMany(
                    x => x.servers.DefaultIfEmpty(),
                    (x, s) => new { x.map, server = s })
                .GroupJoin(
                    ctx.RoomMcpServerSettings,
                    x => new { tenantId = x.map.TenantId, roomId = x.map.RoomId, userId, id = x.map.ServerId },
                    s => new { tenantId = s.TenantId, roomId = s.RoomId, userId, id = s.ServerId },
                    (x, group) => new { x.map, x.server, settings = group })
                .SelectMany(
                    x => x.settings.DefaultIfEmpty(),
                    (x, s) => new { x.map, x.server, settings = s })
                .Select(x =>
                    new DbRoomServerUnit
                    {
                        ServerId = x.map.ServerId,
                        TenantId = x.map.TenantId,
                        RoomId = x.map.RoomId,
                        Server = x.server, 
                        Settings = x.settings
                    }));

    public static readonly Func<AiDbContext, int, IEnumerable<Guid>, Task<int>> DeleteServersAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, IEnumerable<Guid> ids) =>
            ctx.McpServers
                .Where(x => x.TenantId == tenantId && ids.Contains(x.Id))
                .ExecuteDelete());

    public static readonly Func<AiDbContext, int, IEnumerable<Guid>, Task<int>> DeleteSettingsAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, IEnumerable<Guid> id) =>
            ctx.RoomMcpServerSettings
                .Where(x => x.TenantId == tenantId && id.Contains(x.ServerId))
                .ExecuteDelete());

    public static readonly Func<AiDbContext, int, int, IEnumerable<Guid>, Task<int>> DeleteSettingsByRoomAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, int roomId, IEnumerable<Guid> id) =>
            ctx.RoomMcpServerSettings
                .Where(x => x.TenantId == tenantId && x.RoomId == roomId && id.Contains(x.ServerId))
                .ExecuteDelete());


    public static readonly Func<AiDbContext, int, IEnumerable<Guid>, Task<int>> DeleteRoomServersAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, IEnumerable<Guid> ids) =>
            ctx.RoomMcpServers
                .Where(x => x.TenantId == tenantId && ids.Contains(x.ServerId))
                .ExecuteDelete());

    public static readonly Func<AiDbContext, int, int, IEnumerable<Guid>, Task<int>> DeleteRoomServersByRoomAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, int roomId, IEnumerable<Guid> ids) =>
            ctx.RoomMcpServers
                .Where(x => x.TenantId == tenantId && x.RoomId == roomId && ids.Contains(x.ServerId))
                .ExecuteDelete());

    public static readonly Func<AiDbContext, int, IEnumerable<Guid>, Task<int>> DeleteServersStatesAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, IEnumerable<Guid> id) =>
            ctx.McpServerStates
                .Where(x => x.TenantId == tenantId && id.Contains(x.ServerId))
                .ExecuteDelete());

    public static readonly Func<AiDbContext, int, Guid, Task<DbMcpServerState?>> GetServerStateAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, Guid id) =>
            ctx.McpServerStates
                .FirstOrDefault(x => x.TenantId == tenantId && x.ServerId == id));

    public static readonly Func<AiDbContext, int, IEnumerable<Guid>, IAsyncEnumerable<DbMcpServerState>> GetServersStatesAsync =
            EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, IEnumerable<Guid> id) =>
                ctx.McpServerStates
                    .Where(x => x.TenantId == tenantId && id.Contains(x.ServerId)));

    public static readonly Func<AiDbContext, int, int, Guid, Guid, string, Task<int>> UpdateOauthCredentials =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, int roomId, Guid userId, Guid serverId, string token) => 
            ctx.RoomMcpServerSettings
                .Where(x => x.TenantId == tenantId && x.RoomId == roomId && x.UserId == userId && x.ServerId == serverId)
                .ExecuteUpdate(x => 
                    x.SetProperty(y => y.OauthCredentials, token)));
}

public class DbRoomServerUnit
{
    public Guid ServerId { get; init; }
    public int TenantId { get; init; }
    public int RoomId { get; init; }
    public DbMcpServer? Server { get; init; }
    public SystemMcpServer? SystemServer { get; set; }
    public DbMcpServerSettings? Settings { get; init; }
}

public class DbMcpServerUnit
{
    public required DbMcpServer Server { get; init; }
    public DbMcpServerState? State { get; init; }
}