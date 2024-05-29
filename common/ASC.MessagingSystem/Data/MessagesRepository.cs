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

namespace ASC.MessagingSystem.Data;

[Singleton]
public class MessagesRepository : IDisposable
{
    private DateTime _lastSave = DateTime.UtcNow;
    private bool _timerStarted;
    private readonly TimeSpan _cacheTime;
    private readonly Dictionary<string, EventMessage> _cache;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<MessagesRepository> _logger;
    private readonly Timer _timer;
    private readonly int _cacheLimit;
    private readonly SemaphoreSlim _semaphore = new(1);
    private readonly HashSet<MessageAction> _forceSaveAuditActions =
        [MessageAction.RoomInviteLinkUsed, MessageAction.UserSentEmailChangeInstructions, MessageAction.UserSentPasswordChangeInstructions];

    public MessagesRepository(IServiceScopeFactory serviceScopeFactory, ILogger<MessagesRepository> logger, IMapper mapper, IConfiguration configuration)
    {
        _cacheTime = TimeSpan.FromMinutes(1);
        _cache = new Dictionary<string, EventMessage>();
        _timerStarted = false;

        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;

        _timer = new Timer(Callback);

        _mapper = mapper;

        var minutes = configuration["messaging:CacheTimeFromMinutes"];
        var limit = configuration["messaging:CacheLimit"];

        _cacheTime = int.TryParse(minutes, out var cacheTime) ? TimeSpan.FromMinutes(cacheTime) : TimeSpan.FromMinutes(1);
        _cacheLimit = int.TryParse(limit, out var cacheLimit) ? cacheLimit : 100;
        return;

        async void Callback(object state) => await FlushCacheAsync(state);
    }

    ~MessagesRepository()
    {
        FlushCache();
    }

    private bool ForceSave(EventMessage message)
    {
        // messages with action code < 2000 are related to login-history
        if ((int)message.Action < 2000)
        {
            return true;
        }

        return _forceSaveAuditActions.Contains(message.Action);
    }

    public async Task<int> AddAsync(EventMessage message)
    {
        if (ForceSave(message))
        {
            _logger.LogDebug("ForceSave: {Action}", message.Action.ToStringFast());
            
            int id;
            if (!string.IsNullOrEmpty(message.UaHeader))
            {
                try
                {
                    MessageSettings.AddInfoMessage(message);
                }
                catch (Exception e)
                {
                    _logger.ErrorWithException("Add " + message.Id, e);
                }
            }

            using var scope = _serviceScopeFactory.CreateScope();
            await using var ef = await scope.ServiceProvider.GetService<IDbContextFactory<MessagesContext>>().CreateDbContextAsync();

            if ((int)message.Action < 2000)
            {
                id = await AddLoginEventAsync(message, ef);
            }
            else
            {
                id = await AddAuditEventAsync(message, ef);
            }
            return id;
        }

        var now = DateTime.UtcNow;
        var key = $"{message.TenantId}|{message.UserId}|{message.Id}|{now.Ticks}";

        try
        {
            await _semaphore.WaitAsync();
            _cache[key] = message;

            if (!_timerStarted)
            {
                _timer.Change(0, 100);
                _timerStarted = true;
            }
        }
        finally
        {
            _semaphore.Release();
        }
        return 0;
    }
    private async Task FlushCacheAsync(object _)
    {
        await FlushCacheAsync();
    }

    private async Task FlushCacheAsync()
    {
        List<EventMessage> events = null;

        if (DateTime.UtcNow > _lastSave.Add(_cacheTime) || _cache.Count > _cacheLimit)
        {
            try
            {
                await _semaphore.WaitAsync();
                _timer.Change(-1, -1);
                _timerStarted = false;

                events = [.._cache.Values];
                _cache.Clear();
                _lastSave = DateTime.UtcNow;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        if (events == null)
        {
            return;
        }
        
        _logger.LogDebug("FlushCache: events count {Count}", events.Count);

        using var scope = _serviceScopeFactory.CreateScope();
        await using var ef = await scope.ServiceProvider.GetService<IDbContextFactory<MessagesContext>>().CreateDbContextAsync();

        var dict = new Dictionary<string, ClientInfo>();

        var groups = events.GroupBy(e => e.TenantId);
        foreach (var group in groups)
        {
            try
            {
                foreach (var message in group)
                {
                    if (!string.IsNullOrEmpty(message.UaHeader))
                    {
                        try
                        {
                            MessageSettings.AddInfoMessage(message, dict);
                        }
                        catch (Exception e)
                        {
                            _logger.ErrorFlushCache(message.Id, e);
                        }
                    }

                    if (!ForceSave(message))
                    {
                        // messages with action code < 2000 are related to login-history
                        if ((int)message.Action < 2000)
                        {
                            var loginEvent = _mapper.Map<EventMessage, DbLoginEvent>(message);
                            await ef.LoginEvents.AddAsync(loginEvent);
                        }
                        else
                        {
                            var auditEvent = _mapper.Map<EventMessage, DbAuditEvent>(message);
                            await ef.AuditEvents.AddAsync(auditEvent);
                        }
                    }
                }

                await ef.SaveChangesAsync();
            }
            catch(Exception e)
            {
                _logger.ErrorFlushCache(group.Key, e);
            }
        }
    }

    private void FlushCache()
    {
        List<EventMessage> events;

        try
        {
            _semaphore.Wait();
            _timer.Change(-1, -1);
            _timerStarted = false;

            events = [.._cache.Values];
            _cache.Clear();
            _lastSave = DateTime.UtcNow;
        }
        finally
        {
            _semaphore.Release();
        }

        using var scope = _serviceScopeFactory.CreateScope();
        using var ef = scope.ServiceProvider.GetService<IDbContextFactory<MessagesContext>>().CreateDbContext();

        var dict = new Dictionary<string, ClientInfo>();

        var groups = events.GroupBy(e => e.TenantId);
        foreach (var group in groups)
        {
            try
            {
                foreach (var message in group)
                {
                    if (!string.IsNullOrEmpty(message.UaHeader))
                    {
                        try
                        {
                            MessageSettings.AddInfoMessage(message, dict);
                        }
                        catch (Exception e)
                        {
                            _logger.ErrorFlushCache(message.Id, e);
                        }
                    }

                    if (!ForceSave(message))
                    {
                        // messages with action code < 2000 are related to login-history
                        if ((int)message.Action < 2000)
                        {
                            var loginEvent = _mapper.Map<EventMessage, DbLoginEvent>(message);
                            ef.LoginEvents.Add(loginEvent);
                        }
                        else
                        {
                            var auditEvent = _mapper.Map<EventMessage, DbAuditEvent>(message);
                            ef.AuditEvents.Add(auditEvent);
                        }
                    }
                }
                ef.SaveChanges();
            }
            catch(Exception e)
            {
                _logger.ErrorFlushCache(group.Key, e);
            }
        }
    }

    private async Task<int> AddLoginEventAsync(EventMessage message, MessagesContext dbContext)
    {
        var loginEvent = _mapper.Map<EventMessage, DbLoginEvent>(message);

        await dbContext.LoginEvents.AddAsync(loginEvent);
        await dbContext.SaveChangesAsync();

        return loginEvent.Id;
    }

    private async Task<int> AddAuditEventAsync(EventMessage message, MessagesContext dbContext)
    {
        var auditEvent = _mapper.Map<EventMessage, DbAuditEvent>(message);

        await dbContext.AuditEvents.AddAsync(auditEvent);
        await dbContext.SaveChangesAsync();

        return auditEvent.Id;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
