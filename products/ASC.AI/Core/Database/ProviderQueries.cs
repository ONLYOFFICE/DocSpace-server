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
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public Task<DbAiProvider?> GetProviderAsync(int tenantId, int id)
    {
        return Queries.GetProviderAsync(this, tenantId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbAiProvider> GetProvidersAsync(int tenantId, int offset, int limit)
    {
        return Queries.GetProvidersAsync(this, tenantId, offset, limit);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public Task<int> GetProvidersTotalCountAsync(int tenantId)
    {
        return Queries.GetProvidersTotalCountAsync(this, tenantId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, null, null, PreCompileQuery.DefaultDateTime])]
    public Task UpdateProviderAsync(int id, string title, string? url, string key, DateTime modifiedOn)
    {
        return Queries.UpdateProviderAsync(this, id, title, url, key, modifiedOn);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task DeleteProvidersAsync(int tenantId, IEnumerable<int> ids)
    {
        return Queries.DeleteProvidersAsync(this, tenantId, ids);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<string> GetProviderKeysAsync(int tenantId)
    {
        return Queries.GetProviderKeysAsync(this, tenantId);
    }
}

static file class Queries
{
    public static readonly Func<AiDbContext, int, int, Task<DbAiProvider?>> GetProviderAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int tenantId, int id) => 
                ctx.Providers.FirstOrDefault(x => x.TenantId == tenantId && x.Id == id));
    
    public static readonly Func<AiDbContext, int, int, int, IAsyncEnumerable<DbAiProvider>> GetProvidersAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int tenantId, int offset, int limit) => 
            ctx.Providers
                .Where(x => x.TenantId == tenantId)
                .Skip(offset)
                .Take(limit));

    public static readonly Func<AiDbContext, int, Task<int>> GetProvidersTotalCountAsync =
        EF.CompileAsyncQuery((AiDbContext ctx, int tenantId) =>
            ctx.Providers.Count(x => x.TenantId == tenantId));

    public static readonly Func<AiDbContext, int, string, string?, string, DateTime, Task> UpdateProviderAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int id, string title, string? url, string key, DateTime modifiedOn) => 
                ctx.Providers.Where(x => x.Id == id).ExecuteUpdate(x => 
                    x.SetProperty(y => y.Title, title)
                        .SetProperty(y => y.Url, url)
                        .SetProperty(y => y.Key, key)
                        .SetProperty(y => y.ModifiedOn, modifiedOn)));
    
    public static readonly Func<AiDbContext, int, IEnumerable<int>, Task> DeleteProvidersAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int tenantId, IEnumerable<int> ids) => 
                ctx.Providers
                    .Where(x => x.TenantId == tenantId)
                    .Where(x => ids.Contains(x.Id)).ExecuteDelete());

    public static readonly Func<AiDbContext, int, IAsyncEnumerable<string>> GetProviderKeysAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int tenantId) =>
                ctx.Providers
                    .Where(x => x.TenantId == tenantId)
                    .Select(x => x.Key));
}