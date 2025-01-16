// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Core;

[Singleton]
public class CoreBaseSettings(IConfiguration configuration)
{
    private bool? _standalone;
    private string _basedomain;
    private bool? _customMode;
    private string _serverRoot;
    private List<CultureInfo> _enabledCultures;

    public string Basedomain => _basedomain ??= configuration["core:base-domain"] ?? string.Empty;

    public string ServerRoot => _serverRoot ??= configuration["core:server-root"] ?? string.Empty;

    public bool Standalone => _standalone ??= Basedomain == "localhost";

    public bool CustomMode => _customMode ??= string.Equals(configuration["core:custom-mode"], "true", StringComparison.OrdinalIgnoreCase);

    public List<CultureInfo> EnabledCultures => _enabledCultures ??= (configuration.GetSection("web:cultures").Get<string[]>() ?? ["en-US"])
        .Distinct()
        .Select(l => CultureInfo.GetCultureInfo(l.Trim()))
        .ToList();
    
    public string GetRightCultureName(CultureInfo cultureInfo)
    {
        return EnabledCultures.Contains(cultureInfo)
            ? cultureInfo.Name
            : EnabledCultures.Contains(cultureInfo.Parent)
                ? cultureInfo.Parent.Name
                : "en-US";
    }
}

/// <summary>
/// </summary>
[Scope]
public class CoreSettings(
    ITenantService tenantService,
    CoreBaseSettings coreBaseSettings,
    IConfiguration configuration,
    IDistributedLockProvider distributedLockProvider)
{
    /// <summary>Base domain</summary>
    /// <type>System.String, System</type>
    public string BaseDomain
    {
        get
        {
            string result;
            if (coreBaseSettings.Standalone || string.IsNullOrEmpty(coreBaseSettings.Basedomain))
            {
                result = GetSetting("BaseDomain") ?? coreBaseSettings.Basedomain;
            }
            else
            {
                result = coreBaseSettings.Basedomain;
            }

            return result;
        }
    }

    private const string LockKey = "core_settings";

    public string GetBaseDomain(string hostedRegion)
    {
        var baseHost = BaseDomain;

        if (string.IsNullOrEmpty(hostedRegion) || string.IsNullOrEmpty(baseHost) || baseHost.IndexOf('.') < 0)
        {
            return baseHost;
        }

        var subdomain = baseHost.Remove(baseHost.IndexOf('.') + 1);

        return hostedRegion.StartsWith(subdomain) ? hostedRegion : (subdomain + hostedRegion.TrimStart('.'));
    }

    public async Task SaveSettingAsync(string key, string value, int tenant = Tenant.DefaultTenant)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        byte[] bytes = null;
        if (value != null)
        {
            bytes = Crypto.GetV(Encoding.UTF8.GetBytes(value), 2, true);
        }

        await tenantService.SetTenantSettingsAsync(tenant, key, bytes);
    }

    public async Task<string> GetSettingAsync(string key, int tenant = Tenant.DefaultTenant)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        var bytes = await tenantService.GetTenantSettingsAsync(tenant, key);

        var result = bytes != null ? Encoding.UTF8.GetString(Crypto.GetV(bytes, 2, false)) : null;

        return result;
    }

    public string GetSetting(string key, int tenant = Tenant.DefaultTenant)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        var bytes = tenantService.GetTenantSettings(tenant, key);

        var result = bytes != null ? Encoding.UTF8.GetString(Crypto.GetV(bytes, 2, false)) : null;

        return result;
    }

    public async Task<string> GetKeyAsync(int tenant)
    {
        if (coreBaseSettings.Standalone)
        {
            var key = await GetSettingAsync("PortalId");
            if (!string.IsNullOrEmpty(key))
            {
                return key;
            }

            await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKey))
            {
                key = await GetSettingAsync("PortalId");
                if (!string.IsNullOrEmpty(key))
                {
                    return key;
                }

                key = Guid.NewGuid().ToString();
                await SaveSettingAsync("PortalId", key);
            }

            return key;
        }

        var t = await tenantService.GetTenantAsync(tenant);
        if (t != null && !string.IsNullOrWhiteSpace(t.PaymentId))
        {
            return t.PaymentId;
        }

        return configuration["core:payment:region"] + tenant;
    }
}

[Scope]
public class CoreConfiguration(CoreSettings coreSettings, TenantManager tenantManager)
{
    public async Task<SmtpSettings> GetDefaultSmtpSettingsAsync()
    {
        var isDefaultSettings = false;
        var tenant = tenantManager.GetCurrentTenant(false);

        if (tenant != null)
        {
            var settingsValue = await GetSettingAsync("SmtpSettings", tenant.Id);
            if (string.IsNullOrEmpty(settingsValue))
            {
                isDefaultSettings = true;
                settingsValue = await GetSettingAsync("SmtpSettings");
            }

            var settings = SmtpSettings.Deserialize(settingsValue);
            settings.IsDefaultSettings = isDefaultSettings;

            return settings;
        }
        else
        {
            var settingsValue = await GetSettingAsync("SmtpSettings");

            var settings = SmtpSettings.Deserialize(settingsValue);
            settings.IsDefaultSettings = true;

            return settings;
        }
    }

    public async Task SetSmtpSettingsAsync(SmtpSettings value)
    {
        await SaveSettingAsync("SmtpSettings", value?.Serialize(), tenantManager.GetCurrentTenantId());
    }

    #region Methods Get/Save Setting

    public async Task SaveSettingAsync(string key, string value, int tenant = Tenant.DefaultTenant)
    {
        await coreSettings.SaveSettingAsync(key, value, tenant);
    }

    public async Task<string> GetSettingAsync(string key, int tenant = Tenant.DefaultTenant)
    {
        return await coreSettings.GetSettingAsync(key, tenant);
    }

    public string GetSetting(string key, int tenant = Tenant.DefaultTenant)
    {
        return coreSettings.GetSetting(key, tenant);
    }

    #endregion

    #region Methods Get/Set Section

    public async Task<T> GetSectionAsync<T>() where T : class
    {
        return await GetSectionAsync<T>(typeof(T).Name);
    }

    public async Task<T> GetSectionAsync<T>(int tenantId) where T : class
    {
        return await GetSectionAsync<T>(tenantId, typeof(T).Name);
    }

    public async Task<T> GetSectionAsync<T>(string sectionName) where T : class
    {
        return await GetSectionAsync<T>(tenantManager.GetCurrentTenantId(), sectionName);
    }

    public async Task<T> GetSectionAsync<T>(int tenantId, string sectionName) where T : class
    {
        var serializedSection = await GetSettingAsync(sectionName, tenantId);
        if (serializedSection == null && tenantId != Tenant.DefaultTenant)
        {
            serializedSection = await GetSettingAsync(sectionName);
        }

        return serializedSection != null ? JsonSerializer.Deserialize<T>(serializedSection) : null;
    }

    public async Task SaveSectionAsync<T>(string sectionName, T section) where T : class
    {
        await SaveSectionAsync(tenantManager.GetCurrentTenantId(), sectionName, section);
    }

    public async Task SaveSectionAsync<T>(T section) where T : class
    {
        await SaveSectionAsync(typeof(T).Name, section);
    }

    public async Task SaveSectionAsync<T>(int tenantId, T section) where T : class
    {
        await SaveSectionAsync(tenantId, typeof(T).Name, section);
    }

    public async Task SaveSectionAsync<T>(int tenantId, string sectionName, T section) where T : class
    {
        var serializedSection = section != null ? JsonSerializer.Serialize(section) : null;
        await SaveSettingAsync(sectionName, serializedSection, tenantId);
    }

    #endregion
}