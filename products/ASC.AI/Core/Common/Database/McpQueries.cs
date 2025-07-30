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

namespace ASC.AI.Core.Common.Database;

public partial class AiDbContext
{
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<DbMcpServerOptions?> GetServerAsync(int tenantId, Guid id)
    {
        return McpQueries.GetServerAsync(this, tenantId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbMcpServerOptions> GetServersAsync(int tenantId, int offset, int count)
    {
        return McpQueries.GetServersAsync(this, tenantId, offset, count);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public Task<int> GetServersCountAsync(int tenantId)
    {
        return McpQueries.GetServersCountAsync(this, tenantId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<DbMcpServerOptions> GetServersAsync(int tenantId, IEnumerable<Guid> ids)
    {
        return McpQueries.GetServersByIdsAsync(this, tenantId, ids);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<int> GetRoomServersCountAsync(int tenantId, int roomId)
    {
        return McpQueries.GetRoomServersCount(this, tenantId, roomId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<RoomServerQueryResult?> GetRoomServerAsync(int tenantId, int roomId, Guid id)
    {
        return McpQueries.GetRoomServerAsync(this, tenantId, roomId, id);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<RoomServerQueryResult> GetRoomServersAsync(int tenantId, int roomId)
    {
        return McpQueries.GetRoomServersAsync(this, tenantId, roomId);
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
}

static file class McpQueries
{
    public static readonly Func<AiDbContext, int, Guid, Task<DbMcpServerOptions?>> GetServerAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, Guid id) =>
            ctx.McpServers.FirstOrDefault(x => x.TenantId == tenantId && x.Id == id));

    public static readonly Func<AiDbContext, int, int, Guid, Task<RoomServerQueryResult?>> GetRoomServerAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, int roomId, Guid id) =>
            ctx.McpRoomServers
                .Where(x => x.TenantId == tenantId && x.RoomId == roomId && x.ServerId == id)
                .Select(x =>
                    new RoomServerQueryResult
                    {
                        ServerId = x.ServerId,
                        Options = ctx.McpServers.FirstOrDefault(y => y.TenantId == tenantId && y.Id == x.ServerId)
                    })
                .FirstOrDefault());
    
    public static readonly Func<AiDbContext, int, IEnumerable<Guid>, IAsyncEnumerable<DbMcpServerOptions>> GetServersByIdsAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, IEnumerable<Guid> ids) =>
            ctx.McpServers.Where(x => x.TenantId == tenantId && ids.Contains(x.Id)));
    
    public static readonly Func<AiDbContext, int, int, int, IAsyncEnumerable<DbMcpServerOptions>> GetServersAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int tenantId, int offset, int count) => 
                ctx.McpServers
                    .Where(x => x.TenantId == tenantId)
                    .Skip(offset)
                    .Take(count));

    public static readonly Func<AiDbContext, int, Task<int>> GetServersCountAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId) =>
            ctx.McpServers.Count(x => x.TenantId == tenantId));
    
    public static readonly Func<AiDbContext, int, int, Task<int>> GetRoomServersCount =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, int roomId) =>
            ctx.McpRoomServers
                .Count(x => x.TenantId == tenantId && x.RoomId == roomId));

    public static readonly Func<AiDbContext, int, int, IAsyncEnumerable<RoomServerQueryResult>> GetRoomServersAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, int roomId) =>
            ctx.McpRoomServers
                .GroupJoin(
                    ctx.McpServers,
                    m => new { tenantId = m.TenantId, id = m.ServerId },
                    s => new { tenantId = s.TenantId, id = s.Id },
                    (m, group) => new { map = m, servers = group })
                .SelectMany(
                    x => x.servers.DefaultIfEmpty(),
                    (x, s) => new { x.map, server = s })
                .Select(x => 
                    new RoomServerQueryResult 
                    { 
                        ServerId = x.map.ServerId, 
                        Options = x.server 
                    }));
    
    public static readonly Func<AiDbContext, int, IEnumerable<Guid>, Task<int>> DeleteServersAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, IEnumerable<Guid> ids) =>
            ctx.McpServers
                .Where(x => x.TenantId == tenantId && ids.Contains(x.Id))
                .ExecuteDelete());
    
    public static readonly Func<AiDbContext, int, IEnumerable<Guid>, Task<int>> DeleteSettingsAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, IEnumerable<Guid> id) =>
            ctx.McpSettings
                .Where(x => x.TenantId == tenantId && id.Contains(x.ServerId))
                .ExecuteDelete());
    
    public static readonly Func<AiDbContext, int, int, IEnumerable<Guid>, Task<int>> DeleteSettingsByRoomAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, int roomId, IEnumerable<Guid> id) =>
            ctx.McpSettings
                .Where(x => x.TenantId == tenantId && x.RoomId == roomId && id.Contains(x.ServerId))
                .ExecuteDelete());
    
    
    public static readonly Func<AiDbContext, int, IEnumerable<Guid>, Task<int>> DeleteRoomServersAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, IEnumerable<Guid> ids) =>
            ctx.McpRoomServers
                .Where(x => x.TenantId == tenantId && ids.Contains(x.ServerId))
                .ExecuteDelete());
    
    public static readonly Func<AiDbContext, int, int, IEnumerable<Guid>, Task<int>> DeleteRoomServersByRoomAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, int roomId, IEnumerable<Guid> ids) =>
            ctx.McpRoomServers
                .Where(x => x.TenantId == tenantId && x.RoomId == roomId && ids.Contains(x.ServerId))
                .ExecuteDelete());
}

public class RoomServerQueryResult
{
    public Guid ServerId { get; init; }
    public DbMcpServerOptions? Options { get; init; }
}