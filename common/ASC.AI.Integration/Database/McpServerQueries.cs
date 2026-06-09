// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

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
