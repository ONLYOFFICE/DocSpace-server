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

namespace ASC.Core.Tenants;

public class TenantCookieSettings : ISettings<TenantCookieSettings>
{
    public int Index { get; set; }
    public int LifeTime { get; set; }
    public bool Enabled { get; set; }

    public TenantCookieSettings GetDefault()
    {
        return GetInstance();
    }

    public DateTime LastModified { get; set; }

    public bool IsDefault()
    {
        var defaultSettings = GetInstance();

        return LifeTime == defaultSettings.LifeTime && Enabled == defaultSettings.Enabled;
    }

    public static TenantCookieSettings GetInstance()
    {
        return new TenantCookieSettings
        {
            LifeTime = 1440
        };
    }

    public static Guid ID => new("{16FB8E67-E96D-4B22-B217-C80F25C5DE1B}");
}

[Singleton]
public class TenantCookieSettingsConfig(IConfiguration configuration)
{
    public bool IsVisibleSettings { get; } = !(configuration.GetSection("web:hide-settings").Get<string[]>() ?? [])
                    .Contains("CookieSettings", StringComparer.CurrentCultureIgnoreCase);
}

[Scope]
public class TenantCookieSettingsHelper(TenantCookieSettingsConfig configuration, SettingsManager settingsManager)
{
    public async Task<TenantCookieSettings> GetForTenantAsync(int tenantId)
    {
        return configuration.IsVisibleSettings
                   ? await settingsManager.LoadAsync<TenantCookieSettings>(tenantId)
                   : TenantCookieSettings.GetInstance();
    }

    public async Task SetForTenantAsync(int tenantId, TenantCookieSettings settings = null)
    {
        if (!configuration.IsVisibleSettings)
        {
            return;
        }

        await settingsManager.SaveAsync(settings ?? TenantCookieSettings.GetInstance(), tenantId);
    }

    public async Task<TenantCookieSettings> GetForUserAsync(Guid userId)
    {
        return configuration.IsVisibleSettings
                   ? await settingsManager.LoadAsync<TenantCookieSettings>(userId)
                   : TenantCookieSettings.GetInstance();
    }

    public async Task<TenantCookieSettings> GetForUserAsync(int tenantId, Guid userId)
    {
        return configuration.IsVisibleSettings
                   ? await settingsManager.LoadAsync<TenantCookieSettings>(tenantId, userId)
                   : TenantCookieSettings.GetInstance();
    }

    public async Task SetForUserAsync(Guid userId, TenantCookieSettings settings = null)
    {
        if (!configuration.IsVisibleSettings)
        {
            return;
        }

        await settingsManager.SaveAsync(settings ?? TenantCookieSettings.GetInstance(), userId);
    }

    public async Task<DateTime> GetExpiresTimeAsync(int tenantId)
    {
        var settingsTenant = await GetForTenantAsync(tenantId);

        DateTime expires;

        if (settingsTenant.IsDefault() || !settingsTenant.Enabled)
        {
            expires = DateTime.UtcNow.AddYears(1);
        }
        else
        {
            expires = settingsTenant.LifeTime == 0 ? DateTime.MaxValue : DateTime.UtcNow.AddMinutes(settingsTenant.LifeTime);
        }

        return expires;
    }
}