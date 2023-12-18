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
public class FileTrackerHelper
{
    private const string Tracker = "filesTracker";
    private readonly ICache _cache;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<FileTrackerHelper> _logger;
    private static readonly TimeSpan _trackTimeout = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _checkRightTimeout = TimeSpan.FromMinutes(1);
    private readonly Action<object, object, EvictionReason, object> _callbackAction;

    public FileTrackerHelper(ICache cache, IServiceScopeFactory serviceScopeFactory, ILogger<FileTrackerHelper> logger)
    {
        _cache = cache;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _callbackAction = EvictionCallback();
    }


    public bool ProlongEditing<T>(T fileId, Guid tabId, Guid userId, int tenantId, bool editingAlone = false)
    {
        var checkRight = true;
        var tracker = GetTracker(fileId);
        if (tracker != null && IsEditing(fileId))
        {
            if (tracker.EditingBy.TryGetValue(tabId, out var trackInfo))
            {
                trackInfo.TrackTime = DateTime.UtcNow;
                checkRight = DateTime.UtcNow - tracker.EditingBy[tabId].CheckRightTime > _checkRightTimeout;
            }
            else
            {
                tracker.EditingBy.Add(tabId, new TrackInfo
                {
                    UserId = userId,
                    NewScheme = tabId == userId,
                    EditingAlone = editingAlone,
                    TenantId = tenantId
                });
            }
        }
        else
        {
            tracker = new FileTracker(tabId, userId, tabId == userId, editingAlone, tenantId);
        }

        SetTracker(fileId, tracker);

        return checkRight;
    }

    public void Remove<T>(T fileId, Guid tabId = default, Guid userId = default)
    {
        var tracker = GetTracker(fileId);
        if (tracker != null)
        {
            if (tabId != Guid.Empty)
            {
                tracker.EditingBy.Remove(tabId);
                SetTracker(fileId, tracker);

                return;
            }
            if (userId != Guid.Empty)
            {
                var listForRemove = tracker.EditingBy.Where(b => tracker.EditingBy[b.Key].UserId == userId);

                foreach (var editTab in listForRemove)
                {
                    tracker.EditingBy.Remove(editTab.Key);
                }

                SetTracker(fileId, tracker);

                return;
            }
        }

        RemoveTracker(fileId);
    }

    public bool IsEditing<T>(T fileId)
    {
        var tracker = GetTracker(fileId);
        if (tracker != null)
        {
            var now = DateTime.UtcNow;
            var listForRemove = tracker.EditingBy.Where(e => !e.Value.NewScheme && (now - e.Value.TrackTime).Duration() > _trackTimeout);

            foreach (var editTab in listForRemove)
            {
                tracker.EditingBy.Remove(editTab.Key);
            }

            if (tracker.EditingBy.Count == 0)
            {
                RemoveTracker(fileId);

                return false;
            }

            SetTracker(fileId, tracker);

            return true;
        }

        RemoveTracker(fileId);

        return false;
    }

    public bool IsEditingAlone<T>(T fileId)
    {
        var tracker = GetTracker(fileId);

        return tracker != null && tracker.EditingBy.Count == 1 && tracker.EditingBy.FirstOrDefault().Value.EditingAlone;
    }

    public void ChangeRight<T>(T fileId, Guid userId, bool check)
    {
        var tracker = GetTracker(fileId);
        if (tracker != null)
        {
            foreach (var value in tracker.EditingBy.Values)
            {
                if (value.UserId == userId || userId == Guid.Empty)
                {
                    value.CheckRightTime = check ? DateTime.MinValue : DateTime.UtcNow;
                }
            }

            SetTracker(fileId, tracker);
        }
        else
        {
            RemoveTracker(fileId);
        }
    }

    public List<Guid> GetEditingBy<T>(T fileId)
    {
        var tracker = GetTracker(fileId);

        return tracker != null && IsEditing(fileId) ? tracker.EditingBy.Values.Select(i => i.UserId).Distinct().ToList() : new List<Guid>();
    }

    private FileTracker GetTracker<T>(T fileId)
    {
        if (!EqualityComparer<T>.Default.Equals(fileId, default))
        {
            return _cache.Get<FileTracker>(Tracker + fileId);
        }

        return null;
    }

    private void SetTracker<T>(T fileId, FileTracker tracker)
    {
        if (!EqualityComparer<T>.Default.Equals(fileId, default) && tracker != null)
        {
            _cache.Insert(Tracker + fileId, tracker with {}, _cacheTimeout, _callbackAction);
        }
    }
    
    private void RemoveTracker<T>(T fileId)
    {
        if (!EqualityComparer<T>.Default.Equals(fileId, default))
        {
            _cache.Remove(Tracker + fileId);
        }
    }

    private Action<object, object, EvictionReason, object> EvictionCallback()
    {
        return (cacheFileId, fileTracker, reason, _) =>
        {            
            if (reason != EvictionReason.Expired)
            {
                return;
            }
            
            if(int.TryParse(cacheFileId?.ToString(), out var internalFileId))
            {
                Callback(internalFileId, fileTracker as FileTracker).Wait();
            }
            else
            {
                Callback(cacheFileId?.ToString(), fileTracker as FileTracker).Wait();
            }
        };

        async Task Callback<T>(T fileId, FileTracker fileTracker)
        {
            try
            {
                if (fileTracker.EditingBy == null || !fileTracker.EditingBy.Any())
                {
                    return;
                }

                var editedBy = fileTracker.EditingBy.FirstOrDefault();
                await using var scope = _serviceScopeFactory.CreateAsyncScope();
                var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
                await tenantManager.SetCurrentTenantAsync(editedBy.Value.TenantId);

                var helper = scope.ServiceProvider.GetRequiredService<DocumentServiceHelper>();
                var tracker = scope.ServiceProvider.GetRequiredService<DocumentServiceTrackerHelper>();
                var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();

                var docKey = await helper.GetDocKeyAsync(await daoFactory.GetFileDao<T>().GetFileAsync(fileId));
                
                if (await tracker.StartTrackAsync(fileId.ToString(), docKey))
                {
                    _cache.Insert(Tracker + fileId, fileTracker with {}, _cacheTimeout, _callbackAction);
                }
            }
            catch (Exception e)
            {
                _logger.ErrorWithException(e);
            }
        }
    }
}

public record FileTracker
{
    internal Dictionary<Guid, TrackInfo> EditingBy { get; }

    internal FileTracker(Guid tabId, Guid userId, bool newScheme, bool editingAlone, int tenantId)
    {
        EditingBy = new()
        { 
            { tabId, new TrackInfo
                {
                    UserId = userId,
                    NewScheme = newScheme,
                    EditingAlone = editingAlone,
                    TenantId = tenantId
                } 
            } 
        };
    }


    internal class TrackInfo
    {
        public DateTime CheckRightTime { get; set; } = DateTime.UtcNow;
        public DateTime TrackTime { get; set; } = DateTime.UtcNow;
        public required Guid UserId { get; init; }
        public required int TenantId { get; init; }
        public required bool NewScheme { get;  init; }
        public required bool EditingAlone { get;  init; }
    }
}
