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

using ZiggyCreatures.Caching.Fusion.Events;

namespace ASC.Web.Files.Utils;

[Singleton]
public class FileTrackerHelper(IFusionCache cache, IServiceProvider serviceProvider, ILogger<FileTrackerHelper> logger)
{
    private const string Tracker = "filesTracker";

    private static readonly TimeSpan _trackTimeout = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _checkRightTimeout = TimeSpan.FromMinutes(1);
    private readonly Guid _instanceId = Guid.NewGuid();

    public async Task<bool> ProlongEditingAsync<T>(T fileId, Guid tabId, Guid userId, Tenant tenant, string baseUri, string docKey, bool editingAlone = false, string token = null, string fillingSessionId = null)
    {
        var checkRight = true;
        var tracker = await GetTrackerAsync(fileId);
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
                        Token = token
                    });
            }
        }
        else
        {
            tracker = new FileTracker(tabId, userId, tabId == userId, editingAlone, tenant, baseUri, docKey, token, fillingSessionId);
        }

        await SetTrackerAsync(fileId, tracker);

        return checkRight;
    }

    public void Subscribe()
    {
        cache.Events.Memory.Eviction += MemoryOnEviction;
    }

    public async Task RemoveAsync<T>(T fileId, Guid tabId = default, Guid userId = default)
    {
        var tracker = await GetTrackerAsync(fileId);
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

    public record EditingStatus(bool IsEditing, bool IsEditingAlone);

    public async Task<EditingStatus> GetEditingStatusAsync<T>(T fileId, bool setTracker = true)
    {
        var tracker = await GetTrackerAsync(fileId);
        if (tracker == null)
        {
            return new(false, false);
        }

        var now = DateTime.UtcNow;

        var listForRemove = tracker.EditingBy.Where(e => !e.Value.NewScheme && (now - e.Value.TrackTime).Duration() > _trackTimeout)
                      .ToList();

        foreach (var editTab in listForRemove)
        {
            tracker.EditingBy.TryRemove(editTab.Key, out _);
        }

        if (tracker.EditingBy.IsEmpty)
        {
            await RemoveTrackerAsync(fileId);
            return new(false, false);
        }

        if (setTracker)
        {
            await SetTrackerAsync(fileId, tracker);
        }

        var alone = tracker.EditingBy.Count == 1 && tracker.EditingBy.FirstOrDefault().Value.EditingAlone;
        return new(true, alone);
    }

    public async Task<bool> IsEditingAsync<T>(T fileId, bool setTracker = true)
    {
        var tracker = await GetTrackerAsync(fileId);
        if (tracker != null)
        {
            var now = DateTime.UtcNow;

            if (tracker.EditingBy != null)
            {
                var listForRemove = tracker.EditingBy.Where(e => !e.Value.NewScheme && (now - e.Value.TrackTime).Duration() > _trackTimeout)
                    .ToList();

                foreach (var editTab in listForRemove)
                {
                    tracker.EditingBy.TryRemove(editTab.Key, out _);
                }

                if (tracker.EditingBy.IsEmpty)
                {
                    await RemoveTrackerAsync(fileId);

                    return false;
                }
            }

            if (setTracker)
            {
                await SetTrackerAsync(fileId, tracker);
            }

            return true;
        }

        return false;
    }

    public async Task<bool> IsEditingAloneAsync<T>(T fileId)
    {
        var tracker = await GetTrackerAsync(fileId);

        return tracker != null && tracker.EditingBy.Count == 1 && tracker.EditingBy.FirstOrDefault().Value.EditingAlone;
    }

    public async Task ChangeRight<T>(T fileId, Guid userId, bool check)
    {
        var tracker = await GetTrackerAsync(fileId);

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
        var tracker = await GetTrackerAsync(fileId);

        return tracker != null && await IsEditingAsync(fileId)
            ? tracker.EditingBy.Values.Select(i => i.UserId).Distinct().ToList()
            : [];
    }

    public async Task<string> GetTrackerDocKey<T>(T fileId)
    {
        var tracker = await GetTrackerAsync(fileId);

        return tracker?.DocKey;
    }

    private async Task<FileTracker> GetTrackerAsync<T>(T fileId)
    {
        if (EqualityComparer<T>.Default.Equals(fileId, default))
        {
            return null;
        }

        return (await cache.GetOrDefaultAsync<FileTrackerNotify>(GetCacheKey(fileId)))?.FileTracker;
    }

    private async Task SetTrackerAsync<T>(T fileId, FileTracker tracker)
    {
        await cache.SetAsync(GetCacheKey(fileId), new FileTrackerNotify { FileId = fileId.ToString(), FileTracker = tracker, InstanceId = _instanceId }, options =>
        {
            options.Duration = _cacheTimeout;
            options.DistributedCacheDuration = _cacheTimeout * 2;
        });

    }

    private async Task RemoveTrackerAsync<T>(T fileId)
    {
        if (!EqualityComparer<T>.Default.Equals(fileId, default))
        {
            await cache.RemoveAsync(GetCacheKey(fileId));
        }
    }

    private void MemoryOnEviction(object sender, FusionCacheEntryEvictionEventArgs e)
    {
        if (e.Reason != EvictionReason.Expired || e.Value == null)
        {
            return;
        }

        if (e.Value is not FileTrackerNotify trackerNotify || trackerNotify.InstanceId != _instanceId)
        {
            return;
        }

        var fId = e.Key[Tracker.Length..];

        _ = int.TryParse(fId, out var internalFileId) ?
            Callback(internalFileId, trackerNotify.FileTracker).ConfigureAwait(false) :
            Callback(fId, trackerNotify.FileTracker).ConfigureAwait(false);

        return;

        async Task Callback<T>(T fileId, FileTracker fileTracker)
        {
            try
            {
                if (fileTracker.EditingBy == null || fileTracker.EditingBy.IsEmpty)
                {
                    return;
                }

                var token = fileTracker.EditingBy
                    .OrderByDescending(x => x.Value.TrackTime)
                    .Where(x => !string.IsNullOrEmpty(x.Value.Token))
                    .Select(x => x.Value.Token)
                    .FirstOrDefault();

                await using var scope = serviceProvider.CreateAsyncScope();
                var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
                tenantManager.SetCurrentTenant(fileTracker.Tenant);

                var commonLinkUtility = scope.ServiceProvider.GetRequiredService<BaseCommonLinkUtility>();
                commonLinkUtility.ServerUri = fileTracker.BaseUri;

                var tracker = scope.ServiceProvider.GetRequiredService<DocumentServiceTrackerHelper>();
                var tenantId = fileTracker.Tenant.Id;

                using (logger.BeginScope(new[]
                       {
                           new KeyValuePair<string, object>("DocumentServiceConnector", $"{fileId}"),
                           new KeyValuePair<string, object>("TenantId", $"{tenantId}")
                       }))
                {
                    if (await tracker.StartTrackAsync(fileId.ToString(), fileTracker.DocKey, token, tenantId, fileTracker.FillingSessionId))
                    {
                        await SetTrackerAsync(fileId, fileTracker);
                    }
                    else
                    {
                        await RemoveTrackerAsync(fileId);
                    }
                }
            }
            catch (Exception exception)
            {
                logger.ErrorWithException(exception);
            }
        }
    }

    private static string GetCacheKey<T>(T fileId)
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

    [ProtoMember(3)]
    public Guid InstanceId { get; set; }
}

[ProtoContract]
public record FileTracker
{
    [ProtoMember(1)]
    public ConcurrentDictionary<Guid, TrackInfo> EditingBy { get; set; }

    [ProtoMember(2)]
    public Tenant Tenant { get; set; }

    [ProtoMember(3)]
    public string BaseUri { get; set; }

    [ProtoMember(4)]
    public string DocKey { get; set; }

    [ProtoMember(5)]
    public string FillingSessionId { get; set; }

    public FileTracker() { }

    internal FileTracker(Guid tabId, Guid userId, bool newScheme, bool editingAlone, Tenant tenant, string baseUri, string docKey, string token = null, string fillingSessionId = null)
    {
        DocKey = docKey;
        Tenant = tenant;
        BaseUri = baseUri;
        FillingSessionId = fillingSessionId;
        EditingBy = new ConcurrentDictionary<Guid, TrackInfo>();
        EditingBy.TryAdd(tabId, new TrackInfo
        {
            UserId = userId,
            NewScheme = newScheme,
            EditingAlone = editingAlone,
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
        public required bool NewScheme { get; init; }

        [ProtoMember(5)]
        public required bool EditingAlone { get; init; }

        [ProtoMember(6)]
        public string Token { get; init; }
    }
}