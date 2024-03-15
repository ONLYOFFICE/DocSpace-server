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

using SKSvg = SkiaSharp.Extended.Svg.SKSvg;

namespace ASC.Web.Core.WhiteLabel;

public class TenantWhiteLabelSettings : ISettings<TenantWhiteLabelSettings>
{
    public const string DefaultLogoText = BaseWhiteLabelSettings.DefaultLogoText;

    #region Logos information: extension, isDefault, text for img auto generating

    public string LogoLightSmallExt { get; set; }
    public string DarkLogoLightSmallExt { get; set; }

    [JsonPropertyName("DefaultLogoLightSmall")]
    public bool IsDefaultLogoLightSmall { get; set; }

    public string LogoDarkExt { get; set; }
    public string DarkLogoDarkExt { get; set; }

    [JsonPropertyName("DefaultLogoDark")]
    public bool IsDefaultLogoDark { get; set; }

    public string LogoFaviconExt { get; set; }
    public string DarkLogoFaviconExt { get; set; }

    [JsonPropertyName("DefaultLogoFavicon")]
    public bool IsDefaultLogoFavicon { get; set; }

    public string LogoDocsEditorExt { get; set; }
    public string DarkLogoDocsEditorExt { get; set; }

    [JsonPropertyName("DefaultLogoDocsEditor")]
    public bool IsDefaultLogoDocsEditor { get; set; }

    public string LogoDocsEditorEmbedExt { get; set; }
    public string DarkLogoDocsEditorEmbedExt { get; set; }

    [JsonPropertyName("DefaultLogoDocsEditorEmbed")]
    public bool IsDefaultLogoDocsEditorEmbed { get; set; }

    public string LogoLeftMenuExt { get; set; }
    public string DarkLogoLeftMenuExt { get; set; }

    [JsonPropertyName("DefaultLogoLeftMenu")]
    public bool IsDefaultLogoLeftMenu { get; set; }

    public string LogoAboutPageExt { get; set; }
    public string DarkLogoAboutPageExt { get; set; }

    [JsonPropertyName("DefaultLogoAboutPage")]
    public bool IsDefaultLogoAboutPage { get; set; }

    public string LogoText { get; set; }

    public async Task<string> GetLogoTextAsync(SettingsManager settingsManager)
    {
        if (!string.IsNullOrEmpty(LogoText) && LogoText != DefaultLogoText)
        {
            return LogoText;
        }

        var partnerSettings = await settingsManager.LoadForDefaultTenantAsync<TenantWhiteLabelSettings>();
        return string.IsNullOrEmpty(partnerSettings.LogoText) ? DefaultLogoText : partnerSettings.LogoText;
    }

    public void SetLogoText(string val)
    {
        LogoText = val;
    }

    #endregion

    #region Logo available sizes

    public static readonly Size LogoLightSmallSize = new(422, 48);
    public static readonly Size LogoLoginPageSize = new(772, 88);
    public static readonly Size LogoFaviconSize = new(32, 32);
    public static readonly Size LogoDocsEditorSize = new(172, 40);
    public static readonly Size LogoDocsEditorEmbedSize = new(172, 40);
    public static readonly Size LogoLeftMenuSize = new(56, 56);
    public static readonly Size LogoAboutPageSize = new(442, 48);
    public static readonly Size LogoNotificationSize = new(386, 44);
    public static Size GetSize(WhiteLabelLogoType type)
    {
        return type switch
        {
            WhiteLabelLogoType.LightSmall => LogoLightSmallSize,
            WhiteLabelLogoType.LoginPage => LogoLoginPageSize,
            WhiteLabelLogoType.Favicon => LogoFaviconSize,
            WhiteLabelLogoType.DocsEditor => LogoDocsEditorSize,
            WhiteLabelLogoType.DocsEditorEmbed => LogoDocsEditorEmbedSize,
            WhiteLabelLogoType.LeftMenu => LogoLeftMenuSize,
            WhiteLabelLogoType.AboutPage => LogoAboutPageSize,
            _ => new Size()
        };
    }

