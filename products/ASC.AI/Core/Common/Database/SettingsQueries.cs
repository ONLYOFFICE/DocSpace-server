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
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, Scope.Chat])]
    public Task<DbAiSettings?> GetSettingsAsync(int tenantId, Guid userId, Scope scope)
    {
        return Queries.GetSettingsAsync(this, tenantId, userId, scope);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, Scope.Chat, null])]
    public Task<int> UpdateSettingsAsync(int tenantId, Guid userId, Scope scope, int providerId, RunParameters settings)
    {
        return Queries.UpdateSettingsAsync(this, tenantId, userId, scope, providerId, settings);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteSettingsAsync(int tenantId, IEnumerable<int> providersIds)
    {
        return Queries.DeleteSettingsAsync(this, tenantId, providersIds);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, Scope.Chat])]
    public Task<RunConfiguration?> GetRunConfigurationAsync(int tenantId, Guid userId, Scope scope)
    {
        return Queries.GetProviderSettingsAsync(this, tenantId, userId, scope);
    }
}

static file class Queries
{
    public static readonly Func<AiDbContext, int, Guid, Scope, Task<DbAiSettings?>> GetSettingsAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int tenantId, Guid userId, Scope scope) => 
                ctx.Settings
                    .FirstOrDefault(x => x.TenantId == tenantId && x.UserId == userId && x.Scope == scope));
    
    public static readonly Func<AiDbContext, int, Guid, Scope, int, RunParameters, Task<int>> UpdateSettingsAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int tenantId, Guid userId, Scope scope, int providerId, RunParameters settings) => 
                ctx.Settings
                    .Where(x => x.TenantId == tenantId && x.UserId == userId && x.Scope == scope)
                    .ExecuteUpdate(x => 
                        x.SetProperty(y => y.Parameters, settings)
                            .SetProperty(y => y.ProviderId, providerId)));

    public static readonly Func<AiDbContext, int, IEnumerable<int>, Task<int>> DeleteSettingsAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, IEnumerable<int> providersIds) =>
            ctx.Settings
                .Where(x => x.TenantId == tenantId && providersIds.Contains(x.ProviderId))
                .ExecuteDelete());
    
    public static readonly Func<AiDbContext, int, Guid, Scope, Task<RunConfiguration?>> GetProviderSettingsAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId, Guid userId, Scope scope) =>
            ctx.Settings
                .Where(x => x.TenantId == tenantId && x.UserId == userId && x.Scope == scope)
                .Join(ctx.Providers, x => x.ProviderId, y => y.Id, (x, y) => 
                    new RunConfiguration 
                    { 
                        ProviderType = y.Type,
                        Url = y.Url,
                        Key = y.Key,
                        Parameters = x.Parameters
                    })
                .FirstOrDefault());
}