// (c) Copyright Ascensio System SIA 2010-2022
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

namespace ASC.Web.Core;

public class WebPlugin
{
    public string Name { get; set; }
    public string Version { get; set; }
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
    public string Url { get; set; }
    public string Settings { get; set; }
}

[Singleton]
public class WebPluginCache
{
    private readonly ICache _сache;
    private readonly ICacheNotify<WebPluginCacheItem> _notify;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromDays(1);

    public WebPluginCache(ICacheNotify<WebPluginCacheItem> notify, ICache cache)
    {
        _сache = cache;
        _notify = notify;

        _notify.Subscribe((i) => _сache.Remove(i.Key), CacheNotifyAction.Remove);
    }

    public List<WebPlugin> Get(string key)
    {
        return _сache.Get<List<WebPlugin>>(key);
    }

    public void Insert(string key, object value)
    {
        _notify.Publish(new WebPluginCacheItem { Key = key }, CacheNotifyAction.Remove);

        _сache.Insert(key, value, _cacheExpiration);
    }

    public void Remove(string key)
    {
        _notify.Publish(new WebPluginCacheItem { Key = key }, CacheNotifyAction.Remove);

        _сache.Remove(key);
    }
}

[Scope]
public class WebPluginManager
{
    private const string StorageSystemModuleName = "systemwebplugins";
    private const string StorageModuleName = "webplugins";
    private const string ConfigFileName = "config.json";
    private const string PluginFileName = "plugin.js";
    private const string AssetsFolderName = "assets";

    private readonly CoreBaseSettings _coreBaseSettings;
    private readonly SettingsManager _settingsManager;
    private readonly InstanceCrypto _instanceCrypto;
    private readonly WebPluginConfigSettings _webPluginConfigSettings;
    private readonly WebPluginCache _webPluginCache;
    private readonly StorageFactory _storageFactory;
    private readonly AuthContext _authContext;
    private readonly ILogger<WebPluginManager> _log;

    public WebPluginManager(
        CoreBaseSettings coreBaseSettings,
        SettingsManager settingsManager,
        InstanceCrypto instanceCrypto,
        WebPluginConfigSettings webPluginConfigSettings,
        WebPluginCache webPluginCache,
        StorageFactory storageFactory,
        AuthContext authContext,
        ILogger<WebPluginManager> log)
    {
        _coreBaseSettings = coreBaseSettings;
        _settingsManager = settingsManager;
        _instanceCrypto = instanceCrypto;
        _webPluginConfigSettings = webPluginConfigSettings;
        _webPluginCache = webPluginCache;
        _storageFactory = storageFactory;
        _authContext = authContext;
        _log = log;
    }

    private void DemandWebPlugins(string action = null)
    {
        if (!_webPluginConfigSettings.Enabled)
        {
            throw new SecurityException("Plugins disabled");
        }

        if (!string.IsNullOrWhiteSpace(action) && _webPluginConfigSettings.Allow.Any() && !_webPluginConfigSettings.Allow.Contains(action))
        {
            throw new SecurityException("Forbidden action");
        }
    }

    private async Task<IDataStore> GetPluginStorageAsync(int tenantId)
    {
        var module = tenantId == Tenant.DefaultTenant ? StorageSystemModuleName : StorageModuleName;

        var storage = await _storageFactory.GetStorageAsync(tenantId, module);

        return storage;
    }

    private static string GetCacheKey(int tenantId)
    {
        return $"{StorageModuleName}:{tenantId}";
    }

    private static async Task<string> GetPluginUrlTemplateAsync(IDataStore storage)
    {
        var uri = await storage.GetUriAsync(Path.Combine("{0}", PluginFileName));

        return uri?.ToString() ?? string.Empty;
    }

