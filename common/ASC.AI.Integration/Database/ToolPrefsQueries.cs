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
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbToolPrefs> GetAllToolPrefsAsync(int tenantId)
    {
        return ToolPrefsQueriesContainer.GetAllToolPrefsAsync(this, tenantId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<string> GetExistingToolPrefsServerTypesAsync(int tenantId, IEnumerable<string> serverTypes)
    {
        return ToolPrefsQueriesContainer.GetExistingToolPrefsServerTypesAsync(this, tenantId, serverTypes);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteToolPrefsByServerTypeAsync(int tenantId, string serverType)
    {
        return ToolPrefsQueriesContainer.DeleteToolPrefsByServerTypeAsync(this, tenantId, serverType);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteToolPrefsByServerTypesAsync(int tenantId, IEnumerable<string> serverTypes)
    {
        return ToolPrefsQueriesContainer.DeleteToolPrefsByServerTypesAsync(this, tenantId, serverTypes);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public Task<int> DeleteAllToolPrefsAsync(int tenantId)
    {
        return ToolPrefsQueriesContainer.DeleteAllToolPrefsAsync(this, tenantId);
    }
}

static file class ToolPrefsQueriesContainer
{
    public static readonly Func<AiIntegrationContext, int, IAsyncEnumerable<DbToolPrefs>> GetAllToolPrefsAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId) =>
                ctx.ToolPrefs
                    .Where(x => x.TenantId == tenantId)
                    .OrderBy(x => x.ServerType)
                    .AsQueryable());

    public static readonly Func<AiIntegrationContext, int, IEnumerable<string>, IAsyncEnumerable<string>> GetExistingToolPrefsServerTypesAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, IEnumerable<string> serverTypes) =>
                ctx.ToolPrefs
                    .Where(x => x.TenantId == tenantId && serverTypes.Contains(x.ServerType))
                    .Select(x => x.ServerType));

    public static readonly Func<AiIntegrationContext, int, string, Task<int>> DeleteToolPrefsByServerTypeAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, string serverType) =>
                ctx.ToolPrefs
                    .Where(x => x.TenantId == tenantId && x.ServerType == serverType)
                    .ExecuteDelete());

    public static readonly Func<AiIntegrationContext, int, IEnumerable<string>, Task<int>> DeleteToolPrefsByServerTypesAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, IEnumerable<string> serverTypes) =>
                ctx.ToolPrefs
                    .Where(x => x.TenantId == tenantId && serverTypes.Contains(x.ServerType))
                    .ExecuteDelete());

    public static readonly Func<AiIntegrationContext, int, Task<int>> DeleteAllToolPrefsAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId) =>
                ctx.ToolPrefs
                    .Where(x => x.TenantId == tenantId)
                    .ExecuteDelete());
}
