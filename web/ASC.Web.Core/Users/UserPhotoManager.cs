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

namespace ASC.Web.Core.Users;

[Transient]
public sealed class ResizeWorkerItem : DistributedTask
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly UserPhotoManagerCache _userPhotoManagerCache;

    public ResizeWorkerItem()
    {
        
    }
    
    public ResizeWorkerItem(IServiceScopeFactory serviceScopeFactory, UserPhotoManagerCache userPhotoManagerCache)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _userPhotoManagerCache = userPhotoManagerCache;
    }

    public IMagickGeometry Size { get; set; }

    public IDataStore DataStore { get; set; }

    public long MaxFileSize { get;set;  }

    public byte[] Data { get; set; }
    public int TenantId { get; set; }
    public Guid UserId { get; set; }
    public UserPhotoThumbnailSettings Settings { get; set; }

    public string Key { get; set; }
    
    public void Init(int tenantId, Guid userId, byte[] data, long maxFileSize, IMagickGeometry size, IDataStore dataStore, UserPhotoThumbnailSettings settings)
    {
        TenantId = tenantId;
        UserId = userId;
        Data = data;
        MaxFileSize = maxFileSize;
        Size = size;
        DataStore = dataStore;
        Settings = settings;
    }
    
    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not ResizeWorkerItem item)
        {
            return false;
        }

        return Equals(item);
    }

    public bool Equals(ResizeWorkerItem other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other.UserId.Equals(UserId) && other.MaxFileSize == MaxFileSize && other.Size.Equals(Size);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(UserId, MaxFileSize, Size);
    }
    
    protected override async Task DoJob()
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(TenantId);

            var data = Data;
            using var stream = new MemoryStream(data);
            using var img = new MagickImage(stream);
            var imgFormat = img.Format;

            if (Size.CompareTo(new MagickGeometry(img.Width, img.Height)) != 0)
            {
                using var img2 = Settings.IsDefault ?
                    CommonPhotoManager.DoThumbnail(img, Size) :
                    UserPhotoThumbnailManager.GetImage(img, Size, Settings);
                data = await CommonPhotoManager.SaveToBytes(img2);
            }
            else
            {
                data = await CommonPhotoManager.SaveToBytes(img);
            }

            var widening = CommonPhotoManager.GetImgFormatName(imgFormat);
            var fileName = $"{UserId}_size_{Size.Width}-{Size.Height}.{widening}";

            using var stream2 = new MemoryStream(data);
            await DataStore.SaveAsync(fileName, stream2);

            await _userPhotoManagerCache.AddToCache(UserId, Size, fileName, TenantId);
        }
        catch (ArgumentException error)
        {
            throw new UnknownImageFormatException(error);
        }
    }
}

[Singleton]
public class UserPhotoManagerCache
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<CacheSize, string>> _photoFiles;
    private readonly ICacheNotify<UserPhotoManagerCacheItem> _cacheNotify;
    private readonly HashSet<int> _tenantDiskCache;

    public UserPhotoManagerCache(ICacheNotify<UserPhotoManagerCacheItem> notify)
    {
        try
        {
            _photoFiles = new ConcurrentDictionary<Guid, ConcurrentDictionary<CacheSize, string>>();
            _tenantDiskCache = [];
            _cacheNotify = notify;

            _cacheNotify.Subscribe(data =>
            {
                _photoFiles.GetOrAdd(new Guid(data.UserId), _ => new ConcurrentDictionary<CacheSize, string>())
                              .AddOrUpdate(data.Size, data.FileName, (_, _) => data.FileName);
            }, CacheNotifyAction.InsertOrUpdate);

            _cacheNotify.Subscribe(data =>
            {
                _ = _photoFiles.TryRemove(new Guid(data.UserId), out _);

            }, CacheNotifyAction.Remove);
        }
        catch (Exception)
        {

        }
    }

    public bool IsCacheLoadedForTenant(int tenantId)
    {
        return _tenantDiskCache.Contains(tenantId);
    }

    public bool SetCacheLoadedForTenant(bool isLoaded, int tenantId)
    {
        return isLoaded ? _tenantDiskCache.Add(tenantId) : _tenantDiskCache.Remove(tenantId);
    }

    public async Task ClearCacheAsync(Guid userID, int tenantId)
    {
        if (_cacheNotify != null)
        {
            await _cacheNotify.PublishAsync(new UserPhotoManagerCacheItem { UserId = userID.ToString(), TenantId = tenantId }, CacheNotifyAction.Remove);
        }
    }

    public async Task AddToCache(Guid userID, IMagickGeometry size, string fileName, int tenantId)
    {
        if (_cacheNotify != null)
        {
            await _cacheNotify.PublishAsync(new UserPhotoManagerCacheItem { UserId = userID.ToString(), Size = UserPhotoManager.ToCache(size), FileName = fileName, TenantId = tenantId }, CacheNotifyAction.InsertOrUpdate);
        }
    }

    public string SearchInCache(Guid userId, IMagickGeometry size)
    {
        if (!_photoFiles.TryGetValue(userId, out var val))
        {
            return null;
        }

        if (!val.TryGetValue(UserPhotoManager.ToCache(size), out var fileName))
        {
            return null;
        }
        
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = val.Values.FirstOrDefault(x => !string.IsNullOrEmpty(x) && x.Contains("_orig_"));
        }

        return fileName;
    }
}

