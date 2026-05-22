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

namespace ASC.Web.Core.Mobile;

[Scope]
public class CachedMobileAppInstallRegistrator(
    MobileAppInstallRegistrator registrator,
    TimeSpan cacheExpiration,
    TenantManager tenantManager,
    ICache cache)
    : IMobileAppInstallRegistrator
{
    private readonly MobileAppInstallRegistrator _registrator = registrator ?? throw new ArgumentNullException(nameof(registrator));

    public CachedMobileAppInstallRegistrator(MobileAppInstallRegistrator registrator, TenantManager tenantManager, ICache cache)
        : this(registrator, TimeSpan.FromMinutes(30), tenantManager, cache)
    {
    }

    public async Task RegisterInstallAsync(string userEmail, MobileAppType appType)
    {
        if (string.IsNullOrEmpty(userEmail))
        {
            return;
        }

        await _registrator.RegisterInstallAsync(userEmail, appType);
        cache.Insert(GetCacheKey(userEmail, null), true, cacheExpiration);
        cache.Insert(GetCacheKey(userEmail, appType), true, cacheExpiration);
    }

    public async Task<bool> IsInstallRegisteredAsync(string userEmail, MobileAppType? appType)
    {
        if (string.IsNullOrEmpty(userEmail))
        {
            return false;
        }

        var fromCache = cache.Get<string>(GetCacheKey(userEmail, appType));


        if (bool.TryParse(fromCache, out var cachedValue))
        {
            return cachedValue;
        }

        var isRegistered = await _registrator.IsInstallRegisteredAsync(userEmail, appType);
        cache.Insert(GetCacheKey(userEmail, appType), isRegistered.ToString(), cacheExpiration);
        return isRegistered;
    }

    private string GetCacheKey(string userEmail, MobileAppType? appType)
    {
        var cacheKey = appType.HasValue ? userEmail + "/" + appType : userEmail;

        return string.Format("{0}:mobile:{1}", tenantManager.GetCurrentTenantId(), cacheKey);
    }
}