    #endregion

    #region ISettings Members

    public TenantWhiteLabelSettings GetDefault()
    {
        return new TenantWhiteLabelSettings
        {
            LogoLightSmallExt = null,
            DarkLogoLightSmallExt = null,

            LogoDarkExt = null,
            DarkLogoDarkExt = null,

            LogoFaviconExt = null,
            DarkLogoFaviconExt = null,

            LogoDocsEditorExt = null,
            DarkLogoDocsEditorExt = null,

            LogoDocsEditorEmbedExt = null,
            DarkLogoDocsEditorEmbedExt = null,

            LogoLeftMenuExt = null,
            DarkLogoLeftMenuExt = null,

            LogoAboutPageExt = null,
            DarkLogoAboutPageExt = null,

            IsDefaultLogoLightSmall = true,
            IsDefaultLogoDark = true,
            IsDefaultLogoFavicon = true,
            IsDefaultLogoDocsEditor = true,
            IsDefaultLogoDocsEditorEmbed = true,
            IsDefaultLogoLeftMenu = true,
            IsDefaultLogoAboutPage = true,

            LogoText = null
        };
    }
    #endregion

    [JsonIgnore]
    public Guid ID
    {
        get { return new Guid("{05d35540-c80b-4b17-9277-abd9e543bf93}"); }
    }

    #region Get/Set IsDefault and Extension

    public bool GetIsDefault(WhiteLabelLogoType type)
    {
        return type switch
        {
            WhiteLabelLogoType.LightSmall => IsDefaultLogoLightSmall,
            WhiteLabelLogoType.LoginPage => IsDefaultLogoDark,
            WhiteLabelLogoType.Favicon => IsDefaultLogoFavicon,
            WhiteLabelLogoType.DocsEditor => IsDefaultLogoDocsEditor,
            WhiteLabelLogoType.DocsEditorEmbed => IsDefaultLogoDocsEditorEmbed,
            WhiteLabelLogoType.LeftMenu => IsDefaultLogoLeftMenu,
            WhiteLabelLogoType.AboutPage => IsDefaultLogoAboutPage,
            WhiteLabelLogoType.Notification => IsDefaultLogoDark,
            _ => true
        };
    }

    internal void SetIsDefault(WhiteLabelLogoType type, bool value)
    {
        switch (type)
        {
            case WhiteLabelLogoType.LightSmall:
                IsDefaultLogoLightSmall = value;
                break;
            case WhiteLabelLogoType.LoginPage:
                IsDefaultLogoDark = value;
                break;
            case WhiteLabelLogoType.Favicon:
                IsDefaultLogoFavicon = value;
                break;
            case WhiteLabelLogoType.DocsEditor:
                IsDefaultLogoDocsEditor = value;
                break;
            case WhiteLabelLogoType.DocsEditorEmbed:
                IsDefaultLogoDocsEditorEmbed = value;
                break;
            case WhiteLabelLogoType.LeftMenu:
                IsDefaultLogoLeftMenu = value;
                break;
            case WhiteLabelLogoType.AboutPage:
                IsDefaultLogoAboutPage = value;
                break;
        }
    }

    internal string GetExt(WhiteLabelLogoType type, bool dark)
    {
        return type switch
        {
            WhiteLabelLogoType.LightSmall => dark ? DarkLogoLightSmallExt : LogoLightSmallExt,
            WhiteLabelLogoType.LoginPage => dark ? DarkLogoDarkExt : LogoDarkExt,
            WhiteLabelLogoType.Favicon => dark ? DarkLogoFaviconExt : LogoFaviconExt,
            WhiteLabelLogoType.DocsEditor => dark ? DarkLogoDocsEditorExt : LogoDocsEditorExt,
            WhiteLabelLogoType.DocsEditorEmbed => dark ? DarkLogoDocsEditorEmbedExt : LogoDocsEditorEmbedExt,
            WhiteLabelLogoType.LeftMenu => dark ? DarkLogoLeftMenuExt : LogoLeftMenuExt,
            WhiteLabelLogoType.AboutPage => dark ? DarkLogoAboutPageExt : LogoAboutPageExt,
            WhiteLabelLogoType.Notification => "png",
            _ => ""
        };
    }

