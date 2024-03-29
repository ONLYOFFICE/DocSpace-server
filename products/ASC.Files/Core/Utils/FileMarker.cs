// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Web.Files.Utils;

[Singleton]
public class FileMarkerCache
{
    private readonly ICache _cache;
    private readonly ICacheNotify<FileMarkerCacheItem> _notify;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);

    public FileMarkerCache(ICacheNotify<FileMarkerCacheItem> notify, ICache cache)
    {
        _cache = cache;
        _notify = notify;

        _notify.Subscribe((i) => _cache.Remove(i.Key), CacheNotifyAction.Remove);
    }

    public T Get<T>(string key) where T : class
    {
        return _cache.Get<T>(key);
    }

    public void Insert(string key, object value)
    {
        _notify.Publish(new FileMarkerCacheItem { Key = key }, CacheNotifyAction.Remove);

        _cache.Insert(key, value, _cacheExpiration);
    }

    public void Remove(string key)
    {
        _notify.Publish(new FileMarkerCacheItem { Key = key }, CacheNotifyAction.Remove);

        _cache.Remove(key);
    }
}

[Singleton]
public class FileMarkerHelper
{
    private const string CustomDistributedTaskQueueName = "file_marker";
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly DistributedTaskQueue _tasks;

    public FileMarkerHelper(
        IServiceProvider serviceProvider,
        ILogger<FileMarkerHelper> logger,
        IDistributedTaskQueueFactory queueFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _tasks = queueFactory.CreateQueue(CustomDistributedTaskQueueName);
    }

    internal void Add<T>(AsyncTaskData<T> taskData)
    {
        _tasks.EnqueueTask(async (_, _) => await ExecMarkFileAsNewAsync(taskData), taskData);
    }

    private async Task ExecMarkFileAsNewAsync<T>(AsyncTaskData<T> obj)
    {
        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var fileMarker = scope.ServiceProvider.GetService<FileMarker>();
            var socketManager = scope.ServiceProvider.GetService<SocketManager>();
            await fileMarker.ExecMarkFileAsNewAsync(obj, socketManager);
        }
        catch (Exception e)
        {
            _logger.ErrorExecMarkFileAsNew(e);
        }
    }
}

[Scope]
public class FileMarker
{
    private const string CacheKeyFormat = "MarkedAsNew/{0}/folder_{1}";

    private readonly TenantManager _tenantManager;
    private readonly UserManager _userManager;
    private readonly IDaoFactory _daoFactory;
    private readonly GlobalFolder _globalFolder;
    private readonly FileSecurity _fileSecurity;
    private readonly AuthContext _authContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly FilesSettingsHelper _filesSettingsHelper;
    private static readonly SemaphoreSlim _semaphore = new(1);
    private readonly RoomsNotificationSettingsHelper _roomsNotificationSettingsHelper;
    private readonly FileMarkerCache _fileMarkerCache;
    private readonly FileMarkerHelper _fileMarkerHelper;

    public FileMarker(
        TenantManager tenantManager,
        UserManager userManager,
        IDaoFactory daoFactory,
        GlobalFolder globalFolder,
        FileSecurity fileSecurity,
        AuthContext authContext,
        IServiceProvider serviceProvider,
        FilesSettingsHelper filesSettingsHelper,
        RoomsNotificationSettingsHelper roomsNotificationSettingsHelper,
        FileMarkerCache fileMarkerCache,
        FileMarkerHelper fileMarkerHelper)
    {
        _tenantManager = tenantManager;
        _userManager = userManager;
        _daoFactory = daoFactory;
        _globalFolder = globalFolder;
        _fileSecurity = fileSecurity;
        _authContext = authContext;
        _serviceProvider = serviceProvider;
        _filesSettingsHelper = filesSettingsHelper;
        _roomsNotificationSettingsHelper = roomsNotificationSettingsHelper;
        _fileMarkerCache = fileMarkerCache;
        _fileMarkerHelper = fileMarkerHelper;
    }

