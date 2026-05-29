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

namespace ASC.AI.Core.Database;

public partial class AiDbContext
{
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<DbAiModelSettings> GetModelSettingsAsync(int tenantId, int providerId)
    {
        return Queries.GetModelSettingsAsync(this, tenantId, providerId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, null])]
    public Task<DbAiModelSettings?> GetModelSettingAsync(int tenantId, int providerId, string modelId)
    {
        return Queries.GetModelSettingAsync(this, tenantId, providerId, modelId);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, null])]
    public IAsyncEnumerable<DbAiModelSettings> GetModelSettingsForUpdateAsync(int tenantId, int providerId, IEnumerable<string> modelIds)
    {
        return Queries.GetModelSettingsForUpdateAsync(this, tenantId, providerId, modelIds);
    }

    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultInt, null])]
    public Task DeleteModelSettingsAsync(int tenantId, int providerId, string modelId)
    {
        return Queries.DeleteModelSettingsAsync(this, tenantId, providerId, modelId);
    }
}

static file class Queries
{
    public static readonly Func<AiDbContext, int, int, IAsyncEnumerable<DbAiModelSettings>> GetModelSettingsAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int tenantId, int providerId) =>
                ctx.ModelSettings
                    .Where(x => x.TenantId == tenantId && x.ProviderId == providerId));

    public static readonly Func<AiDbContext, int, int, string, Task<DbAiModelSettings?>> GetModelSettingAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int tenantId, int providerId, string modelId) =>
                ctx.ModelSettings
                    .FirstOrDefault(x => x.TenantId == tenantId && x.ProviderId == providerId && x.ModelId == modelId));

    public static readonly Func<AiDbContext, int, int, IEnumerable<string>, IAsyncEnumerable<DbAiModelSettings>> GetModelSettingsForUpdateAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int tenantId, int providerId, IEnumerable<string> modelIds) =>
                ctx.ModelSettings
                    .Where(x => x.TenantId == tenantId && x.ProviderId == providerId && modelIds.Contains(x.ModelId))
                    .AsTracking());

    public static readonly Func<AiDbContext, int, int, string, Task> DeleteModelSettingsAsync =
        EF.CompileAsyncQuery(
            (AiDbContext ctx, int tenantId, int providerId, string modelId) =>
                ctx.ModelSettings
                    .Where(x => x.TenantId == tenantId && x.ProviderId == providerId && x.ModelId == modelId)
                    .ExecuteDelete());
}
