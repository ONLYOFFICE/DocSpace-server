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

namespace ASC.Web.Core.Sms;

public abstract class TfaSettingsBase<T> : ISettings<T> where T : ISettings<T>
{
    
    [JsonPropertyName("Enable")]
    public bool EnableSetting { get; set; }

    [JsonPropertyName("TrustedIps")]
    public List<string> TrustedIps { get; set; }

    [JsonPropertyName("MandatoryUsers")]
    public List<Guid> MandatoryUsers { get; set; }

    [JsonPropertyName("MandatoryGroups")]
    public List<Guid> MandatoryGroups { get; set; }

    public static Guid ID { get; }
    
    public abstract T GetDefault();

    public DateTime LastModified { get; set; }
}


public abstract class TfaSettingsHelperBase<T>(SettingsManager settingsManager,
    IHttpContextAccessor httpContextAccessor,
    UserManager userManager)
    where T : TfaSettingsBase<T>, new()
{
    public async Task<bool> TfaEnabledForUserAsync(Guid userGuid)
    {
        var settings = await settingsManager.LoadAsync<T>();

        if (!settings.EnableSetting)
        {
            return false;
        }

        if (settings.MandatoryGroups != null)
        {
            foreach (var mandatory in settings.MandatoryGroups)
            {
                if (await userManager.IsUserInGroupAsync(userGuid, mandatory))
                {
                    return true;
                }
            }
        }

        if (settings.MandatoryUsers != null)
        {
            foreach (var mandatory in settings.MandatoryUsers)
            {
                if (mandatory == userGuid)
                {
                    return true;
                }
            }
        }

        if (settings.TrustedIps != null && settings.TrustedIps.Count != 0)
        {
            var requestIP = MessageSettings.GetIP(httpContextAccessor.HttpContext.Request);
            if (!string.IsNullOrWhiteSpace(requestIP) && settings.TrustedIps.Any(trustedIp => IPAddressRange.MatchIPs(requestIP, trustedIp)))
            {
                return false;
            }
        }

        return true;
    }

    public bool IsVisibleSettings => SetupInfo.IsVisibleSettings<T>();

    public virtual async Task<bool> GetEnable()
    {
        return (await settingsManager.LoadAsync<T>()).EnableSetting;
    }

    public async Task SetEnable(bool value)
    {
        T settings;
        if (value)
        {
            settings = await settingsManager.LoadAsync<T>();
            settings.EnableSetting = true;
        }
        else
        {
            settings = new T();
        }

        await settingsManager.SaveAsync(settings);
    }
}