    internal async Task ExecMarkFileAsNewAsync<T>(AsyncTaskData<T> obj, SocketManager socketManager)
    {
        await _tenantManager.SetCurrentTenantAsync(obj.TenantId);

        var folderDao = _daoFactory.GetFolderDao<T>();

        var parentFolderId = obj.FileEntry.FileEntryType == FileEntryType.File ? 
            ((File<T>)obj.FileEntry).ParentId : 
            ((Folder<T>)obj.FileEntry).Id;

        var parentFolders = await folderDao.GetParentFoldersAsync(parentFolderId).Reverse().ToListAsync();

        var userIDs = obj.UserIDs;

        var userEntriesData = new Dictionary<Guid, List<FileEntry>>();

        if (obj.FileEntry.RootFolderType == FolderType.BUNCH)
        {
            if (userIDs.Count == 0)
            {
                return;
            }

            var projectsFolder = await _globalFolder.GetFolderProjectsAsync<T>(_daoFactory);
            parentFolders.Add(await folderDao.GetFolderAsync(projectsFolder));

            var entries = new List<FileEntry> { obj.FileEntry };
            entries = entries.Concat(parentFolders).ToList();

            userIDs.ForEach(userID =>
            {
                if (userEntriesData.TryGetValue(userID, out var value))
                {
                    value.AddRange(entries);
                }
                else
                {
                    userEntriesData.Add(userID, entries);
                }

                RemoveFromCache(projectsFolder, userID);
            });
        }
        else
        {
            if (userIDs.Count == 0)
            {
                var guids = await _fileSecurity.WhoCanReadAsync(obj.FileEntry);
                userIDs = guids.Where(x => x != obj.CurrentAccountId).ToList();
            }

            if (obj.FileEntry.ProviderEntry)
            {
                userIDs = await userIDs.ToAsyncEnumerable().WhereAwait(async u => !await _userManager.IsUserAsync(u)).ToListAsync();

                if (obj.FileEntry.RootFolderType == FolderType.VirtualRooms)
                {
                    var parents = new List<Folder<T>>();

                    foreach (var folder in parentFolders)
                    {
                        if (DocSpaceHelper.IsRoom(folder.FolderType))
                        {
                            parents.Add(folder);
                            break;
                        }

                        parents.Add(folder);
                    }

                    parentFolders = parents;
                }
            }

            foreach (var parentFolder in parentFolders)
            {
                var whoCanRead = await _fileSecurity.WhoCanReadAsync(parentFolder);
                var ids = whoCanRead
                    .Where(userId => userIDs.Contains(userId) && userId != obj.CurrentAccountId);
                foreach (var id in ids)
                {
                    if (userEntriesData.TryGetValue(id, out var value))
                    {
                        value.Add(parentFolder);
                    }
                    else
                    {
                        userEntriesData.Add(id, new List<FileEntry> { parentFolder });
                    }
                }
            }


            switch (obj.FileEntry.RootFolderType)
            {
                case FolderType.USER:
                    {
                        var folderDaoInt = _daoFactory.GetFolderDao<int>();
                        var folderShare = await folderDaoInt.GetFolderAsync(await _globalFolder.GetFolderShareAsync(_daoFactory));

                        foreach (var userID in userIDs)
                        {
                            var userFolderId = await folderDaoInt.GetFolderIDUserAsync(false, userID);
                            if (Equals(userFolderId, 0))
                            {
                                continue;
                            }

                            Folder<int> rootFolder = null;
                            if (obj.FileEntry.ProviderEntry)
                            {
                                rootFolder = obj.FileEntry.RootCreateBy == userID
                                    ? await folderDaoInt.GetFolderAsync(userFolderId)
                                    : folderShare;
                            }
                            else if (!Equals(obj.FileEntry.RootId, userFolderId))
                            {
                                rootFolder = folderShare;
                            }
                            else
                            {
                                RemoveFromCache(userFolderId, userID);
                            }

                            if (rootFolder == null)
                            {
                                continue;
                            }

                            if (userEntriesData.TryGetValue(userID, out var value))
                            {
                                value.Add(rootFolder);
                            }
                            else
                            {
                                userEntriesData.Add(userID, new List<FileEntry> { rootFolder });
                            }

                            RemoveFromCache(rootFolder.Id, userID);
                        }

                        break;
                    }
                case FolderType.COMMON:
                    {
                        var commonFolderId = await _globalFolder.GetFolderCommonAsync(_daoFactory);
                        userIDs.ForEach(userID => RemoveFromCache(commonFolderId, userID));

                        if (obj.FileEntry.ProviderEntry)
                        {
                            var commonFolder = await folderDao.GetFolderAsync(await _globalFolder.GetFolderCommonAsync<T>(_daoFactory));
                            userIDs.ForEach(userID =>
                            {
                                if (userEntriesData.TryGetValue(userID, out var value))
                                {
                                    value.Add(commonFolder);
                                }
                                else
                                {
                                    userEntriesData.Add(userID, new List<FileEntry> { commonFolder });
                                }

                                RemoveFromCache(commonFolderId, userID);
                            });
                        }

                        break;
                    }
                case FolderType.VirtualRooms:
                    {
                        var virtualRoomsFolderId = await _globalFolder.GetFolderVirtualRoomsAsync(_daoFactory);
                        userIDs.ForEach(userID => RemoveFromCache(virtualRoomsFolderId, userID));

                        var room = parentFolders.Find(f => DocSpaceHelper.IsRoom(f.FolderType));

                        if (room.CreateBy != obj.CurrentAccountId)
                        {
                            var roomOwnerEntries = parentFolders.Cast<FileEntry>().Concat(new[] { obj.FileEntry }).ToList();
                            userEntriesData.Add(room.CreateBy, roomOwnerEntries);

                            RemoveFromCache(virtualRoomsFolderId, room.CreateBy);
                        }

                        if (obj.FileEntry.ProviderEntry)
                        {
                            var virtualRoomsFolder = await _daoFactory.GetFolderDao<int>().GetFolderAsync(virtualRoomsFolderId);

                            userIDs.ForEach(userID =>
                            {
                                if (userEntriesData.TryGetValue(userID, out var value))
                                {
                                    value.Add(virtualRoomsFolder);
                                }
                                else
                                {
                                    userEntriesData.Add(userID, new List<FileEntry> { virtualRoomsFolder });
                                }

                                RemoveFromCache(virtualRoomsFolderId, userID);
                            });
                        }

                        break;
                    }
                case FolderType.Privacy:
                    {
                        foreach (var userID in userIDs)
                        {
                            var privacyFolderId = await folderDao.GetFolderIDPrivacyAsync(false, userID);
                            if (Equals(privacyFolderId, 0))
                            {
                                continue;
                            }

                            var rootFolder = await folderDao.GetFolderAsync(privacyFolderId);
                            if (rootFolder == null)
                            {
                                continue;
                            }

                            if (userEntriesData.TryGetValue(userID, out var value))
                            {
                                value.Add(rootFolder);
                            }
                            else
                            {
                                userEntriesData.Add(userID, new List<FileEntry> { rootFolder });
                            }

                            RemoveFromCache(rootFolder.Id, userID);
                        }

                        break;
                    }
            }

            userIDs.ForEach(userID =>
            {
                if (userEntriesData.TryGetValue(userID, out var value))
                {
                    value.Add(obj.FileEntry);
                }
                else
                {
                    userEntriesData.Add(userID, new List<FileEntry> { obj.FileEntry });
                }
            });
        }

        var tagDao = _daoFactory.GetTagDao<T>();
        var newTags = new List<Tag>();
        var updateTags = new List<Tag>();

        try
        {
            await _semaphore.WaitAsync();

            foreach (var userId in userEntriesData.Keys)
            {
                if (await tagDao.GetNewTagsAsync(userId, obj.FileEntry).AnyAsync())
                {
                    continue;
                }

                var entries = userEntriesData[userId].Distinct().ToList();

                await GetNewTagsAsync(userId, entries.OfType<FileEntry<int>>().ToList());
                await GetNewTagsAsync(userId, entries.OfType<FileEntry<string>>().ToList());
            }

            if (updateTags.Count > 0)
            {
                await tagDao.IncrementNewTagsAsync(updateTags, obj.CurrentAccountId);
            }

            if (newTags.Count > 0)
            {
                await tagDao.SaveTagsAsync(newTags, obj.CurrentAccountId);
            }
        }
        finally
        {
            _semaphore.Release();
        }

        await SendChangeNoticeAsync(updateTags.Concat(newTags).ToList(), socketManager);
        
        async Task GetNewTagsAsync<T1>(Guid userId, List<FileEntry<T1>> entries)
        {
            var tagDao1 = _daoFactory.GetTagDao<T1>();
            var exist = await tagDao1.GetNewTagsAsync(userId, entries).ToListAsync();
            var update = exist.Where(t => t.EntryType == FileEntryType.Folder).ToList();
            update.ForEach(t => t.Count++);
            updateTags.AddRange(update);

            entries.ForEach(entry =>
            {
                if (entry != null && exist.TrueForAll(tag => tag != null && !(tag.EntryType == entry.FileEntryType && tag.EntryId.Equals(entry.Id))))
                {
                    newTags.Add(Tag.New(userId, entry));
                }
            });
        }
    }