[Scope]
public class UserPhotoManager
{
    //Regex for parsing filenames into groups with id's
    private static readonly Regex _parseFile =
            new(@"^(?'module'\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1}){0,1}" +
                @"(?'user'\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1}){1}" +
                @"_(?'kind'orig|size){1}_(?'size'(?'width'[0-9]{1,5})-{1}(?'height'[0-9]{1,5})){0,1}\..*", RegexOptions.Compiled);
    
    private string _defaultAbsoluteWebPath;
    public string GetDefaultPhotoAbsoluteWebPath()
    {
        return _defaultAbsoluteWebPath ??= _webImageSupplier.GetAbsoluteWebPath(_defaultAvatar);
    }

    public async Task<string> GetRetinaPhotoURL(Guid userID)
    {
        return await GetSizedPhotoAbsoluteWebPath(userID, RetinaFotoSize);
    }

    public async Task<string> GetMaxPhotoURL(Guid userID)
    {
        return await GetSizedPhotoAbsoluteWebPath(userID, MaxFotoSize);
    }

    public async Task<string> GetBigPhotoURL(Guid userID)
    {
        return await GetSizedPhotoAbsoluteWebPath(userID, BigFotoSize);
    }

    public async Task<string> GetMediumPhotoURL(Guid userID)
    {
        return await GetSizedPhotoAbsoluteWebPath(userID, MediumFotoSize);
    }

    public async Task<string> GetSmallPhotoURL(Guid userID)
    {
        return await GetSizedPhotoAbsoluteWebPath(userID, SmallFotoSize);
    }
    
    public static IMagickGeometry OriginalFotoSize { get; } = new MagickGeometry(1280, 1280);

    public static IMagickGeometry RetinaFotoSize { get; } = new MagickGeometry(360, 360);

    public static IMagickGeometry MaxFotoSize { get; } = new MagickGeometry(200, 200);

    public static IMagickGeometry BigFotoSize { get; } = new MagickGeometry(82, 82);

    public static IMagickGeometry MediumFotoSize { get; } = new MagickGeometry(48, 48);

    public static IMagickGeometry SmallFotoSize { get; } = new MagickGeometry(32, 32);

    private static readonly string _defaultRetinaAvatar = "default_user_photo_size_360-360.png";
    private static readonly string _defaultAvatar = "default_user_photo_size_200-200.png";
    private static readonly string _defaultSmallAvatar = "default_user_photo_size_32-32.png";
    private static readonly string _defaultMediumAvatar = "default_user_photo_size_48-48.png";
    private static readonly string _defaultBigAvatar = "default_user_photo_size_82-82.png";
    private static readonly string _tempDomainName = "temp";


