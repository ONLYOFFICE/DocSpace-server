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

namespace ASC.Web.Studio.Core.TFA;

public class TfaAppUserSettings : ISettings<TfaAppUserSettings>
{
    [JsonPropertyName("BackupCodes")]
    public IEnumerable<BackupCode> CodesSetting { get; set; }

    [JsonPropertyName("Salt")]
    public long SaltSetting { get; set; }

    public static Guid ID => new("{EAF10611-BE1E-4634-B7A1-57F913042F78}");

    public TfaAppUserSettings GetDefault()
    {
        return new TfaAppUserSettings
        {
            CodesSetting = new List<BackupCode>(),
            SaltSetting = 0
        };
    }

    public DateTime LastModified { get; set; }

    public static async Task<long> GetSaltAsync(SettingsManager settingsManager, Guid userId)
    {
        var settings = await settingsManager.LoadAsync<TfaAppUserSettings>(userId);
        var salt = settings.SaltSetting;
        if (salt == 0)
        {
            var from = new DateTime(2018, 07, 07, 0, 0, 0, DateTimeKind.Utc);
            settings.SaltSetting = salt = (long)(DateTime.UtcNow - from).TotalMilliseconds;

            await settingsManager.SaveAsync(settings, userId);
        }
        return salt;
    }

    public static async Task<IEnumerable<BackupCode>> BackupCodesForUserAsync(SettingsManager settingsManager, Guid userId)
    {
        return (await settingsManager.LoadAsync<TfaAppUserSettings>(userId)).CodesSetting;
    }

    public static async Task DisableCodeForUserAsync(SettingsManager settingsManager, InstanceCrypto instanceCrypto, Signature signature, Guid userId, string code)
    {
        var settings = await settingsManager.LoadAsync<TfaAppUserSettings>(userId);
        var query = settings.CodesSetting.Where(x => x.GetEncryptedCode(instanceCrypto, signature) == code).ToList();

        if (query.Count > 0)
        {
            query.First().IsUsed = true;
        }

        await settingsManager.SaveAsync(settings, userId);
    }

    public static async Task<bool> EnableForUserAsync(SettingsManager settingsManager, Guid guid)
    {
        return (await settingsManager.LoadAsync<TfaAppUserSettings>(guid)).CodesSetting.Any();
    }

    public static async Task DisableForUserAsync(SettingsManager settingsManager, Guid guid)
    {
        var defaultSettings = settingsManager.GetDefault<TfaAppUserSettings>();
        await settingsManager.SaveAsync(defaultSettings, guid);
    }

    public static async Task<bool> TfaExpiredAndResetAsync(SettingsManager settingsManager, AuditEventsRepository auditEventsRepository, Guid userId)
    {
        var tfaExpired = false;
        var tfaLastEnabled = await settingsManager.LoadAsync<TfaAppUserSettings>(userId);
        if (tfaLastEnabled != null)
        {
            var tfaLastChange = (await auditEventsRepository
                .GetByFilterWithActionsAsync(actions: [MessageAction.TwoFactorAuthenticationEnabledByTfaApp, MessageAction.TwoFactorAuthenticationDisabled], limit: 1))
                .FirstOrDefault();

            if (tfaLastChange is { Action: (int)MessageAction.TwoFactorAuthenticationDisabled })
            {
                tfaExpired = tfaLastEnabled.LastModified.AddDays(1) < DateTime.UtcNow;
                if (tfaExpired)
                {
                    await DisableForUserAsync(settingsManager, userId);
                }
            }
        }

        return tfaExpired;
    }
}