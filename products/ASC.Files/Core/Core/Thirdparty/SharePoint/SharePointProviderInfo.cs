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

using File = Microsoft.SharePoint.Client.File;
using Folder = Microsoft.SharePoint.Client.Folder;

namespace ASC.Files.Thirdparty.SharePoint;

[Transient]
public class SharePointProviderInfo(ILogger<SharePointProviderInfo> logger,
        IServiceProvider serviceProvider,
        TenantUtil tenantUtil,
        SharePointProviderInfoHelper sharePointProviderInfoHelper,
        TempStream tempStream)
    : IProviderInfo<File, Folder, ClientObject>
{
    private ClientContext _clientContext;

    public int ID { get; set; }
    public string ProviderKey { get; set; }
    public Guid Owner { get; set; }
    public FolderType RootFolderType { get; set; }
    public FolderType FolderType { get; set; }
    public DateTime CreateOn { get; set; }
    public DateTime ModifiedOn { get; set; }
    public string CustomerTitle { get; set; }
    public string RootFolderId => $"{Selector.Id}-{ID}";
    public string SpRootFolderId { get; set; } = "/Shared Documents";
    public string FolderId { get; set; }
    public bool Private { get; set; }
    public bool HasLogo { get; set; }
    public string Color { get; set; }

    public Selector Selector { get; } = Selectors.SharePoint;
    public ProviderFilter ProviderFilter { get; } = ProviderFilter.SharePoint;
    public bool MutableEntityId => true;

    public Task<bool> CheckAccessAsync()
    {
        try
        {
            _clientContext.Load(_clientContext.Web);
            _clientContext.ExecuteQuery();

            return Task.FromResult(true);
        }
        catch (Exception e)
        {
            logger.WarningCheckAccess(e);

            return Task.FromResult(false);
        }
    }

    public async Task InvalidateStorageAsync()
    {
        _clientContext?.Dispose();

        await sharePointProviderInfoHelper.InvalidateAsync();
    }

    public void UpdateTitle(string newtitle)
    {
        CustomerTitle = newtitle;
    }

    public void InitClientContext(AuthData authData)
    {
        var authUrl = authData.Url;
        ICredentials credentials = new NetworkCredential(authData.Login, authData.Password);

        if (authData.Login.EndsWith("onmicrosoft.com"))
        {
            var personalPath = string.Concat("/personal/", authData.Login.Replace('@', '_').Replace('.', '_').ToLower());
            SpRootFolderId = string.Concat(personalPath, "/Documents");

            var ss = new SecureString();
            foreach (var p in authData.Password)
            {
                ss.AppendChar(p);
            }

            authUrl = string.Concat(authData.Url.TrimEnd('/'), personalPath);
            //TODO
            //credentials = new SharePointOnlineCredentials(authData.Login, ss);

        }

        _clientContext = new ClientContext(authUrl)
        {
            AuthenticationMode = ClientAuthenticationMode.Default,
            Credentials = credentials
        };
    }

    #region Files

    public async Task<File> GetFileByIdAsync(object id)
    {
        var key = $"{Selectors.SharePoint.Id}f-" + MakeId(id);
        var file = sharePointProviderInfoHelper.GetFile(key);
        if (file == null)
        {
            file = await GetFileAsync(id);
            if (file != null)
            {
                sharePointProviderInfoHelper.AddFile(key, file);
            }
        }

        return file;
    }

    private async Task<File> GetFileAsync(object id)
    {
        var file = _clientContext.Web.GetFileByServerRelativeUrl((string)id);
        _clientContext.Load(file);
        _clientContext.Load(file.ListItemAllFields);

        try
        {
            _clientContext.ExecuteQuery();
        }
        catch (Exception ex)
        {
            await sharePointProviderInfoHelper.PublishFolderAsync(MakeId(GetParentFolderId(id)));
            var serverException = (ServerException)ex;
            if (serverException.ServerErrorTypeName == typeof(FileNotFoundException).ToString())
            {
                return null;
            }

            return new SharePointFileErrorEntry(file.Context, file.Path) { Error = ex.Message, ID = id };
        }

        return file;
    }

    public async Task<Stream> GetFileStreamAsync(object id, int offset = 0)
    {
        var file = await GetFileByIdAsync(id);

        if (file is SharePointFileErrorEntry)
        {
            return null;
        }

        var fileInfo = File.OpenBinaryDirect(_clientContext, (string)id);
        _clientContext.ExecuteQuery();

        var tempBuffer = tempStream.Create();
        await using var str = fileInfo.Stream;
        if (str != null)
        {
            await str.CopyToAsync(tempBuffer);
            await tempBuffer.FlushAsync();
            tempBuffer.Seek(offset, SeekOrigin.Begin);
        }

        return tempBuffer;
    }

    public async Task<File> CreateFileAsync(string id, Stream stream)
    {
        byte[] b;

        using (var br = new BinaryReader(stream))
        {
            b = br.ReadBytes((int)stream.Length);
        }

        var file = _clientContext.Web.RootFolder.Files.Add(new FileCreationInformation { Content = b, Url = id, Overwrite = true });
        _clientContext.Load(file);
        _clientContext.Load(file.ListItemAllFields);
        _clientContext.ExecuteQuery();

        sharePointProviderInfoHelper.AddFile($"{Selectors.SharePoint.Id}f-" + MakeId(id), file);
        await sharePointProviderInfoHelper.PublishFolderAsync(MakeId(GetParentFolderId(id)));

        return file;
    }

    public async Task DeleteFileAsync(string id)
    {
        await sharePointProviderInfoHelper.PublishFileAsync(MakeId(id), MakeId(GetParentFolderId(id)));

        var file = await GetFileByIdAsync(id);

        if (file is SharePointFileErrorEntry)
        {
            return;
        }

        file.DeleteObject();
        _clientContext.ExecuteQuery();
    }

    public async Task<string> RenameFileAsync(string id, string newTitle)
    {
        await sharePointProviderInfoHelper.PublishFileAsync(MakeId(id), MakeId(GetParentFolderId(id)));

        var file = await GetFileByIdAsync(id);

        if (file is SharePointFileErrorEntry)
        {
            return MakeId();
        }

        var newUrl = GetParentFolderId(file.ServerRelativeUrl) + "/" + newTitle;
        file.MoveTo(newUrl, MoveOperations.Overwrite);
        _clientContext.ExecuteQuery();

        return MakeId(newUrl);
    }

    public async Task<string> MoveFileAsync(string id, string toFolderId)
    {
        await sharePointProviderInfoHelper.PublishFileAsync(MakeId(id), MakeId(GetParentFolderId(id)));
        await sharePointProviderInfoHelper.PublishFolderAsync(MakeId(toFolderId));

        var file = await GetFileByIdAsync(id);

        if (file is SharePointFileErrorEntry)
        {
            return MakeId();
        }

        var newUrl = toFolderId + "/" + file.Name;
        file.MoveTo(newUrl, MoveOperations.Overwrite);
        _clientContext.ExecuteQuery();

        return MakeId(newUrl);
    }

    public async Task<File> CopyFileAsync(string id, string toFolderId)
    {
        await sharePointProviderInfoHelper.PublishFolderAsync(MakeId(toFolderId), MakeId(GetParentFolderId(id)));

        var file = await GetFileByIdAsync(id);

        if (file is SharePointFileErrorEntry)
        {
            return file;
        }

        var newUrl = toFolderId + "/" + file.Name;
        file.CopyTo(newUrl, false);
        _clientContext.ExecuteQuery();

        return file;
    }

    public File<string> ToFile(File file)
    {
        if (file == null)
        {
            return null;
        }

        var result = serviceProvider.GetService<File<string>>();

        if (file is SharePointFileErrorEntry errorFile)
        {
            result.Id = MakeId(errorFile.ID);
            result.ParentId = MakeId(GetParentFolderId(errorFile.ID));
            result.CreateBy = Owner;
            result.CreateOn = DateTime.UtcNow;
            result.ModifiedBy = Owner;
            result.ModifiedOn = DateTime.UtcNow;
            result.ProviderId = ID;
            result.ProviderKey = ProviderKey;
            result.RootCreateBy = Owner;
            result.RootId = MakeId(RootFolder.ServerRelativeUrl);
            result.RootFolderType = RootFolderType;
            result.Title = MakeTitle(GetTitleById(errorFile.ID));
            result.Error = errorFile.Error;
            result.Encrypted = Private;

            return result;
        }

        result.Id = MakeId(file.ServerRelativeUrl);
        result.Access = FileShare.None;
        //ContentLength = file.Length,
        result.CreateBy = Owner;
        result.CreateOn = file.TimeCreated.Kind == DateTimeKind.Utc ? tenantUtil.DateTimeFromUtc(file.TimeCreated) : file.TimeCreated;
        result.ParentId = MakeId(GetParentFolderId(file.ServerRelativeUrl));
        result.ModifiedBy = Owner;
        result.ModifiedOn = file.TimeLastModified.Kind == DateTimeKind.Utc ? tenantUtil.DateTimeFromUtc(file.TimeLastModified) : file.TimeLastModified;
        result.ProviderId = ID;
        result.ProviderKey = ProviderKey;
        result.Title = MakeTitle(file.Name);
        result.RootId = MakeId(SpRootFolderId);
        result.RootFolderType = RootFolderType;
        result.RootCreateBy = Owner;
        result.Shared = false;
        result.Version = 1;
        result.Encrypted = Private;

        if (file.IsPropertyAvailable("Length"))
        {
            //TODO
            //result.ContentLength = file.Length;
        }
        else if (file.IsObjectPropertyInstantiated("ListItemAllFields"))
        {
            result.ContentLength = Convert.ToInt64(file.ListItemAllFields["File_x0020_Size"]);
        }

        return result;
    }

    #endregion

    #region Folders

    public Folder RootFolder
    {
        get
        {
            var key = $"{Selectors.SharePoint.Id}d-" + MakeId();
            var folder = sharePointProviderInfoHelper.GetFolder(key);
            if (folder == null)
            {
                folder = GetFolderByIdAsync(SpRootFolderId).Result;
                sharePointProviderInfoHelper.AddFolder(key, folder);
            }

            return folder;
        }
    }

    public int ProviderId { get; set; }

    public Task<IThirdPartyStorage<File, Folder, ClientObject>> StorageAsync => throw new NotImplementedException();

    public async Task<Folder> GetFolderByIdAsync(object id)
    {
        var key = $"{Selectors.SharePoint.Id}d-" + MakeId(id);
        var folder = sharePointProviderInfoHelper.GetFolder(key);
        if (folder == null)
        {
            folder = await GetFolderAsync(id);
            if (folder != null)
            {
                sharePointProviderInfoHelper.AddFolder(key, folder);
            }
        }

        return folder;
    }

    private async Task<Folder> GetFolderAsync(object id)
    {
        if (((string)id).Length == 0)
        {
            id = SpRootFolderId;
        }

        var folder = _clientContext.Web.GetFolderByServerRelativeUrl((string)id);
        _clientContext.Load(folder);
        _clientContext.Load(folder.Files, collection => collection.IncludeWithDefaultProperties(r => r.ListItemAllFields));
        _clientContext.Load(folder.Folders);

        try
        {
            _clientContext.ExecuteQuery();
        }
        catch (Exception ex)
        {
            await sharePointProviderInfoHelper.PublishFolderAsync(MakeId(GetParentFolderId(id)));
            var serverException = (ServerException)ex;
            if (serverException.ServerErrorTypeName == typeof(FileNotFoundException).ToString())
            {
                return null;
            }

            return new SharePointFolderErrorEntry(folder.Context, folder.Path) { Error = ex.Message, ID = id };
        }

        return folder;
    }

    public async Task<Folder> GetParentFolderAsync(string serverRelativeUrl)
    {
        return await GetFolderByIdAsync(GetParentFolderId(serverRelativeUrl));
    }

    public async Task<IEnumerable<File>> GetFolderFilesAsync(object id)
    {
        var folder = await GetFolderByIdAsync(id);
        if (folder is SharePointFolderErrorEntry)
        {
            return new List<File>();
        }

        return folder.Files;
    }

    public async Task<IEnumerable<Folder>> GetFolderFoldersAsync(object id)
    {
        var folder = await GetFolderByIdAsync(id);
        if (folder is SharePointFolderErrorEntry)
        {
            return new List<Folder>();
        }

        return folder.Folders.ToList().Where(r => r.ServerRelativeUrl != SpRootFolderId + "/" + "Forms");
    }

    public async Task<object> RenameFolderAsync(object id, string newTitle)
    {
        await sharePointProviderInfoHelper.PublishFolderAsync(MakeId(id), MakeId(GetParentFolderId(id)));

        var folder = await GetFolderByIdAsync(id);
        if (folder is SharePointFolderErrorEntry)
        {
            return MakeId(id);
        }

        var moveFld = await MoveFldAsync(folder, GetParentFolderId(id) + "/" + newTitle);

        return MakeId(moveFld.ServerRelativeUrl);
    }

    public async Task<string> MoveFolderAsync(string id, string toFolderId)
    {
        await sharePointProviderInfoHelper.PublishFolderAsync(MakeId(id), MakeId(GetParentFolderId(id)), MakeId(toFolderId));

        var folder = await GetFolderByIdAsync(id);
        if (folder is SharePointFolderErrorEntry)
        {
            return MakeId(id);
        }

        var nameById = await GetFolderByIdAsync(id);
        var folderName = await MoveFldAsync(folder, toFolderId + "/" + nameById.Name);

        return MakeId(folderName.ServerRelativeUrl);
    }

    public async Task<Folder> CopyFolderAsync(object id, object toFolderId)
    {
        await sharePointProviderInfoHelper.PublishFolderAsync(MakeId(toFolderId));

        var folder = await GetFolderByIdAsync(id);
        if (folder is SharePointFolderErrorEntry)
        {
            return folder;
        }

        var folderById = await GetFolderByIdAsync(id);

        return await MoveFldAsync(folder, toFolderId + "/" + folderById.Name, false);
    }

    private async Task<Folder> MoveFldAsync(Folder folder, string newUrl, bool delete = true)
    {
        var newFolder = await CreateFolderAsync(newUrl);

        if (delete)
        {
            foreach (var f in folder.Folders)
            {
                await MoveFolderAsync(f.ServerRelativeUrl, newUrl);
            }

            foreach (var f in folder.Files)
            {
                await MoveFileAsync(f.ServerRelativeUrl, newUrl);
            }

            folder.DeleteObject();
            _clientContext.ExecuteQuery();
        }
        else
        {
            foreach (var f in folder.Folders)
            {
                await CopyFolderAsync(f.ServerRelativeUrl, newUrl);
            }

            foreach (var f in folder.Files)
            {
                await CopyFileAsync(f.ServerRelativeUrl, newUrl);
            }
        }

        return newFolder;
    }

    public async Task<Folder> CreateFolderAsync(string id)
    {
        var folder = _clientContext.Web.RootFolder.Folders.Add(id);
        _clientContext.Load(folder);
        _clientContext.ExecuteQuery();

        await sharePointProviderInfoHelper.CreateFolderAsync(id, MakeId(GetParentFolderId(id)), folder);

        return folder;
    }

    public async Task DeleteFolderAsync(string id)
    {
        await sharePointProviderInfoHelper.PublishFolderAsync(MakeId(id), MakeId(GetParentFolderId(id)));

        var folder = await GetFolderByIdAsync(id);

        if (folder is SharePointFolderErrorEntry)
        {
            return;
        }

        folder.DeleteObject();
        _clientContext.ExecuteQuery();
    }

    public Folder<string> ToFolder(Folder folder)
    {
        if (folder == null)
        {
            return null;
        }

        var result = serviceProvider.GetService<Folder<string>>();

        if (folder is SharePointFolderErrorEntry errorFolder)
        {
            result.Id = MakeId(errorFolder.ID);
            result.ParentId = null;
            result.CreateBy = Owner;
            result.CreateOn = DateTime.UtcNow;
            result.FolderType = FolderType.DEFAULT;
            result.ModifiedBy = Owner;
            result.ModifiedOn = DateTime.UtcNow;
            result.ProviderId = ID;
            result.ProviderKey = ProviderKey;
            result.RootCreateBy = Owner;
            result.RootId = MakeId(SpRootFolderId);
            result.RootFolderType = RootFolderType;
            result.Shareable = false;
            result.Title = MakeTitle(GetTitleById(errorFolder.ID));
            result.FilesCount = 0;
            result.FoldersCount = 0;
            result.Error = errorFolder.Error;
            result.SettingsPrivate = Private;
            result.SettingsHasLogo = HasLogo;
            result.SettingsColor = Color;

            return result;
        }

        var isRoot = folder.ServerRelativeUrl == SpRootFolderId;

        result.Id = MakeId(isRoot ? "" : folder.ServerRelativeUrl);
        result.ParentId = isRoot ? null : MakeId(GetParentFolderId(folder.ServerRelativeUrl));
        result.CreateBy = Owner;
        result.CreateOn = CreateOn;
        result.FolderType = FolderType.DEFAULT;
        result.ModifiedBy = Owner;
        result.ModifiedOn = ModifiedOn;
        result.ProviderId = ID;
        result.ProviderKey = ProviderKey;
        result.RootCreateBy = Owner;
        result.RootId = MakeId(RootFolder.ServerRelativeUrl);
        result.RootFolderType = RootFolderType;
        result.Shareable = false;
        result.Title = isRoot ? CustomerTitle : MakeTitle(folder.Name);
        result.FilesCount = 0;
        result.FoldersCount = 0;
        result.SettingsPrivate = Private;
        result.SettingsHasLogo = HasLogo;
        
        SetFolderType(result, isRoot);

        return result;
    }

    #endregion

    public string MakeId(string path = "")
    {
        path = path.Replace(SpRootFolderId, "");
        var p = string.IsNullOrEmpty(path) || path == "/" || path == SpRootFolderId ? "" : ("-" + path.Replace('/', '|'));

        return $"{ID}{p}";
    }

    private string MakeId(object path)
    {
        return MakeId((string)path);
    }

    protected string MakeTitle(string name)
    {
        return Global.ReplaceInvalidCharsAndTruncate(name);
    }

    protected string GetParentFolderId(string serverRelativeUrl)
    {
        var path = serverRelativeUrl.Split('/');

        return string.Join("/", path.Take(path.Length - 1));
    }

    protected string GetParentFolderId(object serverRelativeUrl)
    {
        return GetParentFolderId((string)serverRelativeUrl);
    }

    protected string GetTitleById(object serverRelativeUrl)
    {
        return ((string)serverRelativeUrl).Split('/').Last();
    }

    public void Dispose()
    {
        _clientContext?.Dispose();
    }

    private void SetFolderType(Folder<string> folder, bool isRoot)
    {
        if (isRoot && RootFolderType is FolderType.VirtualRooms or FolderType.Archive)
        {
            folder.FolderType = RootFolderType;
        }
        else if (FolderId == folder.Id)
        {
            folder.FolderType = FolderType;
        }
    }

    public Task CacheResetAsync(string id = null, bool? isFile = null)
    {
        throw new NotImplementedException();
    }
}

