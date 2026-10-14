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


namespace ASC.Files.Core;

/// <summary>
/// Whether the tenant has any metadata template at all. The question is asked on the hottest read paths (every listing
/// page, every text search) by tenants that mostly have no metadata, so the answer is cached; the DAO drops it on
/// every node when a template is created or deleted.
/// </summary>
[Scope]
public class MetadataTemplatesCache(
    IFusionCacheProvider cacheProvider,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    TenantManager tenantManager)
{
    /// <summary>
    /// The templates are written through the DAO, which drops the entry; the expiration only bounds the staleness
    /// of a write that bypassed it (a restored backup).
    /// </summary>
    private static readonly TimeSpan _expiration = TimeSpan.FromMinutes(10);

    private readonly IFusionCache _cache = cacheProvider.GetMemoryCache();

    public static string GetCacheKey(int tenantId)
    {
        return tenantId + "metadatatemplates";
    }

    /// <summary>
    /// Whether the tenant has any template, the hidden system template of the custom fields included. Without one
    /// there are no links and no values either, so a listing can skip its metadata queries and a text search its
    /// metadata part.
    /// </summary>
    public async Task<bool> HasTemplatesAsync()
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        return await _cache.GetOrSetAsync<bool>(GetCacheKey(tenantId), async (_, token) =>
        {
            await using var filesDbContext = await dbContextFactory.CreateDbContextAsync(token);

            return await filesDbContext.MetadataTemplatesExistAsync(tenantId);
        }, _expiration, [CacheExtention.GetMetadataTemplatesTag(tenantId)]);
    }

    /// <summary>
    /// Drops the answer on every node: the cache is the L1-only one, and a bare removal would leave the other services
    /// skipping the metadata of the first template until the entry expired.
    /// </summary>
    public async Task InvalidateAsync(int tenantId)
    {
        await _cache.RemoveByTagAndNotifyAsync(CacheExtention.GetMetadataTemplatesTag(tenantId));
    }
}