    internal void SetExt(WhiteLabelLogoType type, string fileExt, bool dark)
    {
        switch (type)
        {
            case WhiteLabelLogoType.LightSmall:
                if (dark)
                {
                    DarkLogoLightSmallExt = fileExt;
                }
                else
                {
                    LogoLightSmallExt = fileExt;
                }
                break;
            case WhiteLabelLogoType.LoginPage:
                if (dark)
                {
                    DarkLogoDarkExt = fileExt;
                }
                else
                {
                    LogoDarkExt = fileExt;
                }
                break;
            case WhiteLabelLogoType.Favicon:
                if (dark)
                {
                    DarkLogoFaviconExt = fileExt;
                }
                else
                {
                    LogoFaviconExt = fileExt;
                }
                break;
            case WhiteLabelLogoType.DocsEditor:
                if (dark)
                {
                    DarkLogoDocsEditorExt = fileExt;
                }
                else
                {
                    LogoDocsEditorExt = fileExt;
                }
                break;
            case WhiteLabelLogoType.DocsEditorEmbed:
                if (dark)
                {
                    DarkLogoDocsEditorEmbedExt = fileExt;
                }
                else
                {
                    LogoDocsEditorEmbedExt = fileExt;
                }
                break;
            case WhiteLabelLogoType.LeftMenu:
                if (dark)
                {
                    DarkLogoLeftMenuExt = fileExt;
                }
                else
                {
                    LogoLeftMenuExt = fileExt;
                }
                break;
            case WhiteLabelLogoType.AboutPage:
                if (dark)
                {
                    DarkLogoAboutPageExt = fileExt;
                }
                else
                {
                    LogoAboutPageExt = fileExt;
                }
                break;
        }
    }

    #endregion
}

