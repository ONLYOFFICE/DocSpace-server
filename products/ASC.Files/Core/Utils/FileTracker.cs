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
public class FileTrackerHelper
{
    private const string Tracker = "filesTracker";

    private readonly ICacheNotify<FileTrackerNotify> _cacheNotify;
    private readonly ICache _cache;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FileTrackerHelper> _logger;
    private static readonly TimeSpan _trackTimeout = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _checkRightTimeout = TimeSpan.FromMinutes(1);
    private readonly Action<object, object, EvictionReason, object> _callbackAction;

    public FileTrackerHelper(ICacheNotify<FileTrackerNotify> cacheNotify, ICache cache, IServiceProvider serviceProvider, ILogger<FileTrackerHelper> logger)
    {
        _cacheNotify = cacheNotify;
        _cache = cache;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _callbackAction = EvictionCallback();
    }

    public void Subscribe()
    {
        _cacheNotify.Subscribe(a =>
        {
            try
            {
                _cache.Insert(GetCacheKey(a.FileId), a.FileTracker, _cacheTimeout, _callbackAction);
            }
            catch (ObjectDisposedException)
            {
            }

        }, CacheNotifyAction.InsertOrUpdate);
        
        _cacheNotify.Subscribe(a =>
        {
            try
            {
                _cache.Remove(GetCacheKey(a.FileId));
            }
            catch (ObjectDisposedException)
            {
                
            }
        }, CacheNotifyAction.Remove);
        
        _logger.Debug("FileTracker subscribed");
    }

    public async Task<bool> ProlongEditingAsync<T>(T fileId, Guid tabId, Guid userId, int tenantId, string baseUri, bool editingAlone = false, string token = null)
    {
        var checkRight = true;
        var tracker = GetTracker(fileId);
        if (tracker != null && await IsEditingAsync(fileId))
        {
            if (tracker.EditingBy.TryGetValue(tabId, out var trackInfo))
            {
                trackInfo.TrackTime = DateTime.UtcNow;
                checkRight = DateTime.UtcNow - tracker.EditingBy[tabId].CheckRightTime > _checkRightTimeout;
            }
            else
            {
                tracker.EditingBy.TryAdd(tabId,
                    new TrackInfo
                {
                    UserId = userId,
                    NewScheme = tabId == userId,
                    EditingAlone = editingAlone,
                    TenantId = tenantId,
                    BaseUri = baseUri,
                    Token = token
                });
            }
        }
        else
        {
            tracker = new FileTracker(tabId, userId, tabId == userId, editingAlone, tenantId, baseUri, token);
        }

        await SetTrackerAsync(fileId, tracker);

        return checkRight;
    }

    public async Task RemoveAsync<T>(T fileId, Guid tabId = default, Guid userId = default)
    {
        var tracker = GetTracker(fileId);
        if (tracker != null)
        {
            if (tabId != Guid.Empty)
            {
                tracker.EditingBy.TryRemove(tabId, out _);
                await SetTrackerAsync(fileId, tracker);

                return;
            }

            if (userId != Guid.Empty)
            {
                var listForRemove = tracker.EditingBy.Where(b => tracker.EditingBy[b.Key].UserId == userId);

                foreach (var editTab in listForRemove)
                {
                    tracker.EditingBy.TryRemove(editTab.Key, out _);
                }

                await SetTrackerAsync(fileId, tracker);

                return;
            }
        }

        await RemoveTrackerAsync(fileId);
    }

    public async Task<bool> IsEditingAsync<T>(T fileId)
    {
        var tracker = GetTracker(fileId);
        if (tracker != null)
        {
            var now = DateTime.UtcNow;
            var listForRemove = tracker.EditingBy.Where(e =>
                !e.Value.NewScheme && (now - e.Value.TrackTime).Duration() > _trackTimeout);

            foreach (var editTab in listForRemove)
            {
                tracker.EditingBy.TryRemove(editTab.Key, out _);
            }

            if (tracker.EditingBy.IsEmpty)
            {
                await RemoveTrackerAsync(fileId);

                return false;
            }

            await SetTrackerAsync(fileId, tracker);

            return true;
        }

        await RemoveTrackerAsync(fileId);

        return false;
    }

    public bool IsEditingAlone<T>(T fileId)
    {
        var tracker = GetTracker(fileId);

        return tracker != null && tracker.EditingBy.Count == 1 && tracker.EditingBy.FirstOrDefault().Value.EditingAlone;
    }

    public async Task ChangeRight<T>(T fileId, Guid userId, bool check)
    {
        var tracker = GetTracker(fileId);
        if (tracker != null)
        {
            foreach (var value in tracker.EditingBy.Values.Where(value => value.UserId == userId || userId == Guid.Empty))
            {
                value.CheckRightTime = check ? DateTime.MinValue : DateTime.UtcNow;
            }

            await SetTrackerAsync(fileId, tracker);
        }
    }

    public async Task<List<Guid>> GetEditingByAsync<T>(T fileId)
    {
        var tracker = GetTracker(fileId);

        return tracker != null && await IsEditingAsync(fileId)
            ? tracker.EditingBy.Values.Select(i => i.UserId).Distinct().ToList()
            : [];
    }