    public async ValueTask MarkAsNewAsync<T>(FileEntry<T> fileEntry, List<Guid> userIDs = null)
    {
        if (fileEntry == null)
        {
            return;
        }

        userIDs ??= new List<Guid>();

        var taskData = new AsyncTaskData<T>
        {
            TenantId = await _tenantManager.GetCurrentTenantIdAsync(),
            CurrentAccountId = _authContext.CurrentAccount.ID,
            FileEntry = (FileEntry<T>)fileEntry.Clone(),
            UserIDs = userIDs
        };

        if (fileEntry.RootFolderType == FolderType.BUNCH && userIDs.Count == 0)
        {
            var folderDao = _daoFactory.GetFolderDao<T>();
            var path = await folderDao.GetBunchObjectIDAsync(fileEntry.RootId);

            var projectID = path.Split('/').Last();
            if (string.IsNullOrEmpty(projectID))
            {
                return;
            }

            var whoCanRead = await _fileSecurity.WhoCanReadAsync(fileEntry);
            var projectTeam = whoCanRead.Where(x => x != _authContext.CurrentAccount.ID).ToList();

            if (projectTeam.Count == 0)
            {
                return;
            }

            taskData.UserIDs = projectTeam;
        }

        _fileMarkerHelper.Add(taskData);
    }

