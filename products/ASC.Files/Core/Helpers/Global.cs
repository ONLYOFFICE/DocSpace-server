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

namespace ASC.Web.Files.Classes;

[Singleton]
public class GlobalNotify
{
    private ILogger Logger { get; set; }
    private readonly ICacheNotify<AscCacheItem> _notify;

    public GlobalNotify(ICacheNotify<AscCacheItem> notify, ILoggerProvider options, CoreBaseSettings coreBaseSettings)
    {
        _notify = notify;
        Logger = options.CreateLogger("ASC.Files");
        if (coreBaseSettings.Standalone)
        {
            ClearCache();
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
    CoreSettings coreSettings,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    CustomNamingPeople customNamingPeople,
    FileSecurityCommon fileSecurityCommon,
    IDistributedLockProvider distributedLockProvider)
{
    #region Property

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
            
            if (!ThumbnailExtensionExtensions.TryParse(configuration["files:thumbnail:exts"] ?? "webp", true,  out var fromConfig))
            {
                fromConfig = ThumbnailExtension.jpg;
            }
            
            _thumbnailExtension = fromConfig;
            return fromConfig;
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

    public async Task<string> GetDocDbKeyAsync()
    {
        const string dbKey = "UniqueDocument";
        
        // check without lock
        var resultKey = await coreSettings.GetSettingAsync(dbKey);
        if (!string.IsNullOrEmpty(resultKey))
        {
            return resultKey;
        }
        
        await using (await distributedLockProvider.TryAcquireFairLockAsync(dbKey))
        {
            // check again with lock
            resultKey = await coreSettings.GetSettingAsync(dbKey);
            if (!string.IsNullOrEmpty(resultKey))
            {
                return resultKey;
            }
            
            resultKey = Guid.NewGuid().ToString();
            await coreSettings.SaveSettingAsync(dbKey, resultKey);

            return resultKey;
        }
    }

    #endregion

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
public class GlobalStore(StorageFactory storageFactory, TenantManager tenantManager)
{    
    public async Task<IDataStore> GetStoreAsync(bool currentTenant = true)
    {
        return await GetStoreAsync(currentTenant ? await tenantManager.GetCurrentTenantIdAsync() : -1);
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
            return default;
        }

        var tenant = await tenantManager.GetCurrentTenantAsync();
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
        var key = $"vrooms/{await tenantManager.GetCurrentTenantIdAsync()}";

        if (DocSpaceFolderCache.TryGetValue(key, out var result))
        {
            return result;
        }

        result = await daoFactory.GetFolderDao<int>().GetFolderIDVirtualRooms(createIfNotExist);

        if (result != default)
        {
            DocSpaceFolderCache[key] = result;
        }

        return result;
    }

    public async ValueTask<int> GetFolderArchiveAsync(IDaoFactory daoFactory)
    {
        var key = $"archive/{await tenantManager.GetCurrentTenantIdAsync()}";

        if (!DocSpaceFolderCache.TryGetValue(key, out var result))
        {
            result = await daoFactory.GetFolderDao<int>().GetFolderIDArchive(true);

            DocSpaceFolderCache[key] = result;
        }

        return result;
    }

    internal static readonly ConcurrentDictionary<string, Lazy<int>> UserRootFolderCache = new(); /*Use SYNCHRONIZED for cross thread blocks*/

    public async ValueTask<int> GetFolderMyAsync(IDaoFactory daoFactory)
    {
        if (!authContext.IsAuthenticated)
        {
            return default;
        }

        if (await userManager.IsUserAsync(authContext.CurrentAccount.ID))
        {
            return default;
        }

        var cacheKey = $"my/{await tenantManager.GetCurrentTenantIdAsync()}/{authContext.CurrentAccount.ID}";

        var myFolderId = UserRootFolderCache.GetOrAdd(cacheKey, _ => new Lazy<int>(() => GetFolderIdAndProcessFirstVisitAsync(daoFactory, true).Result));

        return myFolderId.Value;
    }

    internal static readonly IDictionary<int, int> CommonFolderCache =
            new ConcurrentDictionary<int, int>(); /*Use SYNCHRONIZED for cross thread blocks*/

    public async ValueTask<T> GetFolderCommonAsync<T>(IDaoFactory daoFactory)
    {
        return IdConverter.Convert<T>(await GetFolderCommonAsync(daoFactory));
    }

    public async ValueTask<int> GetFolderCommonAsync(IDaoFactory daoFactory)
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();
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
            return default;
        }

        var tenant = await tenantManager.GetCurrentTenantAsync();
        if (!ShareFolderCache.TryGetValue(tenant.Id, out var sharedFolderId))
        {
            sharedFolderId = await daoFactory.GetFolderDao<int>().GetFolderIDShareAsync(true);

            if (!sharedFolderId.Equals(default))
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

        var tenant = await tenantManager.GetCurrentTenantAsync();
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

        var tenant = await tenantManager.GetCurrentTenantAsync();
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

        if (await userManager.IsUserAsync(authContext.CurrentAccount.ID))
        {
            return 0;
        }
        var tenant = await tenantManager.GetCurrentTenantAsync();
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

        if (await userManager.IsUserAsync(authContext.CurrentAccount.ID))
        {
            return 0;
        }

        var cacheKey = $"privacy/{await tenantManager.GetCurrentTenantIdAsync()}/{authContext.CurrentAccount.ID}";

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
        var id = 0;
        if (await IsOutsiderAsync)
        {
            return id;
        }

        var cacheKey = $"trash/{(await tenantManager.GetCurrentTenantAsync()).Id}/{authContext.CurrentAccount.ID}";
        if (!TrashFolderCache.TryGetValue(cacheKey, out var trashFolderId))
        {
            id = authContext.IsAuthenticated ? await daoFactory.GetFolderDao<int>().GetFolderIDTrashAsync(true) : 0;
            TrashFolderCache[cacheKey] = id;
        }
        else
        {
            id = (int)trashFolderId;
        }

        return id;
    }

    public async Task SetFolderTrashAsync(object value)
    {
        var cacheKey = $"trash/{await tenantManager.GetCurrentTenantIdAsync()}/{value}";
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
        
        if (!(await settingsManager.LoadForDefaultTenantAsync<AdditionalWhiteLabelSettings>()).StartDocsEnabled)
        {
            return id;
        }

        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        var userId = authContext.CurrentAccount.ID;

        var task = new Task(async () => await CreateSampleDocumentsAsync(serviceProvider, tenantId, userId, id, my), 
            TaskCreationOptions.LongRunning);

        _ = task.ConfigureAwait(false);
        
        task.Start();

        return id;
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
            var culture = my ? (await userManager.GetUsersAsync(userId)).GetCulture() : (await tenantManager.GetCurrentTenantAsync()).GetCulture();

            var globalStore = scope.ServiceProvider.GetRequiredService<GlobalStore>();
            var storeTemplate = await globalStore.GetStoreTemplateAsync();
            
            var path = FileConstant.StartDocPath + culture + "/";
            
            if (!await storeTemplate.IsDirectoryAsync(path))
            {
                path = FileConstant.StartDocPath + "en-US/";
            }
        
            path += my ? "my/" : "corporate/";

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
        string filePath, int folderId, IEnumerable<string> files)
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

            var fileExt = FileUtility.GetFileExtension(fileName);
            if (FileUtility.GetFileTypeByExtention(fileExt) == FileType.Pdf)
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
    public ValueTask<int> FolderArchiveAsync => globalFolder.GetFolderArchiveAsync(daoFactory);

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

    public async ValueTask<T> GetFolderShareAsync<T>()
    {
        return IdConverter.Convert<T>(await FolderShareAsync);
    }

    public async ValueTask<T> GetFolderRecentAsync<T>()
    {
        return IdConverter.Convert<T>(await FolderRecentAsync);
    }
    
    public ValueTask<int> FolderShareAsync => globalFolder.GetFolderShareAsync(daoFactory);

    public async Task SetFolderTrashAsync(object value)
    {
        await globalFolder.SetFolderTrashAsync(value);
    }
    public ValueTask<int> FolderTrashAsync
    {
        get => globalFolder.GetFolderTrashAsync(daoFactory);
    }

}
