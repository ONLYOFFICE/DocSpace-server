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
    public IAsyncEnumerable<DbToolPreference> GetAllToolPrefsAsync(int tenantId, Guid createdBy)
    {
        return ToolPrefsQueriesContainer.GetAllToolPrefsAsync(this, tenantId, createdBy);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbToolPreference> GetAllToolPrefsByEntryAsync(int tenantId, Guid createdBy, int entryId)
    {
        return ToolPrefsQueriesContainer.GetAllToolPrefsByEntryAsync(this, tenantId, createdBy, entryId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbToolPreference> GetToolPrefsByServerTypesAsync(int tenantId, Guid createdBy, IEnumerable<string> serverTypes)
    {
        return ToolPrefsQueriesContainer.GetToolPrefsByServerTypesAsync(this, tenantId, createdBy, serverTypes);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbToolPreference> GetToolPrefsByServerTypesAndEntryAsync(int tenantId, Guid createdBy, int entryId, IEnumerable<string> serverTypes)
    {
        return ToolPrefsQueriesContainer.GetToolPrefsByServerTypesAndEntryAsync(this, tenantId, createdBy, entryId, serverTypes);
    }

    [PreCompileQuery]
    public Task<int> DeleteToolPrefsByServerTypeAsync(int tenantId, string serverType)
    {
        return ToolPrefsQueriesContainer.DeleteToolPrefsByServerTypeAsync(this, tenantId, serverType);
    }

    [PreCompileQuery]
    public Task<int> DeleteToolPrefsByServerTypeAndEntryAsync(int tenantId, string serverType, int entryId)
    {
        return ToolPrefsQueriesContainer.DeleteToolPrefsByServerTypeAndEntryAsync(this, tenantId, serverType, entryId);
    }

    [PreCompileQuery]
    public Task<int> ClearToolPrefsDisabledAsync(int tenantId, Guid createdBy)
    {
        return ToolPrefsQueriesContainer.ClearToolPrefsDisabledAsync(this, tenantId, createdBy);
    }

    [PreCompileQuery]
    public Task<int> ClearToolPrefsAllowAlwaysAsync(int tenantId, Guid createdBy)
    {
        return ToolPrefsQueriesContainer.ClearToolPrefsAllowAlwaysAsync(this, tenantId, createdBy);
    }

    [PreCompileQuery]
    public Task<int> DeleteEmptyToolPrefsAsync(int tenantId, Guid createdBy)
    {
        return ToolPrefsQueriesContainer.DeleteEmptyToolPrefsAsync(this, tenantId, createdBy);
    }
}

static file class ToolPrefsQueriesContainer
{
    public static readonly Func<AiIntegrationContext, int, Guid, IAsyncEnumerable<DbToolPreference>> GetAllToolPrefsAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid createdBy) =>
                ctx.ToolPrefs
                    .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy && x.EntryId == null)
                    .OrderBy(x => x.ServerType)
                    .AsQueryable());

    public static readonly Func<AiIntegrationContext, int, Guid, int, IAsyncEnumerable<DbToolPreference>> GetAllToolPrefsByEntryAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid createdBy, int entryId) =>
                ctx.ToolPrefs
                    .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy && x.EntryId == entryId)
                    .OrderBy(x => x.ServerType)
                    .AsQueryable());

    public static readonly Func<AiIntegrationContext, int, Guid, IEnumerable<string>, IAsyncEnumerable<DbToolPreference>> GetToolPrefsByServerTypesAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid createdBy, IEnumerable<string> serverTypes) =>
                ctx.ToolPrefs
                    .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy && x.EntryId == null && serverTypes.Contains(x.ServerType)));

    public static readonly Func<AiIntegrationContext, int, Guid, int, IEnumerable<string>, IAsyncEnumerable<DbToolPreference>> GetToolPrefsByServerTypesAndEntryAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid createdBy, int entryId, IEnumerable<string> serverTypes) =>
                ctx.ToolPrefs
                    .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy && x.EntryId == entryId && serverTypes.Contains(x.ServerType)));

    public static readonly Func<AiIntegrationContext, int, string, Task<int>> DeleteToolPrefsByServerTypeAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, string serverType) =>
                ctx.ToolPrefs
                    .Where(x => x.TenantId == tenantId && x.ServerType == serverType && x.EntryId == null)
                    .ExecuteDelete());

    public static readonly Func<AiIntegrationContext, int, string, int, Task<int>> DeleteToolPrefsByServerTypeAndEntryAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, string serverType, int entryId) =>
                ctx.ToolPrefs
                    .Where(x => x.TenantId == tenantId && x.ServerType == serverType && x.EntryId == entryId)
                    .ExecuteDelete());

    public static readonly Func<AiIntegrationContext, int, Guid, Task<int>> ClearToolPrefsDisabledAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid createdBy) =>
                ctx.ToolPrefs
                    .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy && x.Disabled != null)
                    .ExecuteUpdate(s => s.SetProperty(x => x.Disabled, (HashSet<string>?)null)));

    public static readonly Func<AiIntegrationContext, int, Guid, Task<int>> ClearToolPrefsAllowAlwaysAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid createdBy) =>
                ctx.ToolPrefs
                    .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy && x.AllowAlways != null)
                    .ExecuteUpdate(s => s.SetProperty(x => x.AllowAlways, (HashSet<string>?)null)));

    public static readonly Func<AiIntegrationContext, int, Guid, Task<int>> DeleteEmptyToolPrefsAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid createdBy) =>
                ctx.ToolPrefs
                    .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy && x.Disabled == null && x.AllowAlways == null)
                    .ExecuteDelete());
}
