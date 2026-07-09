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

using ASC.Files.Core.Configuration;

namespace ASC.Web.Files.Classes;

[Singleton]
public class GlobalNotify
{
    private ILogger Logger { get; set; }
    private readonly ICacheNotify<AscCacheItem> _notify;
    private readonly ICacheNotify<ClearMyFolderItem> _notifyMyFolder;

    public GlobalNotify(ICacheNotify<AscCacheItem> notify, ICacheNotify<ClearMyFolderItem> notifyMyFolder, ILoggerFactory loggerFactory, CoreBaseSettings coreBaseSettings)
    {
        _notify = notify;
        _notifyMyFolder = notifyMyFolder;
        Logger = loggerFactory.CreateLogger("ASC.Files");
        if (coreBaseSettings.Standalone)
        {
            ClearCache();
        }
        ClearMyFolderCache();
    }

    private void ClearMyFolderCache()
    {
        try
        {
            _notifyMyFolder.Subscribe(r =>
            {
                try
                {
                    GlobalFolder.UserRootFolderCache.Remove(r.Key, out _);
                }
                catch (Exception e)
                {
                    Logger.CriticalClearCacheAction(e);
                }
            }, CacheNotifyAction.Remove);
        }
        catch (Exception e)
        {
            Logger.CriticalClearCacheSubscribe(e);
        }
    }

    private void ClearCache()
    {
        try
        {
            _notify.Subscribe(_ =>
            {
                try
                {
                    GlobalFolder.ProjectsRootFolderCache.Clear();
                    GlobalFolder.UserRootFolderCache.Clear();
                    GlobalFolder.CommonFolderCache.Clear();
                    GlobalFolder.ShareFolderCache.Clear();
                    GlobalFolder.RecentFolderCache.Clear();
                    GlobalFolder.FavoritesFolderCache.Clear();
                    GlobalFolder.TemplatesFolderCache.Clear();
                    GlobalFolder.PrivacyFolderCache.Clear();
                    GlobalFolder.TrashFolderCache.Clear();
                }
                catch (Exception e)
                {
                    Logger.CriticalClearCacheAction(e);
                }
            }, CacheNotifyAction.Any);
        }
        catch (Exception e)
        {
            Logger.CriticalClearCacheSubscribe(e);
        }
    }
}

[EnumExtensions]
public enum ThumbnailExtension
{
    bmp,
    gif,
    jpg,
    png,
    pbm,
    tiff,
    tga,
    webp
}

[EnumExtensions]
public enum DocThumbnailExtension
{
    bmp,
    gif,
    jpg,
    png
}

[Scope]
public partial class Global(
    IConfiguration configuration,
    AuthContext authContext,
    UserManager userManager,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    CustomNamingPeople customNamingPeople,
    FileSecurityCommon fileSecurityCommon,
    IDistributedLockProvider distributedLockProvider)
{
    private DocThumbnailExtension? _docThumbnailExtension;
    public DocThumbnailExtension DocThumbnailExtension
    {
        get
        {
            if (_docThumbnailExtension.HasValue)
            {
                return _docThumbnailExtension.Value;
            }

            if (!DocThumbnailExtensionExtensions.TryParse(configuration["files:thumbnail:docs-exts"] ?? "jpg", true, out var fromConfig))
            {
                fromConfig = DocThumbnailExtension.jpg;
            }

            _docThumbnailExtension = fromConfig;
            return fromConfig;
        }
    }

    private ThumbnailExtension? _thumbnailExtension;
    public ThumbnailExtension ThumbnailExtension
    {
        get
        {
            if (_thumbnailExtension.HasValue)
            {
                return _thumbnailExtension.Value;
            }

            if (!ThumbnailExtensionExtensions.TryParse(configuration["files:thumbnail:exts"] ?? "webp", true, out var fromConfig))
            {
                fromConfig = ThumbnailExtension.jpg;
            }

            _thumbnailExtension = fromConfig;
            return fromConfig;
        }
    }

    public List<string> ImageThumbnailExtension
    {
        get
        {
            if (field != null)
            {
                return field;
            }

            field = configuration.GetSection("files:thumbnail:img-exts").Get<List<string>>() ?? [".bmp", ".gif", ".jpeg", ".jpg", ".pbm", ".png", ".tiff", ".tif", ".tga", ".webp", ".heic"];
            return field;
        }
    }

    private const int MaxTitle = 170;

    private static readonly Regex _invalidTitleChars = new("[\t*\\+:\"<>?|\\\\/\\p{Cs}]");

    public bool EnableUploadFilter => bool.TrueString.Equals(configuration["files:upload-filter"] ?? "false", StringComparison.InvariantCultureIgnoreCase);

    public TimeSpan StreamUrlExpire
    {
        get
        {
            int.TryParse(configuration["files:stream-url-minute"], out var validateTimespan);
            if (validateTimespan <= 0)
            {
                validateTimespan = 16;
            }

            return TimeSpan.FromMinutes(validateTimespan);
        }
    }

    public Task<bool> IsDocSpaceAdministratorAsync => fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID);


    public static string ReplaceInvalidCharsAndTruncate(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return title;
        }

        title = title.Trim();
        if (MaxTitle < title.Length)
        {
            var pos = title.LastIndexOf('.');
            if (MaxTitle - 20 < pos)
            {
                title = title[..(MaxTitle - (title.Length - pos))] + title[pos..];
            }
            else
            {
                title = title[..MaxTitle];
            }
        }

        return _invalidTitleChars.Replace(title, "_");
    }

    public async Task<string> GetUserNameAsync(Guid userId, bool alive = false)
    {
        if (userId.Equals(authContext.CurrentAccount.ID))
        {
            return FilesCommonResource.Author_Me;
        }

        if (userId.Equals(ASC.Core.Configuration.Constants.Guest.ID))
        {
            return FilesCommonResource.Guest;
        }

        var userInfo = await userManager.GetUsersAsync(userId);
        if (userInfo.Equals(Constants.LostUser))
        {
            return alive ? FilesCommonResource.Guest : await customNamingPeople.Substitute<FilesCommonResource>("ProfileRemoved");
        }

        return userInfo.DisplayUserName(false, displayUserSettingsHelper);
    }

    public async Task<string> GetAvailableTitleAsync<T>(string requestTitle, T parentFolderId, Func<string, T, Task<bool>> isExist, FileEntryType fileEntryType)
    {
        await using (await distributedLockProvider.TryAcquireFairLockAsync($"{nameof(GetAvailableTitleAsync)}_{parentFolderId}"))
        {
            if (!await isExist(requestTitle, parentFolderId))
            {
                return requestTitle;
            }

            var re = MyRegex();

            var insertIndex = requestTitle.Length;
            if (fileEntryType == FileEntryType.File && requestTitle.LastIndexOf('.') != -1)
            {
                insertIndex = requestTitle.LastIndexOf('.');
            }

            requestTitle = requestTitle.Insert(insertIndex, " (1)");

            while (await isExist(requestTitle, parentFolderId))
            {
                requestTitle = re.Replace(requestTitle, MatchEvaluator);
            }

            return requestTitle;
        }
    }

    private static string MatchEvaluator(Match match)
    {
        var index = Convert.ToInt32(match.Groups[2].Value);
        var staticText = match.Value[$" ({index})".Length..];

        return $" ({index + 1}){staticText}";
    }

    [GeneratedRegex(@"( \(((?<index>[0-9])+)\)(\.[^\.]*)?)$")]
    private static partial Regex MyRegex();
}