    public async Task<bool> UserHasAvatar(Guid userID)
    {
        var path = await GetPhotoAbsoluteWebPath(userID);
        var fileName = Path.GetFileName(path);
        return fileName != _defaultAvatar;
    }

    public async Task<string> GetPhotoAbsoluteWebPath(Guid userID)
    {
        var path = await SearchInCache(userID, SizeExtend.Empty);
        if (!string.IsNullOrEmpty(path))
        {
            return path;
        }

        try
        {
            var data = await _userManager.GetUserPhotoAsync(userID);
            string photoUrl;
            string fileName;
            if (data == null || data.Length == 0)
            {
                photoUrl = GetDefaultPhotoAbsoluteWebPath();
                fileName = "default";
            }
            else
            {
                (photoUrl, fileName) = await SaveOrUpdatePhotoAsync(userID, data, -1, null, false);
            }

            await _userPhotoManagerCache.AddToCache(userID, SizeExtend.Empty, fileName, (_tenantManager.GetCurrentTenant()).Id);

            return photoUrl;
        }
        catch
        {
        }
        return GetDefaultPhotoAbsoluteWebPath();
    }

    private async Task<string> GetSizedPhotoAbsoluteWebPath(Guid userID, IMagickGeometry size)
    {
        var res = await SearchInCache(userID, size);
        if (!string.IsNullOrEmpty(res))
        {
            return res;
        }

        try
        {
            var data = await _userManager.GetUserPhotoAsync(userID);

            if (data == null || data.Length == 0)
            {
                //empty photo. cache default
                var photoUrl = GetDefaultPhotoAbsoluteWebPath(size);

                await _userPhotoManagerCache.AddToCache(userID, size, "default", (_tenantManager.GetCurrentTenant()).Id);

                return photoUrl;
            }

            //Enqueue for sizing
            await SizePhoto(userID, data, -1, size);
        }
        catch { }

        return GetDefaultPhotoAbsoluteWebPath(size);
    }

    private string GetDefaultPhotoAbsoluteWebPath(IMagickGeometry size)
    {
        return (size) switch
        {
            _ when size.Width == RetinaFotoSize.Width && size.Height == RetinaFotoSize.Height => _webImageSupplier.GetAbsoluteWebPath(_defaultRetinaAvatar),
            _ when size.Width == MaxFotoSize.Width && size.Height == MaxFotoSize.Height => _webImageSupplier.GetAbsoluteWebPath(_defaultAvatar),
            _ when size.Width == BigFotoSize.Width && size.Height == BigFotoSize.Height => _webImageSupplier.GetAbsoluteWebPath(_defaultBigAvatar),
            _ when size.Width == SmallFotoSize.Width && size.Height == SmallFotoSize.Height => _webImageSupplier.GetAbsoluteWebPath(_defaultSmallAvatar),
            _ when size.Width == MediumFotoSize.Width && size.Height == MediumFotoSize.Height => _webImageSupplier.GetAbsoluteWebPath(_defaultMediumAvatar),
            _ => GetDefaultPhotoAbsoluteWebPath()
        };
    }

    private static readonly SemaphoreSlim _semaphore = new(1);


    private async Task<string> SearchInCache(Guid userId, IMagickGeometry size)
    {
        if (!_userPhotoManagerCache.IsCacheLoadedForTenant((_tenantManager.GetCurrentTenant()).Id))
        {
            await LoadDiskCache();
        }

        var fileName = _userPhotoManagerCache.SearchInCache(userId, size);

        if (fileName != null && fileName.StartsWith("default"))
        {
            return GetDefaultPhotoAbsoluteWebPath(size);
        }

        if (!string.IsNullOrEmpty(fileName))
        {
            var store = await GetDataStoreAsync();
            var uri = await store.GetUriAsync(fileName);

            return uri.ToString();
        }

        return null;
    }