    public async ValueTask RemoveMarkAsNewAsync<T>(FileEntry<T> fileEntry, Guid userID = default)
    {
        if (fileEntry == null)
        {
            return;
        }

        userID = userID.Equals(Guid.Empty) ? _authContext.CurrentAccount.ID : userID;

        var tagDao = _daoFactory.GetTagDao<T>();
        var internalFolderDao = _daoFactory.GetFolderDao<int>();
        var folderDao = _daoFactory.GetFolderDao<T>();

        if (!await tagDao.GetNewTagsAsync(userID, fileEntry).AnyAsync())
        {
            return;
        }

        T folderId;
        var valueNew = 0;
        var userFolderId = await internalFolderDao.GetFolderIDUserAsync(false, userID);
        var privacyFolderId = await internalFolderDao.GetFolderIDPrivacyAsync(false, userID);

        var removeTags = new List<Tag>();

        if (fileEntry.FileEntryType == FileEntryType.File)
        {
            folderId = ((File<T>)fileEntry).ParentId;

            removeTags.Add(Tag.New(userID, fileEntry));
            valueNew = 1;
        }
        else
        {
            folderId = fileEntry.Id;

            var listTags = await tagDao.GetNewTagsAsync(userID, (Folder<T>)fileEntry, true).ToListAsync();
            var fileTag  = listTags.Find(tag => tag.EntryType == FileEntryType.Folder && tag.EntryId.Equals(fileEntry.Id));
            if (fileTag != null)
            {
                valueNew = fileTag.Count;
            }

            if (Equals(fileEntry.Id, userFolderId) || Equals(fileEntry.Id, await _globalFolder.GetFolderCommonAsync(_daoFactory)) || Equals(fileEntry.Id, await _globalFolder.GetFolderShareAsync(_daoFactory)))
            {
                var folderTags = listTags.Where(tag => tag.EntryType == FileEntryType.Folder);

                foreach (var tag in folderTags)
                {
                    var folderEntry = await folderDao.GetFolderAsync((T)tag.EntryId);
                    if (folderEntry is { ProviderEntry: true })
                    {
                        listTags.Remove(tag);
                        listTags.AddRange(await tagDao.GetNewTagsAsync(userID, folderEntry, true).ToListAsync());
                    }
                }
            }

            removeTags.AddRange(listTags);
        }

        var parentFolders = await folderDao.GetParentFoldersAsync(folderId).Reverse().ToListAsync();

        var rootFolder = parentFolders.LastOrDefault();
        int rootFolderId = default;
        int cacheFolderId = default;
        if (rootFolder != null)
        {
            switch (rootFolder.RootFolderType)
            {
                case FolderType.VirtualRooms:
                    cacheFolderId = rootFolderId = await _globalFolder.GetFolderVirtualRoomsAsync(_daoFactory);
                    break;
                case FolderType.BUNCH:
                    cacheFolderId = rootFolderId = await _globalFolder.GetFolderProjectsAsync(_daoFactory);
                    break;
                case FolderType.COMMON when rootFolder.ProviderEntry:
                    cacheFolderId = rootFolderId = await _globalFolder.GetFolderCommonAsync(_daoFactory);
                    break;
                case FolderType.COMMON:
                    cacheFolderId = await _globalFolder.GetFolderCommonAsync(_daoFactory);
                    break;
                case FolderType.USER when rootFolder.ProviderEntry && rootFolder.RootCreateBy == userID:
                    cacheFolderId = rootFolderId = userFolderId;
                    break;
                case FolderType.USER when !rootFolder.ProviderEntry && !Equals(rootFolder.RootId, userFolderId)
                                          || rootFolder.ProviderEntry && rootFolder.RootCreateBy != userID:
                    cacheFolderId = rootFolderId = await _globalFolder.GetFolderShareAsync(_daoFactory);
                    break;
                case FolderType.USER:
                    cacheFolderId = userFolderId;
                    break;
                case FolderType.Privacy:
                    {
                        if (!Equals(privacyFolderId, 0))
                        {
                            cacheFolderId = rootFolderId = privacyFolderId;
                        }

                        break;
                    }
                case FolderType.SHARE:
                    cacheFolderId = await _globalFolder.GetFolderShareAsync(_daoFactory);
                    break;
            }
        }

        var updateTags = new List<Tag>();

        if (!rootFolderId.Equals(default))
        {
            var internalRootFolder = await internalFolderDao.GetFolderAsync(rootFolderId);
            await UpdateRemoveTags(internalRootFolder, userID, valueNew, updateTags, removeTags);
        }
        
        RemoveFromCache(cacheFolderId, userID);

        foreach (var parentFolder in parentFolders)
        {
            await UpdateRemoveTags(parentFolder, userID, valueNew, updateTags, removeTags);
        }

        if (updateTags.Count > 0)
        {
            await tagDao.UpdateNewTags(updateTags);
        }

        if (removeTags.Count > 0)
        {
            await tagDao.RemoveTagsAsync(removeTags);
        }

        var socketManager = _serviceProvider.GetRequiredService<SocketManager>();

        var toRemove = removeTags.Select(r => new Tag(r.Name, r.Type, r.Owner, 0)
        {
            EntryId = r.EntryId,
            EntryType = r.EntryType
        });

        await SendChangeNoticeAsync(updateTags.Concat(toRemove).ToList(), socketManager);
    }