[Scope]
public class GlobalStore(StorageFactory storageFactory, TenantManager tenantManager, CoreBaseSettings coreBaseSettings)
{
    public async Task<IDataStore> GetStoreAsync(bool currentTenant = true)
    {
        return await GetStoreAsync(currentTenant ? tenantManager.GetCurrentTenantId() : -1);
    }

    private readonly ConcurrentDictionary<int, IDataStore> _currentTenantStore = new();
    internal async Task<IDataStore> GetStoreAsync(int tenantId)
    {
        if (_currentTenantStore.TryGetValue(tenantId, out var result))
        {
            return result;
        }

        result = await storageFactory.GetStorageAsync(tenantId, FileConstant.StorageModule);
        _currentTenantStore.TryAdd(tenantId, result);

        return result;
    }

    public async Task<IDataStore> GetStoreTemplateAsync()
    {
        return await storageFactory.GetStorageAsync(-1, FileConstant.StorageTemplate);
    }

    public async Task<string> GetNewDocTemplatePath(IDataStore storeTemplate, CultureInfo culture = null)
    {
        var defaultPath = coreBaseSettings.CustomMode
                ? FileConstant.NewDocDefaultCustomModePath
                : FileConstant.NewDocDefaultPath;

        return await GetPathDependingOnCulture(storeTemplate, FileConstant.NewDocPath, defaultPath, culture);
    }

    public async Task<string> GetNewDocTemplatePath(IDataStore storeTemplate, string extension, CultureInfo culture = null)
    {
        return $"{await GetNewDocTemplatePath(storeTemplate, culture)}{FileConstant.NewDocFileName}{extension}";
    }

    public class DocTemplate
    {
        public string Title { get; init; }
        public string FileExtension { get; init; }
        public Func<Task<Stream>> GetStreamAsync { get; init; }
        public long FileSize { get; init; }
        public string ThumbnailPath { get; init; }
        public string FileName => Title + FileExtension;
    }

    public async Task<DocTemplate> GetNewDocTemplate(IServiceProvider serviceProvider, IDataStore storeTemplate, string extension, CultureInfo culture = null, bool ignoreTemplates = false)
    {
        if (!ignoreTemplates)
        {
            var templateSettingsHelper = serviceProvider.GetRequiredService<DefaultTemplateSettingsHelper>();
            var templateSettings = await templateSettingsHelper.GetSettingsAsync();

            var templateSetting = templateSettings.Items.FirstOrDefault(t => t.FileExtension == extension);
            if (templateSetting?.SelectedFile != null)
            {
                var fileDao = serviceProvider.GetRequiredService<IFileDao<int>>();
                var file = await fileDao.GetFileAsync(templateSetting.SelectedFile.Value);

                return new DocTemplate
                {
                    Title = file.Title,
                    FileExtension = extension,
                    FileSize = file.ContentLength,
                    GetStreamAsync = () => fileDao.GetFileStreamAsync(file)
                };
            }
        }

        var defaultPath = coreBaseSettings.CustomMode
            ? FileConstant.NewDocDefaultCustomModePath
            : FileConstant.NewDocDefaultPath;

        var path = await GetPathDependingOnCulture(storeTemplate, FileConstant.NewDocPath, defaultPath, culture);
        var filePath = $"{path}{FileConstant.NewDocFileName}{extension}";

        return await storeTemplate.IsFileAsync("", filePath)
            ? new DocTemplate
            {
                Title = Path.GetFileNameWithoutExtension(filePath),
                FileExtension = extension,
                GetStreamAsync = () => storeTemplate.GetReadStreamAsync(filePath),
                FileSize = await storeTemplate.GetFileSizeAsync(filePath),
                ThumbnailPath = filePath.Replace(Path.GetFileName(filePath), string.Empty)
            }
            : null;
    }