    private async Task LoadDiskCache()
    {
        await _semaphore.WaitAsync();
        var tenantId = _tenantManager.GetCurrentTenantId();
        if (!_userPhotoManagerCache.IsCacheLoadedForTenant(tenantId))
        {
            try
            {
                var listFileNames = await (await GetDataStoreAsync()).ListFilesRelativeAsync("", "", "*.*", false).ToArrayAsync();
                foreach (var fileName in listFileNames)
                {
                    //Try parse fileName
                    if (fileName != null)
                    {
                        var match = _parseFile.Match(fileName);
                        if (match.Success && match.Groups["user"].Success)
                        {
                            var parsedUserId = new Guid(match.Groups["user"].Value);
                            var size = SizeExtend.Empty;
                            if (match.Groups["width"].Success && match.Groups["height"].Success)
                            {
                                //Parse size
                                size = new MagickGeometry(uint.Parse(match.Groups["width"].Value), uint.Parse(match.Groups["height"].Value));
                            }
                            await _userPhotoManagerCache.AddToCache(parsedUserId, size, fileName,tenantId);
                        }
                    }
                }
                _userPhotoManagerCache.SetCacheLoadedForTenant(true,tenantId);
            }
            catch (Exception err)
            {
                _logger.ErrorLoadDiskCache(err);
            }
        }
        _semaphore.Release();
    }
    public async Task ResetThumbnailSettingsAsync(Guid userId)
    {
        var thumbSettings = _settingsManager.GetDefault<UserPhotoThumbnailSettings>();
        await _settingsManager.SaveAsync(thumbSettings, userId);
    }

    public async Task<(string, string)> SaveOrUpdatePhoto(Guid userID, byte[] data)
    {
        return await SaveOrUpdatePhotoAsync(userID, data, -1, OriginalFotoSize, true);
    }

    public async Task RemovePhotoAsync(Guid idUser)
    {
        await _userManager.SaveUserPhotoAsync(idUser, null);
        try
        {
            var storage = await GetDataStoreAsync();
            if (await storage.IsDirectoryAsync(""))
            {
                await storage.DeleteFilesAsync("", idUser + "*.*", false);
            }
        }
        catch (AggregateException e)
        {
            if (e.InnerException is DirectoryNotFoundException exc)
            {
                _logger.ErrorRemovePhoto(exc);
            }
            else
            {
                throw;
            }
        }
        catch (DirectoryNotFoundException e)
        {
            _logger.ErrorRemovePhoto(e);
        }

        await _userManager.SaveUserPhotoAsync(idUser, null);
        await _userPhotoManagerCache.ClearCacheAsync(idUser, _tenantManager.GetCurrentTenantId());
    }

    public async Task SyncPhotoAsync(Guid userID, byte[] data)
    {
        (data, _, var width, var height) = await TryParseImage(data, -1, OriginalFotoSize);
        await _userManager.SaveUserPhotoAsync(userID, data);
        await SetUserPhotoThumbnailSettingsAsync(userID, width, height);
        //   _userPhotoManagerCache.ClearCache(userID, _tenantManager.GetCurrentTenant().Id);
    }


    private async Task<(string, string)> SaveOrUpdatePhotoAsync(Guid userID, byte[] data, long maxFileSize, IMagickGeometry size, bool saveInCoreContext)
    {
        (data, var imgFormat, var width, var height) = await TryParseImage(data, maxFileSize, size);

        var widening = CommonPhotoManager.GetImgFormatName(imgFormat);
        var fileName = $"{userID}_orig_{width}-{height}.{widening}";

        if (saveInCoreContext)
        {
            await _userManager.SaveUserPhotoAsync(userID, data);
            await SetUserPhotoThumbnailSettingsAsync(userID, width, height);
            // _userPhotoManagerCache.ClearCache(userID, _tenantManager.GetCurrentTenant().Id);
        }

        var store = await GetDataStoreAsync();

        var photoUrl = GetDefaultPhotoAbsoluteWebPath();
        if (data is { Length: > 0 })
        {
            using (var stream = new MemoryStream(data))
            {
                photoUrl = (await store.SaveAsync(fileName, stream)).ToString();
            }
            //Queue resizing
            var t1 = SizePhoto(userID, data, -1, SmallFotoSize, true);
            var t2 = SizePhoto(userID, data, -1, MediumFotoSize, true);
            var t3 = SizePhoto(userID, data, -1, BigFotoSize, true);
            var t4 = SizePhoto(userID, data, -1, MaxFotoSize, true);
            var t5 = SizePhoto(userID, data, -1, RetinaFotoSize, true);

            await Task.WhenAll(t1, t2, t3, t4, t5);
        }
        
        await _userPhotoManagerCache.AddToCache(userID, SizeExtend.Empty, fileName, _tenantManager.GetCurrentTenantId());
        
        return (photoUrl, fileName);
    }