    private async Task UpdateRemoveTags<TFolder>(FileEntry<TFolder> folder, Guid userId, int valueNew, ICollection<Tag> updateTags,  ICollection<Tag> removeTags)
    {
        var tagDao = _daoFactory.GetTagDao<TFolder>();
        var newTags = tagDao.GetNewTagsAsync(userId, folder);
        var parentTag = await newTags.FirstOrDefaultAsync();

        if (parentTag != null)
        {
            parentTag.Count -= valueNew;

            if (parentTag.Count > 0)
            {
                updateTags.Add(parentTag);
            }
            else
            {
                removeTags.Add(parentTag);
            }
        }
    }
    
    public async Task RemoveMarkAsNewForAllAsync<T>(FileEntry<T> fileEntry)
    {
        var tagDao = _daoFactory.GetTagDao<T>();
        var tags = tagDao.GetTagsAsync(fileEntry.Id, fileEntry.FileEntryType == FileEntryType.File ? FileEntryType.File : FileEntryType.Folder, TagType.New);
        var userIDs = tags.Select(tag => tag.Owner).Distinct();

        await foreach (var userID in userIDs)
        {
            await RemoveMarkAsNewAsync(fileEntry, userID);
        }
    }


    public async Task<int> GetRootFoldersIdMarkedAsNewAsync<T>(T rootId)
    {
        var fromCache = GetCountFromCache(rootId);
        if (fromCache == -1)
        {
            var tagDao = _daoFactory.GetTagDao<T>();
            var folderDao = _daoFactory.GetFolderDao<T>();
            var requestTags = tagDao.GetNewTagsAsync(_authContext.CurrentAccount.ID, await folderDao.GetFolderAsync(rootId));
            var requestTag = await requestTags.FirstOrDefaultAsync(tag => tag.EntryType == FileEntryType.Folder && tag.EntryId.Equals(rootId));
            var count = requestTag == null ? 0 : requestTag.Count;
            InsertToCache(rootId, count);

            return count;
        }
        else if (fromCache > 0)
        {
            return fromCache;
        }

        return 0;
    }