    private FileTracker GetTracker<T>(T fileId)
    {
        if (EqualityComparer<T>.Default.Equals(fileId, default))
        {
            return null;
        }

        return _cache.Get<FileTracker>(GetCacheKey(fileId));
    }

    private async Task SetTrackerAsync<T>(T fileId, FileTracker tracker)
    {
        if (!EqualityComparer<T>.Default.Equals(fileId, default) && tracker != null)
        {
            await _cacheNotify.PublishAsync(new FileTrackerNotify { FileId = fileId.ToString(), FileTracker = tracker }, CacheNotifyAction.Insert);
        }
    }
    
    private async Task RemoveTrackerAsync<T>(T fileId)
    {
        if (!EqualityComparer<T>.Default.Equals(fileId, default))
        {
            await _cacheNotify.PublishAsync(new FileTrackerNotify { FileId = fileId.ToString(), FileTracker = new FileTracker() }, CacheNotifyAction.Remove);
        }
    }

    private Action<object, object, EvictionReason, object> EvictionCallback()
    {
        return (cacheFileId, fileTracker, reason, _) =>
        {
            if (reason != EvictionReason.Expired || cacheFileId == null)
            {
                return;
            }

            var fId = cacheFileId.ToString()?.Substring(Tracker.Length);
            
            var t = int.TryParse(fId, out var internalFileId) ? 
                Callback(internalFileId, fileTracker as FileTracker).ConfigureAwait(false) : 
                Callback(fId, fileTracker as FileTracker).ConfigureAwait(false);

            t.GetAwaiter().GetResult();
        };

        async Task Callback<T>(T fileId, FileTracker fileTracker)
        {
            try
            {
                if (fileTracker.EditingBy == null || fileTracker.EditingBy.IsEmpty)
                {
                    return;
                }

                var editedBy = fileTracker.EditingBy.FirstOrDefault();
                
                var token = fileTracker.EditingBy
                    .OrderByDescending(x => x.Value.TrackTime)
                    .Where(x => !string.IsNullOrEmpty(x.Value.Token))
                    .Select(x => x.Value.Token)
                    .FirstOrDefault();

                await using var scope = _serviceProvider.CreateAsyncScope();
                var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
                await tenantManager.SetCurrentTenantAsync(editedBy.Value.TenantId);

                var commonLinkUtility = scope.ServiceProvider.GetRequiredService<BaseCommonLinkUtility>();
                commonLinkUtility.ServerUri = editedBy.Value.BaseUri;
                
                var helper = scope.ServiceProvider.GetRequiredService<DocumentServiceHelper>();
                var tracker = scope.ServiceProvider.GetRequiredService<DocumentServiceTrackerHelper>();
                var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();

                var file = await daoFactory.GetFileDao<T>().GetFileStableAsync(fileId);
                if (file == null)
                {
                    return;
                }

                var docKey = await helper.GetDocKeyAsync(file);
                using (_logger.BeginScope(new[] { new KeyValuePair<string, object>("DocumentServiceConnector", $"{fileId}") }))
                {
                    if (await tracker.StartTrackAsync(fileId.ToString(), docKey, token))
                    {
                        await SetTrackerAsync(fileId, fileTracker);
                    }
                    else
                    {
                        await RemoveTrackerAsync(fileId);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.ErrorWithException(e);
            }
        }
    }

    private string GetCacheKey<T>(T fileId)
    {
        return Tracker + fileId;
    }
}

[ProtoContract]
public record FileTrackerNotify
{
    [ProtoMember(1)]
    public string FileId { get; set; }
    
    [ProtoMember(2)]
    public FileTracker FileTracker { get; set; }
}

[ProtoContract]
public record FileTracker
{
    [ProtoMember(1)]
    public ConcurrentDictionary<Guid, TrackInfo> EditingBy { get; set; }

    public FileTracker() { }
    
    internal FileTracker(Guid tabId, Guid userId, bool newScheme, bool editingAlone, int tenantId, string baseUri, string token = null)
    {
        EditingBy = new ConcurrentDictionary<Guid, TrackInfo>();
        EditingBy.TryAdd(tabId, new TrackInfo 
        {
            UserId = userId,
            NewScheme = newScheme,
            EditingAlone = editingAlone,
            TenantId = tenantId,
            BaseUri = baseUri,
            Token = token
        });
    }

    [ProtoContract]
    public class TrackInfo
    {
        [ProtoMember(1)]
        public DateTime CheckRightTime { get; set; } = DateTime.UtcNow;
        
        [ProtoMember(2)]
        public DateTime TrackTime { get; set; } = DateTime.UtcNow;
        
        [ProtoMember(3)]
        public required Guid UserId { get; init; }
        
        [ProtoMember(4)]
        public required int TenantId { get; init; }
        
        [ProtoMember(5)]
        public required string BaseUri { get; init; }
        
        [ProtoMember(6)]
        public required bool NewScheme { get;  init; }
        
        [ProtoMember(7)]
        public required bool EditingAlone { get;  init; }
        
        [ProtoMember(8)]
        public string Token { get; init; }
    }
}