[Singleton]
public class SharePointProviderInfoHelper
{
    private readonly TimeSpan _cacheExpiration;
    private readonly ICache _cache;
    private readonly ICacheNotify<SharePointProviderCacheItem> _notify;
    private readonly ConcurrentDictionary<string, object> _cacheKeys;

    public SharePointProviderInfoHelper(ICacheNotify<SharePointProviderCacheItem> notify, ICache cache)
    {
        _cacheExpiration = TimeSpan.FromMinutes(1);
        _cache = cache;
        _cacheKeys = new ConcurrentDictionary<string, object>();
        _notify = notify;

        _notify.Subscribe(i =>
        {
            if (!string.IsNullOrEmpty(i.FileKey))
            {
                _cache.Remove($"{Selectors.SharePoint.Id}f-" + i.FileKey);
            }
            if (!string.IsNullOrEmpty(i.FolderKey))
            {
                _cache.Remove($"{Selectors.SharePoint.Id}d-" + i.FolderKey);
            }
            if (string.IsNullOrEmpty(i.FileKey) && string.IsNullOrEmpty(i.FolderKey))
            {
                _cache.Remove(_cacheKeys, new Regex($"^{Selectors.SharePoint.Id}f-.*"));
                _cache.Remove(_cacheKeys, new Regex($"^{Selectors.SharePoint.Id}d-.*"));
            }
        }, CacheNotifyAction.Remove);
    }