    public IAsyncEnumerable<FileEntry> MarkedItemsAsync<T>(Folder<T> folder)
    {
        if (folder == null)
        {
            throw new ArgumentNullException(nameof(folder), FilesCommonResource.ErrorMassage_FolderNotFound);
        }

        return InternalMarkedItemsAsync(folder);
    }

    private async IAsyncEnumerable<FileEntry> InternalMarkedItemsAsync<T>(Folder<T> folder)
    {
        if (!await _fileSecurity.CanReadAsync(folder))
        {
            throw new SecurityException(FilesCommonResource.ErrorMassage_SecurityException_ViewFolder);
        }

        if (folder.RootFolderType == FolderType.TRASH && !Equals(folder.Id, await _globalFolder.GetFolderTrashAsync(_daoFactory)))
        {
            throw new SecurityException(FilesCommonResource.ErrorMassage_ViewTrashItem);
        }

        var tagDao = _daoFactory.GetTagDao<T>();
        var providerFolderDao = _daoFactory.GetFolderDao<string>();
        var providerTagDao = _daoFactory.GetTagDao<string>();
        var tags = await (tagDao.GetNewTagsAsync(_authContext.CurrentAccount.ID, folder, true) ?? AsyncEnumerable.Empty<Tag>()).ToListAsync();

        if (!tags.Any())
        {
            yield break;
        }

        if (Equals(folder.Id, await _globalFolder.GetFolderMyAsync(this, _daoFactory)) ||
            Equals(folder.Id, await _globalFolder.GetFolderCommonAsync(_daoFactory)) ||
            Equals(folder.Id, await _globalFolder.GetFolderShareAsync(_daoFactory)) ||
            Equals(folder.Id, await _globalFolder.GetFolderVirtualRoomsAsync(_daoFactory)))
        {
            var folderTags = tags.Where(tag => tag.EntryType == FileEntryType.Folder && (tag.EntryId is string));

            var providerFolderTags = new List<KeyValuePair<Tag, Folder<string>>>();

            foreach (var tag in folderTags)
            {
                var pair = new KeyValuePair<Tag, Folder<string>>(tag, await providerFolderDao.GetFolderAsync(tag.EntryId.ToString()));
                if (pair.Value is { ProviderEntry: true })
                {
                    providerFolderTags.Add(pair);
                }
            }

            providerFolderTags.Reverse();

            foreach (var providerFolderTag in providerFolderTags)
            {
                tags.AddRange(await providerTagDao.GetNewTagsAsync(_authContext.CurrentAccount.ID, providerFolderTag.Value, true).ToListAsync());
            }
        }

        tags = tags
            .Where(r => r.EntryType == FileEntryType.Folder && !Equals(r.EntryId, folder.Id) ||  r.EntryType == FileEntryType.File)
                .Distinct()
                .ToList();

        //TODO: refactoring
        var entryTagsProvider = await GetEntryTagsAsync<string>(tags.Where(r => r.EntryId is string).ToAsyncEnumerable());
        var entryTagsInternal = await GetEntryTagsAsync<int>(tags.Where(r => r.EntryId is int).ToAsyncEnumerable());

        foreach (var entryTag in entryTagsInternal)
        {
            var parentEntry = entryTagsInternal.Keys
                .FirstOrDefault(entryCountTag => entryCountTag.FileEntryType == FileEntryType.Folder && Equals(entryCountTag.Id, entryTag.Key.ParentId));

            if (parentEntry != null)
            {
                entryTagsInternal[parentEntry].Count -= entryTag.Value.Count;
            }
        }

        foreach (var entryTag in entryTagsProvider)
        {
            if (int.TryParse(entryTag.Key.ParentId, out var fId))
            {
                var parentEntryInt = entryTagsInternal.Keys
                        .FirstOrDefault(entryCountTag => entryCountTag.FileEntryType == FileEntryType.Folder && Equals(entryCountTag.Id, fId));

                if (parentEntryInt != null)
                {
                    entryTagsInternal[parentEntryInt].Count -= entryTag.Value.Count;
                }

                continue;
            }

            var parentEntry = entryTagsProvider.Keys
                .FirstOrDefault(entryCountTag => entryCountTag.FileEntryType == FileEntryType.Folder && Equals(entryCountTag.Id, entryTag.Key.ParentId));

            if (parentEntry != null)
            {
                entryTagsProvider[parentEntry].Count -= entryTag.Value.Count;
            }
        }

        await foreach (var r in GetResultAsync(entryTagsInternal))
        {
            yield return r;
        }

        await foreach (var r in GetResultAsync(entryTagsProvider))
        {
            yield return r;
        }

        async IAsyncEnumerable<FileEntry> GetResultAsync<TEntry>(Dictionary<FileEntry<TEntry>, Tag> entryTags)
        {
            foreach (var entryTag in entryTags)
            {
                if (!string.IsNullOrEmpty(entryTag.Key.Error))
                {
                    await RemoveMarkAsNewAsync(entryTag.Key);
                    continue;
                }

                if (entryTag.Value.Count > 0)
                {
                    yield return entryTag.Key;
                }
            }
        }
    }

