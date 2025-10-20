// (c) Copyright Ascensio System SIA 2009-2025
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
    public string Url { get; set; }
    public string Settings { get; set; }

    public WebPlugin Clone()
    {
        return (WebPlugin)MemberwiseClone();
    }
}

[Scope]
public class WebPluginManager(
    CoreBaseSettings coreBaseSettings,
    SettingsManager settingsManager,
    InstanceCrypto instanceCrypto,
    WebPluginConfigSettings webPluginConfigSettings,
    StorageFactory storageFactory,
    AuthContext authContext,
    TempPath tempPath,
    ILogger<WebPluginManager> log,
    IFusionCacheProvider cacheProvider)
{
    private const string StorageSystemModuleName = "systemwebplugins";
    private const string StorageModuleName = "webplugins";
    private const string ConfigFileName = "config.json";
    private const string PluginFileName = "plugin.js";
    private const string AssetsFolderName = "assets";

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

    private static async Task<string> GetPluginUrlTemplateAsync(IDataStore storage)
    {
        var uri = await storage.GetUriAsync(Path.Combine("{0}", PluginFileName));

        return uri?.ToString() ?? string.Empty;
    }

    public async Task<WebPlugin> AddWebPluginFromFileAsync(int tenantId, IFormFile file, bool system)
    {
        DemandWebPlugins(upload: true);

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

        var tenantWebPlugins = await GetWebPluginsForTenantAsync(tenantId);
        if (tenantWebPlugins.Count + 1 > webPluginConfigSettings.MaxSize)
        {
            throw new InvalidOperationException(Resource.ErrorWebPluginMaxCount);
        }

        string tempDirToDelete = null;

        try
        {
            using var zipFile = new ZipFile(file.OpenReadStream());

            long maxEntriesCount = webPluginConfigSettings.AssetMaxCount + 3; // asset files, config file, plugin file, asset folder

            AnalyzeZip(zipFile, webPluginConfigSettings.MaxSize, maxEntriesCount, 100, 2); // 100:1 ratio, 2 depth levels

            var (webPlugin, tempPath) = await SafeExtractPluginToTemp(zipFile, tenantId, system);

            tempDirToDelete = tempPath;

            var storage = await GetPluginStorageAsync(system ? Tenant.DefaultTenant : tenantId);

            await CopyPluginFromTempToStore(webPlugin, tempPath, storage);

            webPlugin.System = system;

            var urlTemplate = await GetPluginUrlTemplateAsync(storage);

            var hash = string.IsNullOrEmpty(webPlugin.Version) ? string.Empty : $"?hash={webPlugin.Version}";

            webPlugin.Url = string.Format(urlTemplate, webPlugin.Name) + hash;

            var existingSettings = await GetWebPluginSettingsAsync(webPlugin.Name);

            webPlugin = await UpdateWebPluginAsync(tenantId, webPlugin, true, existingSettings);

            return webPlugin;
        }
        catch (Exception ex)
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

    private async Task ValidatePlugin(WebPlugin webPlugin, int tenantId, bool system)
    {
        var nameRegex = new Regex(@"^[a-z0-9_.-]+$");

        if (string.IsNullOrEmpty(webPlugin.Name) || !nameRegex.IsMatch(webPlugin.Name) || webPlugin.Name.StartsWith('.'))
        {
            throw new Exception(Resource.ErrorWebPluginName);
        }

        var jsVariableRegex = new Regex(@"^[0-9a-zA-Z_$]+$");

        if (string.IsNullOrEmpty(webPlugin.PluginName) || !jsVariableRegex.IsMatch(webPlugin.PluginName))
        {
            throw new Exception(Resource.ErrorWebPluginName);
        }

        var systemWebPlugins = await GetWebPluginsFromCacheAsync(Tenant.DefaultTenant);

        var tenantWebPlugins = await GetWebPluginsFromCacheAsync(tenantId);

        if (system)
        {
            if (tenantWebPlugins.Any(x =>
                    x.PluginName.Equals(webPlugin.PluginName, StringComparison.InvariantCulture) ||
                    x.Name.Equals(webPlugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new Exception(Resource.ErrorWebPluginExist);
            }

            if (systemWebPlugins.Any(x =>
                    x.PluginName.Equals(webPlugin.PluginName, StringComparison.InvariantCulture) &&
                    !x.Name.Equals(webPlugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new Exception(Resource.ErrorWebPluginExist);
            }
        }
        else
        {
            if (systemWebPlugins.Any(x =>
                    x.PluginName.Equals(webPlugin.PluginName, StringComparison.InvariantCulture) ||
                    x.Name.Equals(webPlugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new Exception(Resource.ErrorWebPluginExist);
            }

            if (tenantWebPlugins.Any(x =>
                    x.PluginName.Equals(webPlugin.PluginName, StringComparison.InvariantCulture) &&
                    !x.Name.Equals(webPlugin.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new Exception(Resource.ErrorWebPluginExist);
            }
        }
    }

    public async Task<List<WebPlugin>> GetWebPluginsAsync(int tenantId)
    {
        DemandWebPlugins();

        var webPlugins = await GetWebPluginsForTenantAsync(tenantId);

        if (webPlugins.Count == 0)
        {
            return webPlugins;
        }

        var webPluginSettings = await settingsManager.LoadAsync<WebPluginSettings>();

        var enabledPlugins = webPluginSettings?.EnabledPlugins ?? [];

        if (enabledPlugins.Count != 0)
        {
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

                        await UpdateWebPluginAsync(tenantId, webPlugin, false, null);
                    }
                }
            }
        }

        return webPlugins;
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

    private async Task<List<WebPlugin>> GetWebPluginsFromCacheAsync(int tenantId)
    {
        var key = GetCacheKey(tenantId);

        var webPlugins = await _cache.GetOrSetAsync<List<WebPlugin>>(key, async (ctx, token) =>
        {
            var webPlugins = await GetWebPluginsFromStorageAsync(tenantId);

            ctx.Tags = [CacheExtention.GetWebPluginsTag(tenantId)];
            return ctx.Modified(webPlugins);

        });

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

                var webPlugin = JsonSerializer.Deserialize<WebPlugin>(configContent, options);

                webPlugin.System = system;

                var hash = string.IsNullOrEmpty(webPlugin.Version) ? string.Empty : $"?hash={webPlugin.Version}";

                webPlugin.Url = string.Format(urlTemplate, webPlugin.Name) + hash;

                webPlugins.Add(webPlugin);
            }
            catch (Exception e)
            {
                log.ErrorWithException(e);
            }
        }

        return webPlugins;
    }

    public async Task<WebPlugin> GetWebPluginByNameAsync(int tenantId, string name)
    {
        var webPlugins = await GetWebPluginsAsync(tenantId);

        var webPlugin = webPlugins.Find(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) ?? throw new ItemNotFoundException(Resource.ErrorWebPluginNotFound);

        return webPlugin;
    }

    public async Task<WebPlugin> UpdateWebPluginAsync(int tenantId, string name, bool enabled, string settings)
    {
        var webPlugin = await GetWebPluginByNameAsync(tenantId, name);

        return await UpdateWebPluginAsync(tenantId, webPlugin, enabled, settings);
    }

    private async Task<string> GetWebPluginSettingsAsync(string name)
    {
        var webPluginSettings = await settingsManager.LoadAsync<WebPluginSettings>();

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

    private async Task<WebPlugin> UpdateWebPluginAsync(int tenantId, WebPlugin webPlugin, bool enabled, string settings)
    {
        var webPluginSettings = await settingsManager.LoadAsync<WebPluginSettings>();

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

        await settingsManager.SaveAsync(webPluginSettings);

        webPlugin.Enabled = enabled;
        webPlugin.Settings = settings;

        var tag = CacheExtention.GetWebPluginsTag(webPlugin.System ? Tenant.DefaultTenant : tenantId);

        await _cache.RemoveByTagAsync(tag);

        return webPlugin;
    }

    public async Task<WebPlugin> DeleteWebPluginAsync(int tenantId, string name)
    {
        DemandWebPlugins(delete: true);

        var webPlugin = await GetWebPluginByNameAsync(tenantId, name);

        if (webPlugin.System && !coreBaseSettings.Standalone)
        {
            throw new InvalidOperationException(Resource.ErrorWebPluginForbiddenSystem);
        }

        var storage = await GetPluginStorageAsync(tenantId);

        if (!await storage.IsDirectoryAsync(webPlugin.Name))
        {
            throw new ItemNotFoundException(Resource.ErrorWebPluginNotFound);
        }

        await storage.DeleteDirectoryAsync(webPlugin.Name);

        await UpdateWebPluginAsync(tenantId, webPlugin, false, null);

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

    private async Task<(WebPlugin webPlugin, string tempPath)> SafeExtractPluginToTemp(ZipFile zipFile, int tenantId, bool system)
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

            await ValidatePlugin(webPlugin, tenantId, system);

            webPlugin.CreateBy = authContext.CurrentAccount.ID;
            webPlugin.CreateOn = DateTime.UtcNow;

            configFileContent = JsonSerializer.Serialize(webPlugin, options);

            await File.WriteAllTextAsync(configFilePath, configFileContent);


            // extract plugin file
            var pluginFile = zipFile.GetEntry(PluginFileName);
            if (pluginFile == null)
            {
                throw new Exception($"The required entry {PluginFileName} not found.");
            }

            await ExtractEntry(zipFile, pluginFile, tempDir);


            // extract assets
            var assetsCount = 0;
            foreach (ZipEntry zipEntry in zipFile)
            {
                if (zipEntry.IsFile && zipEntry.Name.StartsWith(AssetsFolderName))
                {
                    var ext = Path.GetExtension(zipEntry.Name);

                    if (webPluginConfigSettings.AssetExtensions.Length != 0 && !webPluginConfigSettings.AssetExtensions.Contains(ext))
                    {
                        continue;
                    }

                    await ExtractEntry(zipFile, zipEntry, tempDir);

                    if (assetsCount++ >= webPluginConfigSettings.AssetMaxCount)
                    {
                        break;
                    }
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
        else
        {
            var directory = Path.GetDirectoryName(entryPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        using var zipStream = zipFile.GetInputStream(entry);
        using var outputStream = new FileStream(entryPath, FileMode.Create, FileAccess.Write);

        await ExtractWithSizeMonitoring(zipStream, outputStream, entry);

        return entryPath;
    }

    private async Task ExtractWithSizeMonitoring(Stream inputStream, Stream outputStream, ZipEntry entry)
    {
        var buffer = new byte[4096];
        long totalBytesRead = 0;
        int bytesRead;

        while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
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

            await outputStream.WriteAsync(buffer, 0, bytesRead);
        }

        if (totalBytesRead != entry.Size)
        {
            throw new Exception($"The entry {entry.Name} size mismatch: expected {entry.Size}, got {totalBytesRead}.");
        }
    }

    private async Task CopyPluginFromTempToStore(WebPlugin webPlugin, string tempPath, IDataStore storage)
    {
        if (await storage.IsDirectoryAsync(webPlugin.Name))
        {
            await storage.DeleteDirectoryAsync(webPlugin.Name);
        }

        var files = Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
            var path = Path.Combine(webPlugin.Name, file.Replace(tempPath, string.Empty).TrimStart('/').TrimStart('\\'));
            await storage.SaveAsync(path, stream);
        }
    }
}