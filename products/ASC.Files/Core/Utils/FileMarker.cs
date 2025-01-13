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

        _notify.Subscribe(i => _cache.Remove(i.Key), CacheNotifyAction.Remove);
    }

    public T Get<T>(string key) where T : class
    {
        return _cache.Get<T>(key);
    }

    public async Task InsertAsync(string key, object value)
    {
        await _notify.PublishAsync(new FileMarkerCacheItem { Key = key }, CacheNotifyAction.Remove);

        _cache.Insert(key, value, _cacheExpiration);
    }

    public async Task RemoveAsync(string key)
    {
        await _notify.PublishAsync(new FileMarkerCacheItem { Key = key }, CacheNotifyAction.Remove);

        _cache.Remove(key);
    }
}

[Singleton]
public class FileMarkerHelper(
    IServiceProvider serviceProvider,
    ILogger<FileMarkerHelper> logger,
    IDistributedTaskQueueFactory queueFactory)
{
    private const string CustomDistributedTaskQueueName = "file_marker";
    private readonly ILogger _logger = logger;
    private readonly DistributedTaskQueue _tasks = queueFactory.CreateQueue(CustomDistributedTaskQueueName);

    internal async Task Add<T>(AsyncTaskData<T> taskData)
    {
        await _tasks.EnqueueTask(async (_, _) => await ExecMarkFileAsNewAsync(taskData), taskData);
    }

    private async Task ExecMarkFileAsNewAsync<T>(AsyncTaskData<T> obj)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
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
public class FileMarker(
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
    IDistributedLockProvider distributedLockProvider,
    FileMarkerHelper fileMarkerHelper,
    EntryStatusManager entryStatusManager)
{
    private const string CacheKeyFormat = "MarkedAsNew/{0}/folder_{1}";
    private const string LockKey = "file_marker";

    internal async Task ExecMarkFileAsNewAsync<T>(AsyncTaskData<T> obj, SocketManager socketManager)
    {
        await tenantManager.SetCurrentTenantAsync(obj.TenantId);

        var folderDao = daoFactory.GetFolderDao<T>();

        var parentFolderId = obj.FileEntry.FileEntryType == FileEntryType.File ? ((File<T>)obj.FileEntry).ParentId : ((Folder<T>)obj.FileEntry).Id;

        var parentFolders = await folderDao.GetParentFoldersAsync(parentFolderId).Reverse().ToListAsync();

        var userIDs = obj.UserIDs;

        var userEntriesData = new Dictionary<Guid, Data>();

        if (obj.FileEntry.RootFolderType == FolderType.BUNCH)
        {
            if (userIDs.Count == 0)
            {
                return;
            }

            var projectsFolder = await globalFolder.GetFolderProjectsAsync<T>(daoFactory);
            parentFolders.Add(await folderDao.GetFolderAsync(projectsFolder));

            var entries = new List<FileEntry> { obj.FileEntry };
            entries = entries.Concat(parentFolders).ToList();

            var rootId = projectsFolder.ToString();

            foreach (var userId in userIDs)
            {
                if (userEntriesData.TryGetValue(userId, out var value))
                {
                    value.Entries.AddRange(entries);
                }
                else
                {
                    userEntriesData.Add(userId, new Data { Entries = entries, RootId = rootId });
                }
            }
        }
        else
        {
            var additionalSubjects = Array.Empty<Guid>();
            if (obj.FileEntry.RootFolderType == FolderType.VirtualRooms)
            {
                var room = parentFolders.Find(f => DocSpaceHelper.IsRoom(f.FolderType));
                if (room.CreateBy != obj.CurrentAccountId)
                {
                    additionalSubjects = [room.CreateBy];
                }
            }
            
            if (userIDs.Count == 0)
            {
                var parentFolder = parentFolders.FirstOrDefault();
                var guids = await fileSecurity.WhoCanReadAsync(obj.FileEntry);
                if (parentFolder.FolderType != FolderType.FormFillingFolderDone && parentFolder.FolderType != FolderType.FormFillingFolderInProgress &&
                    parentFolder.FolderType != FolderType.FillingFormsRoom)
                {
                    userIDs = guids.Where(x => x != obj.CurrentAccountId)
                        .ToList();
                }
                else
                {
                    var room = parentFolders.Find(f => DocSpaceHelper.IsRoom(f.FolderType));
                    
                    await foreach (var ace in fileSecurity.GetPureSharesAsync(room, guids))
                    {
                        if (ace.Share != FileShare.FillForms && ace.Subject != obj.CurrentAccountId)
                        {
                            userIDs.Add(ace.Subject);
                        }
                    }
                }

                userIDs.AddRange(additionalSubjects);
            }

            if (obj.FileEntry.ProviderEntry)
            {
                userIDs = await userIDs.ToAsyncEnumerable().WhereAwait(async u => !await userManager.IsGuestAsync(u)).ToListAsync();

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
                var whoCanRead = await fileSecurity.WhoCanReadAsync(parentFolder);
                var ids = whoCanRead
                    .Where(userId => userIDs.Contains(userId) && userId != obj.CurrentAccountId)
                    .Concat(additionalSubjects);
                
                foreach (var id in ids)
                {
                    if (userEntriesData.TryGetValue(id, out var value))
                    {
                        value.Entries.Add(parentFolder);
                    }
                    else
                    {
                        userEntriesData.Add(id, new Data { Entries = [parentFolder] });
                    }
                }
            }

            switch (obj.FileEntry.RootFolderType)
            {
                case FolderType.USER:
                    {
                        var folderDaoInt = daoFactory.GetFolderDao<int>();
                        var folderShare = await folderDaoInt.GetFolderAsync(await globalFolder.GetFolderShareAsync(daoFactory));

                        foreach (var id in userIDs)
                        {
                            var userFolderId = await folderDaoInt.GetFolderIDUserAsync(false, id);
                            if (Equals(userFolderId, 0))
                            {
                                continue;
                            }

                            Folder<int> rootFolder = null;
                            if (obj.FileEntry.ProviderEntry)
                            {
                                rootFolder = obj.FileEntry.RootCreateBy == id 
                                    ? await folderDaoInt.GetFolderAsync(userFolderId) 
                                    : folderShare;
                            }
                            else if (!Equals(obj.FileEntry.RootId, userFolderId))
                            {
                                rootFolder = folderShare;
                            }
                            else
                            {
                                if (userEntriesData.TryGetValue(id, out var value1))
                                {
                                    value1.RootId = userFolderId.ToString();
                                }
                                else
                                {
                                    userEntriesData.Add(id, new Data { RootId = userFolderId.ToString() });
                                }
                            }

                            if (rootFolder == null)
                            {
                                continue;
                            }

                            if (userEntriesData.TryGetValue(id, out var value))
                            {
                                value.Entries.Add(rootFolder);
                            }
                            else
                            {
                                userEntriesData.Add(id, new Data { Entries = [rootFolder], RootId = rootFolder.Id.ToString() });
                            }
                        }

                        break;
                    }
                case FolderType.COMMON:
                {
                    var commonFolderId = await globalFolder.GetFolderCommonAsync(daoFactory);
                    var rootId = commonFolderId.ToString();
                    
                    foreach (var id in userIDs)
                    {
                        if (userEntriesData.TryGetValue(id, out var value))
                        {
                            value.RootId = rootId;
                        }
                        else
                        {
                            userEntriesData.Add(id, new Data { RootId = rootId });
                        }
                    }

                    if (obj.FileEntry.ProviderEntry)
                    {
                        var commonFolder = await folderDao.GetFolderAsync(await globalFolder.GetFolderCommonAsync<T>(daoFactory));

                        foreach (var id in userIDs)
                        {
                            if (userEntriesData.TryGetValue(id, out var value))
                            {
                                value.Entries.Add(commonFolder);
                            }
                            else
                            {
                                userEntriesData.Add(id, new Data { Entries = [commonFolder], RootId = rootId});
                            }
                        }
                    }
                    break;
                }
                case FolderType.VirtualRooms:
                {
                    var virtualRoomsFolderId = await globalFolder.GetFolderVirtualRoomsAsync(daoFactory);
                    var rootId = virtualRoomsFolderId.ToString();
                    
                    foreach (var id in userIDs)
                    {
                        if (userEntriesData.TryGetValue(id, out var data))
                        {
                            data.RootId = rootId;
                        }
                        else
                        {
                            userEntriesData.Add(id, new Data { RootId = rootId });
                        }
                    }

                    if (obj.FileEntry.ProviderEntry)
                    {
                        var virtualRoomsFolder = await daoFactory.GetFolderDao<int>().GetFolderAsync(virtualRoomsFolderId);

                        foreach (var id in userIDs)
                        {
                            if (userEntriesData.TryGetValue(id, out var value))
                            {
                                value.Entries.Add(virtualRoomsFolder);
                                value.RootId = rootId;
                            }
                            else
                            {
                                userEntriesData.Add(id, new Data { Entries = [virtualRoomsFolder], RootId = rootId });
                            }
                        }
                    }

                    break;
                }
                case FolderType.Privacy:
                {
                    foreach (var id in userIDs)
                    {
                        var privacyFolderId = await folderDao.GetFolderIDPrivacyAsync(false, id);
                        if (Equals(privacyFolderId, 0))
                        {
                            continue;
                        }

                        var rootFolder = await folderDao.GetFolderAsync(privacyFolderId);
                        if (rootFolder == null)
                        {
                            continue;
                        }

                        if (userEntriesData.TryGetValue(id, out var value))
                        {
                            value.Entries.Add(rootFolder);
                        }
                        else
                        {
                            userEntriesData.Add(id, new Data { Entries = [rootFolder], RootId = rootFolder.Id.ToString() });
                        }
                    }

                    break;
                }
            }

            userIDs.ForEach(id =>
            {
                if (userEntriesData.TryGetValue(id, out var value))
                {
                    value.Entries.Add(obj.FileEntry);
                }
                else
                {
                    userEntriesData.Add(id, new Data { Entries = [obj.FileEntry] });
                }
            });
        }

        var tagDao = daoFactory.GetTagDao<T>();
        var newTags = new List<Tag>();
        var updateTags = new List<Tag>();

        var tenantId = tenantManager.GetCurrentTenantId();

        await using (await distributedLockProvider.TryAcquireLockAsync($"${LockKey}_{tenantId}", TimeSpan.FromMinutes(5)))
        {
            foreach (var userId in userEntriesData.Keys)
            {
                if (await tagDao.GetNewTagsAsync(userId, obj.FileEntry).AnyAsync())
                {
                    continue;
                }
                
                var data = userEntriesData[userId];

                var entries = data.Entries.Distinct().ToList();
                var rootId = data.RootId;

                await GetNewTagsAsync(userId, entries.OfType<FileEntry<int>>().ToList());
                await GetNewTagsAsync(userId, entries.OfType<FileEntry<string>>().ToList());

                if (string.IsNullOrEmpty(rootId))
                {
                    continue;
                }

                if (int.TryParse(rootId, out var id))
                {
                    await RemoveFromCacheAsync(id, userId);
                }
                else
                {
                    await RemoveFromCacheAsync(rootId, userId);
                }
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

        await SendChangeNoticeAsync(updateTags.Concat(newTags).ToList(), socketManager);
        
        return;

        async Task GetNewTagsAsync<T1>(Guid userId, List<FileEntry<T1>> entries)
        {
            var tagDao1 = daoFactory.GetTagDao<T1>();
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

        userIDs ??= [];

        var taskData = new AsyncTaskData<T>
        {
            TenantId = tenantManager.GetCurrentTenantId(),
            CurrentAccountId = authContext.CurrentAccount.ID,
            FileEntry = (FileEntry<T>)fileEntry.Clone(),
            UserIDs = userIDs
        };

        if (fileEntry.RootFolderType == FolderType.BUNCH && userIDs.Count == 0)
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            var path = await folderDao.GetBunchObjectIDAsync(fileEntry.RootId);

            var projectId = path.Split('/').Last();
            if (string.IsNullOrEmpty(projectId))
            {
                return;
            }

            var whoCanRead = await fileSecurity.WhoCanReadAsync(fileEntry);
            var projectTeam = whoCanRead.Where(x => x != authContext.CurrentAccount.ID).ToList();

            if (projectTeam.Count == 0)
            {
                return;
            }

            taskData.UserIDs = projectTeam;
        }

        await fileMarkerHelper.Add(taskData);
    }

    public async ValueTask RemoveMarkAsNewAsync<T>(FileEntry<T> fileEntry, Guid userId = default)
    {
        if (fileEntry == null)
        {
            return;
        }

        userId = userId.Equals(Guid.Empty) ? authContext.CurrentAccount.ID : userId;

        var tagDao = daoFactory.GetTagDao<T>();
        var internalFolderDao = daoFactory.GetFolderDao<int>();
        var folderDao = daoFactory.GetFolderDao<T>();

        if (!await tagDao.GetNewTagsAsync(userId, fileEntry).AnyAsync())
        {
            return;
        }

        T folderId;
        var valueNew = 0;
        var userFolderId = await internalFolderDao.GetFolderIDUserAsync(false, userId);
        var privacyFolderId = await internalFolderDao.GetFolderIDPrivacyAsync(false, userId);

        var removeTags = new List<Tag>();

        if (fileEntry.FileEntryType == FileEntryType.File)
        {
            folderId = ((File<T>)fileEntry).ParentId;

            removeTags.Add(Tag.New(userId, fileEntry));
            valueNew = 1;
        }
        else
        {
            folderId = fileEntry.Id;

            var listTags = await tagDao.GetNewTagsAsync(userId, (Folder<T>)fileEntry, true).ToListAsync();
            var fileTag  = listTags.Find(tag => tag.EntryType == FileEntryType.Folder && tag.EntryId.Equals(fileEntry.Id));
            if (fileTag != null)
            {
                valueNew = fileTag.Count;
            }

            if (Equals(fileEntry.Id, userFolderId) || Equals(fileEntry.Id, await globalFolder.GetFolderCommonAsync(daoFactory)) ||
                Equals(fileEntry.Id, await globalFolder.GetFolderShareAsync(daoFactory)))
            {
                var folderTags = listTags.Where(tag => tag.EntryType == FileEntryType.Folder);

                foreach (var tag in folderTags)
                {
                    var folderEntry = await folderDao.GetFolderAsync((T)tag.EntryId);
                    if (folderEntry is { ProviderEntry: true })
                    {
                        listTags.Remove(tag);
                        listTags.AddRange(await tagDao.GetNewTagsAsync(userId, folderEntry, true).ToListAsync());
                    }
                }
            }

            removeTags.AddRange(listTags);
        }

        var parentFolders = await folderDao.GetParentFoldersAsync(folderId).Reverse().ToListAsync();

        var rootFolder = parentFolders.LastOrDefault();
        var rootFolderId = 0;
        var cacheFolderId = 0;
        if (rootFolder != null)
        {
            switch (rootFolder.RootFolderType)
            {
                case FolderType.VirtualRooms:
                    cacheFolderId = rootFolderId = await globalFolder.GetFolderVirtualRoomsAsync(daoFactory);
                    break;
                case FolderType.BUNCH:
                    cacheFolderId = rootFolderId = await globalFolder.GetFolderProjectsAsync(daoFactory);
                    break;
                case FolderType.COMMON when rootFolder.ProviderEntry:
                    cacheFolderId = rootFolderId = await globalFolder.GetFolderCommonAsync(daoFactory);
                    break;
                case FolderType.COMMON:
                    cacheFolderId = await globalFolder.GetFolderCommonAsync(daoFactory);
                    break;
                case FolderType.USER when rootFolder.ProviderEntry && rootFolder.RootCreateBy == userId:
                    cacheFolderId = rootFolderId = userFolderId;
                    break;
                case FolderType.USER when (!rootFolder.ProviderEntry && !Equals(rootFolder.RootId, userFolderId))
                                          || (rootFolder.ProviderEntry && rootFolder.RootCreateBy != userId):
                    cacheFolderId = rootFolderId = await globalFolder.GetFolderShareAsync(daoFactory);
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
                    cacheFolderId = await globalFolder.GetFolderShareAsync(daoFactory);
                    break;
            }
        }

        var updateTags = new List<Tag>();

        if (!rootFolderId.Equals(0))
        {
            var internalRootFolder = await internalFolderDao.GetFolderAsync(rootFolderId);
            await UpdateRemoveTags(internalRootFolder, userId, valueNew, updateTags, removeTags);
        }

        await RemoveFromCacheAsync(cacheFolderId, userId);

        foreach (var parentFolder in parentFolders)
        {
            await UpdateRemoveTags(parentFolder, userId, valueNew, updateTags, removeTags);
        }

        if (updateTags.Count > 0)
        {
            await tagDao.UpdateNewTags(updateTags);
        }

        if (removeTags.Count > 0)
        {
            await tagDao.RemoveTagsAsync(removeTags);
        }

        var socketManager = serviceProvider.GetRequiredService<SocketManager>();

        var toRemove = removeTags.Select(r => new Tag(r.Name, r.Type, r.Owner) { EntryId = r.EntryId, EntryType = r.EntryType });

        await SendChangeNoticeAsync(updateTags.Concat(toRemove).ToList(), socketManager);
    }

    private async Task UpdateRemoveTags<TFolder>(FileEntry<TFolder> folder, Guid userId, int valueNew, List<Tag> updateTags,  List<Tag> removeTags)
    {
        var tagDao = daoFactory.GetTagDao<TFolder>();
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
        var tagDao = daoFactory.GetTagDao<T>();
        var tags = tagDao.GetTagsAsync(fileEntry.Id, fileEntry.FileEntryType == FileEntryType.File ? FileEntryType.File : FileEntryType.Folder, TagType.New);
        var userIDs = tags.Select(tag => tag.Owner).Distinct();

        await foreach (var userId in userIDs)
        {
            await RemoveMarkAsNewAsync(fileEntry, userId);
        }
    }


    public async Task<int> GetRootFoldersIdMarkedAsNewAsync<T>(T rootId)
    {
        var fromCache = GetCountFromCache(rootId);
        if (fromCache == -1)
        {
            var tagDao = daoFactory.GetTagDao<T>();
            var folderDao = daoFactory.GetFolderDao<T>();
            var requestTags = tagDao.GetNewTagsAsync(authContext.CurrentAccount.ID, await folderDao.GetFolderAsync(rootId));
            var requestTag = await requestTags.FirstOrDefaultAsync(tag => tag.EntryType == FileEntryType.Folder && tag.EntryId.Equals(rootId));
            var count = requestTag?.Count ?? 0;
            await InsertToCacheAsync(rootId, count);

            return count;
        }

        if (fromCache > 0)
        {
            return fromCache;
        }

        return 0;
    }

    public async Task<Dictionary<DateTime, Dictionary<FileEntry, List<FileEntry>>>> GetRoomGroupedNewItemsAsync()
    {
        var roomsId = await globalFolder.GetFolderVirtualRoomsAsync(daoFactory, false);
        var roomsRoot = await daoFactory.GetFolderDao<int>().GetFolderAsync(roomsId);
        
        var (entryTagsProvider, entryTagsInternal) = await GetMarkedEntriesAsync(roomsRoot);
        if (entryTagsProvider.Count == 0 && entryTagsInternal.Count == 0)
        {
            return [];
        }
        
        var treeInternal = MakeTree(entryTagsInternal);
        var treeProvider = MakeTree(entryTagsProvider);
        
        var groupedEntries = new Dictionary<DateTime, Dictionary<FileEntry, List<FileEntry>>>();
        
        var t1 = RemoveErrorEntriesAsync(treeInternal);
        var t2 = RemoveErrorEntriesAsync(treeProvider);
        
        await Task.WhenAll(t1, t2);
        
        GroupEntries(treeInternal);
        GroupEntries(treeProvider);
        
        return groupedEntries;
        
        async Task RemoveErrorEntriesAsync<TId>(Dictionary<string, FileEntry<TId>> entryTags)
        {
            foreach (var (path, entry) in entryTags)
            {
                if (string.IsNullOrEmpty(entry.Error))
                {
                    continue;
                }

                await RemoveMarkAsNewAsync(entry);
                entryTags.Remove(path);
            }
        }

        void GroupEntries<TId>(Dictionary<string, FileEntry<TId>> entriesTree)
        {
            var rooms = entriesTree
                .Where(x => x.Value is IFolder f && DocSpaceHelper.IsRoom(f.FolderType))
                .ToDictionary(x => x.Key, x => x.Value);

            foreach (var (path, entry) in entriesTree)
            {
                if (entry is IFolder folder && DocSpaceHelper.IsRoom(folder.FolderType))
                {
                    continue;
                }

                var roomPath = path.Split('/').FirstOrDefault();
                
                if (string.IsNullOrEmpty(roomPath) || !rooms.TryGetValue(roomPath, out var room))
                {
                    continue;
                }
                
                if (!groupedEntries.TryGetValue(entry.ModifiedOn.Date, out var roomEntries))
                {
                    roomEntries = new Dictionary<FileEntry, List<FileEntry>>();
                    groupedEntries[entry.ModifiedOn.Date] = roomEntries;
                }
                
                if (!roomEntries.TryGetValue(room, out var entries))
                {
                    entries = [];
                    roomEntries[room] = entries;
                }
                
                entries.Add(entry);
            }
        }
    }

    public async IAsyncEnumerable<FileEntry> MarkedItemsAsync<T>(Folder<T> folder)
    {
        var (entryTagsProvider, entryTagsInternal) = await GetMarkedEntriesAsync(folder);
        if (entryTagsProvider.Count == 0 && entryTagsInternal.Count == 0)
        {
            yield break;
        }

        await foreach (var r in GetResultAsync(entryTagsInternal))
        {
            yield return r;
        }

        await foreach (var r in GetResultAsync(entryTagsProvider))
        {
            yield return r;
        }

        yield break;

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

    private async Task<(Dictionary<FileEntry<string>, Tag> entryTagsProvider, Dictionary<FileEntry<int>, Tag> entryTagsInternal)> GetMarkedEntriesAsync<T>(Folder<T> folder)
    {
        if (folder == null)
        {
            throw new ArgumentNullException(nameof(folder), FilesCommonResource.ErrorMessage_FolderNotFound);
        }
        
        if (!await fileSecurity.CanReadAsync(folder))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_ViewFolder);
        }

        if (folder.RootFolderType == FolderType.TRASH && !Equals(folder.Id, await globalFolder.GetFolderTrashAsync(daoFactory)))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_ViewTrashItem);
        }

        var tagDao = daoFactory.GetTagDao<T>();
        var providerFolderDao = daoFactory.GetFolderDao<string>();
        var providerTagDao = daoFactory.GetTagDao<string>();
        var tags = await (tagDao.GetNewTagsAsync(authContext.CurrentAccount.ID, folder, true) ?? AsyncEnumerable.Empty<Tag>()).ToListAsync();

        if (tags.Count == 0)
        {
            return ([], []);
        }

        if (Equals(folder.Id, await globalFolder.GetFolderMyAsync(daoFactory)) ||
            Equals(folder.Id, await globalFolder.GetFolderCommonAsync(daoFactory)) ||
            Equals(folder.Id, await globalFolder.GetFolderShareAsync(daoFactory)) ||
            Equals(folder.Id, await globalFolder.GetFolderVirtualRoomsAsync(daoFactory)))
        {
            var folderTags = tags.Where(tag => tag.EntryType == FileEntryType.Folder && tag.EntryId is string);

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
                tags.AddRange(await providerTagDao.GetNewTagsAsync(authContext.CurrentAccount.ID, providerFolderTag.Value, true).ToListAsync());
            }
        }

        tags = tags
            .Where(r => (r.EntryType == FileEntryType.Folder && !Equals(r.EntryId, folder.Id)) || r.EntryType == FileEntryType.File)
            .Distinct()
            .ToList();
        
        var entryTagsProvider = await filesSettingsHelper.GetEnableThirdParty() 
            ? await GetEntryTagsAsync<string>(tags.Where(r => r.EntryId is string)) 
            : [];
        
        var entryTagsInternal = await GetEntryTagsAsync<int>(tags.Where(r => r.EntryId is int));

        foreach (var entryTag in entryTagsInternal)
        {
            if (entryTag.Value.CreateOn.HasValue)
            {
                entryTag.Key.ModifiedOn = entryTag.Value.CreateOn.Value;
            }
            
            var parentEntry = entryTagsInternal.Keys
                .FirstOrDefault(entryCountTag => entryCountTag.FileEntryType == FileEntryType.Folder && Equals(entryCountTag.Id, entryTag.Key.ParentId));

            if (parentEntry != null)
            {
                entryTagsInternal[parentEntry].Count -= entryTag.Value.Count;
            }
        }

        foreach (var entryTag in entryTagsProvider)
        {
            if (entryTag.Value.CreateOn.HasValue)
            {
                entryTag.Key.ModifiedOn = entryTag.Value.CreateOn.Value;
            }
            
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

        return (entryTagsProvider, entryTagsInternal);
    }
    
    private static Dictionary<string, FileEntry<TEntry>> MakeTree<TEntry>(Dictionary<FileEntry<TEntry>, Tag> entryTags)
    {
        var tree = new Dictionary<string, FileEntry<TEntry>>();
        var parentMap = entryTags.Keys
            .Where(e => e.FileEntryType == FileEntryType.Folder)
            .ToDictionary(e => e.Id, e => e.ParentId);

        foreach (var (entry, _) in entryTags)
        {
            var key = entry.Id.ToString()!;
            var parentId = entry.ParentId;

            while (parentId != null && parentMap.TryGetValue(parentId, out var grandParentId))
            {
                key = $"{parentId}/{key}";
                parentId = grandParentId;
            }

            tree[key] = entry;
        }

        return tree;
    }
    
    private async Task<Dictionary<FileEntry<T>, Tag>> GetEntryTagsAsync<T>(IEnumerable<Tag> tags)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();
        var entryTags = new Dictionary<FileEntry<T>, Tag>();

        var filesTags = tags.Where(t => t.EntryType == FileEntryType.File).ToDictionary(t => (T)t.EntryId);
        var foldersTags = tags.Where(t => t.EntryType == FileEntryType.Folder).ToDictionary(t => (T)t.EntryId);

        var files = await fileDao.GetFilesAsync(filesTags.Keys).ToListAsync();
        var folders = await folderDao.GetFoldersAsync(foldersTags.Keys).ToListAsync();

        await entryStatusManager.SetFormInfoAsync(files);

        foreach (var file in files)
        {
            if (filesTags.TryGetValue(file.Id, out var tag))
            {
                entryTags[file] = tag;
            }
        }
        
        foreach (var folder in folders)
        {
            if (foldersTags.TryGetValue(folder.Id, out var tag))
            {
                entryTags[folder] = tag;
            }
        }

        return entryTags;
    }

    public async Task SetTagsNewAsync<T>(Folder<T> parent, IEnumerable<FileEntry> entries)
    {
        var tagDao = daoFactory.GetTagDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();
        var totalTags = await tagDao.GetNewTagsAsync(authContext.CurrentAccount.ID, parent, false).ToListAsync();

        if (totalTags.Count <= 0)
        {
            return;
        }

        var shareFolder = await globalFolder.GetFolderShareAsync<T>(daoFactory);
        var parentFolderTag = Equals(shareFolder, parent.Id)
                                    ? await tagDao.GetNewTagsAsync(authContext.CurrentAccount.ID, await folderDao.GetFolderAsync(shareFolder)).FirstOrDefaultAsync()
                                    : totalTags.Find(tag => tag.EntryType == FileEntryType.Folder && Equals(tag.EntryId, parent.Id));

        totalTags = totalTags.Where(e => !Equals(e, parentFolderTag)).ToList();

        foreach (var e in entries)
        {
            switch (e)
            {
                case FileEntry<int> entry:
                    SetTagNewForEntry(entry);
                    break;
                case FileEntry<string> thirdPartyEntry:
                    SetTagNewForEntry(thirdPartyEntry);
                    break;
            }
        }

        if (parent.FolderType == FolderType.VirtualRooms)
        {
            var disabledRooms = await roomsNotificationSettingsHelper.GetDisabledRoomsForCurrentUserAsync();
            totalTags = totalTags.Where(e => !disabledRooms.Contains(e.EntryId.ToString())).ToList();
        }

        var countSubNew = 0;
        totalTags.ForEach(tag =>
        {
            countSubNew += tag.Count;
        });

        if (parentFolderTag == null)
        {
            parentFolderTag = Tag.New(authContext.CurrentAccount.ID, parent, 0);
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

                await RemoveFromCacheAsync(parent.Id);
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
                var parentsList = await daoFactory.GetFolderDao<T>().GetParentFoldersAsync(parent.Id).Reverse().ToListAsync();
                parentsList.Remove(parent);

                if (parentsList.Count > 0)
                {
                    var rootFolder = parentsList.Last();
                    T rootFolderId = default;
                    cacheFolderId = rootFolder.Id;
                    
                    if (rootFolder.RootFolderType == FolderType.BUNCH)
                    {
                        cacheFolderId = rootFolderId = await globalFolder.GetFolderProjectsAsync<T>(daoFactory);
                    }
                    else if (rootFolder.RootFolderType == FolderType.USER && !Equals(rootFolder.RootId, await globalFolder.GetFolderMyAsync(daoFactory)))
                    {
                        cacheFolderId = rootFolderId = shareFolder;
                    }
                    else if (rootFolder.RootFolderType == FolderType.VirtualRooms)
                    {
                        rootFolderId = IdConverter.Convert<T>(await globalFolder.GetFolderVirtualRoomsAsync(daoFactory));
                    }

                    if (!Equals(rootFolderId, default(T)))
                    {
                        parentsList.Add(await daoFactory.GetFolderDao<T>().GetFolderAsync(rootFolderId));
                    }

                    foreach (var folderFromList in parentsList)
                    {
                        var parentTreeTag = await tagDao.GetNewTagsAsync(authContext.CurrentAccount.ID, folderFromList).FirstOrDefaultAsync();

                        if (parentTreeTag == null)
                        {
                            if (await fileSecurity.CanReadAsync(folderFromList))
                            {
                                await tagDao.SaveTagsAsync(Tag.New(authContext.CurrentAccount.ID, folderFromList, -diff));
                            }
                        }
                        else
                        {
                            parentTreeTag.Count -= diff;
                            await tagDao.UpdateNewTags(parentTreeTag);
                        }
                    }
                }

                await RemoveFromCacheAsync(cacheFolderId);
            }
            else
            {
                await RemoveMarkAsNewAsync(parent);
            }
        }

        return;

        void SetTagNewForEntry<TEntry>(FileEntry<TEntry> entry)
        {
            var curTag = totalTags.Find(tag => tag.EntryType == entry.FileEntryType && tag.EntryId.Equals(entry.Id));

            if (curTag == null)
            {
                return;
            }

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
    
    public async Task<MarkResult> MarkAsRecentByLink<T>(FileEntry<T> entry, Guid linkId)
    {
        if (entry is File<T>)
        {
            if (entry.RootFolderType is not FolderType.USER)
            {
                return MarkResult.NotMarked;
            }

            if (await globalFolder.GetFolderMyAsync(daoFactory) == 0)
            {
                return MarkResult.NotMarked;
            }
        }

        if (entry is Folder<T> folder && !DocSpaceHelper.IsRoom(folder.FolderType))
        {
            return MarkResult.NotMarked;
        }
        
        var tagDao = daoFactory.GetTagDao<T>();
        var userId = authContext.CurrentAccount.ID;
        var linkIdString = linkId.ToString();

        var tags = await tagDao.GetTagsAsync(userId, TagType.RecentByLink, [entry])
            .ToDictionaryAsync(k => k.Name);

        if (tags.Count > 0)
        {
            var toRemove = tags.Values.Where(t => t.Name != linkIdString);

            await tagDao.RemoveTagsAsync(toRemove);
        }

        if (tags.ContainsKey(linkIdString))
        {
            return MarkResult.MarkExists;
        }

        var tag = Tag.RecentByLink(authContext.CurrentAccount.ID, linkId, entry);
        await tagDao.SaveTagsAsync(tag);

        return MarkResult.Marked;
    }

    private async Task InsertToCacheAsync(object folderId, int count)
    {
        var key = string.Format(CacheKeyFormat, authContext.CurrentAccount.ID, folderId);
        await fileMarkerCache.InsertAsync(key, count.ToString());
    }

    private int GetCountFromCache(object folderId)
    {
        var key = string.Format(CacheKeyFormat, authContext.CurrentAccount.ID, folderId);
        var count = fileMarkerCache.Get<string>(key);

        return count == null ? -1 : int.Parse(count);
    }

    private async Task RemoveFromCacheAsync<T>(T folderId)
    {
        await RemoveFromCacheAsync(folderId, authContext.CurrentAccount.ID);
    }

    private async Task RemoveFromCacheAsync<T>(T folderId, Guid userId)
    {
        if (Equals(folderId, null))
        {
            return;
        }

        var key = string.Format(CacheKeyFormat, userId, folderId);
        await fileMarkerCache.RemoveAsync(key);
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
    
    private record Data
    {
        public List<FileEntry> Entries { get; init; } = [];
        public string RootId { get; set; }
    }
}

public class AsyncTaskData<T> : DistributedTask
{
    public int TenantId { get; init; }
    public FileEntry<T> FileEntry { get; init; }
    public List<Guid> UserIDs { get; set; }
    public Guid CurrentAccountId { get; init; }
}

public enum MarkResult
{
    Marked,
    NotMarked,
    MarkExists
}