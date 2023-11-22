// (c) Copyright Ascensio System SIA 2010-2022
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

using IDistributedLockProvider = Medallion.Threading.IDistributedLockProvider;

namespace ASC.Common.Threading.DistributedLock.RedisLock;

public class RedisLockProvider : Abstractions.IDistributedLockProvider
{
    private const string KeyPrefix = "lock";
    private const string ChannelName = $"{KeyPrefix}:notify";
    private const string LockQueueName = $"{KeyPrefix}:queue";
    private const string LockQueueItemTimeoutName = $"{KeyPrefix}:timeout";

    private readonly IDistributedLockProvider _internalLockProvider;
    private readonly IRedisClient _redisClient;
    private readonly RedisLockOptions _options;
    private readonly ILogger<RedisLockProvider> _logger;
    private readonly long _expiryInMilliseconds;
    
    private static readonly string _lockIdPrefix;
    private static readonly IDistributedLockHandle _defaultHandle = new DefaultHandle();

    static RedisLockProvider()
    {
        using var currentProcess = Process.GetCurrentProcess();
        _lockIdPrefix = $"{Environment.MachineName}_{currentProcess.Id}";
    }

    public RedisLockProvider(
        IRedisClient redisClient, 
        ILogger<RedisLockProvider> logger,
        IDistributedLockProvider internalLockProvider,
        Action<RedisLockOptionsBuilder> optionBuilder)
    {
        _redisClient = redisClient;
        _logger = logger;
        _internalLockProvider = internalLockProvider;
        _options = RedisLockOptionsBuilder.GetOptions(optionBuilder);
        _expiryInMilliseconds = (long)_options.Expiry.TotalMilliseconds;
    }
    
    public async Task<IDistributedLockHandle> TryAcquireFairLockAsync(string resource, TimeSpan timeout, bool throwIfNotAcquired = true, CancellationToken cancellationToken = default)
    {
        if (timeout < _options.MinTimeout || timeout == Timeout.InfiniteTimeSpan || timeout == TimeSpan.MaxValue)
        {
            timeout = _options.MinTimeout;
        }
        
        PeriodicTimer timer;

        var database = _redisClient.GetDefaultDatabase();
        var lockId = GetLockId();
        var queueKey = RedisLockUtils.PrefixName(LockQueueName, resource);
        var queueItemTimeoutKey = RedisLockUtils.PrefixName(LockQueueItemTimeoutName, resource);
        
        var stopWatch = Stopwatch.StartNew();
        
        var status = await TryAcquireLockInternalAsync(database, resource, lockId, queueKey, queueItemTimeoutKey, timeout);
        if (status == LockStatus.Acquired)
        {
            timer = StartExtendLockLoop(database, resource, lockId, cancellationToken);
            
            _logger.DebugTryAcquireLock(resource, stopWatch.ElapsedMilliseconds);
            
            return new RedisFairLockHandle(database, resource, lockId, ChannelName, queueKey, queueItemTimeoutKey, timer);
        }

        var messageWaiter = new TaskCompletionSource();
        var channel = new RedisChannel($"{ChannelName}:{lockId}", RedisChannel.PatternMode.Auto);

        await database.SubscribeAsync<int>(channel, _ =>
        {
            messageWaiter.TrySetResult();
            return Task.CompletedTask;
        });

        var messageWaitingTimeout = timeout / 3;
        
        try
        {
            while (stopWatch.Elapsed <= timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var newTimeout = timeout - stopWatch.Elapsed;
                if (newTimeout <= TimeSpan.Zero)
                {
                    break;
                }

                status = await TryAcquireLockInternalAsync(database, resource, lockId, queueKey, queueItemTimeoutKey, newTimeout);
                if (status is LockStatus.Acquired or LockStatus.Expired)
                {
                    break;
                }

                await Task.WhenAny(messageWaiter.Task, Task.Delay(messageWaitingTimeout, cancellationToken));

                messageWaiter = new TaskCompletionSource();
            }
        }
        finally
        {
            await database.UnsubscribeAsync<int>(channel, _ => Task.CompletedTask);
        }

        if (status != LockStatus.Acquired)
        {
            if (throwIfNotAcquired)
            {
                throw new DistributedLockException(status, resource, stopWatch.ElapsedMilliseconds);
            }
            
            _logger.ErrorTryAcquireLock(resource, stopWatch.ElapsedMilliseconds);
            
            return _defaultHandle;
        }
        
        _logger.DebugTryAcquireLock(resource, stopWatch.ElapsedMilliseconds);
        
        timer = StartExtendLockLoop(database, resource, lockId, cancellationToken);

        return new RedisFairLockHandle(database, resource, lockId, ChannelName, queueKey, queueItemTimeoutKey, timer);
    }

    public IDistributedLockHandle TryAcquireFairLock(string resource, TimeSpan timeout, bool throwIfNotAcquired = true,  CancellationToken cancellationToken = default)
    {
        return TryAcquireFairLockAsync(resource, timeout, throwIfNotAcquired, cancellationToken).GetAwaiter().GetResult();
    }