    public async Task<WebPlugin> AddWebPluginFromFileAsync(int tenantId, IFormFile file, bool system)
    {
        DemandWebPlugins("upload");

        if (system && !_coreBaseSettings.Standalone)
        {
            throw new CustomHttpException(HttpStatusCode.Forbidden, Resource.ErrorWebPluginForbiddenSystem);
        }

        if (Path.GetExtension(file.FileName).ToLowerInvariant() != _webPluginConfigSettings.Extension)
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginFileExtension);
        }

        if (file.Length > _webPluginConfigSettings.MaxSize)
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginFileSize);
        }

        using var zipFile = new ZipFile(file.OpenReadStream());

        var configFile = zipFile.GetEntry(ConfigFileName);
        var pluginFile = zipFile.GetEntry(PluginFileName);

        if (configFile == null || pluginFile == null)
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginArchive);
        }

        var storage = await GetPluginStorageAsync(system ? Tenant.DefaultTenant : tenantId);

        WebPlugin webPlugin;

        await using (var stream = zipFile.GetInputStream(configFile))
        using (var reader = new StreamReader(stream))
        {
            var configContent = await reader.ReadToEndAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
            };

            webPlugin = System.Text.Json.JsonSerializer.Deserialize<WebPlugin>(configContent, options);

            await ValidatePlugin(webPlugin, tenantId, system);

            if (await storage.IsDirectoryAsync(webPlugin.Name))
            {
                await storage.DeleteDirectoryAsync(webPlugin.Name);
            }

            webPlugin.CreateBy = _authContext.CurrentAccount.ID;
            webPlugin.CreateOn = DateTime.UtcNow;

            var configString = System.Text.Json.JsonSerializer.Serialize(webPlugin, options);

            using var configStream = new MemoryStream(Encoding.UTF8.GetBytes(configString));

            await storage.SaveAsync(Path.Combine(webPlugin.Name, ConfigFileName), configStream);
        }

        await using (var stream = zipFile.GetInputStream(pluginFile))
        {
            await storage.SaveAsync(Path.Combine(webPlugin.Name, PluginFileName), stream);
        }

        foreach (ZipEntry zipEntry in zipFile)
        {
            if (zipEntry.IsFile && zipEntry.Name.StartsWith(AssetsFolderName))
            {
                var ext = Path.GetExtension(zipEntry.Name);

                if (_webPluginConfigSettings.AssetExtensions.Any() && !_webPluginConfigSettings.AssetExtensions.Contains(ext))
                {
                    continue;
                }

                await using var stream = zipFile.GetInputStream(zipEntry);
                await storage.SaveAsync(Path.Combine(webPlugin.Name, zipEntry.Name), stream);
            }
        }

        webPlugin.System = system;

        var urlTemplate = await GetPluginUrlTemplateAsync(storage);

        webPlugin.Url = string.Format(urlTemplate, webPlugin.Name);

        webPlugin = await UpdateWebPluginAsync(tenantId, webPlugin, true, null);

        return webPlugin;
    }

    private async Task ValidatePlugin(WebPlugin webPlugin, int tenantId, bool system)
    {
        if (webPlugin == null)
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginArchive);
        }

        var nameRegex = new Regex(@"^[a-z0-9_.-]+$");

        if (string.IsNullOrEmpty(webPlugin.Name) || !nameRegex.IsMatch(webPlugin.Name) || webPlugin.Name.StartsWith('.'))
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginName);
        }

        var jsVariableRegex = new Regex(@"^[0-9a-zA-Z_$]+$");

        if (string.IsNullOrEmpty(webPlugin.PluginName) || !jsVariableRegex.IsMatch(webPlugin.PluginName))
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginName);
        }

        var systemWebPlugins = await GetWebPluginsFromCacheAsync(Tenant.DefaultTenant);

        var tenantWebPlugins = await GetWebPluginsFromCacheAsync(tenantId);

        if (system)
        {
            if (tenantWebPlugins.Any(x => 
                x.PluginName.Equals(webPlugin.PluginName, StringComparison.InvariantCulture) ||
                x.Name.Equals(webPlugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginExist);
            }

            if (systemWebPlugins.Any(x => 
                x.PluginName.Equals(webPlugin.PluginName, StringComparison.InvariantCulture) &&
                !x.Name.Equals(webPlugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginExist);
            }
        }
        else
        {
            if (systemWebPlugins.Any(x =>
                x.PluginName.Equals(webPlugin.PluginName, StringComparison.InvariantCulture) ||
                x.Name.Equals(webPlugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginExist);
            }

            if (tenantWebPlugins.Any(x =>
                x.PluginName.Equals(webPlugin.PluginName, StringComparison.InvariantCulture) &&
                !x.Name.Equals(webPlugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginExist);
            }
        }
    }

    public async Task<List<WebPlugin>> GetWebPluginsAsync(int tenantId)
    {
        DemandWebPlugins();

        var webPlugins = new List<WebPlugin>();

        webPlugins.AddRange(await GetWebPluginsFromCacheAsync(Tenant.DefaultTenant));
        webPlugins.AddRange(await GetWebPluginsFromCacheAsync(tenantId));

        var webPluginSettings = await _settingsManager.LoadAsync<WebPluginSettings>();

        var enabledPlugins = webPluginSettings?.EnabledPlugins ?? new Dictionary<string, WebPluginState>();

        if (enabledPlugins.Any())
        {
            foreach (var webPlugin in webPlugins)
            {
                if (enabledPlugins.TryGetValue(webPlugin.Name, out var webPluginState))
                {
                    webPlugin.Enabled = webPluginState.Enabled;
                    webPlugin.Settings = string.IsNullOrEmpty(webPluginState.Settings) ? null : _instanceCrypto.Decrypt(webPluginState.Settings);
                }
            }
        }

        return webPlugins;
    }

    private async Task<List<WebPlugin>> GetWebPluginsFromCacheAsync(int tenantId)
    {
        var key = GetCacheKey(tenantId);

        var webPlugins = _webPluginCache.Get(key);

        if (webPlugins == null)
        {
            webPlugins = await GetWebPluginsFromStorageAsync(tenantId);

            _webPluginCache.Insert(key, webPlugins);
        }

        return webPlugins;
    }

    private async Task<List<WebPlugin>> GetWebPluginsFromStorageAsync(int tenantId)
    {
        var webPlugins = new List<WebPlugin>();

        var system = tenantId == Tenant.DefaultTenant;

        var storage = await GetPluginStorageAsync(tenantId);

        var urlTemplate = await GetPluginUrlTemplateAsync(storage);

        var configFiles = await storage.ListFilesRelativeAsync(string.Empty, string.Empty, ConfigFileName, true).ToArrayAsync();

        foreach (var path in configFiles)
        {
            try
            {
                await using var readStream = await storage.GetReadStreamAsync(path);

                using var reader = new StreamReader(readStream);

                var configContent = await reader.ReadToEndAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var webPlugin = System.Text.Json.JsonSerializer.Deserialize<WebPlugin>(configContent, options);

                webPlugin.System = system;

                webPlugin.Url = string.Format(urlTemplate, webPlugin.Name);

                webPlugins.Add(webPlugin);
            }
            catch (Exception e)
            {
                _log.ErrorWithException(e);
            }
        }

        return webPlugins;
    }

    public async Task<WebPlugin> GetWebPluginByNameAsync(int tenantId, string name)
    {
        var webPlugins = await GetWebPluginsAsync(tenantId);

        var webPlugin = webPlugins.Find(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) ?? throw new CustomHttpException(HttpStatusCode.NotFound, Resource.ErrorWebPluginNotFound);

        return webPlugin;
    }

    public async Task<WebPlugin> UpdateWebPluginAsync(int tenantId, string name, bool enabled, string settings)
    {
        var webPlugin = await GetWebPluginByNameAsync(tenantId, name);

        return await UpdateWebPluginAsync(tenantId, webPlugin, enabled, settings);
    }

    private async Task<WebPlugin> UpdateWebPluginAsync(int tenantId, WebPlugin webPlugin, bool enabled, string settings)
    {
        var webPluginSettings = await _settingsManager.LoadAsync<WebPluginSettings>();

        var enabledPlugins = webPluginSettings?.EnabledPlugins ?? new Dictionary<string, WebPluginState>();

        var encryptedSettings = string.IsNullOrEmpty(settings) ? null : _instanceCrypto.Encrypt(settings);

        if (enabled || encryptedSettings != null)
        {
            var webPluginState = new WebPluginState(enabled, encryptedSettings);

            if (enabledPlugins.ContainsKey(webPlugin.Name))
            {
                enabledPlugins[webPlugin.Name] = webPluginState;
            }
            else
            {
                enabledPlugins.Add(webPlugin.Name, webPluginState);
            }
        }
        else
        {
            settings = null;

            enabledPlugins.Remove(webPlugin.Name);
        }

        webPluginSettings.EnabledPlugins = enabledPlugins.Any() ? enabledPlugins : null;

        await _settingsManager.SaveAsync(webPluginSettings);

        webPlugin.Enabled = enabled;
        webPlugin.Settings = settings;

        var key = GetCacheKey(webPlugin.System ? Tenant.DefaultTenant : tenantId);

        _webPluginCache.Remove(key);

        return webPlugin;
    }

    public async Task<WebPlugin> DeleteWebPluginAsync(int tenantId, string name)
    {
        DemandWebPlugins("delete");

        var webPlugin = await GetWebPluginByNameAsync(tenantId, name);

        if (webPlugin.System && !_coreBaseSettings.Standalone)
        {
            throw new CustomHttpException(HttpStatusCode.Forbidden, Resource.ErrorWebPluginForbiddenSystem);
        }

        var storage = await GetPluginStorageAsync(tenantId);

        if (!await storage.IsDirectoryAsync(webPlugin.Name))
        {
            throw new CustomHttpException(HttpStatusCode.NotFound, Resource.ErrorWebPluginNotFound);
        }

        await storage.DeleteDirectoryAsync(webPlugin.Name);

        await UpdateWebPluginAsync(tenantId, webPlugin, false, null);

        return webPlugin;
    }
}
