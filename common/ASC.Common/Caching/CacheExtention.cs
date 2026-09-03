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

namespace ASC.Common.Caching;

public static class CacheExtention
{
    public static IFusionCache GetMemoryCache(this IFusionCacheProvider cacheProvider)
    {
        return cacheProvider.GetCache("memory");
    }

    /// <summary>
    /// Removes a key from the "memory" cache AND tells the other nodes to drop it.
    /// The "memory" cache skips backplane notifications by default (an L1-only cache that notified on every
    /// factory fill turned shared keys into a cross-process DB ping-pong), so an invalidation that must be
    /// seen by other processes has to opt back in — always use this instead of a bare RemoveAsync.
    /// </summary>
    public static ValueTask RemoveAndNotifyAsync(this IFusionCache cache, string key, CancellationToken token = default)
    {
        return cache.RemoveAsync(key, options => options.SetSkipBackplaneNotifications(false), token);
    }

    /// <summary>
    /// Expires every "memory" cache entry carrying <paramref name="tag"/> on this node AND on the other nodes.
    /// Tag invalidation is a tag-timestamp entry, and with <c>options == null</c> FusionCache writes it with
    /// <c>TagsDefaultEntryOptions</c> — which, unlike the memory cache's <c>DefaultEntryOptions</c>, neither skips
    /// the distributed cache nor the backplane, so the timestamp lands in Redis where every process checks it.
    /// Do NOT pass <c>DefaultEntryOptions</c> here: with <c>SkipDistributedCacheWrite = true</c> the timestamp
    /// stays on one node and the other services keep serving stale data.
    /// </summary>
    public static ValueTask RemoveByTagAndNotifyAsync(this IFusionCache cache, string tag, CancellationToken token = default)
    {
        return cache.RemoveByTagAsync(tag, options: null, token);
    }

    public static string GetUserTag(int tenant, Guid userId)
    {
        return $"user-{tenant}-{userId}";
    }

    public static string GetUserPhotoTag(int tenant, Guid userId)
    {
        return $"userphoto-{tenant}-{userId}";
    }

    public static string GetSettingsTag(int tenant, string settingName)
    {
        return $"settings-{tenant}-{settingName}";
    }

    public static string GetSettingsTag(int tenant, Guid userId, string settingName)
    {
        return $"settings-{tenant}-{userId}-{settingName}";
    }

    public static string GetTenantQuotaTag(string key)
    {
        return $"quota-{key}";
    }

    /// <summary>
    /// Tenant-wide tag for every cached quota-row list (portal-wide and per-user) of the tenant.
    /// Deliberately not derived from the rows themselves: a list cached while the tenant has no rows yet
    /// would otherwise carry no tags and could never be expired.
    /// </summary>
    public static string GetTenantQuotaRowsTag(int tenant)
    {
        return $"quotarows-{tenant}";
    }

    public static string GetRelationTag(int tenant, Guid sourceUserId)
    {
        return $"relation-{tenant}-{sourceUserId}";
    }

    public static string GetGroupTag(int tenant, Guid id)
    {
        return $"group-{tenant}-{id}";
    }

    public static string GetGroupRefsTag(int tenant)
    {
        return $"refs-{tenant}";
    }

    public static string GetWebPluginsTag(int tenant)
    {
        return $"webplugin-{tenant}";
    }

    public static string GetProviderFileTag(string selector, int id, string fileId)
    {
        return $"provider-file-{selector}-{id}-{fileId}";
    }

    public static string GetProviderFolderTag(string selector, int id, string folderId)
    {
        return $"provider-folder-{selector}-{id}-{folderId}";
    }

    public static string GetProviderFolderItemsTag(string selector, int id, string folderId)
    {
        return $"provider-folder-items-{selector}-{id}-{folderId}";
    }

    public static string GetProviderTag(string selector, int id)
    {
        return $"provider-{selector}-{id}";
    }

    public static string GetWebItemSecurityTag(int tenant)
    {
        return $"webItem-{tenant}";
    }

    public static string GetTenantSettingsTag(int tenant, string key)
    {
        return $"settings-{tenant}-{key}";
    }
}