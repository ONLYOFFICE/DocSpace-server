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

using ASC.Web.Core.Files;

namespace ASC.Web.Core.WhiteLabel;

[Scope]
public class TenantLogoManager(
    TenantWhiteLabelSettingsHelper tenantWhiteLabelSettingsHelper,
    SettingsManager settingsManager,
    TenantInfoSettingsHelper tenantInfoSettingsHelper,
    TenantManager tenantManager,
    AuthContext authContext,
    IConfiguration configuration,
    IFusionCache hybridCache,
    CommonLinkUtility commonLinkUtility,
    IHttpClientFactory clientFactory,
    ILogger<TenantLogoManager> logger)
{
    public bool WhiteLabelEnabled
    {
        get
        {
            var hideSettings = configuration.GetSection("web:hide-settings").Get<string[]>() ?? [];
            return !hideSettings.Contains("WhiteLabel", StringComparer.CurrentCultureIgnoreCase);
        }
    }

    public async Task<string> GetFaviconAsync(bool timeParam, bool dark)
    {
        string faviconPath;
        var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();
        if (WhiteLabelEnabled)
        {
            faviconPath = await tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, WhiteLabelLogoType.Favicon, dark);
            if (timeParam)
            {
                var now = DateTime.Now;
                faviconPath = $"{faviconPath}?t={now.Ticks}";
            }
        }
        else
        {
            faviconPath = await tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(WhiteLabelLogoType.Favicon, dark);
        }

        return faviconPath;
    }

    public async Task<string> GetTopLogoAsync(bool dark)//LogoLightSmall
    {
        var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        if (WhiteLabelEnabled)
        {
            return await tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, WhiteLabelLogoType.LightSmall, dark);
        }
        return await tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(WhiteLabelLogoType.LightSmall, dark);
    }

    /// <summary>
    /// Returns the portal top logo (LogoLightSmall) as an absolute URL.
    /// </summary>
    public async Task<string> GetTopLogoAbsoluteUrlAsync(bool dark = false)
    {
        var logoPath = await GetTopLogoAsync(dark);
        return commonLinkUtility.GetFullAbsolutePath(logoPath.Split('?')[0]);
    }

    /// <summary>
    /// Whether the portal top logo (LogoLightSmall) is the built-in one rather than an uploaded
    /// white-label logo. The two are authored at different proportions, so consumers that lay the
    /// logo out into a box of their own have to tell them apart.
    /// </summary>
    public async Task<bool> IsDefaultTopLogoAsync()
    {
        if (!WhiteLabelEnabled)
        {
            return true;
        }

        var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        return tenantWhiteLabelSettings.GetIsDefault(WhiteLabelLogoType.LightSmall);
    }

    /// <summary>
    /// Returns the portal top logo (LogoLightSmall) as a self-contained base64 data URI
    /// (e.g. "data:image/svg+xml;base64,..."), for consumers that must embed the image rather than
    /// reference it by URL. The built-in default logo never changes, so it is cached.
    /// </summary>
    public async Task<string> GetTopLogoDataUriAsync(bool dark = false)
    {
        const WhiteLabelLogoType logoType = WhiteLabelLogoType.LightSmall;

        if (WhiteLabelEnabled)
        {
            var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

            if (!tenantWhiteLabelSettings.GetIsDefault(logoType))
            {
                var logo = await tenantWhiteLabelSettingsHelper.GetWhitelabelLogoData(tenantWhiteLabelSettings, logoType);
                var data = await ReadStreamToByteArrayAsync(logo);
                if (data != null)
                {
                    return ToDataUri(data, tenantWhiteLabelSettings.GetExt(logoType, dark));
                }
            }
        }

        return await GetDefaultTopLogoDataUriAsync(dark);
    }

    // The default (or partner) logo is portal-independent and does not change, so it is fetched once and cached.
    private async Task<string> GetDefaultTopLogoDataUriAsync(bool dark)
    {
        var cacheKey = $"toplogodatauri_default_{dark}";

        var cached = await hybridCache.GetOrDefaultAsync<string>(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            return cached;
        }

        try
        {
            var logoPath = await tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(WhiteLabelLogoType.LightSmall, dark);
            var logoUrl = commonLinkUtility.GetFullAbsolutePath(logoPath.Split('?')[0]);

#pragma warning disable CA2000
            var httpClient = clientFactory.CreateClient();
#pragma warning restore CA2000
            var data = await httpClient.GetByteArrayAsync(logoUrl);

            var dataUri = ToDataUri(data, Path.GetExtension(logoUrl).TrimStart('.'));

            if (!string.IsNullOrEmpty(dataUri))
            {
                await hybridCache.SetAsync(cacheKey, dataUri, TimeSpan.FromDays(1));
            }

            return dataUri;
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            return null;
        }
    }

    private static string ToDataUri(byte[] data, string ext)
    {
        if (data is not { Length: > 0 })
        {
            return null;
        }

        var mime = (ext ?? string.Empty).ToLowerInvariant() switch
        {
            "svg" => "image/svg+xml",
            "jpg" or "jpeg" => "image/jpeg",
            "ico" => "image/x-icon",
            "gif" => "image/gif",
            _ => "image/png"
        };

        return $"data:{mime};base64,{Convert.ToBase64String(data)}";
    }

    public async Task<string> GetLogoDarkAsync(bool dark)
    {
        if (WhiteLabelEnabled)
        {
            var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();
            return await tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, WhiteLabelLogoType.Notification, dark);
        }

        /*** simple scheme ***/
        return await tenantInfoSettingsHelper.GetAbsoluteCompanyLogoPathAsync(await settingsManager.LoadAsync<TenantInfoSettings>());
        /***/
    }

    public async Task<string> GetLogoDocsEditorAsync(FileType fileType, bool dark)
    {
        var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        var logoType = WhiteLabelLogoTypeHelper.GetEditorLogoType(fileType, false);

        if (WhiteLabelEnabled)
        {
            return await tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, logoType, dark);
        }
        return await tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(logoType, dark);
    }

    public async Task<string> GetLogoDocsEditorEmbedAsync(FileType fileType, bool dark)
    {
        var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        var logoType = WhiteLabelLogoTypeHelper.GetEditorLogoType(fileType, true);

        if (WhiteLabelEnabled)
        {
            return await tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, logoType, dark);
        }
        return await tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(logoType, dark);
    }


    public async Task<string> GetLogoTextAsync()
    {
        if (WhiteLabelEnabled)
        {
            var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

            return await tenantWhiteLabelSettings.GetLogoTextAsync(settingsManager);
        }
        return TenantWhiteLabelSettings.DefaultLogoText;
    }

    public async Task<bool> IsDefaultLogoSettingsAsync()
    {
        if (WhiteLabelEnabled)
        {
            var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

            return await tenantWhiteLabelSettings.GetIsDefault(settingsManager);
        }
        return true;
    }

    public bool IsRetina(HttpRequest request)
    {
        var cookie = request?.Cookies["is_retina"];
        if (cookie != null && !string.IsNullOrEmpty(cookie) && bool.TryParse(cookie, out var result))
        {
            return result;
        }
        return !authContext.IsAuthenticated;
    }

    private async Task<bool> GetWhiteLabelPaidAsync()
    {
        return (await tenantManager.GetCurrentTenantQuotaAsync()).Customization;
    }

    public async Task<bool> GetEnableWhitelabelAsync()
    {
        return WhiteLabelEnabled && await GetWhiteLabelPaidAsync();
    }

    public async Task DemandWhiteLabelPermissionAsync()
    {
        if (!await GetEnableWhitelabelAsync())
        {
            throw new BillingException(Resource.ErrorNotAllowedOption);
        }
    }

    /// <summary>
    /// Get logo stream or null in case of default logo
    /// </summary>
    private async Task<Stream> GetWhitelabelMailLogoAsync()
    {
        if (WhiteLabelEnabled)
        {
            var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();
            return await tenantWhiteLabelSettingsHelper.GetWhitelabelLogoData(tenantWhiteLabelSettings, WhiteLabelLogoType.Notification);
        }

        /*** simple scheme ***/
        return await tenantInfoSettingsHelper.GetStorageLogoData(await settingsManager.LoadAsync<TenantInfoSettings>());
        /***/
    }

    public async Task<NotifyMessageAttachment> GetMailLogoAsAttachmentAsync(CultureInfo cultureInfo)
    {
        var culture = cultureInfo.Name;

        var logoData = await GetMailLogoDataFromCacheAsync(culture);

        if (logoData == null)
        {
            var logoStream = await GetWhitelabelMailLogoAsync();
            logoData = await ReadStreamToByteArrayAsync(logoStream) ?? await GetDefaultMailLogoAsync(culture);

            if (logoData != null)
            {
                await InsertMailLogoDataToCacheAsync(logoData, culture);
            }
        }

        if (logoData != null)
        {
            var attachment = new NotifyMessageAttachment
            {
                FileName = "logo.png",
                Content = logoData,
                ContentId = MimeUtils.GenerateMessageId()
            };

            return attachment;
        }

        return null;
    }

    public async Task RemoveMailLogoDataFromCacheAsync()
    {
        var customCultures = GetLogoCustomCultures();

        foreach (var customCulture in customCultures)
        {
            await hybridCache.RemoveAsync(GetCacheKey(customCulture));
        }

        await hybridCache.RemoveAsync(GetCacheKey(string.Empty));
    }


    private async Task<byte[]> GetMailLogoDataFromCacheAsync(string culture)
    {
        return await hybridCache.GetOrDefaultAsync<byte[]>(GetCacheKey(culture));
    }

    private async Task InsertMailLogoDataToCacheAsync(byte[] data, string culture)
    {
        await hybridCache.SetAsync(GetCacheKey(culture), data, TimeSpan.FromDays(1));
    }

    private string GetCacheKey(string culture)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var regionalPath = GetLogoRegionalPath(culture);
        return $"letterlogodata{tenantId}{regionalPath}";
    }

    private static async Task<byte[]> ReadStreamToByteArrayAsync(Stream inputStream)
    {
        if (inputStream == null)
        {
            return null;
        }

        await using (inputStream)
        {
            using var memoryStream = new MemoryStream();
            await inputStream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }

    private async Task<byte[]> GetDefaultMailLogoAsync(string culture)
    {
        var regionalPath = GetLogoRegionalPath(culture);
        var myAssembly = Assembly.GetExecutingAssembly();
        await using var stream = myAssembly.GetManifestResourceStream($"ASC.Web.Core.PublicResources.logo{regionalPath}.png");
        if (stream != null)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        return null;
    }

    private string[] GetLogoCustomCultures()
    {
        return configuration.GetSection("web:logo:custom-cultures").Get<string[]>() ?? [];
    }

    private string GetLogoRegionalPath(string culture)
    {
        var customCultures = GetLogoCustomCultures();

        return customCultures.Contains(culture, StringComparer.InvariantCultureIgnoreCase)
            ? culture.ToLower()
            : string.Empty;
    }
}