    public async Task<string> GetStartDocsPath(IDataStore storeTemplate, bool my, CultureInfo culture = null)
    {
        var path = await GetPathDependingOnCulture(storeTemplate, FileConstant.StartDocPath, FileConstant.StartDocDefaultPath, culture);

        return $"{path}{(my ? FileConstant.StartDocMyPath : FileConstant.StartDocCorporatePath)}";
    }

    private async Task<string> GetPathDependingOnCulture(IDataStore storeTemplate, string targetDir, string defaultSubDir, CultureInfo culture)
    {
        var path = $"{targetDir}{defaultSubDir}";

        if (culture != null)
        {
            var ciltureName = culture.ToString();

            await foreach (var dirName in storeTemplate.ListDirectoriesRelativeAsync(targetDir, false))
            {
                if (dirName.StartsWith(ciltureName))
                {
                    path = $"{targetDir}{dirName}/";
                    break;
                }
            }
        }

        return path;
    }
}

[Scope]
public class GlobalFolder(
    WebItemManager webItemManager,
    WebItemSecurity webItemSecurity,
    AuthContext authContext,
    TenantManager tenantManager,
    UserManager userManager,
    SettingsManager settingsManager,
    ILogger<GlobalFolder> logger,
    IServiceProvider serviceProvider)
{
    internal static readonly IDictionary<int, int> ProjectsRootFolderCache = new ConcurrentDictionary<int, int>(); /*Use SYNCHRONIZED for cross thread blocks*/

    public async ValueTask<int> GetFolderProjectsAsync(IDaoFactory daoFactory)
    {
        if (await webItemManager[WebItemManager.ProjectsProductID].IsDisabledAsync(webItemSecurity, authContext))
        {
            return 0;
        }

        var tenant = tenantManager.GetCurrentTenant();
        var folderDao = daoFactory.GetFolderDao<int>();
        if (!ProjectsRootFolderCache.TryGetValue(tenant.Id, out var result))
        {
            result = await folderDao.GetFolderIDProjectsAsync(true);

            ProjectsRootFolderCache[tenant.Id] = result;
        }

        return result;
    }

    public async ValueTask<T> GetFolderProjectsAsync<T>(IDaoFactory daoFactory)
    {
        return IdConverter.Convert<T>(await GetFolderProjectsAsync(daoFactory));
    }

    internal static readonly ConcurrentDictionary<string, int> DocSpaceFolderCache = new();

    public async ValueTask<int> GetFolderVirtualRoomsAsync(IDaoFactory daoFactory, bool createIfNotExist = true)
    {
        var key = $"vrooms/{tenantManager.GetCurrentTenantId()}";

        if (DocSpaceFolderCache.TryGetValue(key, out var result))
        {
            return result;
        }

        result = await daoFactory.GetFolderDao<int>().GetFolderIDVirtualRooms(createIfNotExist);

        if (result != 0)
        {
            DocSpaceFolderCache[key] = result;
        }

        return result;
    }

    public async ValueTask<int> GetFolderRoomTemplatesAsync(IDaoFactory daoFactory, bool createIfNotExist = true)
    {
        var key = $"roomTemplates/{tenantManager.GetCurrentTenantId()}";

        if (DocSpaceFolderCache.TryGetValue(key, out var result))
        {
            return result;
        }

        result = await daoFactory.GetFolderDao<int>().GetFolderIDRoomTemplatesAsync(createIfNotExist);

        if (result != default)
        {
            DocSpaceFolderCache[key] = result;
        }

        return result;
    }

    public async ValueTask<int> GetFolderDefaultTemplatesAsync(IDaoFactory daoFactory, bool createIfNotExist = true)
    {
        var key = $"defaultTemplates/{tenantManager.GetCurrentTenantId()}";

        if (DocSpaceFolderCache.TryGetValue(key, out var result))
        {
            return result;
        }

        result = await daoFactory.GetFolderDao<int>().GetFolderIDDefaultTemplatesAsync(createIfNotExist);

        if (result != default)
        {
            DocSpaceFolderCache[key] = result;
        }

        return result;
    }

    public async ValueTask<int> GetFolderArchiveAsync(IDaoFactory daoFactory)
    {
        var key = $"archive/{tenantManager.GetCurrentTenantId()}";

        if (!DocSpaceFolderCache.TryGetValue(key, out var result))
        {
            result = await daoFactory.GetFolderDao<int>().GetFolderIDArchive(true);

            DocSpaceFolderCache[key] = result;
        }

        return result;
    }

    public async ValueTask<int> GetFolderAiAgentsAsync(IDaoFactory daoFactory)
    {
        var key = $"aiagents/{tenantManager.GetCurrentTenantId()}";

        if (!DocSpaceFolderCache.TryGetValue(key, out var result))
        {
            result = await daoFactory.GetFolderDao<int>().GetFolderIDAiAgentsAsync(true);

            DocSpaceFolderCache[key] = result;
        }

        return result;
    }

    public async ValueTask<int> GetFolderFormsAsync(IDaoFactory daoFactory)
    {
        var key = $"forms/{tenantManager.GetCurrentTenantId()}";

        if (!DocSpaceFolderCache.TryGetValue(key, out var result))
        {
            result = await daoFactory.GetFolderDao<int>().GetFolderIDFormsAsync(true);

            DocSpaceFolderCache[key] = result;
        }

        return result;
    }

    internal static readonly ConcurrentDictionary<string, Lazy<int>> UserRootFolderCache = new(); /*Use SYNCHRONIZED for cross thread blocks*/

    public async ValueTask<int> GetFolderMyAsync(IDaoFactory daoFactory)
    {
        if (!authContext.IsAuthenticated)
        {
            return 0;
        }

        var cacheKey = $"my/{tenantManager.GetCurrentTenantId()}/{authContext.CurrentAccount.ID}";

        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            var myFolderId = UserRootFolderCache.GetOrAdd(cacheKey, _ => new Lazy<int>(() => GetFolderIDUserAsync(daoFactory).Result));
            return myFolderId.Value;
        }
        else
        {
            var myFolderId = UserRootFolderCache.GetOrAdd(cacheKey, _ => new Lazy<int>(() => GetFolderIdAndProcessFirstVisitAsync(daoFactory, true).Result));
            if (myFolderId.Value == 0)
            {
                UserRootFolderCache.Remove(cacheKey, out _);
                myFolderId = UserRootFolderCache.GetOrAdd(cacheKey, _ => new Lazy<int>(() => GetFolderIdAndProcessFirstVisitAsync(daoFactory, true).Result));
            }
            return myFolderId.Value;
        }
    }

    private async Task<int> GetFolderIDUserAsync(IDaoFactory daoFactory)
    {
        var folderDao = daoFactory.GetFolderDao<int>();
        return await folderDao.GetFolderIDUserAsync(false);
    }

    internal static readonly IDictionary<int, int> CommonFolderCache =
            new ConcurrentDictionary<int, int>(); /*Use SYNCHRONIZED for cross thread blocks*/

    public async ValueTask<T> GetFolderCommonAsync<T>(IDaoFactory daoFactory)
    {
        return IdConverter.Convert<T>(await GetFolderCommonAsync(daoFactory));
    }

    public async ValueTask<int> GetFolderCommonAsync(IDaoFactory daoFactory)
    {
        var tenant = tenantManager.GetCurrentTenant();
        if (CommonFolderCache.TryGetValue(tenant.Id, out var commonFolderId))
        {
            return commonFolderId;
        }

        commonFolderId = await GetFolderIdAndProcessFirstVisitAsync(daoFactory, false);

        if (!Equals(commonFolderId, 0))
        {
            CommonFolderCache[tenant.Id] = commonFolderId;
        }

        return commonFolderId;
    }

    internal static readonly IDictionary<int, int> ShareFolderCache =
        new ConcurrentDictionary<int, int>(); /*Use SYNCHRONIZED for cross thread blocks*/

    public async ValueTask<int> GetFolderShareAsync(IDaoFactory daoFactory)
    {
        if (await IsOutsiderAsync)
        {
            return 0;
        }

        var tenant = tenantManager.GetCurrentTenant();
        if (!ShareFolderCache.TryGetValue(tenant.Id, out var sharedFolderId))
        {
            sharedFolderId = await daoFactory.GetFolderDao<int>().GetFolderIDShareAsync(true);

            if (!sharedFolderId.Equals(0))
            {
                ShareFolderCache[tenant.Id] = sharedFolderId;
            }
        }

        return sharedFolderId;
    }

    public async ValueTask<T> GetFolderShareAsync<T>(IDaoFactory daoFactory)
    {
        return IdConverter.Convert<T>(await GetFolderShareAsync(daoFactory));
    }

    internal static readonly IDictionary<int, int> RecentFolderCache =
        new ConcurrentDictionary<int, int>(); /*Use SYNCHRONIZED for cross thread blocks*/

    public async ValueTask<int> GetFolderRecentAsync(IDaoFactory daoFactory)
    {
        if (!authContext.IsAuthenticated)
        {
            return 0;
        }

        var tenant = tenantManager.GetCurrentTenant();
        if (!RecentFolderCache.TryGetValue(tenant.Id, out var recentFolderId))
        {
            var folderDao = daoFactory.GetFolderDao<int>();
            recentFolderId = await folderDao.GetFolderIDRecentAsync(true);

            if (!recentFolderId.Equals(0))
            {
                RecentFolderCache[tenant.Id] = recentFolderId;
            }
        }

        return recentFolderId;
    }

    internal static readonly IDictionary<int, int> FavoritesFolderCache =
        new ConcurrentDictionary<int, int>(); /*Use SYNCHRONIZED for cross thread blocks*/

    public async ValueTask<int> GetFolderFavoritesAsync(IDaoFactory daoFactory)
    {
        if (!authContext.IsAuthenticated)
        {
            return 0;
        }

        var tenant = tenantManager.GetCurrentTenant();
        if (!FavoritesFolderCache.TryGetValue(tenant.Id, out var favoriteFolderId))
        {
            var folderDao = daoFactory.GetFolderDao<int>();
            favoriteFolderId = await folderDao.GetFolderIDFavoritesAsync(true);

            if (!favoriteFolderId.Equals(0))
            {
                FavoritesFolderCache[tenant.Id] = favoriteFolderId;
            }
        }

        return favoriteFolderId;
    }

    internal static readonly IDictionary<int, int> TemplatesFolderCache =
        new ConcurrentDictionary<int, int>(); /*Use SYNCHRONIZED for cross thread blocks*/

    public async ValueTask<int> GetFolderTemplatesAsync(IDaoFactory daoFactory)
    {
        if (!authContext.IsAuthenticated)
        {
            return 0;
        }

        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            return 0;
        }
        var tenant = tenantManager.GetCurrentTenant();
        if (!TemplatesFolderCache.TryGetValue(tenant.Id, out var templatesFolderId))
        {
            var folderDao = daoFactory.GetFolderDao<int>();
            templatesFolderId = await folderDao.GetFolderIDTemplatesAsync(true);

            if (!templatesFolderId.Equals(0))
            {
                TemplatesFolderCache[tenant.Id] = templatesFolderId;
            }
        }

        return templatesFolderId;
    }

    internal static readonly IDictionary<string, int> PrivacyFolderCache =
        new ConcurrentDictionary<string, int>(); /*Use SYNCHRONIZED for cross thread blocks*/

    public async ValueTask<T> GetFolderPrivacyAsync<T>(IDaoFactory daoFactory)
    {
        return IdConverter.Convert<T>(await GetFolderPrivacyAsync(daoFactory));
    }

    public async ValueTask<int> GetFolderPrivacyAsync(IDaoFactory daoFactory)
    {
        if (!authContext.IsAuthenticated)
        {
            return 0;
        }

        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            return 0;
        }

        var cacheKey = $"privacy/{tenantManager.GetCurrentTenantId()}/{authContext.CurrentAccount.ID}";

        if (!PrivacyFolderCache.TryGetValue(cacheKey, out var privacyFolderId))
        {
            var folderDao = daoFactory.GetFolderDao<int>();
            privacyFolderId = await folderDao.GetFolderIDPrivacyAsync(true);

            if (!Equals(privacyFolderId, 0))
            {
                PrivacyFolderCache[cacheKey] = privacyFolderId;
            }
        }

        return privacyFolderId;
    }


    internal static readonly IDictionary<string, object> TrashFolderCache =
        new ConcurrentDictionary<string, object>(); /*Use SYNCHRONIZED for cross thread blocks*/

    public async ValueTask<int> GetFolderTrashAsync(IDaoFactory daoFactory)
    {
        return await GetFolderTrashAsync(daoFactory, authContext.CurrentAccount.ID);
    }

    public async ValueTask<int> GetFolderTrashAsync(IDaoFactory daoFactory, Guid userId)
    {
        var id = 0;
        if (await IsOutsiderAsync)
        {
            return id;
        }

        var cacheKey = $"trash/{tenantManager.GetCurrentTenant().Id}/{userId}";
        if (!TrashFolderCache.TryGetValue(cacheKey, out var trashFolderId))
        {
            id = authContext.IsAuthenticated ? await daoFactory.GetFolderDao<int>().GetFolderIDTrashAsync(true, userId) : 0;
            TrashFolderCache[cacheKey] = id;
        }
        else
        {
            id = (int)trashFolderId;
        }

        return id;
    }

    public void SetFolderTrashAsync(object value)
    {
        var cacheKey = $"trash/{tenantManager.GetCurrentTenantId()}/{value}";
        TrashFolderCache.Remove(cacheKey);
    }

    private async Task<int> GetFolderIdAndProcessFirstVisitAsync(IDaoFactory daoFactory, bool my)
    {
        var folderDao = (FolderDao)daoFactory.GetFolderDao<int>();

        var id = my ? await folderDao.GetFolderIDUserAsync(false) : await folderDao.GetFolderIDCommonAsync(false);

        if (!Equals(id, 0))
        {
            return id;
        }

        id = my ? await folderDao.GetFolderIDUserAsync(true) : await folderDao.GetFolderIDCommonAsync(true);

        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;

        if (my && userId == tenantManager.GetCurrentTenant().OwnerId)
        {
            await TryScheduleDemoFormRoomAsync(tenantId, userId);
        }

        if (!(await settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>()).StartDocsEnabled)
        {
            return id;
        }

        RunFireAndForget(async () => await CreateSampleDocumentsAsync(serviceProvider, tenantId, userId, id, my));

        return id;
    }

    private static void RunFireAndForget(System.Action action)
    {
        var task = new Task(action, TaskCreationOptions.LongRunning);

        _ = task.ConfigureAwait(false);

        task.Start();
    }

    private async Task TryScheduleDemoFormRoomAsync(int tenantId, Guid userId)
    {
        var settings = await settingsManager.LoadAsync<DemoFormRoomSettings>(tenantId);
        if (settings.IsCreated)
        {
            return;
        }

        settings.IsCreated = true;
        await settingsManager.SaveAsync(settings, tenantId);

        RunFireAndForget(async () => await CreateDemoFormRoomAsync(serviceProvider, tenantId, userId));
    }

    /// <summary>
    /// Provisions the owner-only onboarding demo: a form-filling room with a sample form and a batch of
    /// synthetic filled-in submissions synced to the built-in forms database.
    /// </summary>
    private async Task CreateDemoFormRoomAsync(IServiceProvider serviceProvider, int tenantId, Guid userId)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();

            await tenantManager.SetCurrentTenantAsync(tenantId);
            await securityContext.AuthenticateMeWithoutCookieAsync(userId);

            var globalStore = scope.ServiceProvider.GetRequiredService<GlobalStore>();
            var storeTemplate = await globalStore.GetStoreTemplateAsync();
            var formPath = $"{FileConstant.StartDocPath}{FileConstant.StartDocDefaultPath}{FileConstant.DemoFormRoomPath}{FileConstant.DemoFormRoomFileName}";

            if (!await storeTemplate.IsFileAsync("", formPath))
            {
                logger.WarnDemoFormAssetMissing(formPath);

                // The asset may simply not have been deployed yet — un-flag as "created" so the next
                // owner visit retries, instead of permanently skipping the demo for this tenant.
                await ResetDemoFormRoomFlagAsync(tenantId);

                return;
            }

            var (roomId, savedFile) = await CreateDemoFormRoomFileAsync(scope.ServiceProvider, storeTemplate, formPath);

            await SeedDemoFormSubmissionsAsync(scope.ServiceProvider, tenantId, roomId, savedFile);
        }
        catch (Exception e)
        {
            logger.ErrorCreateDemoFormRoom(e);

            // Un-flag as "created" so a transient failure (unreachable service, quota, etc.) doesn't
            // permanently skip the demo for this tenant — the next owner visit will retry.
            await ResetDemoFormRoomFlagAsync(tenantId);
        }
    }

    private async Task ResetDemoFormRoomFlagAsync(int tenantId)
    {
        try
        {
            var settings = await settingsManager.LoadAsync<DemoFormRoomSettings>(tenantId);
            settings.IsCreated = false;
            await settingsManager.SaveAsync(settings, tenantId);
        }
        catch (Exception e)
        {
            logger.ErrorCreateDemoFormRoom(e);
        }
    }

    private static async Task<(int RoomId, File<int> SavedFile)> CreateDemoFormRoomFileAsync(
        IServiceProvider scopedProvider, IDataStore storeTemplate, string formPath)
    {
        var fileStorageService = scopedProvider.GetRequiredService<FileStorageService>();

        var room = await fileStorageService.CreateRoomAsync(
            "Demo: Customer Feedback", RoomType.FillingFormsRoom, privacy: false, indexing: true,
            share: null, quota: null, lifetime: null, denyDownload: false, watermark: null,
            color: null, cover: null, tags: ["demo"], logo: null, chatSettings: null,
            sendFormToExternalDB: true, saveFormAsXLSX: false);

        var fileDao = (FileDao)scopedProvider.GetRequiredService<IFileDao<int>>();
        var fileMarker = scopedProvider.GetRequiredService<FileMarker>();
        var socketManager = scopedProvider.GetRequiredService<SocketManager>();

        var newFile = scopedProvider.GetRequiredService<File<int>>();
        newFile.Title = FileConstant.DemoFormRoomFileName;
        newFile.ParentId = room.Id;
        newFile.Category = (int)FilterType.PdfForm;
        newFile.Comment = FilesCommonResource.CommentCreate;

        File<int> savedFile;
        await using (var stream = await storeTemplate.GetReadStreamAsync("", formPath))
        {
            newFile.ContentLength = stream.CanSeek ? stream.Length : await storeTemplate.GetFileSizeAsync("", formPath);
            savedFile = await fileDao.SaveFileAsync(newFile, stream, false, true);
        }

        await fileMarker.MarkAsNewAsync(savedFile);
        await socketManager.CreateFileAsync(savedFile);

        await fileStorageService.ManageFormFilling(savedFile.Id, FormFillingManageAction.Start);

        return (room.Id, savedFile);
    }

    private async Task SeedDemoFormSubmissionsAsync(IServiceProvider scopedProvider, int tenantId, int roomId, File<int> savedFile)
    {
        var originalFormId = savedFile.Id;
        var originalFormVersion = savedFile.Version;

        var (metadata, submissions) = GenerateSyntheticSubmissions(tenantId, roomId, originalFormId, originalFormVersion);

        // Best-effort: also index into OpenSearch so the demo data is consistent with what a real
        // submission would produce (search, reconciliation) — but never block seeding on it, since
        // it is not guaranteed to be reachable in every environment.
        try
        {
            var factoryIndexerFormMetadata = scopedProvider.GetRequiredService<FactoryIndexerFormMetadata>();
            await factoryIndexerFormMetadata.IndexAsync(new DbFormsMetadataSearch
            {
                Id = DbFormsMetadataSearch.ComputeId(originalFormId, originalFormVersion),
                TenantId = tenantId,
                OriginalFormId = originalFormId,
                OriginalFormVersion = originalFormVersion,
                RoomId = roomId,
                Metadata = metadata
            }, waitForCompletion: true);

            var factoryIndexerForm = scopedProvider.GetRequiredService<FactoryIndexerForm>();
            foreach (var submission in submissions)
            {
                await factoryIndexerForm.IndexAsync(submission, waitForCompletion: true);
            }
        }
        catch (Exception e)
        {
            logger.WarnDemoFormOpenSearchIndexingFailed(e);
        }

        // Write straight into the built-in Postgres DB — this is the path the AI tools actually read
        // from, and it must not depend on OpenSearch being reachable.
        var formFillingReportCreator = scopedProvider.GetRequiredService<FormFillingReportCreator>();
        await formFillingReportCreator.SeedBuiltinDbDirectlyAsync(
            originalFormId, originalFormVersion, metadata,
            submissions.Select(s => (s.Id, s.CreateOn, (SubmitFormsData)s)));
    }

    private const int SyntheticSubmissionIdBase = 900_000_000;

    private static (List<FormMetadata> Metadata, List<DbFormsItemDataSearch> Submissions) GenerateSyntheticSubmissions(
        int tenantId, int roomId, int originalFormId, int originalFormVersion)
    {
        string[] satisfactionLevels = ["Excellent", "Good", "Average", "Poor"];
        string[] comments =
        [
            "Great service, will definitely come back.",
            "The support team resolved my issue quickly.",
            "Delivery took longer than expected.",
            "Exactly what I was looking for.",
            "Could be better, but overall satisfied.",
            "Outstanding experience from start to finish.",
            "A few rough edges, but the team was helpful.",
            "Not what I expected, needs improvement.",
            "Very happy with the quality.",
            "Average experience, nothing special.",
            "Exceeded my expectations!",
            "Support response time was slow.",
            "Smooth process, no complaints.",
            "Would recommend to a friend.",
            "It was okay, could use more options."
        ];

        var metadata = new List<FormMetadata>
        {
            new() { Key = "FormNumber", Type = "text" },
            new() { Key = "Satisfaction", Type = "radio", PossibleValues = [.. satisfactionLevels] },
            new() { Key = "Comment", Type = "text" },
            new() { Key = "SubmittedDate", Type = "dateTime", Format = "DD.MM.YYYY HH:mm" },
            new() { Key = "WouldRecommend", Type = "checkBox" }
        };

        var random = new Random();
        var submissions = new List<DbFormsItemDataSearch>();

        for (var i = 1; i <= 15; i++)
        {
            var satisfaction = satisfactionLevels[random.Next(satisfactionLevels.Length)];
            var recommend = satisfaction is "Excellent" or "Good" ? random.Next(100) < 85 : random.Next(100) < 30;
            var submittedOn = DateTime.UtcNow.AddDays(-random.Next(1, 42)).AddHours(-random.Next(0, 24));

            submissions.Add(new DbFormsItemDataSearch
            {
                Id = SyntheticSubmissionIdBase + i,
                TenantId = tenantId,
                ParentId = roomId,
                OriginalFormId = originalFormId,
                OriginalFormVersion = originalFormVersion,
                RoomId = roomId,
                CreateOn = submittedOn,
                FormsData =
                [
                    new FormsItemData { Key = "FormNumber", Value = i.ToString(), Type = "text" },
                    new FormsItemData { Key = "Satisfaction", Value = satisfaction, Type = "radio" },
                    new FormsItemData { Key = "Comment", Value = comments[random.Next(comments.Length)], Type = "text" },
                    new FormsItemData { Key = "SubmittedDate", Value = submittedOn.ToString("dd.MM.yyyy HH:mm"), Type = "dateTime" },
                    new FormsItemData { Key = "WouldRecommend", Value = recommend.ToString(), Type = "checkBox" }
                ]
            });
        }

        return (metadata, submissions);
    }

    private async Task CreateSampleDocumentsAsync(IServiceProvider serviceProvider, int tenantId, Guid userId, int folderId, bool my)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();

            await tenantManager.SetCurrentTenantAsync(tenantId);
            await securityContext.AuthenticateMeWithoutCookieAsync(userId);

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
            var culture = my ? (await userManager.GetUsersAsync(userId)).GetCulture() : tenantManager.GetCurrentTenant().GetCulture();

            var globalStore = scope.ServiceProvider.GetRequiredService<GlobalStore>();
            var storeTemplate = await globalStore.GetStoreTemplateAsync();

            var path = await globalStore.GetStartDocsPath(storeTemplate, my, culture);

            var fileMarker = scope.ServiceProvider.GetRequiredService<FileMarker>();
            var fileDao = (FileDao)scope.ServiceProvider.GetRequiredService<IFileDao<int>>();
            var folderDao = (FolderDao)scope.ServiceProvider.GetRequiredService<IFolderDao<int>>();
            var socketManager = scope.ServiceProvider.GetRequiredService<SocketManager>();

            await SaveSampleDocumentsAsync(scope.ServiceProvider, fileMarker, folderDao, fileDao, socketManager, folderId, path, storeTemplate);
        }
        catch (Exception e)
        {
            logger.ErrorCreateSampleDocuments(e);
        }
    }

    private async Task SaveSampleDocumentsAsync(IServiceProvider serviceProvider, FileMarker fileMarker, FolderDao folderDao, FileDao fileDao, SocketManager socketManager,
        int folderId, string path, IDataStore storeTemplate)
    {
        var files = await storeTemplate.ListFilesRelativeAsync("", path, "*", false)
            .Where(f => FileUtility.GetFileTypeByFileName(f) is not (FileType.Audio or FileType.Video))
            .ToListAsync();

        logger.Debug($"Found {files.Count} sample documents. Path: {path}");

        foreach (var file in files)
        {
            await SaveFileAsync(serviceProvider, storeTemplate, fileMarker, fileDao, socketManager, path + file, folderId, files);
        }

        await foreach (var folderName in storeTemplate.ListDirectoriesRelativeAsync(path, false))
        {
            try
            {
                var folder = serviceProvider.GetRequiredService<Folder<int>>();
                folder.Title = folderName;
                folder.ParentId = folderId;

                var subFolderId = await folderDao.SaveFolderAsync(folder);

                var subFolder = await folderDao.GetFolderAsync(subFolderId);
                await socketManager.CreateFolderAsync(subFolder);

                await SaveSampleDocumentsAsync(serviceProvider, fileMarker, folderDao, fileDao, socketManager, folderId, path + folderName + "/", storeTemplate);
            }
            catch (Exception e)
            {
                logger.ErrorSaveSampleFolder(e);
            }
        }
    }

    private async Task SaveFileAsync(IServiceProvider serviceProvider, IDataStore storeTemplate, FileMarker fileMarker, FileDao fileDao, SocketManager socketManager,
        string filePath, int folderId, List<string> files)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);

            foreach (var ext in Enum.GetValues<ThumbnailExtension>())
            {
                if (FileUtility.GetFileExtension(filePath) == "." + ext
                    && files.Contains(Regex.Replace(fileName, "\\." + ext + "$", "")))
                {
                    return;
                }
            }

            var newFile = serviceProvider.GetRequiredService<File<int>>();

            newFile.Title = fileName;
            newFile.ParentId = folderId;
            newFile.Comment = FilesCommonResource.CommentCreate;

            var fileType = FileUtility.GetFileTypeByFileName(fileName);
            if (fileType == FileType.Pdf)
            {
                newFile.Category = (int)FilterType.PdfForm;
            }

            await using (var stream = await storeTemplate.GetReadStreamAsync("", filePath))
            {
                newFile.ContentLength = stream.CanSeek ? stream.Length : await storeTemplate.GetFileSizeAsync("", filePath);
                newFile = await fileDao.SaveFileAsync(newFile, stream, false, true);
            }

            await fileMarker.MarkAsNewAsync(newFile);
            await socketManager.CreateFileAsync(newFile);
        }
        catch (Exception e)
        {
            logger.ErrorSaveSampleFile(e);
        }
    }

    private Task<bool> IsOutsiderAsync => userManager.IsOutsiderAsync(authContext.CurrentAccount.ID);
}

