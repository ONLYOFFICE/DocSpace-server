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

using System.Net;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using ASC.Common;
using ASC.Common.Log;
using ASC.Common.Web;
using ASC.Core;
using ASC.Core.Common.Settings;
using ASC.Core.Tenants;
using ASC.Data.Storage;
using ASC.Data.Storage.DiscStorage;
using ASC.Security.Cryptography;
using ASC.Web.Core;
using ASC.Web.Core.PublicResources;

using ICSharpCode.SharpZipLib.Zip;

namespace ASC.Plugins;

[Scope]
public class PluginManager(PluginConfigSettings pluginConfigSettings, 
    StorageFactory storageFactory,
    CoreBaseSettings coreBaseSettings,
    AuthContext authContext,
    PluginCache pluginCache,
    ILogger<PluginManager> log,
    SettingsManager settingsManager,
    InstanceCrypto instanceCrypto,
    PluginControllerManager pluginControllerManager)
{
    private const string StorageSystemModuleName = "systemplugins";
    private const string StorageModuleName = "plugins";
    private const string ConfigFileName = "config.json";

    private void DemandPlugins(bool upload = false, bool delete = false)
    {
        if (!pluginConfigSettings.Enabled)
        {
            throw new SecurityException("Plugins disabled");
        }

        if ((upload && !pluginConfigSettings.Upload) || (delete && !pluginConfigSettings.Delete))
        {
            throw new SecurityException("Forbidden action");
        }
    }

    private async Task<IDataStore> GetPluginStorageAsync(int tenantId)
    {
        var module = tenantId == Tenant.DefaultTenant ? StorageSystemModuleName : StorageModuleName;

        var storage = await storageFactory.GetStorageAsync(tenantId, module);

        return storage;
    }

    private static string GetCacheKey(int tenantId)
    {
        return $"{StorageModuleName}:{tenantId}";
    }

    public async Task<PluginConfig> AddPluginFromFileAsync(int tenantId, IFormFile file, bool system)
    {
        DemandPlugins(upload: true);

        if (system && !coreBaseSettings.Standalone)
        {
            throw new CustomHttpException(HttpStatusCode.Forbidden, Resource.ErrorWebPluginForbiddenSystem);
        }

        if (Path.GetExtension(file.FileName).ToLowerInvariant() != pluginConfigSettings.Extension)
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginFileExtension);
        }

        if (file.Length > pluginConfigSettings.MaxSize)
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginFileSize);
        }

        using var zipFile = new ZipFile(file.OpenReadStream());

        var configFile = zipFile.GetEntry(ConfigFileName);

        if (configFile == null)
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginArchive);
        }

        var storage = await GetPluginStorageAsync(system ? Tenant.DefaultTenant : tenantId) as DiscDataStore;

        PluginConfig config;

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

            config = JsonSerializer.Deserialize<PluginConfig>(configContent, options);

            await ValidatePlugin(config, tenantId, system);

            if (await storage.IsDirectoryAsync(config.Name))
            {
                await storage.DeleteDirectoryAsync(config.Name);
            }

            config.CreateBy = authContext.CurrentAccount.ID;
            config.CreateOn = DateTime.UtcNow;

            var configString = JsonSerializer.Serialize(config, options);

            using var configStream = new MemoryStream(Encoding.UTF8.GetBytes(configString));

            await storage.SaveAsync(Path.Combine(config.Name, ConfigFileName), configStream);
        }

        foreach (ZipEntry zipEntry in zipFile)
        {
            await using var stream = zipFile.GetInputStream(zipEntry);
            await storage.SaveAsync(Path.Combine(config.Name, zipEntry.Name), stream);
        }

        config.System = system;

        config = await UpdatePluginAsync(tenantId, config, true, null);

        var path = storage.GetPhysicalPath("", Path.Combine(config.Name, config.Name + ".dll"));
        var assembly = LoadPlugin(path);
        pluginControllerManager.AddControllers(assembly);
        return config;
    }

    private Assembly LoadPlugin(string path)
    {
        var loadContext = new PluginLoadContext(path);
        return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(path)));
    }

    private async Task ValidatePlugin(PluginConfig plugin, int tenantId, bool system)
    {
        if (plugin == null)
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginArchive);
        }

        var jsVariableRegex = new Regex(@"^[0-9a-zA-Z_$]+$");

        if (string.IsNullOrEmpty(plugin.Name) || !jsVariableRegex.IsMatch(plugin.Name))
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginName);
        }

        var systemPlugins = await GetPluginsFromCacheAsync(Tenant.DefaultTenant);

        var tenantPlugins = await GetPluginsFromCacheAsync(tenantId);

        if (system)
        {
            if (tenantPlugins.Any(x =>
                    x.Name.Equals(plugin.Name, StringComparison.InvariantCulture) ||
                    x.Name.Equals(plugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginExist);
            }

            if (systemPlugins.Any(x =>
                    x.Name.Equals(plugin.Name, StringComparison.InvariantCulture) &&
                    !x.Name.Equals(plugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginExist);
            }
        }
        else
        {
            if (systemPlugins.Any(x =>
                    x.Name.Equals(plugin.Name, StringComparison.InvariantCulture) ||
                    x.Name.Equals(plugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginExist);
            }

            if (tenantPlugins.Any(x =>
                    x.Name.Equals(plugin.Name, StringComparison.InvariantCulture) &&
                    !x.Name.Equals(plugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginExist);
            }
        }
    }

    private async Task<List<PluginConfig>> GetPluginsFromCacheAsync(int tenantId)
    {
        var key = GetCacheKey(tenantId);

        var plugins = pluginCache.Get(key);

        if (plugins == null)
        {
            plugins = await GetPluginsFromStorageAsync(tenantId);

            pluginCache.Insert(key, plugins);
        }

        return plugins;
    }

    private async Task<List<PluginConfig>> GetPluginsFromStorageAsync(int tenantId)
    {
        var plugins = new List<PluginConfig>();

        var system = tenantId == Tenant.DefaultTenant;

        var storage = await GetPluginStorageAsync(tenantId);

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

                var plugin = JsonSerializer.Deserialize<PluginConfig>(configContent, options);

                plugin.System = system;

                plugins.Add(plugin);
            }
            catch (Exception e)
            {
                log.ErrorWithException(e);
            }
        }

        return plugins;
    }

    public async Task<List<PluginConfig>> GetPluginsAsync(int tenantId)
    {
        DemandPlugins();

        var plugins = new List<PluginConfig>();

        plugins.AddRange((await GetPluginsFromCacheAsync(Tenant.DefaultTenant)).Select(x => x.Clone()));
        plugins.AddRange((await GetPluginsFromCacheAsync(tenantId)).Select(x => x.Clone()));

        var pluginSettings = await settingsManager.LoadAsync<PluginSettings>();

        var enabledPlugins = pluginSettings?.EnabledPlugins ?? new Dictionary<string, PluginState>();

        if (enabledPlugins.Any())
        {
            foreach (var plugin in plugins)
            {
                if (enabledPlugins.TryGetValue(plugin.Name, out var pluginState))
                {
                    plugin.Enabled = pluginState.Enabled;
                    plugin.Settings = string.IsNullOrEmpty(pluginState.Settings) ? null : instanceCrypto.Decrypt(pluginState.Settings);
                }
            }
        }

        return plugins;
    }

    public async Task<PluginConfig> GetPluginByNameAsync(int tenantId, string name)
    {
        var plugins = await GetPluginsAsync(tenantId);

        var plugin = plugins.Find(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) ?? throw new CustomHttpException(HttpStatusCode.NotFound, Resource.ErrorWebPluginNotFound);

        return plugin;
    }

    public async Task<PluginConfig> UpdatePluginAsync(int tenantId, string name, bool enabled, string settings)
    {
        var plugin = await GetPluginByNameAsync(tenantId, name);

        return await UpdatePluginAsync(tenantId, plugin, enabled, settings);
    }

    private async Task<PluginConfig> UpdatePluginAsync(int tenantId, PluginConfig plugin, bool enabled, string settings)
    {
        var pluginSettings = await settingsManager.LoadAsync<PluginSettings>();

        var enabledPlugins = pluginSettings?.EnabledPlugins ?? new Dictionary<string, PluginState>();

        var encryptedSettings = string.IsNullOrEmpty(settings) ? null : instanceCrypto.Encrypt(settings);

        if (enabled || encryptedSettings != null)
        {
            var pluginState = new PluginState(enabled, encryptedSettings);

            enabledPlugins[plugin.Name] = pluginState;
        }
        else
        {
            settings = null;

            enabledPlugins.Remove(plugin.Name);
        }

        pluginSettings.EnabledPlugins = enabledPlugins.Any() ? enabledPlugins : null;

        await settingsManager.SaveAsync(pluginSettings);

        plugin.Enabled = enabled;
        plugin.Settings = settings;

        var key = GetCacheKey(plugin.System ? Tenant.DefaultTenant : tenantId);

        pluginCache.Remove(key);

        return plugin;
    }

    public async Task<PluginConfig> DeletePluginAsync(int tenantId, string name)
    {
        DemandPlugins(delete: true);

        var webPlugin = await GetPluginByNameAsync(tenantId, name);

        if (webPlugin.System && !coreBaseSettings.Standalone)
        {
            throw new CustomHttpException(HttpStatusCode.Forbidden, Resource.ErrorWebPluginForbiddenSystem);
        }

        var storage = await GetPluginStorageAsync(tenantId);

        if (!await storage.IsDirectoryAsync(webPlugin.Name))
        {
            throw new CustomHttpException(HttpStatusCode.NotFound, Resource.ErrorWebPluginNotFound);
        }

        await storage.DeleteDirectoryAsync(webPlugin.Name);

        await UpdatePluginAsync(tenantId, webPlugin, false, null);

        return webPlugin;
    }
}
