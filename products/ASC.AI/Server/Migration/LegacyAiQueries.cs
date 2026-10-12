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

namespace ASC.AI.Migration;

public partial class LegacyAiDbContext
{
    [PreCompileQuery]
    public IAsyncEnumerable<int> TenantIdsWithProvidersAsync()
    {
        return LegacyAiQueries.TenantIdsWithProvidersAsync(this);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbAiProvider> ProvidersAsync(int tenantId)
    {
        return LegacyAiQueries.ProvidersAsync(this, tenantId);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbAiModelSettings> ModelSettingsAsync(int tenantId)
    {
        return LegacyAiQueries.ModelSettingsAsync(this, tenantId);
    }

    [PreCompileQuery]
    public Task<DbDefaultAiProvider?> DefaultProviderAsync(int tenantId)
    {
        return LegacyAiQueries.DefaultProviderAsync(this, tenantId);
    }
}

static file class LegacyAiQueries
{
    public static readonly Func<LegacyAiDbContext, IAsyncEnumerable<int>> TenantIdsWithProvidersAsync =
        EF.CompileAsyncQuery(
            (LegacyAiDbContext ctx) =>
                ctx.Providers
                    .Where(p => p.Type != ProviderType.PortalAi)
                    .Select(p => p.TenantId)
                    .Distinct());

    public static readonly Func<LegacyAiDbContext, int, IAsyncEnumerable<DbAiProvider>> ProvidersAsync =
        EF.CompileAsyncQuery(
            (LegacyAiDbContext ctx, int tenantId) =>
                ctx.Providers
                    .Where(p => p.TenantId == tenantId && p.Type != ProviderType.PortalAi));

    public static readonly Func<LegacyAiDbContext, int, IAsyncEnumerable<DbAiModelSettings>> ModelSettingsAsync =
        EF.CompileAsyncQuery(
            (LegacyAiDbContext ctx, int tenantId) =>
                ctx.ModelSettings
                    .Where(m => m.TenantId == tenantId));

    public static readonly Func<LegacyAiDbContext, int, Task<DbDefaultAiProvider?>> DefaultProviderAsync =
        EF.CompileAsyncQuery(
            (LegacyAiDbContext ctx, int tenantId) =>
                ctx.DefaultProviders.FirstOrDefault(d => d.TenantId == tenantId));
}
