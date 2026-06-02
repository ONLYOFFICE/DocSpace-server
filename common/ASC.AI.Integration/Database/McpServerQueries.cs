// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.AI.Integration.Database;

public partial class AiIntegrationContext
{
    [PreCompileQuery]
    public Task<DbMcpServer?> GetMcpServerAsync(int tenantId, string name)
    {
        return McpServerQueriesContainer.GetMcpServerAsync(this, tenantId, name);
    }

    [PreCompileQuery]
    public Task<DbMcpServer?> GetMcpServerByEntryAsync(int tenantId, string name, int entryId)
    {
        return McpServerQueriesContainer.GetMcpServerByEntryAsync(this, tenantId, name, entryId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbMcpServer> GetAllMcpServersAsync(int tenantId)
    {
        return McpServerQueriesContainer.GetAllMcpServersAsync(this, tenantId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbMcpServer> GetAllMcpServersByEntryAsync(int tenantId, int entryId)
    {
        return McpServerQueriesContainer.GetAllMcpServersByEntryAsync(this, tenantId, entryId);
    }

    [PreCompileQuery]
    public Task<int> UpdateMcpServerConfigAsync(int tenantId, string name, string config)
    {
        return McpServerQueriesContainer.UpdateMcpServerConfigAsync(this, tenantId, name, config);
    }

    [PreCompileQuery]
    public Task<int> UpdateMcpServerConfigByEntryAsync(int tenantId, string name, int entryId, string config)
    {
        return McpServerQueriesContainer.UpdateMcpServerConfigByEntryAsync(this, tenantId, name, entryId, config);
    }

    [PreCompileQuery]
    public Task<int> DeleteMcpServerAsync(int tenantId, string name)
    {
        return McpServerQueriesContainer.DeleteMcpServerAsync(this, tenantId, name);
    }

    [PreCompileQuery]
    public Task<int> DeleteMcpServerByEntryAsync(int tenantId, string name, int entryId)
    {
        return McpServerQueriesContainer.DeleteMcpServerByEntryAsync(this, tenantId, name, entryId);
    }

    [PreCompileQuery]
    public Task<int> DeleteAllMcpServersAsync(int tenantId)
    {
        return McpServerQueriesContainer.DeleteAllMcpServersAsync(this, tenantId);
    }

    [PreCompileQuery]
    public Task<int> DeleteAllMcpServersByEntryAsync(int tenantId, int entryId)
    {
        return McpServerQueriesContainer.DeleteAllMcpServersByEntryAsync(this, tenantId, entryId);
    }
}

static file class McpServerQueriesContainer
{
    public static readonly Func<AiIntegrationContext, int, string, Task<DbMcpServer?>> GetMcpServerAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, string name) =>
                ctx.McpServers.FirstOrDefault(x => x.TenantId == tenantId && x.Name == name && x.EntryId == null));

    public static readonly Func<AiIntegrationContext, int, string, int, Task<DbMcpServer?>> GetMcpServerByEntryAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, string name, int entryId) =>
                ctx.McpServers.FirstOrDefault(x => x.TenantId == tenantId && x.Name == name && x.EntryId == entryId));

    public static readonly Func<AiIntegrationContext, int, IAsyncEnumerable<DbMcpServer>> GetAllMcpServersAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId) =>
                ctx.McpServers
                    .Where(x => x.TenantId == tenantId && x.EntryId == null)
                    .OrderBy(x => x.Name)
                    .AsQueryable());

    public static readonly Func<AiIntegrationContext, int, int, IAsyncEnumerable<DbMcpServer>> GetAllMcpServersByEntryAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, int entryId) =>
                ctx.McpServers
                    .Where(x => x.TenantId == tenantId && x.EntryId == entryId)
                    .OrderBy(x => x.Name)
                    .AsQueryable());

    public static readonly Func<AiIntegrationContext, int, string, string, Task<int>> UpdateMcpServerConfigAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, string name, string config) =>
                ctx.McpServers
                    .Where(x => x.TenantId == tenantId && x.Name == name && x.EntryId == null)
                    .ExecuteUpdate(x => x.SetProperty(y => y.Config, config)));

    public static readonly Func<AiIntegrationContext, int, string, int, string, Task<int>> UpdateMcpServerConfigByEntryAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, string name, int entryId, string config) =>
                ctx.McpServers
                    .Where(x => x.TenantId == tenantId && x.Name == name && x.EntryId == entryId)
                    .ExecuteUpdate(x => x.SetProperty(y => y.Config, config)));

    public static readonly Func<AiIntegrationContext, int, string, Task<int>> DeleteMcpServerAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, string name) =>
                ctx.McpServers
                    .Where(x => x.TenantId == tenantId && x.Name == name && x.EntryId == null)
                    .ExecuteDelete());

    public static readonly Func<AiIntegrationContext, int, string, int, Task<int>> DeleteMcpServerByEntryAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, string name, int entryId) =>
                ctx.McpServers
                    .Where(x => x.TenantId == tenantId && x.Name == name && x.EntryId == entryId)
                    .ExecuteDelete());

    public static readonly Func<AiIntegrationContext, int, Task<int>> DeleteAllMcpServersAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId) =>
                ctx.McpServers
                    .Where(x => x.TenantId == tenantId && x.EntryId == null)
                    .ExecuteDelete());

    public static readonly Func<AiIntegrationContext, int, int, Task<int>> DeleteAllMcpServersByEntryAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, int entryId) =>
                ctx.McpServers
                    .Where(x => x.TenantId == tenantId && x.EntryId == entryId)
                    .ExecuteDelete());
}