    private async Task SetUserPhotoThumbnailSettingsAsync(Guid userId, uint width, uint height)
    {
        var max = Math.Max(Math.Max(width, height), SmallFotoSize.Width);
        var min = Math.Max(Math.Min(width, height), SmallFotoSize.Width);

        var pos = (max - min) / 2;

        var settings = new UserPhotoThumbnailSettings(
            width >= height ? new Point((int)pos, 0) : new Point(0, (int)pos),
            new MagickGeometry(min, min));

        await _settingsManager.SaveAsync(settings, userId);
    }

    private async Task<(byte[], MagickFormat, uint, uint)> TryParseImage(byte[] data, long maxFileSize, IMagickGeometry maxsize)
    {
        if (data is not { Length: > 0 })
        {
            throw new UnknownImageFormatException();
        }

        if (maxFileSize != -1 && data.Length > maxFileSize)
        {
            throw new ImageSizeLimitException();
        }

        //data = ImageHelper.RotateImageByExifOrientationData(data, Log);

        try
        {
            using var img = new MagickImage(data);

            var imgFormat = img.Format;
            var width = img.Width;
            var height = img.Height;
            if (maxsize != null)
            {
                var maxWidth = maxsize.Width;
                var maxHeight = maxsize.Height;

                if (img.Height > maxHeight || img.Width > maxWidth)
                {
                    #region calulate height and width

                    if (width > maxWidth && height > maxHeight)
                    {

                        if (width > height)
                        {
                            height = (uint)(height * (double)maxWidth / width + 0.5);
                            width = maxWidth;
                        }
                        else
                        {
                            width = (uint)(width * (double)maxHeight / height + 0.5);
                            height = maxHeight;
                        }
                    }

                    if (width > maxWidth && height <= maxHeight)
                    {
                        height = (uint)(height * (double)maxWidth / width + 0.5);
                        width = maxWidth;
                    }

                    if (width <= maxWidth && height > maxHeight)
                    {
                        width = (uint)(width * (double)maxHeight / height + 0.5);
                        height = maxHeight;
                    }

                    #endregion

                    var g = new MagickGeometry(width, height) { FillArea = true };
                    using var destRound = img.CloneAndMutate(x => x.Resize(g));

                    data = await CommonPhotoManager.SaveToBytes(destRound);
                }
            }

            return (data, imgFormat, width, height);
        }
        catch (OutOfMemoryException)
        {
            throw new ImageSizeLimitException();
        }
        catch (ArgumentException error)
        {
            throw new UnknownImageFormatException(error);
        }
    }

    private async Task SizePhoto(Guid userID, byte[] data, long maxFileSize, IMagickGeometry size)
    {
        await SizePhoto(userID, data, maxFileSize, size, false);
    }

    private async Task<string> SizePhoto(Guid userID, byte[] data, long maxFileSize, IMagickGeometry size, bool now)
    {
        if (data is not { Length: > 0 })
        {
            throw new UnknownImageFormatException();
        }

        if (maxFileSize != -1 && data.Length > maxFileSize)
        {
            throw new ImageWeightLimitException();
        }

        var resizeTask = _serviceProvider.GetRequiredService<ResizeWorkerItem>();
        resizeTask.Init(_tenantManager.GetCurrentTenantId(), userID, data, maxFileSize, size, await GetDataStoreAsync(), await _settingsManager.LoadAsync<UserPhotoThumbnailSettings>(userID));
        
        var key = $"{userID}{size}";
        resizeTask.Key = key;

        if (now)
        {
            //Resize synchronously
            await resizeTask.RunJob(CancellationToken.None);
            return await GetSizedPhotoAbsoluteWebPath(userID, size);
        }
        
        await _userPhotoResizeManager.EnqueueTaskAsync(key, resizeTask);
        return GetDefaultPhotoAbsoluteWebPath(size);
        //NOTE: return default photo here. Since task will update cache
    }

   

