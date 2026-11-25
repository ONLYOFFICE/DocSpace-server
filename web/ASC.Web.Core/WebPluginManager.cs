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

using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ASC.Web.Core;

public class WebPlugin
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string MinDocSpaceVersion { get; set; }
    public string Description { get; set; }
    public string License { get; set; }
    public string Author { get; set; }
    public string HomePage { get; set; }
    public string PluginName { get; set; }
    public string Scopes { get; set; }
    public string Image { get; set; }
    public string CspDomains { get; set; }
    public Guid CreateBy { get; set; }
    public DateTime CreateOn { get; set; }
    public bool Enabled { get; set; }
    public bool System { get; set; }
    [JsonIgnore]
    public bool Personal { get; set; }
    public string Url { get; set; }
    public string CssUrl { get; set; }
    public string Settings { get; set; }
    public Dictionary<string,string> NameLocale { get; set; }
    public Dictionary<string,string> DescriptionLocale { get; set; }
    public string Runtime { get; set; }

    public WebPlugin Clone()
    {
        return (WebPlugin)MemberwiseClone();
    }
}

[Scope]
public class WebPluginManager(
    CoreBaseSettings coreBaseSettings,
    SettingsManager settingsManager,
    QuotaSocketManager quotaSocketManager,
    InstanceCrypto instanceCrypto,
    WebPluginConfigSettings webPluginConfigSettings,
    StorageFactory storageFactory,
    AuthContext authContext,
    UserManager userManager,
    TempPath tempPath,
    ILogger<WebPluginManager> log,
    IFusionCacheProvider cacheProvider)
{
    private const string StorageSystemModuleName = "systemwebplugins";
    private const string StorageModuleName = "webplugins";
    private const string StoragePersonalModuleName = "personalwebplugins";

    public const string PersonalCspCookieName = "asc_personal_csp";
    private const string ConfigFileName = "config.json";
    private const int MaxPersonalCspDomainsLength = 15 * 1024;
    private const string PluginFileName = "plugin.js";
    private const string PluginCssFileName = "plugin.css";
    private const string AssetsFolderName = "assets";

    private static readonly JsonSerializerOptions _configJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IFusionCache _cache = cacheProvider.GetMemoryCache();

    private void DemandWebPlugins(bool upload = false, bool delete = false)
    {
        if (!webPluginConfigSettings.Enabled)
        {
            throw new SecurityException("Plugins disabled");
        }

        if ((upload && !webPluginConfigSettings.Upload) || (delete && !webPluginConfigSettings.Delete))
        {
            throw new SecurityException("Forbidden action");
        }
    }

    public async Task DemandPersonalWebPluginsAsync(bool upload = false)
    {
        DemandWebPlugins(upload: upload);

        if (!await IsPersonalWebPluginsAllowedAsync())
        {
            throw new SecurityException("Personal plugins disabled");
        }
    }

    private async Task<bool> IsPersonalWebPluginsAllowedAsync()
    {
        return await IsPersonalWebPluginsEnabledAsync() && !await userManager.IsGuestAsync(authContext.CurrentAccount.ID);
    }

    public async Task<bool> IsPersonalWebPluginsEnabledAsync()
    {
        return webPluginConfigSettings.Personal && (await settingsManager.LoadAsync<PersonalWebPluginsSettings>()).Enabled;
    }

    public async Task SetPersonalWebPluginsEnabledAsync(bool enabled)
    {
        DemandWebPlugins();

        if (!webPluginConfigSettings.Personal)
        {
            throw new SecurityException("Personal plugins disabled");
        }

        await settingsManager.SaveAsync(new PersonalWebPluginsSettings { Enabled = enabled });

        try
        {
            await quotaSocketManager.ChangePersonalWebPluginsAsync(enabled);
        }
        catch (Exception e)
        {
            log.WarningPersonalWebPluginsChangeNotSent(e);
        }
    }

    private async Task<IDataStore> GetPluginStorageAsync(int tenantId, bool personal = false)
    {
        var module = personal ? StoragePersonalModuleName : tenantId == Tenant.DefaultTenant ? StorageSystemModuleName : StorageModuleName;

        var storage = await storageFactory.GetStorageAsync(tenantId, module);

        return storage;
    }

    private static string GetCacheKey(int tenantId, Guid? owner = null)
    {
        return owner.HasValue ? $"{StorageModuleName}:{tenantId}:{owner}" : $"{StorageModuleName}:{tenantId}";
    }

    private static string GetBasePath(Guid? owner)
    {
        return owner?.ToString() ?? string.Empty;
    }

    public static void ClearManifestState(WebPlugin webPlugin, bool system)
    {
        if (system)
        {
            return;
        }

        webPlugin.Enabled = false;
        webPlugin.Settings = null;
        webPlugin.CssUrl = null;
    }

    public static void PrepareStoredPlugin(WebPlugin webPlugin, bool system, Guid? owner)
    {
        webPlugin.System = system;
        webPlugin.Personal = owner.HasValue;

        if (owner.HasValue)
        {
            webPlugin.CreateBy = owner.Value;
        }

        ClearManifestState(webPlugin, system);
    }

    private static bool IsPluginConfigPath(string path)
    {
        return path.Split('/', '\\').Length == 2;
    }

    private static bool ClashesWith(WebPlugin webPlugin, IEnumerable<WebPlugin> webPlugins)
    {
        return webPlugins.Any(x =>
            string.Equals(x.Name, webPlugin.Name, StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals(x.PluginName, webPlugin.PluginName, StringComparison.InvariantCulture));
    }

    private Task<WebPluginSettings> LoadWebPluginSettingsAsync(bool personal)
    {
        return personal ? settingsManager.LoadForCurrentUserAsync<WebPluginSettings>() : settingsManager.LoadAsync<WebPluginSettings>();
    }

    private Task<bool> SaveWebPluginSettingsAsync(WebPluginSettings webPluginSettings, bool personal)
    {
        return personal ? settingsManager.SaveForCurrentUserAsync(webPluginSettings) : settingsManager.SaveAsync(webPluginSettings);
    }

    // The name is also the plugin folder in the storage, so it must never carry a path
    private static bool IsValidName(string name)
    {
        return !string.IsNullOrEmpty(name) && Regex.IsMatch(name, @"^[a-z0-9_.-]+$") && !name.StartsWith('.');
    }

    // The cache holds the plugin manifests read from the storage, not the enabled state and the settings of a portal,
    // so it is reset when the storage changes and otherwise expires after an hour
    private ValueTask ResetWebPluginsCacheAsync(int tenantId, Guid? owner = null)
    {
        return owner.HasValue
            ? _cache.RemoveAndNotifyAsync(GetCacheKey(tenantId, owner))
            : _cache.RemoveByTagAndNotifyAsync(CacheExtention.GetWebPluginsTag(tenantId));
    }

    private static async Task<string> GetPluginUrlTemplateAsync(IDataStore storage, string basePath)
    {
        var uri = await storage.GetUriAsync(Path.Combine(basePath, "{0}", PluginFileName));

        return uri?.ToString() ?? string.Empty;
    }

    public async Task<WebPlugin> AddWebPluginFromFileAsync(int tenantId, IFormFile file, bool system, bool personal = false)
    {
        if (personal)
        {
            await DemandPersonalWebPluginsAsync(upload: true);

            if (system)
            {
                throw new ArgumentException(Resource.ErrorWebPluginForbiddenSystem);
            }
        }
        else
        {
            DemandWebPlugins(upload: true);
        }

        if (system && !coreBaseSettings.Standalone)
        {
            throw new InvalidOperationException(Resource.ErrorWebPluginForbiddenSystem);
        }

        if (Path.GetExtension(file.FileName).ToLowerInvariant() != webPluginConfigSettings.Extension)
        {
            throw new ArgumentException(Resource.ErrorWebPluginFileExtension);
        }

        if (file.Length <= 0)
        {
            throw new ArgumentException(Resource.ErrorWebPluginNoInputFile);
        }

        if (file.Length > webPluginConfigSettings.MaxSize)
        {
            throw new ArgumentException(Resource.ErrorWebPluginFileSize);
        }

        var owner = personal ? authContext.CurrentAccount.ID : (Guid?)null;

        var existingWebPlugins = owner.HasValue
            ? await GetWebPluginsFromCacheAsync(tenantId, owner)
            : await GetWebPluginsForTenantAsync(tenantId);

        string tempDirToDelete = null;

        try
        {
            using var zipFile = new ZipFile(file.OpenReadStream());

            long maxEntriesCount = webPluginConfigSettings.AssetMaxCount + 3; // asset files, config file, plugin file, asset folder

            AnalyzeZip(zipFile, webPluginConfigSettings.MaxSize, maxEntriesCount, 100, 2); // 100:1 ratio, 2 depth levels

            var (webPlugin, tempPath) = await SafeExtractPluginToTemp(zipFile, tenantId, system, owner);

            tempDirToDelete = tempPath;

            var maxCount = owner.HasValue ? webPluginConfigSettings.PersonalMaxCount : webPluginConfigSettings.MaxCount;

            if (existingWebPlugins.Count + 1 > maxCount &&
                !existingWebPlugins.Exists(x => string.Equals(x.Name, webPlugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new WebPluginRefusedException(Resource.ErrorWebPluginMaxCount);
            }

            var storage = await GetPluginStorageAsync(system ? Tenant.DefaultTenant : tenantId, personal);

            var basePath = GetBasePath(owner);

            await CopyPluginFromTempToStore(webPlugin, tempPath, storage, basePath, owner.HasValue ? ACL.Private : ACL.Auto);

            await ResetWebPluginsCacheAsync(system ? Tenant.DefaultTenant : tenantId, owner);

            webPlugin.System = system;
            webPlugin.Personal = personal;

            var urlTemplate = await GetPluginUrlTemplateAsync(storage, basePath);

            var hash = string.IsNullOrEmpty(webPlugin.Version) ? string.Empty : $"?hash={webPlugin.Version}";

            webPlugin.Url = string.Format(urlTemplate, webPlugin.Name) + hash;

            if (await storage.IsFileAsync(Path.Combine(basePath, webPlugin.Name, PluginCssFileName)))
            {
                webPlugin.CssUrl = webPlugin.Url.Replace(PluginFileName, PluginCssFileName);
            }

            var existingSettings = await GetWebPluginSettingsAsync(webPlugin.Name, personal);

            webPlugin = await UpdateWebPluginAsync(webPlugin, true, existingSettings);

            return webPlugin;
        }
        catch (Exception ex) when (ex is not WebPluginRefusedException)
        {
            throw new ArgumentException($"{Resource.ErrorWebPluginArchive}. {ex.Message}");
        }
        finally
        {
            if (!string.IsNullOrEmpty(tempDirToDelete) && Directory.Exists(tempDirToDelete))
            {
                Directory.Delete(tempDirToDelete, true);
            }
        }
    }

    private async Task ValidatePlugin(WebPlugin webPlugin, int tenantId, bool system, Guid? owner)
    {
        if (!IsValidName(webPlugin.Name))
        {
            throw new Exception(Resource.ErrorWebPluginName);
        }

        var jsVariableRegex = new Regex(@"^[0-9a-zA-Z_$]+$");

        if (string.IsNullOrEmpty(webPlugin.PluginName) || !jsVariableRegex.IsMatch(webPlugin.PluginName))
        {
            throw new Exception(Resource.ErrorWebPluginName);
        }

        if (owner.HasValue)
        {
            if (ClashesWith(webPlugin, await GetWebPluginsForTenantAsync(tenantId)))
            {
                throw new WebPluginRefusedException(Resource.ErrorWebPluginInstalledForPortal);
            }

            var personalWebPlugins = await GetWebPluginsFromCacheAsync(tenantId, owner);

            if (personalWebPlugins.Any(x =>
                    x.PluginName.Equals(webPlugin.PluginName, StringComparison.InvariantCulture) &&
                    !x.Name.Equals(webPlugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new WebPluginRefusedException(Resource.ErrorWebPluginExist);
            }

            if (!IsValidPersonalCspDomains(webPlugin.CspDomains))
            {
                throw new WebPluginRefusedException(Resource.ErrorWebPluginCspDomains);
            }

            return;
        }

        var systemWebPlugins = await GetWebPluginsFromCacheAsync(Tenant.DefaultTenant);

        var tenantWebPlugins = await GetWebPluginsFromCacheAsync(tenantId);

        if (system)
        {
            if (tenantWebPlugins.Any(x =>
                    x.PluginName.Equals(webPlugin.PluginName, StringComparison.InvariantCulture) ||
                    x.Name.Equals(webPlugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new WebPluginRefusedException(Resource.ErrorWebPluginExist);
            }

            if (systemWebPlugins.Any(x =>
                    x.PluginName.Equals(webPlugin.PluginName, StringComparison.InvariantCulture) &&
                    !x.Name.Equals(webPlugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new WebPluginRefusedException(Resource.ErrorWebPluginExist);
            }
        }
        else
        {
            if (systemWebPlugins.Any(x =>
                    x.PluginName.Equals(webPlugin.PluginName, StringComparison.InvariantCulture) ||
                    x.Name.Equals(webPlugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new WebPluginRefusedException(Resource.ErrorWebPluginExist);
            }

            if (tenantWebPlugins.Any(x =>
                    x.PluginName.Equals(webPlugin.PluginName, StringComparison.InvariantCulture) &&
                    !x.Name.Equals(webPlugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new WebPluginRefusedException(Resource.ErrorWebPluginExist);
            }
        }
    }

    public async Task<List<WebPlugin>> GetWebPluginsAsync(int tenantId)
    {
        DemandWebPlugins();

        var webPlugins = await GetWebPluginsForTenantAsync(tenantId);

        await ApplyWebPluginStatesAsync(webPlugins, false);

        return webPlugins;
    }

    public async Task<List<WebPlugin>> GetPersonalWebPluginsAsync(int tenantId)
    {
        await DemandPersonalWebPluginsAsync();

        var webPlugins = (await GetWebPluginsFromCacheAsync(tenantId, authContext.CurrentAccount.ID))
            .Select(x => x.Clone())
            .ToList();

        webPlugins.ForEach(x => x.Personal = true);

        await ApplyWebPluginStatesAsync(webPlugins, true);

        return webPlugins;
    }

    public async Task<List<WebPlugin>> GetAvailableWebPluginsAsync(int tenantId)
    {
        var webPlugins = await GetWebPluginsAsync(tenantId);

        if (!await IsPersonalWebPluginsAllowedAsync())
        {
            return webPlugins;
        }

        var personalWebPlugins = await GetPersonalWebPluginsAsync(tenantId);

        webPlugins.AddRange(personalWebPlugins.Where(x => !ClashesWith(x, webPlugins)).ToList());

        return webPlugins;
    }

    public async Task<List<string>> GetPersonalCspDomainsAsync(int tenantId)
    {
        if (!authContext.IsAuthenticated || !webPluginConfigSettings.Enabled || !await IsPersonalWebPluginsAllowedAsync())
        {
            return [];
        }

        var personalWebPlugins = (await GetWebPluginsFromCacheAsync(tenantId, authContext.CurrentAccount.ID))
            .Where(x => !string.IsNullOrWhiteSpace(x.CspDomains))
            .ToList();

        if (personalWebPlugins.Count == 0)
        {
            return [];
        }

        var webPluginStates = (await LoadWebPluginSettingsAsync(true))?.EnabledPlugins ?? [];
        var webPlugins = await GetWebPluginsForTenantAsync(tenantId);

        return personalWebPlugins
            .Where(x => webPluginStates.TryGetValue(x.Name, out var state) && state.Enabled && !ClashesWith(x, webPlugins))
            .SelectMany(x => SplitCspDomains(x.CspDomains))
            .Distinct()
            .ToList();
    }

    public static List<string> GetPersonalCspDomains(IEnumerable<WebPlugin> availableWebPlugins)
    {
        return availableWebPlugins
            .Where(x => x.Personal && x.Enabled && !string.IsNullOrWhiteSpace(x.CspDomains))
            .SelectMany(x => SplitCspDomains(x.CspDomains))
            .Distinct()
            .ToList();
    }

    private static string[] SplitCspDomains(string cspDomains)
    {
        return cspDomains.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool IsValidPersonalCspDomains(string cspDomains)
    {
        if (string.IsNullOrWhiteSpace(cspDomains))
        {
            return true;
        }

        return cspDomains.Length <= MaxPersonalCspDomainsLength && SplitCspDomains(cspDomains).All(IsValidPersonalCspDomain);
    }

    public static bool IsValidPersonalCspDomain(string domain)
    {
        if (domain.EndsWith(':') || domain.Any(c => char.IsWhiteSpace(c) || c is ';' or '\'' or '"' or ',') || !CspSettings.IsValidDomain(domain))
        {
            return false;
        }

        var schemeEnd = domain.IndexOf(Uri.SchemeDelimiter, StringComparison.Ordinal);
        var source = schemeEnd < 0 ? $"{Uri.UriSchemeHttps}{Uri.SchemeDelimiter}{domain}" : domain;

        if (schemeEnd >= 0 && domain[..schemeEnd].ToLowerInvariant() is not ("https" or "http" or "wss" or "ws"))
        {
            return false;
        }

        var wildcard = source.Contains($"{Uri.SchemeDelimiter}*.", StringComparison.Ordinal);

        if (!Uri.TryCreate(wildcard ? source.Replace($"{Uri.SchemeDelimiter}*.", Uri.SchemeDelimiter) : source, UriKind.Absolute, out var uri) ||
            uri.HostNameType is not (UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6))
        {
            return false;
        }

        return !wildcard || uri.Host.Split('.').Length >= 2;
    }

    public async Task<List<WebPlugin>> GetMembersWebPluginsAsync(int tenantId)
    {
        DemandWebPlugins();

        var storage = await GetPluginStorageAsync(tenantId, true);

        var configFiles = await storage.ListFilesRelativeAsync(string.Empty, string.Empty, ConfigFileName, true).ToArrayAsync();

        var webPlugins = new List<WebPlugin>();

        foreach (var owner in GetPersonalPluginOwners(configFiles))
        {
            webPlugins.AddRange(await GetMemberWebPluginsAsync(tenantId, owner));
        }

        return webPlugins;
    }

    public static List<Guid> GetPersonalPluginOwners(IEnumerable<string> configFiles)
    {
        return configFiles
            .Select(path => path.Split('/', '\\'))
            .Where(segments => segments.Length == 3)
            .Select(segments => Guid.TryParse(segments[0], out var owner) ? owner : Guid.Empty)
            .Where(owner => owner != Guid.Empty)
            .Distinct()
            .ToList();
    }

    private async Task<List<WebPlugin>> GetMemberWebPluginsAsync(int tenantId, Guid owner)
    {
        var webPlugins = (await GetWebPluginsFromCacheAsync(tenantId, owner))
            .Select(x => x.Clone())
            .ToList();

        if (webPlugins.Count == 0)
        {
            return webPlugins;
        }

        var enabledPlugins = (await settingsManager.LoadAsync<WebPluginSettings>(owner))?.EnabledPlugins ?? [];

        foreach (var webPlugin in webPlugins)
        {
            webPlugin.Personal = true;
            webPlugin.Enabled = enabledPlugins.TryGetValue(webPlugin.Name, out var state) && state.Enabled;
            webPlugin.Settings = null;
        }

        return webPlugins;
    }

    private async Task<WebPlugin> GetMemberWebPluginByNameAsync(int tenantId, Guid owner, string name)
    {
        return (await GetMemberWebPluginsAsync(tenantId, owner))
            .Find(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) ?? throw new ItemNotFoundException(Resource.ErrorWebPluginNotFound);
    }

    public async Task<WebPlugin> SetMemberWebPluginEnabledAsync(int tenantId, Guid owner, string name, bool enabled)
    {
        DemandWebPlugins();

        var webPlugin = await GetMemberWebPluginByNameAsync(tenantId, owner, name);

        var webPluginSettings = await settingsManager.LoadAsync<WebPluginSettings>(owner);

        var enabledPlugins = webPluginSettings.EnabledPlugins ?? [];

        enabledPlugins.TryGetValue(webPlugin.Name, out var state);

        if (enabled || !string.IsNullOrEmpty(state?.Settings))
        {
            enabledPlugins[webPlugin.Name] = new WebPluginState(enabled, state?.Settings);
        }
        else
        {
            enabledPlugins.Remove(webPlugin.Name);
        }

        webPluginSettings.EnabledPlugins = enabledPlugins.Count != 0 ? enabledPlugins : null;

        await settingsManager.SaveAsync(webPluginSettings, owner);

        webPlugin.Enabled = enabled;

        await SendPersonalWebPluginChangeAsync(owner, webPlugin.Name, enabled);

        return webPlugin;
    }

    public async Task<WebPlugin> DeleteMemberWebPluginAsync(int tenantId, Guid owner, string name)
    {
        DemandWebPlugins();

        var webPlugin = await GetMemberWebPluginByNameAsync(tenantId, owner, name);

        var storage = await GetPluginStorageAsync(tenantId, true);

        await storage.DeleteDirectoryAsync(Path.Combine(GetBasePath(owner), webPlugin.Name));

        await ResetWebPluginsCacheAsync(tenantId, owner);

        var webPluginSettings = await settingsManager.LoadAsync<WebPluginSettings>(owner);

        if (webPluginSettings.EnabledPlugins?.Remove(webPlugin.Name) == true)
        {
            webPluginSettings.EnabledPlugins = webPluginSettings.EnabledPlugins.Count != 0 ? webPluginSettings.EnabledPlugins : null;

            await settingsManager.SaveAsync(webPluginSettings, owner);
        }

        webPlugin.Enabled = false;

        await SendPersonalWebPluginChangeAsync(owner, webPlugin.Name, false);

        return webPlugin;
    }

    private async Task SendPersonalWebPluginChangeAsync(Guid owner, string name, bool enabled)
    {
        try
        {
            await quotaSocketManager.ChangePersonalWebPluginAsync(owner, name, enabled);
        }
        catch (Exception e)
        {
            log.WarningWebPluginChangeNotSent(name, e);
        }
    }

    public async Task<WebPlugin> GetPersonalWebPluginByNameAsync(int tenantId, string name)
    {
        var webPlugins = await GetPersonalWebPluginsAsync(tenantId);

        return webPlugins.Find(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) ?? throw new ItemNotFoundException(Resource.ErrorWebPluginNotFound);
    }

    private async Task ApplyWebPluginStatesAsync(List<WebPlugin> webPlugins, bool personal)
    {
        if (webPlugins.Count == 0)
        {
            return;
        }

        var webPluginSettings = await LoadWebPluginSettingsAsync(personal);

        var enabledPlugins = webPluginSettings?.EnabledPlugins ?? [];

        if (enabledPlugins.Count == 0)
        {
            return;
        }

        foreach (var webPlugin in webPlugins)
        {
            if (enabledPlugins.TryGetValue(webPlugin.Name, out var webPluginState))
            {
                try
                {
                    webPlugin.Enabled = webPluginState.Enabled;
                    webPlugin.Settings = string.IsNullOrEmpty(webPluginState.Settings) ? null : await instanceCrypto.DecryptAsync(webPluginState.Settings);
                }
                catch (CryptographicException e)
                {
                    log.ErrorWithException(webPlugin.Name, e);

                    webPlugin.Enabled = false;
                    webPlugin.Settings = null;

                    await UpdateWebPluginAsync(webPlugin, false, null);
                }
            }
        }
    }

    private async Task<List<WebPlugin>> GetWebPluginsForTenantAsync(int tenantId)
    {
        var webPlugins = new List<WebPlugin>();

        webPlugins.AddRange((await GetWebPluginsFromCacheAsync(Tenant.DefaultTenant))
            .Select(x => x.Clone()));

        webPlugins.AddRange((await GetWebPluginsFromCacheAsync(tenantId))
            .Where(tenantPlugin => webPlugins.All(systemPlugin => systemPlugin.Name != tenantPlugin.Name))
            .Select(x => x.Clone()));

        if (webPlugins.Count > webPluginConfigSettings.MaxCount)
        {
            webPlugins = webPlugins.Take(webPluginConfigSettings.MaxCount).ToList();
        }

        return webPlugins;
    }

    private async Task<List<WebPlugin>> GetWebPluginsFromCacheAsync(int tenantId, Guid? owner = null)
    {
        var key = GetCacheKey(tenantId, owner);

        var webPlugins = await _cache.GetOrSetAsync<List<WebPlugin>>(key, async (ctx, token) =>
        {
            var webPlugins = await GetWebPluginsFromStorageAsync(tenantId, owner);

            ctx.Tags = [CacheExtention.GetWebPluginsTag(tenantId)];
            return ctx.Modified(webPlugins);

        }, opt => opt.Duration = TimeSpan.FromHours(1));

        return webPlugins;
    }

    private async Task<List<WebPlugin>> GetWebPluginsFromStorageAsync(int tenantId, Guid? owner = null)
    {
        var webPlugins = new List<WebPlugin>();

        var system = tenantId == Tenant.DefaultTenant;

        var storage = await GetPluginStorageAsync(tenantId, owner.HasValue);

        var basePath = GetBasePath(owner);

        var urlTemplate = await GetPluginUrlTemplateAsync(storage, basePath);

        var configFiles = await storage.ListFilesRelativeAsync(string.Empty, basePath, ConfigFileName, true).ToArrayAsync();

        foreach (var path in configFiles.Where(IsPluginConfigPath))
        {
            try
            {
                var webPlugin = await ReadWebPluginConfigAsync(storage, Path.Combine(basePath, path));

                PrepareStoredPlugin(webPlugin, system, owner);

                var hash = string.IsNullOrEmpty(webPlugin.Version) ? string.Empty : $"?hash={webPlugin.Version}";

                webPlugin.Url = string.Format(urlTemplate, webPlugin.Name) + hash;

                if (await storage.IsFileAsync(Path.Combine(basePath, webPlugin.Name, PluginCssFileName)))
                {
                    webPlugin.CssUrl = webPlugin.Url.Replace(PluginFileName, PluginCssFileName);
                }

                webPlugins.Add(webPlugin);
            }
            catch (Exception e)
            {
                log.ErrorWithException(e);
            }
        }

        return webPlugins;
    }

    private static async Task<WebPlugin> ReadWebPluginConfigAsync(IDataStore storage, string path)
    {
        await using var readStream = await storage.GetReadStreamAsync(path);

        using var reader = new StreamReader(readStream);

        var configContent = await reader.ReadToEndAsync();

        return JsonSerializer.Deserialize<WebPlugin>(configContent, _configJsonOptions);
    }

    // For plugin files replaced in the storage bypassing the upload: the cached list is reset only when the version
    // in the cache differs from the manifest in the storage, so the call cannot be used to flush the cache at will
    public async Task<WebPlugin> RefreshWebPluginAsync(int tenantId, string name)
    {
        DemandWebPlugins();

        name = name?.ToLowerInvariant();

        if (!IsValidName(name))
        {
            throw new ItemNotFoundException(Resource.ErrorWebPluginNotFound);
        }

        var refreshed = false;

        foreach (var pluginsTenantId in (int[])[Tenant.DefaultTenant, tenantId])
        {
            if (await IsCachedWebPluginOutdatedAsync(pluginsTenantId, name))
            {
                await ResetWebPluginsCacheAsync(pluginsTenantId);

                refreshed = true;
            }
        }

        var webPlugin = await GetWebPluginByNameAsync(tenantId, name);

        if (refreshed)
        {
            await quotaSocketManager.ChangeWebPlugin(webPlugin.Name, webPlugin.Enabled);
        }

        return webPlugin;
    }

    private async Task<bool> IsCachedWebPluginOutdatedAsync(int tenantId, string name)
    {
        var cachedWebPlugin = (await GetWebPluginsFromCacheAsync(tenantId))
            .Find(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        var storage = await GetPluginStorageAsync(tenantId);

        var path = Path.Combine(name, ConfigFileName);

        WebPlugin storedWebPlugin = null;

        if (await storage.IsFileAsync(path))
        {
            try
            {
                storedWebPlugin = await ReadWebPluginConfigAsync(storage, path);
            }
            catch (Exception e)
            {
                // a broken manifest is skipped by the list as well
                log.ErrorWithException(e);
            }
        }

        if (cachedWebPlugin is null || storedWebPlugin is null)
        {
            return cachedWebPlugin is not null || storedWebPlugin is not null;
        }

        return !string.Equals(cachedWebPlugin.Version, storedWebPlugin.Version, StringComparison.Ordinal);
    }

    public async Task<WebPlugin> GetWebPluginByNameAsync(int tenantId, string name)
    {
        var webPlugins = await GetWebPluginsAsync(tenantId);

        var webPlugin = webPlugins.Find(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) ?? throw new ItemNotFoundException(Resource.ErrorWebPluginNotFound);

        return webPlugin;
    }

    public async Task<WebPlugin> UpdateWebPluginAsync(int tenantId, string name, bool enabled, string settings, bool personal = false)
    {
        var webPlugin = personal
            ? await GetPersonalWebPluginByNameAsync(tenantId, name)
            : await GetWebPluginByNameAsync(tenantId, name);

        return await UpdateWebPluginAsync(webPlugin, enabled, settings);
    }

    private async Task<string> GetWebPluginSettingsAsync(string name, bool personal)
    {
        var webPluginSettings = await LoadWebPluginSettingsAsync(personal);

        var enabledPlugins = webPluginSettings?.EnabledPlugins ?? [];

        if (enabledPlugins.TryGetValue(name, out var webPluginState))
        {
            try
            {
                return string.IsNullOrEmpty(webPluginState.Settings) ? null : await instanceCrypto.DecryptAsync(webPluginState.Settings);
            }
            catch (Exception e)
            {
                log.ErrorWithException(e);
            }
        }

        return null;
    }

    private async Task<WebPlugin> UpdateWebPluginAsync(WebPlugin webPlugin, bool enabled, string settings)
    {
        var webPluginSettings = await LoadWebPluginSettingsAsync(webPlugin.Personal);

        var enabledPlugins = webPluginSettings?.EnabledPlugins ?? [];

        var encryptedSettings = string.IsNullOrEmpty(settings) ? null : await instanceCrypto.EncryptAsync(settings);

        if (enabled || encryptedSettings != null)
        {
            var webPluginState = new WebPluginState(enabled, encryptedSettings);

            enabledPlugins[webPlugin.Name] = webPluginState;
        }
        else
        {
            settings = null;

            enabledPlugins.Remove(webPlugin.Name);
        }

        webPluginSettings.EnabledPlugins = enabledPlugins.Count != 0 ? enabledPlugins : null;

        await SaveWebPluginSettingsAsync(webPluginSettings, webPlugin.Personal);

        webPlugin.Enabled = enabled;
        webPlugin.Settings = settings;

        try
        {
            if (webPlugin.Personal)
            {
                await quotaSocketManager.ChangePersonalWebPluginAsync(authContext.CurrentAccount.ID, webPlugin.Name, enabled);
            }
            else
            {
                await quotaSocketManager.ChangeWebPlugin(webPlugin.Name, enabled);
            }
        }
        catch (Exception e)
        {
            log.WarningWebPluginChangeNotSent(webPlugin.Name, e);
        }

        return webPlugin;
    }

    public async Task<WebPlugin> DeleteWebPluginAsync(int tenantId, string name, bool personal = false)
    {
        WebPlugin webPlugin;

        if (personal)
        {
            webPlugin = await GetPersonalWebPluginByNameAsync(tenantId, name);
        }
        else
        {
            DemandWebPlugins(delete: true);

            webPlugin = await GetWebPluginByNameAsync(tenantId, name);

            if (webPlugin.System && !coreBaseSettings.Standalone)
            {
                throw new InvalidOperationException(Resource.ErrorWebPluginForbiddenSystem);
            }
        }

        var owner = personal ? authContext.CurrentAccount.ID : (Guid?)null;

        var storageTenantId = webPlugin.System ? Tenant.DefaultTenant : tenantId;

        var storage = await GetPluginStorageAsync(storageTenantId, personal);

        var pluginPath = Path.Combine(GetBasePath(owner), webPlugin.Name);

        if (!await storage.IsDirectoryAsync(pluginPath))
        {
            throw new ItemNotFoundException(Resource.ErrorWebPluginNotFound);
        }

        await storage.DeleteDirectoryAsync(pluginPath);

        await ResetWebPluginsCacheAsync(storageTenantId, owner);

        await UpdateWebPluginAsync(webPlugin, false, null);

        return webPlugin;
    }


    private static void AnalyzeZip(ZipFile zip, long maxFileSize, long maxEntriesCount, long maxCompressionRatio, int maxDirectoryDepth)
    {
        if (zip.Count > maxEntriesCount)
        {
            throw new Exception("Too many entries in zip file.");
        }

        foreach (ZipEntry entry in zip)
        {
            var entryName = entry.Name;

            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                throw new Exception("Invalid entry name");
            }

            if (entryName.Contains("..") || entryName.StartsWith('/') || Path.IsPathRooted(entryName))
            {
                throw new Exception($"Unsafe path detected: {entryName}");
            }

            if (entry.IsDirectory)
            {
                continue;
            }

            if (entry.Size <= 0)
            {
                throw new Exception($"The entry {entryName} has unknown size");
            }

            if (entry.Size > maxFileSize)
            {
                throw new Exception($"The entry {entryName} exceeds the maximum size.");
            }

            if (entry.CompressedSize > 0)
            {
                var ratio = entry.Size / entry.CompressedSize;
                if (ratio > maxCompressionRatio)
                {
                    throw new Exception($"The entry {entryName} exceeds the maximum compression ratio.");
                }
            }

            var depth = entryName.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length;
            if (depth > maxDirectoryDepth)
            {
                throw new Exception($"The entry {entryName} exceeds the maximum directory depth.");
            }
        }
    }

    private async Task<(WebPlugin webPlugin, string tempPath)> SafeExtractPluginToTemp(ZipFile zipFile, int tenantId, bool system, Guid? owner)
    {
        var rootTempDir = tempPath.GetTempPath();

        var tempDir = CrossPlatform.PathCombine(rootTempDir, Guid.NewGuid().ToString());

        while (Directory.Exists(tempDir))
        {
            tempDir = CrossPlatform.PathCombine(rootTempDir, Guid.NewGuid().ToString());
        }

        Directory.CreateDirectory(tempDir);

        try
        {
            // extract and change config file
            var configFile = zipFile.GetEntry(ConfigFileName);
            if (configFile == null)
            {
                throw new Exception($"The required entry {ConfigFileName} not found.");
            }

            var configFilePath = await ExtractEntry(zipFile, configFile, tempDir);

            var configFileContent = await File.ReadAllTextAsync(configFilePath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };

            var webPlugin = JsonSerializer.Deserialize<WebPlugin>(configFileContent, options);

            if (webPlugin == null)
            {
                throw new Exception("Can't deserialize web plugin config file.");
            }

            await ValidatePlugin(webPlugin, tenantId, system, owner);

            webPlugin.CreateBy = authContext.CurrentAccount.ID;
            webPlugin.CreateOn = DateTime.UtcNow;

            ClearManifestState(webPlugin, system);

            configFileContent = JsonSerializer.Serialize(webPlugin, options);

            await File.WriteAllTextAsync(configFilePath, configFileContent);


            // extract plugin file
            var pluginFile = zipFile.GetEntry(PluginFileName);
            if (pluginFile == null)
            {
                throw new Exception($"The required entry {PluginFileName} not found.");
            }

            await ExtractEntry(zipFile, pluginFile, tempDir);


            // extract plugin css file
            var pluginCssFile = zipFile.GetEntry(PluginCssFileName);
            if (pluginCssFile != null)
            {
                await ExtractEntry(zipFile, pluginCssFile, tempDir);
            }


            // extract assets
            var assetsCount = 0;
            foreach (ZipEntry zipEntry in zipFile)
            {
                if (zipEntry.IsFile && zipEntry.Name.StartsWith(AssetsFolderName))
                {
                    var ext = Path.GetExtension(zipEntry.Name);

                    if (!webPluginConfigSettings.AssetExtensions.Contains(ext))
                    {
                        continue;
                    }

                    if (assetsCount++ >= webPluginConfigSettings.AssetMaxCount)
                    {
                        break;
                    }

                    await ExtractEntry(zipFile, zipEntry, tempDir);
                }
            }

            return (webPlugin, tempDir);
        }
        catch (Exception)
        {
            Directory.Delete(tempDir, true);
            throw;
        }
    }

    private async Task<string> ExtractEntry(ZipFile zipFile, ZipEntry entry, string outputPath)
    {
        var entryPath = CrossPlatform.PathCombine(outputPath, entry.Name);

        if (entry.IsDirectory)
        {
            if (!Directory.Exists(entryPath))
            {
                Directory.CreateDirectory(entryPath);
            }

            return entryPath;
        }

        var directory = Path.GetDirectoryName(entryPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var zipStream = zipFile.GetInputStream(entry);
        await using var outputStream = new FileStream(entryPath, FileMode.Create, FileAccess.Write);

        await ExtractWithSizeMonitoring(zipStream, outputStream, entry);

        return entryPath;
    }

    private async Task ExtractWithSizeMonitoring(Stream inputStream, Stream outputStream, ZipEntry entry)
    {
        var buffer = new byte[4096];
        long totalBytesRead = 0;
        int bytesRead;

        while ((bytesRead = await inputStream.ReadAsync(buffer)) > 0)
        {
            totalBytesRead += bytesRead;

            if (totalBytesRead > entry.Size)
            {
                throw new Exception($"The entry {entry.Name} actual size exceeds declared size.");
            }

            if (totalBytesRead > webPluginConfigSettings.MaxSize)
            {
                throw new Exception($"The entry {entry.Name} exceeds the maximum size.");
            }

            await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead));
        }

        if (totalBytesRead != entry.Size)
        {
            throw new Exception($"The entry {entry.Name} size mismatch: expected {entry.Size}, got {totalBytesRead}.");
        }
    }

    private async Task CopyPluginFromTempToStore(WebPlugin webPlugin, string tempPath, IDataStore storage, string basePath, ACL acl)
    {
        var pluginPath = Path.Combine(basePath, webPlugin.Name);

        if (await storage.IsDirectoryAsync(pluginPath))
        {
            await storage.DeleteDirectoryAsync(pluginPath);
        }

        var files = Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
            var path = Path.Combine(pluginPath, file.Replace(tempPath, string.Empty).TrimStart('/').TrimStart('\\'));
            await storage.SaveAsync(string.Empty, path, stream, acl);
        }
    }

    private sealed class WebPluginRefusedException(string message) : ArgumentException(message);
}