    public async Task InvalidateAsync()
    {
        await _notify.PublishAsync(new SharePointProviderCacheItem(), CacheNotifyAction.Remove);
    }

    public async Task PublishFolderAsync(string id)
    {
        await _notify.PublishAsync(new SharePointProviderCacheItem { FolderKey = id }, CacheNotifyAction.Remove);
    }

    public async Task PublishFolderAsync(string id1, string id2)
    {
        await PublishFolderAsync(id1);
        await PublishFolderAsync(id2);
    }

    public async Task PublishFolderAsync(string id1, string id2, string id3)
    {
        await PublishFolderAsync(id1, id2);
        await PublishFolderAsync(id3);
    }

    public async Task PublishFileAsync(string fileId, string folderId)
    {
        await _notify.PublishAsync(new SharePointProviderCacheItem { FileKey = fileId, FolderKey = folderId }, CacheNotifyAction.Remove);
    }

    public async Task CreateFolderAsync(string id, string parentFolderId, Folder folder)
    {
        await PublishFolderAsync(parentFolderId);
        var key = $"{Selectors.SharePoint.Id}d-" + id;
        _cache.Insert(key, folder, DateTime.UtcNow.Add(_cacheExpiration), EvictionCallback);
        _cacheKeys.TryAdd(key, null);
    }

    public Folder GetFolder(string key)
    {
        return _cache.Get<Folder>(key);
    }

    public void AddFolder(string key, Folder folder)
    {
        _cache.Insert(key, folder, DateTime.UtcNow.Add(_cacheExpiration), EvictionCallback);
        _cacheKeys.TryAdd(key, null);
    }

    public File GetFile(string key)
    {
        return _cache.Get<File>(key);
    }

    public void AddFile(string key, File file)
    {
        _cache.Insert(key, file, DateTime.UtcNow.Add(_cacheExpiration), EvictionCallback);
        _cacheKeys.TryAdd(key, null);
    }

    private void EvictionCallback(object key, object value, EvictionReason reason, object state)
    {
        _cacheKeys.TryRemove(key.ToString(), out _);
    }
}