    private async Task<Dictionary<FileEntry<T>, Tag>> GetEntryTagsAsync<T>(IAsyncEnumerable<Tag> tags)
    {
        var fileDao = _daoFactory.GetFileDao<T>();
        var folderDao = _daoFactory.GetFolderDao<T>();
        var entryTags = new Dictionary<FileEntry<T>, Tag>();

        await foreach (var tag in tags)
        {
            var entry = tag.EntryType == FileEntryType.File
                            ? await fileDao.GetFileAsync((T)tag.EntryId)
                            : (FileEntry<T>)await folderDao.GetFolderAsync((T)tag.EntryId);
            if (entry != null && (!entry.ProviderEntry || _filesSettingsHelper.EnableThirdParty))
            {
                entryTags.Add(entry, tag);
            }
            else
            {
                //todo: RemoveMarkAsNew(tag);
            }
        }

        return entryTags;
    }

    public async Task SetTagsNewAsync<T>(Folder<T> parent, IEnumerable<FileEntry> entries)
    {
        var tagDao = _daoFactory.GetTagDao<T>();
        var folderDao = _daoFactory.GetFolderDao<T>();
        var totalTags = await tagDao.GetNewTagsAsync(_authContext.CurrentAccount.ID, parent, false).ToListAsync();

        if (totalTags.Count <= 0)
        {
            return;
        }

        var shareFolder = await _globalFolder.GetFolderShareAsync<T>(_daoFactory);
        var parentFolderTag = Equals(shareFolder, parent.Id)
                                    ? await tagDao.GetNewTagsAsync(_authContext.CurrentAccount.ID, await folderDao.GetFolderAsync(shareFolder)).FirstOrDefaultAsync()
                                    : totalTags.Find(tag => tag.EntryType == FileEntryType.Folder && Equals(tag.EntryId, parent.Id));

        totalTags = totalTags.Where(e => e != parentFolderTag).ToList();

        foreach (var e in entries)
        {
            if (e is FileEntry<int> entry)
            {
                SetTagNewForEntry(entry);
            }
            else if (e is FileEntry<string> thirdPartyEntry)
            {
                SetTagNewForEntry(thirdPartyEntry);
            }
        }

        if (parent.FolderType == FolderType.VirtualRooms)
        {
            var disabledRooms = _roomsNotificationSettingsHelper.GetDisabledRoomsForCurrentUser();
            totalTags = totalTags.Where(e => !disabledRooms.Contains(e.EntryId.ToString())).ToList();
        }

        var countSubNew = 0;
        totalTags.ForEach(tag =>
        {
            countSubNew += tag.Count;
        });

        if (parentFolderTag == null)
        {
            parentFolderTag = Tag.New(_authContext.CurrentAccount.ID, parent, 0);
            parentFolderTag.Id = -1;
        }
        else
        {
            ((IFolder)parent).NewForMe = parentFolderTag.Count;
        }

        if (parent.FolderType != FolderType.VirtualRooms && parent.RootFolderType == FolderType.VirtualRooms && parent.ProviderEntry)
        {
            countSubNew = parentFolderTag.Count;
        }

        if (parentFolderTag.Count != countSubNew)
        {
            if (parent.FolderType == FolderType.VirtualRooms)
            {
                parentFolderTag.Count = countSubNew;
                if (parentFolderTag.Id == -1)
                {
                    await tagDao.SaveTagsAsync(parentFolderTag);
                }
                else
                {
                    await tagDao.UpdateNewTags(parentFolderTag);
                }
                
                RemoveFromCache(parent.Id);
            }
            else if (countSubNew > 0)
            {
                var diff = parentFolderTag.Count - countSubNew;

                parentFolderTag.Count -= diff;
                if (parentFolderTag.Id == -1)
                {
                    await tagDao.SaveTagsAsync(parentFolderTag);
                }
                else
                {
                    await tagDao.UpdateNewTags(parentFolderTag);
                }

                var cacheFolderId = parent.Id;
                var parentsList = await _daoFactory.GetFolderDao<T>().GetParentFoldersAsync(parent.Id).Reverse().ToListAsync();
                parentsList.Remove(parent);

                if (parentsList.Count > 0)
                {
                    var rootFolder = parentsList.Last();
                    T rootFolderId = default;
                    cacheFolderId = rootFolder.Id;
                    if (rootFolder.RootFolderType == FolderType.BUNCH)
                    {
                        cacheFolderId = rootFolderId = await _globalFolder.GetFolderProjectsAsync<T>(_daoFactory);
                    }
                    else if (rootFolder.RootFolderType == FolderType.USER && !Equals(rootFolder.RootId, await _globalFolder.GetFolderMyAsync(this, _daoFactory)))
                    {
                        cacheFolderId = rootFolderId = shareFolder;
                    }
                    else if (rootFolder.RootFolderType == FolderType.VirtualRooms)
                    {
                        rootFolderId = IdConverter.Convert<T>(await _globalFolder.GetFolderVirtualRoomsAsync(_daoFactory));
                    }

                    if (rootFolderId != null)
                    {
                        parentsList.Add(await _daoFactory.GetFolderDao<T>().GetFolderAsync(rootFolderId));
                    }

                    foreach (var folderFromList in parentsList)
                    {
                        var parentTreeTag = await tagDao.GetNewTagsAsync(_authContext.CurrentAccount.ID, folderFromList).FirstOrDefaultAsync();

                        if (parentTreeTag == null)
                        {
                            if (await _fileSecurity.CanReadAsync(folderFromList))
                            {
                                await tagDao.SaveTagsAsync(Tag.New(_authContext.CurrentAccount.ID, folderFromList, -diff));
                            }
                        }
                        else
                        {
                            parentTreeTag.Count -= diff;
                            await tagDao.UpdateNewTags(parentTreeTag);
                        }
                    }
                }
                
                RemoveFromCache(cacheFolderId);
            }
            else
            {
                await RemoveMarkAsNewAsync(parent);
            }
        }

        void SetTagNewForEntry<TEntry>(FileEntry<TEntry> entry)
        {
            var curTag = totalTags.Find(tag => tag.EntryType == entry.FileEntryType && tag.EntryId.Equals(entry.Id));

            if (curTag != null)
            {
                if (entry.FileEntryType == FileEntryType.Folder)
                {
                    ((IFolder)entry).NewForMe = curTag.Count;
                }
                else
                {
                    entry.IsNew = true;
                }
            }
        }
    }