    public async Task<string> SaveTempPhoto(byte[] data, long maxFileSize, uint maxWidth, uint maxHeight)
    {
        (data, var imgFormat, _, _) = await TryParseImage(data, maxFileSize, new MagickGeometry(maxWidth, maxHeight));

        var fileName = Guid.NewGuid() + "." + CommonPhotoManager.GetImgFormatName(imgFormat);

        var store = await GetDataStoreAsync();
        using var stream = new MemoryStream(data);
        return (await store.SaveAsync(_tempDomainName, fileName, stream)).ToString();
    }

    public async Task<string> SaveTempPhoto(byte[] data, long maxFileSize, string ext)
    {
        if (maxFileSize != -1 && data.Length > maxFileSize)
        {
            throw new ImageSizeLimitException();
        }

        using var stream = new MemoryStream(data);
        var fileName = Guid.NewGuid() + $".{ext}";
        var store = await GetDataStoreAsync();
        return (await store.SaveAsync(_tempDomainName, fileName, stream)).ToString();
    }

    public async Task<byte[]> GetTempPhotoData(string fileName)
    {
        await using var s = await (await GetDataStoreAsync()).GetReadStreamAsync(_tempDomainName, fileName);
        var data = new MemoryStream();
        var buffer = new byte[1024 * 10];

        while (true)
        {
            var count = await s.ReadAsync(buffer);
            if (count == 0)
            {
                break;
            }

            await data.WriteAsync(buffer.AsMemory(0, count));
        }

        return data.ToArray();
    }

    public async Task RemoveTempPhotoAsync(string fileName)
    {
        var index = fileName.LastIndexOf('.');
        var fileNameWithoutExt = (index != -1) ? fileName[..index] : fileName;
        try
        {
            var store = await GetDataStoreAsync();
            await store.DeleteFilesAsync(_tempDomainName, "", fileNameWithoutExt + "*.*", false);
        }
        catch { }
    }


    public async Task<(MagickImage, MagickFormat)> GetPhotoImageAsync(Guid userID)
    {
        try
        {
            var data = await _userManager.GetUserPhotoAsync(userID);
            if (data != null)
            {
                var img = new MagickImage(data);

                var format = img.Format;

                return (img, format);
            }
        }
        catch { }
        return (null, default);
    }

    public async Task<string> SaveThumbnail(Guid userID, IMagickImage img, MagickFormat format)
    {
        var moduleID = Guid.Empty;
        var widening = CommonPhotoManager.GetImgFormatName(format);
        var size = new MagickGeometry(img.Width, img.Height);
        var fileName = string.Format("{0}{1}_size_{2}-{3}.{4}", moduleID == Guid.Empty ? "" : moduleID.ToString(), userID, img.Width, img.Height, widening);

        var store = await GetDataStoreAsync();
        string photoUrl;
        using (var s = new MemoryStream(await CommonPhotoManager.SaveToBytes(img)))
        {
            img.Dispose();
            photoUrl = (await store.SaveAsync(fileName, s)).ToString();
        }

        await _userPhotoManagerCache.AddToCache(userID, size, fileName, (_tenantManager.GetCurrentTenant()).Id);
        return photoUrl;
    }

    public async Task<byte[]> GetUserPhotoData(Guid userId, IMagickGeometry size)
    {
        try
        {
            var pattern = $"{userId}_size_{size.Width}-{size.Height}.*";

            var fileName = await (await GetDataStoreAsync()).ListFilesRelativeAsync("", "", pattern, false).FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            await using var s = await (await GetDataStoreAsync()).GetReadStreamAsync("", fileName);
            var data = new MemoryStream();
            var buffer = new byte[1024 * 10];
            while (true)
            {
                var count = await s.ReadAsync(buffer);
                if (count == 0)
                {
                    break;
                }

                await data.WriteAsync(buffer.AsMemory(0, count));
            }
            
            return data.ToArray();
        }
        catch (Exception err)
        {
            _logger.ErrorGetUserPhotoData(err);
            return null;
        }
    }

