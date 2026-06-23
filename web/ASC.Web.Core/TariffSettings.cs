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

namespace ASC.Web.Studio.UserControls.Management;

public class TariffSettings : ISettings<TariffSettings>
{
    private static readonly CultureInfo _cultureInfo = CultureInfo.CreateSpecificCulture("en-US");

    [JsonPropertyName("HideNotify")]
    public bool HideNotifySetting { get; set; }

    [JsonPropertyName("HidePricingPage")]
    public bool HidePricingPageForUsers { get; set; }

    [JsonPropertyName("LicenseAccept")]
    public string LicenseAcceptSetting { get; set; }

    public TariffSettings GetDefault()
    {
        return new TariffSettings
        {
            HideNotifySetting = false,
            HidePricingPageForUsers = false,
            LicenseAcceptSetting = DateTime.MinValue.ToString(_cultureInfo)
        };
    }

    public DateTime LastModified { get; set; }

    public static Guid ID => new("{07956D46-86F7-433b-A657-226768EF9B0D}");

    public static async Task<bool> GetHideNotifyAsync(SettingsManager settingsManager)
    {
        return (await settingsManager.LoadForCurrentUserAsync<TariffSettings>()).HideNotifySetting;
    }

    public static async Task SetHideNotifyAsync(SettingsManager settingsManager, bool newVal)
    {
        var tariffSettings = await settingsManager.LoadForCurrentUserAsync<TariffSettings>();
        tariffSettings.HideNotifySetting = newVal;
        await settingsManager.SaveForCurrentUserAsync(tariffSettings);
    }

    public static async Task<bool> GetHidePricingPageAsync(SettingsManager settingsManager)
    {
        return (await settingsManager.LoadAsync<TariffSettings>()).HidePricingPageForUsers;
    }

    public static async Task SetHidePricingPageAsync(SettingsManager settingsManager, bool newVal)
    {
        var tariffSettings = await settingsManager.LoadAsync<TariffSettings>();
        tariffSettings.HidePricingPageForUsers = newVal;
        await settingsManager.SaveAsync(tariffSettings);
    }

    public static async Task<bool> GetLicenseAcceptAsync(SettingsManager settingsManager)
    {
        return !DateTime.MinValue.ToString(_cultureInfo).Equals((await settingsManager.LoadForDefaultTenantAsync<TariffSettings>()).LicenseAcceptSetting);
    }

    public static async Task SetLicenseAcceptAsync(SettingsManager settingsManager)
    {
        var tariffSettings = await settingsManager.LoadForDefaultTenantAsync<TariffSettings>();
        if (DateTime.MinValue.ToString(_cultureInfo).Equals(tariffSettings.LicenseAcceptSetting))
        {
            tariffSettings.LicenseAcceptSetting = DateTime.UtcNow.ToString(_cultureInfo);
            await settingsManager.SaveForDefaultTenantAsync(tariffSettings);
        }
    }
}