    private void InsertToCache(object folderId, int count)
    {
        var key = string.Format(CacheKeyFormat, _authContext.CurrentAccount.ID, folderId);
        _fileMarkerCache.Insert(key, count.ToString());
    }

    private int GetCountFromCache(object folderId)
    {
        var key = string.Format(CacheKeyFormat, _authContext.CurrentAccount.ID, folderId);
        var count = _fileMarkerCache.Get<string>(key);

        return count == null ? -1 : int.Parse(count);
    }

    private void RemoveFromCache<T>(T folderId)
    {
        RemoveFromCache(folderId, _authContext.CurrentAccount.ID);
    }

    private void RemoveFromCache<T>(T folderId, Guid userId)
    {
        if (Equals(folderId, default))
        {
            return;
        }
        var key = string.Format(CacheKeyFormat, userId, folderId);
        _fileMarkerCache.Remove(key);
    }

    private static async Task SendChangeNoticeAsync(IReadOnlyCollection<Tag> tags, SocketManager socketManager)
    {
        const int chunkSize = 1000;

        foreach (var chunk in tags.Where(t => t.EntryType == FileEntryType.File).Chunk(chunkSize))
        {
            await socketManager.ExecMarkAsNewFilesAsync(chunk);
        }

        foreach (var chunk in tags.Where(t => t.EntryType == FileEntryType.Folder).Chunk(chunkSize))
        {
            await socketManager.ExecMarkAsNewFoldersAsync(chunk);
        }
    }
}

public class AsyncTaskData<T> : DistributedTask
{
    public int TenantId { get; init; }
    public FileEntry<T> FileEntry { get; init; }
    public List<Guid> UserIDs { get; set; }
    public Guid CurrentAccountId { get; init; }
}