    private IDataStore _dataStore;
    private readonly UserManager _userManager;
    private readonly WebImageSupplier _webImageSupplier;
    private readonly TenantManager _tenantManager;
    private readonly StorageFactory _storageFactory;
    private readonly UserPhotoManagerCache _userPhotoManagerCache;
    private readonly ILogger<UserPhotoManager> _logger;
    private readonly UserPhotoResizeManager _userPhotoResizeManager;
    private readonly SettingsManager _settingsManager;
    private readonly IServiceProvider _serviceProvider;

    public UserPhotoManager(UserManager userManager,
        WebImageSupplier webImageSupplier,
        TenantManager tenantManager,
        StorageFactory storageFactory,
        UserPhotoManagerCache userPhotoManagerCache,
        ILogger<UserPhotoManager> logger,
        UserPhotoResizeManager userPhotoResizeManager,
        SettingsManager settingsManager,
        IServiceProvider serviceProvider)
    {
        _userManager = userManager;
        _webImageSupplier = webImageSupplier;
        _tenantManager = tenantManager;
        _storageFactory = storageFactory;
        _userPhotoManagerCache = userPhotoManagerCache;
        _logger = logger;
        _userPhotoResizeManager = userPhotoResizeManager;
        _settingsManager = settingsManager;
        _serviceProvider = serviceProvider;
    }

    private async ValueTask<IDataStore> GetDataStoreAsync()
    {
        return _dataStore ??= await _storageFactory.GetStorageAsync(_tenantManager.GetCurrentTenantId(), "userPhotos");
    }

    public static CacheSize ToCache(IMagickGeometry size)
    {
        return size switch
        {
            _ when size.Width == RetinaFotoSize.Width && size.Height == RetinaFotoSize.Height => CacheSize.Retina,
            _ when size.Width == MaxFotoSize.Width && size.Height == MaxFotoSize.Height => CacheSize.Max,
            _ when size.Width == BigFotoSize.Width && size.Height == BigFotoSize.Height => CacheSize.Big,
            _ when size.Width == SmallFotoSize.Width && size.Height == SmallFotoSize.Height => CacheSize.Small,
            _ when size.Width == MediumFotoSize.Width && size.Height == MediumFotoSize.Height => CacheSize.Medium,
            _ => CacheSize.Original
        };
    }
}

[Singleton]
public class UserPhotoResizeManager(IDistributedTaskQueueFactory queueFactory)
{
    //note: using auto stop queue
    private readonly DistributedTaskQueue<ResizeWorkerItem> _resizeQueue = queueFactory.CreateQueue<ResizeWorkerItem>();//TODO: configure

    public async Task EnqueueTaskAsync(string key, ResizeWorkerItem resizeTask)
    {
        if ((await _resizeQueue.GetAllTasks()).All(r => r.Key != key))
        {
            //Add
            await _resizeQueue.EnqueueTask(resizeTask);
        }
    }
}

#region Exception Classes

public class UnknownImageFormatException : Exception
{
    public UnknownImageFormatException() : base("unknown image file type") { }

    public UnknownImageFormatException(Exception inner) : base("unknown image file type", inner) { }
}

public class ImageWeightLimitException : Exception
{
    public ImageWeightLimitException() : base("image width is too large")
    {
    }
}

public class ImageSizeLimitException : Exception
{
    public ImageSizeLimitException() : base("image size is too large")
    {
    }
}

#endregion