[Scope]
public class TenantWhiteLabelSettingsHelper(WebImageSupplier webImageSupplier,
    UserPhotoManager userPhotoManager,
    StorageFactory storageFactory,
    WhiteLabelHelper whiteLabelHelper,
    TenantManager tenantManager,
    SettingsManager settingsManager,
    ILogger<TenantWhiteLabelSettingsHelper> logger)
{
    private const string ModuleName = "whitelabel";

    #region Restore default

    public async Task RestoreDefault(TenantWhiteLabelSettings tenantWhiteLabelSettings, TenantLogoManager tenantLogoManager, int tenantId, IDataStore storage = null)
    {
        tenantWhiteLabelSettings.LogoLightSmallExt = null;
        tenantWhiteLabelSettings.DarkLogoLightSmallExt = null;

        tenantWhiteLabelSettings.LogoDarkExt = null;
        tenantWhiteLabelSettings.DarkLogoDarkExt = null;

        tenantWhiteLabelSettings.LogoFaviconExt = null;
        tenantWhiteLabelSettings.DarkLogoFaviconExt = null;

        tenantWhiteLabelSettings.LogoDocsEditorExt = null;
        tenantWhiteLabelSettings.DarkLogoDocsEditorExt = null;

        tenantWhiteLabelSettings.LogoDocsEditorEmbedExt = null;
        tenantWhiteLabelSettings.DarkLogoDocsEditorEmbedExt = null;

        tenantWhiteLabelSettings.LogoLeftMenuExt = null;
        tenantWhiteLabelSettings.DarkLogoLeftMenuExt = null;

        tenantWhiteLabelSettings.LogoAboutPageExt = null;
        tenantWhiteLabelSettings.DarkLogoAboutPageExt = null;

        tenantWhiteLabelSettings.IsDefaultLogoLightSmall = true;
        tenantWhiteLabelSettings.IsDefaultLogoDark = true;
        tenantWhiteLabelSettings.IsDefaultLogoFavicon = true;
        tenantWhiteLabelSettings.IsDefaultLogoDocsEditor = true;
        tenantWhiteLabelSettings.IsDefaultLogoDocsEditorEmbed = true;
        tenantWhiteLabelSettings.IsDefaultLogoLeftMenu = true;
        tenantWhiteLabelSettings.IsDefaultLogoAboutPage = true;

        tenantWhiteLabelSettings.SetLogoText(null);

        var store = storage ?? await storageFactory.GetStorageAsync(tenantId, ModuleName);

        try
        {
            await store.DeleteFilesAsync("", "*", false);
        }
        catch (Exception e)
        {
            logger.ErrorRestoreDefault(e);
        }

        await SaveAsync(tenantWhiteLabelSettings, tenantId, tenantLogoManager, true);
    }

    #endregion

    #region Set logo

    private async Task SetLogoAsync(TenantWhiteLabelSettings tenantWhiteLabelSettings, WhiteLabelLogoType type, string logoFileExt, byte[] data, bool dark, IDataStore storage = null)
    {
        var store = storage ?? await storageFactory.GetStorageAsync(await tenantManager.GetCurrentTenantIdAsync(), ModuleName);

        #region delete from storage if already exists

        var isAlreadyHaveBeenChanged = !tenantWhiteLabelSettings.GetIsDefault(type);

        if (isAlreadyHaveBeenChanged)
        {
            try
            {
                await DeleteLogoFromStore(tenantWhiteLabelSettings, store, type, dark);
            }
            catch (Exception e)
            {
                logger.ErrorSetLogo(e);
            }
        }
        #endregion

        using var memory = new MemoryStream(data);
        var logoFileName = BuildLogoFileName(type, logoFileExt, dark);

        memory.Seek(0, SeekOrigin.Begin);
        await store.SaveAsync(logoFileName, memory);
    }

    public async Task SetLogo(TenantWhiteLabelSettings tenantWhiteLabelSettings, Dictionary<int, KeyValuePair<string, string>> logo, IDataStore storage = null)
    {
        foreach (var currentLogo in logo)
        {
            var currentLogoType = (WhiteLabelLogoType)currentLogo.Key;

            var (lightData, extLight) = await GetLogoData(currentLogo.Value.Key);

            var (darkData, extDark) = await GetLogoData(currentLogo.Value.Value);

            if (lightData == null && darkData == null)
            {
                return;
            }

            if (lightData != null)
            {
                await SetLogoAsync(tenantWhiteLabelSettings, currentLogoType, extLight, lightData, false, storage);
                tenantWhiteLabelSettings.SetExt(currentLogoType, extLight, false);
                if (currentLogoType == WhiteLabelLogoType.LoginPage)
                {
                    var (notificationData, extNotification) = GetNotificationLogoData(lightData, extLight, tenantWhiteLabelSettings);

                    if (notificationData != null)
                    {
                        await SetLogoAsync(tenantWhiteLabelSettings, WhiteLabelLogoType.Notification, extNotification, notificationData, false, storage);
                    }
                }
            }

            if (darkData != null && CanBeDark(currentLogoType))
            {
                await SetLogoAsync(tenantWhiteLabelSettings, currentLogoType, extDark, darkData, true, storage);
                tenantWhiteLabelSettings.SetExt(currentLogoType, extDark, true);
            }

            tenantWhiteLabelSettings.SetIsDefault(currentLogoType, false);
        }
    }

    private async Task<(byte[], string)> GetLogoData(string logo)
    {
        var supportedFormats = new[]
        {
            new {
                    mime = "image/jpeg",
                    ext = "jpg"
                },
            new {
                    mime = "image/png",
                    ext = "png"
                },
            new {
                    mime = "image/svg+xml",
                    ext = "svg"
                }
        };

        string ext = null;

        if (!string.IsNullOrEmpty(logo))
        {
            byte[] data;
            var format = supportedFormats.FirstOrDefault(r => logo.StartsWith($"data:{r.mime};base64,"));
            if (format == null)
            {
                var fileName = Path.GetFileName(logo);
                ext = fileName.Split('.').Last();
                data = await userPhotoManager.GetTempPhotoData(fileName);
                try
                {
                    await userPhotoManager.RemoveTempPhotoAsync(fileName);
                }
                catch (Exception ex)
                {
                    logger.ErrorSetLogo(ex);
                }
            }
            else
            {
                ext = format.ext;
                var xB64 = logo[$"data:{format.mime};base64,".Length..]; // Get the Base64 string
                data = Convert.FromBase64String(xB64); // Convert the Base64 string to binary data
            }

            return (data, ext);
        }

        return (null, ext);
    }

    private (byte[], string) GetNotificationLogoData(byte[] logoData, string extLogo, TenantWhiteLabelSettings tenantWhiteLabelSettings)
    {
        var extNotification = tenantWhiteLabelSettings.GetExt(WhiteLabelLogoType.Notification, false);

        switch (extLogo)
        {
            case "png":
                return (logoData, extNotification);
            case "svg":
                return (GetLogoDataFromSvg(), extNotification);
            case "jpg":
            case "jpeg":
                return (GetLogoDataFromJpg(), extNotification);
            default:
                return (null, extNotification);
        }

        byte[] GetLogoDataFromSvg()
        {
            var size = GetSize(WhiteLabelLogoType.Notification);
            var skSize = new SKSize(size.Width, size.Height);

            var svg = new SKSvg(skSize);

            using (var stream = new MemoryStream(logoData))
            {
                svg.Load(stream);
            }

            using (var bitMap = new SKBitmap((int)svg.CanvasSize.Width, (int)svg.CanvasSize.Height))
            using (var canvas = new SKCanvas(bitMap))
            {
                canvas.DrawPicture(svg.Picture);

                using (var image = SKImage.FromBitmap(bitMap))
                using (var pngData = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    return pngData.ToArray();
                }
            }
        }

        byte[] GetLogoDataFromJpg()
        {
            using var image = SKImage.FromEncodedData(logoData);
            using var pngData = image.Encode(SKEncodedImageFormat.Png, 100);
            return pngData.ToArray();
        }
    }

    public async Task SetLogoFromStream(TenantWhiteLabelSettings tenantWhiteLabelSettings, WhiteLabelLogoType type, string fileExt, Stream fileStream, bool dark, IDataStore storage = null)
    {
        var data = GetData(fileStream);

        var canSet = true;
        if (dark)
        {
            canSet = CanBeDark(type);
        }
        if (data != null && canSet)
        {
            await SetLogoAsync(tenantWhiteLabelSettings, type, fileExt, data, dark, storage);
            tenantWhiteLabelSettings.SetExt(type, fileExt, dark);
        }

        tenantWhiteLabelSettings.SetIsDefault(type, false);
    }

    private byte[] GetData(Stream stream)
    {
        if (stream != null)
        {
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
        return Array.Empty<byte>();
    }

    #endregion

    #region Get logo path

    public async Task<string> GetAbsoluteLogoPathAsync(TenantWhiteLabelSettings tenantWhiteLabelSettings, WhiteLabelLogoType type, bool dark = false)
    {
        if (tenantWhiteLabelSettings.GetIsDefault(type))
        {
            return await GetAbsoluteDefaultLogoPathAsync(type, dark);
        }

        return await GetAbsoluteStorageLogoPath(tenantWhiteLabelSettings, type, dark);
    }

    private async Task<string> GetAbsoluteStorageLogoPath(TenantWhiteLabelSettings tenantWhiteLabelSettings, WhiteLabelLogoType type, bool dark)
    {
        var store = await storageFactory.GetStorageAsync(await tenantManager.GetCurrentTenantIdAsync(), ModuleName);
        var fileName = BuildLogoFileName(type, tenantWhiteLabelSettings.GetExt(type, dark), dark);

        if (await store.IsFileAsync(fileName))
        {
            return (await store.GetUriAsync(fileName)).ToString();
        }
        return await GetAbsoluteDefaultLogoPathAsync(type, dark);
    }

    public async Task<string> GetAbsoluteDefaultLogoPathAsync(WhiteLabelLogoType type, bool dark)
    {
        var partnerLogoPath = await GetPartnerStorageLogoPathAsync(type, dark);
        if (!string.IsNullOrEmpty(partnerLogoPath))
        {
            return partnerLogoPath;
        }

        var ext = type switch
        {
            WhiteLabelLogoType.Favicon => "ico",
            WhiteLabelLogoType.Notification => "png",
            _ => "svg"
        };

        var path = type switch
        {
            WhiteLabelLogoType.Notification => "notifications/",
            _ => "logo/"
        };

        return webImageSupplier.GetAbsoluteWebPath(path + BuildLogoFileName(type, ext, dark));
    }

    private async Task<string> GetPartnerStorageLogoPathAsync(WhiteLabelLogoType type, bool dark)
    {
        var partnerSettings = await settingsManager.LoadForDefaultTenantAsync<TenantWhiteLabelSettings>();

        if (partnerSettings.GetIsDefault(type))
        {
            return null;
        }

        var partnerStorage = await storageFactory.GetStorageAsync(Tenant.DefaultTenant, "static_partnerdata");

        if (partnerStorage == null)
        {
            return null;
        }

        var logoPath = BuildLogoFileName(type, partnerSettings.GetExt(type, dark), dark);

        return (await partnerStorage.IsFileAsync(logoPath)) ? (await partnerStorage.GetUriAsync(logoPath)).ToString() : null;
    }

    #endregion

    #region Get Whitelabel Logo Stream

    /// <summary>
    /// Get logo stream or null in case of default whitelabel
    /// </summary>
    public async Task<Stream> GetWhitelabelLogoData(TenantWhiteLabelSettings tenantWhiteLabelSettings, WhiteLabelLogoType type, bool dark = false)
    {
        if (tenantWhiteLabelSettings.GetIsDefault(type))
        {
            return await GetPartnerStorageLogoData(type, dark);
        }

        return await GetStorageLogoData(tenantWhiteLabelSettings, type, dark);
    }

    private async Task<Stream> GetStorageLogoData(TenantWhiteLabelSettings tenantWhiteLabelSettings, WhiteLabelLogoType type, bool dark)
    {
        var storage = await storageFactory.GetStorageAsync(await tenantManager.GetCurrentTenantIdAsync(), ModuleName);

        if (storage == null)
        {
            return null;
        }

        var fileName = BuildLogoFileName(type, tenantWhiteLabelSettings.GetExt(type, dark), dark);

        return await storage.IsFileAsync(fileName) ? await storage.GetReadStreamAsync(fileName) : null;
    }

    private async Task<Stream> GetPartnerStorageLogoData(WhiteLabelLogoType type, bool dark)
    {
        var partnerSettings = await settingsManager.LoadForDefaultTenantAsync<TenantWhiteLabelSettings>();

        if (partnerSettings.GetIsDefault(type))
        {
            return null;
        }

        var partnerStorage = await storageFactory.GetStorageAsync(Tenant.DefaultTenant, "static_partnerdata");

        if (partnerStorage == null)
        {
            return null;
        }

        var fileName = BuildLogoFileName(type, partnerSettings.GetExt(type, dark), dark);

        return await partnerStorage.IsFileAsync(fileName) ? await partnerStorage.GetReadStreamAsync(fileName) : null;
    }

    #endregion

    private static string BuildLogoFileName(WhiteLabelLogoType type, string fileExt, bool dark)
    {
        if (CanBeDark(type))
        {
            return $"{(dark ? "dark_" : "")}{type.ToString().ToLowerInvariant()}.{fileExt}";
        }

        return $"{type.ToString().ToLowerInvariant()}.{fileExt}";
    }

    private static Size GetSize(WhiteLabelLogoType type)
    {
        return type switch
        {
            WhiteLabelLogoType.LightSmall => TenantWhiteLabelSettings.LogoLightSmallSize,
            WhiteLabelLogoType.LoginPage => TenantWhiteLabelSettings.LogoLoginPageSize,
            WhiteLabelLogoType.Favicon => TenantWhiteLabelSettings.LogoFaviconSize,
            WhiteLabelLogoType.DocsEditor => TenantWhiteLabelSettings.LogoDocsEditorSize,
            WhiteLabelLogoType.DocsEditorEmbed => TenantWhiteLabelSettings.LogoDocsEditorEmbedSize,
            WhiteLabelLogoType.LeftMenu => TenantWhiteLabelSettings.LogoLeftMenuSize,
            WhiteLabelLogoType.AboutPage => TenantWhiteLabelSettings.LogoAboutPageSize,
            WhiteLabelLogoType.Notification => TenantWhiteLabelSettings.LogoNotificationSize,
            _ => new Size(0, 0)
        };
    }

    #region Save for Resource replacement

    private static readonly List<int> _appliedTenants = new();

    public async Task ApplyAsync(TenantWhiteLabelSettings tenantWhiteLabelSettings, int tenantId)
    {
        if (_appliedTenants.Contains(tenantId))
        {
            return;
        }

        await SetNewLogoTextAsync(tenantWhiteLabelSettings, tenantId);

        if (!_appliedTenants.Contains(tenantId))
        {
            _appliedTenants.Add(tenantId);
        }
    }

    public async Task SaveAsync(TenantWhiteLabelSettings tenantWhiteLabelSettings, int tenantId, TenantLogoManager tenantLogoManager, bool restore = false)
    {
        await settingsManager.SaveAsync(tenantWhiteLabelSettings, tenantId);

        if (tenantId == Tenant.DefaultTenant)
        {
            _appliedTenants.Clear();
        }
        else
        {
            await SetNewLogoTextAsync(tenantWhiteLabelSettings, tenantId, restore);
            await tenantLogoManager.RemoveMailLogoDataFromCacheAsync();
        }
    }

    private async Task SetNewLogoTextAsync(TenantWhiteLabelSettings tenantWhiteLabelSettings, int tenantId, bool restore = false)
    {
        whiteLabelHelper.DefaultLogoText = TenantWhiteLabelSettings.DefaultLogoText;
        var partnerSettings = await settingsManager.LoadForDefaultTenantAsync<TenantWhiteLabelSettings>();

        if (restore && string.IsNullOrEmpty(await partnerSettings.GetLogoTextAsync(settingsManager)))
        {
            whiteLabelHelper.RestoreOldText(tenantId);
        }
        else
        {
            whiteLabelHelper.SetNewText(tenantId, await tenantWhiteLabelSettings.GetLogoTextAsync(settingsManager));
        }
    }

    #endregion

    #region Delete from Store

    private async Task DeleteLogoFromStore(TenantWhiteLabelSettings tenantWhiteLabelSettings, IDataStore store, WhiteLabelLogoType type, bool dark)
    {
        await DeleteLogoFromStoreByGeneral(tenantWhiteLabelSettings, store, type, dark);
    }

    private async Task DeleteLogoFromStoreByGeneral(TenantWhiteLabelSettings tenantWhiteLabelSettings, IDataStore store, WhiteLabelLogoType type, bool dark)
    {
        var fileExt = tenantWhiteLabelSettings.GetExt(type, dark);
        var logo = BuildLogoFileName(type, fileExt, dark);
        if (await store.IsFileAsync(logo))
        {
            await store.DeleteAsync(logo);
        }
    }

    #endregion

    private static bool CanBeDark(WhiteLabelLogoType type)
    {
        return type switch
        {
            WhiteLabelLogoType.Favicon => false,
            WhiteLabelLogoType.DocsEditor => false,
            WhiteLabelLogoType.DocsEditorEmbed => false,
            _ => true
        };
    }
}