[Scope]
public class GlobalFolderHelper(IDaoFactory daoFactory, GlobalFolder globalFolder)
{
    public ValueTask<int> FolderProjectsAsync => globalFolder.GetFolderProjectsAsync(daoFactory);
    public ValueTask<int> FolderCommonAsync => globalFolder.GetFolderCommonAsync(daoFactory);
    public ValueTask<int> FolderMyAsync => globalFolder.GetFolderMyAsync(daoFactory);
    public ValueTask<int> FolderPrivacyAsync => globalFolder.GetFolderPrivacyAsync(daoFactory);
    public ValueTask<int> FolderRecentAsync => globalFolder.GetFolderRecentAsync(daoFactory);
    public ValueTask<int> FolderFavoritesAsync => globalFolder.GetFolderFavoritesAsync(daoFactory);
    public ValueTask<int> FolderTemplatesAsync => globalFolder.GetFolderTemplatesAsync(daoFactory);
    public ValueTask<int> FolderVirtualRoomsAsync => globalFolder.GetFolderVirtualRoomsAsync(daoFactory);
    public ValueTask<int> FolderRoomTemplatesAsync => globalFolder.GetFolderRoomTemplatesAsync(daoFactory);
    public ValueTask<int> FolderDefaultTemplatesAsync => globalFolder.GetFolderDefaultTemplatesAsync(daoFactory);
    public ValueTask<int> FolderArchiveAsync => globalFolder.GetFolderArchiveAsync(daoFactory);
    public ValueTask<int> FolderAiAgentsAsync => globalFolder.GetFolderAiAgentsAsync(daoFactory);
    public ValueTask<int> FolderFormsAsync => globalFolder.GetFolderFormsAsync(daoFactory);