/// <summary>
/// Helper class for manipulating images.
/// </summary>
/*public static class ImageHelper
{
    /// <summary>
    /// Rotate the given image byte array according to Exif Orientation data
    /// </summary>
    /// <param name="data">source image byte array</param>
    /// <param name="updateExifData">set it to TRUE to update image Exif data after rotation (default is TRUE)</param>
    /// <returns>The rotated image byte array. If no rotation occurred, source data will be returned.</returns>
    public static byte[] RotateImageByExifOrientationData(byte[] data, ILog Log, bool updateExifData = true)
    {
        try
        {
            using var stream = new MemoryStream(data);
            using var img = Image.Load(stream);

            var fType = RotateImageByExifOrientationData(img, updateExifData);
            if (fType != RotateFlipType.RotateNoneFlipNone)
            {
                using var tempStream = new MemoryStream();
                img.Save(tempStream, System.Drawing.Imaging.ImageFormat.Png);
                data = tempStream.ToArray();
            }
        }
        catch (Exception err)
        {
            Log.Error(err);
        }

        return data;
    }

    /// <summary>
    /// Rotate the given image file according to Exif Orientation data
    /// </summary>
    /// <param name="sourceFilePath">path of source file</param>
    /// <param name="targetFilePath">path of target file</param>
    /// <param name="targetFormat">target format</param>
    /// <param name="updateExifData">set it to TRUE to update image Exif data after rotation (default is TRUE)</param>
    /// <returns>The RotateFlipType value corresponding to the applied rotation. If no rotation occurred, RotateFlipType.RotateNoneFlipNone will be returned.</returns>
    public static RotateFlipType RotateImageByExifOrientationData(string sourceFilePath, string targetFilePath, ImageFormat targetFormat, bool updateExifData = true)
    {
        // Rotate the image according to EXIF data
        using var bmp = new Bitmap(sourceFilePath);
        var fType = RotateImageByExifOrientationData(bmp, updateExifData);
        if (fType != RotateFlipType.RotateNoneFlipNone)
        {
            bmp.Save(targetFilePath, targetFormat);
        }
        return fType;
    }

    /// <summary>
    /// Rotate the given bitmap according to Exif Orientation data
    /// </summary>
    /// <param name="img">source image</param>
    /// <param name="updateExifData">set it to TRUE to update image Exif data after rotation (default is TRUE)</param>
    /// <returns>The RotateFlipType value corresponding to the applied rotation. If no rotation occurred, RotateFlipType.RotateNoneFlipNone will be returned.</returns>
    public static RotateFlipType RotateImageByExifOrientationData(Image img, bool updateExifData = true)
    {
        const int orientationId = 0x0112;
        var fType = RotateFlipType.RotateNoneFlipNone;
        if (img.PropertyIdList.Contains(orientationId))
        {
            var pItem = img.GetPropertyItem(orientationId);
            fType = GetRotateFlipTypeByExifOrientationData(pItem.Value[0]);
            if (fType != RotateFlipType.RotateNoneFlipNone)
            {
                img.RotateFlip(fType);
                if (updateExifData) img.RemovePropertyItem(orientationId); // Remove Exif orientation tag
            }
        }
        return fType;
    }

    /// <summary>
    /// Return the proper System.Drawing.RotateFlipType according to given orientation EXIF metadata
    /// </summary>
    /// <param name="orientation">Exif "Orientation"</param>
    /// <returns>the corresponding System.Drawing.RotateFlipType enum value</returns>
    public static RotateFlipType GetRotateFlipTypeByExifOrientationData(int orientation)
    {
        return orientation switch
        {
            1 => RotateFlipType.RotateNoneFlipNone,
            2 => RotateFlipType.RotateNoneFlipX,
            3 => RotateFlipType.Rotate180FlipNone,
            4 => RotateFlipType.Rotate180FlipX,
            5 => RotateFlipType.Rotate90FlipX,
            6 => RotateFlipType.Rotate90FlipNone,
            7 => RotateFlipType.Rotate270FlipX,
            8 => RotateFlipType.Rotate270FlipNone,
            _ => RotateFlipType.RotateNoneFlipNone,
        };
    }
}*/

public static class SizeExtend
{
    public static void Deconstruct(this IMagickGeometry size, out uint w, out uint h)
    {
        (w, h) = (size.Width, size.Height);
    }

    public static IMagickGeometry Empty => new MagickGeometry(0, 0);

}