    public async Task<IDistributedLockHandle> TryAcquireLockAsync(string resource, TimeSpan timeout = default, bool throwIfNotAcquired = true, CancellationToken cancellationToken = default)
    {
        var stopWatch = Stopwatch.StartNew();
        
        var internalHandle = await _internalLockProvider.TryAcquireLockAsync(resource, timeout, cancellationToken);

        return GetHandle(internalHandle, resource, stopWatch.ElapsedMilliseconds, throwIfNotAcquired);
    }

    public IDistributedLockHandle TryAcquireLock(string resource, TimeSpan timeout = default, bool throwIfNotAcquired = true, CancellationToken cancellationToken = default)
    {
        var stopWatch = Stopwatch.StartNew();
        
        var internalHandle = _internalLockProvider.TryAcquireLock(resource, timeout, cancellationToken);

        return GetHandle(internalHandle, resource, stopWatch.ElapsedMilliseconds, throwIfNotAcquired);
    }

    private async Task<LockStatus> TryAcquireLockInternalAsync(IRedisDatabase database, string resource, string lockId, string queueKey, string queueItemTimeoutKey, TimeSpan timeout)
    {
        var timeoutInMilliseconds = (long)timeout.TotalMilliseconds;

        var code = (int)(await database.Database.ScriptEvaluateAsync(_lockTakeScript, new
        {
            lockKey = resource,
            queue = queueKey,
            queueTimeout = queueItemTimeoutKey,
            expiry = _expiryInMilliseconds,
            id = lockId,
            lockTimeout = timeoutInMilliseconds,
            currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }));

        return (LockStatus)code;
    }
    
    private PeriodicTimer StartExtendLockLoop(IRedisDatabase database, string resource, string lockId, CancellationToken cancellationToken)
    {
        var timer = new PeriodicTimer(_options.ExtendInterval);

        _ = Task.Run((async () =>
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await database.Database.ScriptEvaluateAsync(_lockExtendScript,
                    new
                    {
                        id = lockId,
                        lockKey = resource,
                        expiry = _expiryInMilliseconds
                    });
            }
        }), cancellationToken);

        return timer;
    }
    
    private IDistributedLockHandle GetHandle(IDistributedSynchronizationHandle handle, string resource, long elapsedMilliseconds, bool throwIfNotAcquired)
    {
        if (handle != null)
        {
            _logger.DebugTryAcquireLock(resource, elapsedMilliseconds);

            return new RedisLockHandle(handle);
        }

        if (throwIfNotAcquired)
        {
            throw new DistributedLockException(LockStatus.NotAcquired, resource, elapsedMilliseconds);
        }
        
        _logger.ErrorTryAcquireLock(resource, elapsedMilliseconds);

        return _defaultHandle;
    }
    
    private static string GetLockId()
    {
        return $"{_lockIdPrefix}_{Guid.NewGuid():n}";
    }

    private static readonly LuaScript _lockTakeScript = LuaScript.Prepare(RedisLockUtils.RemoveExtraneousWhitespace(
        """
        while true do
            local firstLockId = redis.call('lindex', @queue, 0);
            if firstLockId == false then
                break;
            end;
        
            local expiryTimeKey = @queueTimeout .. ':' .. firstLockId;
            local expiryTime = redis.call('get', expiryTimeKey);
        
            if expiryTime ~= false then
                if tonumber(expiryTime) <= tonumber(@currentTime) then
                    redis.call('del', expiryTimeKey);
                    redis.call('lpop', @queue);
                else
                    break;
                end;
            elseif expiryTime == false then
                redis.call('lpop', @queue);
            end;
        end;
        
        local expiryTimeKey1 = @queueTimeout .. ':' .. @id;
        
        if (redis.call('exists', @lockKey) == 0)
            and ((redis.call('exists', @queue) == 0)
                or (redis.call('lindex', @queue, 0) == @id)) then
            redis.call('lpop', @queue);
            redis.call('del', expiryTimeKey1);
            redis.call('set', @lockKey, @id, 'px', @expiry);
            return 0;
        end;
        
        local expiryTime1 = redis.call('get', expiryTimeKey1);
        
        if expiryTime1 ~= false then
            if tonumber(expiryTime1) <= tonumber(@currentTime) then
                redis.call('del', expiryTimeKey1);
                return 2;
            end;
        else
            redis.call('rpush', @queue, @id);
            redis.call('set', expiryTimeKey1, tonumber(@currentTime) + tonumber(@lockTimeout));
        end;
        
        return 1;
        """));
    
    private static readonly LuaScript _lockExtendScript = LuaScript.Prepare(RedisLockUtils.RemoveExtraneousWhitespace(
        """
            local currentLockId = redis.call('get', @lockKey);
            if currentLockId == false then
                return redis.call('set', @lockKey, @id, 'px', @expiry);
            elseif currentLockId == @id then
                return redis.call('pexpire', @lockKey, @expiry);
            end;
            
            return -1;
        """));
}