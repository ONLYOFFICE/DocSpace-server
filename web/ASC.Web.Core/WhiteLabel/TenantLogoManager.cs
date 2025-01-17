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

namespace ASC.Web.Core.WhiteLabel;

[Scope]
public class TenantLogoManager(
    TenantWhiteLabelSettingsHelper tenantWhiteLabelSettingsHelper,
    SettingsManager settingsManager,
    TenantInfoSettingsHelper tenantInfoSettingsHelper,
    TenantManager tenantManager,
    AuthContext authContext,
    IConfiguration configuration,
    IDistributedCache distributedCache)
{
    public bool WhiteLabelEnabled
    {
        get
        {
            var hideSettings = (configuration["web:hide-settings"] ?? "").Split(',', ';', ' ');
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

    public async Task<string> GetLogoDocsEditorAsync(bool dark)
    {
        var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        if (WhiteLabelEnabled)
        {
            return await tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, WhiteLabelLogoType.DocsEditor, dark);
        }
        return await tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(WhiteLabelLogoType.DocsEditor, dark);
    }

    public async Task<string> GetLogoDocsEditorEmbedAsync(bool dark)
    {
        var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

        if (WhiteLabelEnabled)
        {
            return await tenantWhiteLabelSettingsHelper.GetAbsoluteLogoPathAsync(tenantWhiteLabelSettings, WhiteLabelLogoType.DocsEditorEmbed, dark);
        }
        return await tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(WhiteLabelLogoType.DocsEditorEmbed, dark);
    }


    public async Task<string> GetLogoTextAsync()
    {
        if (WhiteLabelEnabled)
        {
            var tenantWhiteLabelSettings = await settingsManager.LoadAsync<TenantWhiteLabelSettings>();

            return await tenantWhiteLabelSettings.GetLogoTextAsync(settingsManager) ?? TenantWhiteLabelSettings.DefaultLogoText;
        }
        return TenantWhiteLabelSettings.DefaultLogoText;
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
            throw new BillingException(Resource.ErrorNotAllowedOption, "Customization");
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
            await distributedCache.RemoveAsync(GetCacheKey(customCulture));
        }

        await distributedCache.RemoveAsync(GetCacheKey(string.Empty));
    }


    private async Task<byte[]> GetMailLogoDataFromCacheAsync(string culture)
    {
        return await distributedCache.GetAsync(GetCacheKey(culture));
    }

    private async Task InsertMailLogoDataToCacheAsync(byte[] data, string culture)
    {
        await distributedCache.SetAsync(GetCacheKey(culture), data, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTime.UtcNow.Add(TimeSpan.FromDays(1))
        });
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