    public async Task<T> GetFolderMyAsync<T>()
    {
        return IdConverter.Convert<T>(await FolderMyAsync);
    }

    public async ValueTask<T> GetFolderProjectsAsync<T>()
    {
        return IdConverter.Convert<T>(await FolderProjectsAsync);
    }

    public async ValueTask<T> GetFolderPrivacyAsync<T>()
    {
        return IdConverter.Convert<T>(await FolderPrivacyAsync);
    }

    public async ValueTask<int> GetFolderVirtualRooms()
    {
        return await FolderVirtualRoomsAsync;
    }

    public async ValueTask<int> GetFolderArchive()
    {
        return await FolderArchiveAsync;
    }

    public async ValueTask<int> GetFolderAiAgentsAsync()
    {
        return await FolderAiAgentsAsync;
    }

    public async ValueTask<int> GetFolderFormsAsync()
    {
        return await FolderFormsAsync;
    }

    public async ValueTask<int> GetFolderRoomTemplatesAsync()
    {
        return await FolderRoomTemplatesAsync;
    }

    public async ValueTask<int> GetFolderDefaultTemplatesAsync()
    {
        return await FolderDefaultTemplatesAsync;
    }

    public async ValueTask<T> GetFolderShareAsync<T>()
    {
        return IdConverter.Convert<T>(await FolderShareAsync);
    }

    public async ValueTask<T> GetFolderRecentAsync<T>()
    {
        return IdConverter.Convert<T>(await FolderRecentAsync);
    }

    public async ValueTask<T> GetFolderFavoritesAsync<T>()
    {
        return IdConverter.Convert<T>(await FolderFavoritesAsync);
    }

    public ValueTask<int> FolderShareAsync => globalFolder.GetFolderShareAsync(daoFactory);

    public void SetFolderTrashAsync(object value)
    {
        globalFolder.SetFolderTrashAsync(value);
    }
    public ValueTask<int> FolderTrashAsync => globalFolder.GetFolderTrashAsync(daoFactory);
}
