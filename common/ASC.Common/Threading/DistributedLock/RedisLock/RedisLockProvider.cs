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

namespace ASC.Common.Threading.DistributedLock.RedisLock;

public class RedisLockProvider : Abstractions.IDistributedLockProvider
{
    private const string KeyPrefix = "lock";
    private const string ChannelName = $"{KeyPrefix}:notify";
    private const string LockQueueName = $"{KeyPrefix}:queue";
    private const string LockQueueItemTimeoutName = $"{KeyPrefix}:timeout";
    
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
        Action<RedisLockOptionsBuilder> optionBuilder)
    {
        _redisClient = redisClient;
        _logger = logger;
        _options = RedisLockOptionsBuilder.GetOptions(optionBuilder);
        _expiryInMilliseconds = (long)_options.Expiry.TotalMilliseconds;
    }
    
    public async Task<IDistributedLockHandle> TryAcquireLockAsync(string resource, TimeSpan timeout, bool throwIfNotAcquired = false, CancellationToken cancellationToken = default)
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
            
            return new RedisLockHandle(database, resource, lockId, ChannelName, queueKey, queueItemTimeoutKey, timer, _expiryInMilliseconds);
        }

        var messageWaiter = new TaskCompletionSource();
        var channel = new RedisChannel($"{ChannelName}:{lockId}", RedisChannel.PatternMode.Auto);

        await database.SubscribeAsync<int>(channel, _ =>
        {
            messageWaiter.TrySetResult();
            return Task.CompletedTask;
        });

        var delay = timeout / 2;
        
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

                await Task.WhenAny(messageWaiter.Task, Task.Delay(delay, cancellationToken));

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

        return new RedisLockHandle(database, resource, lockId, ChannelName, queueKey, queueItemTimeoutKey, timer, _expiryInMilliseconds);
    }

    public IDistributedLockHandle TryAcquireLock(string resource, TimeSpan timeout, bool throwIfNotAcquired = false,  CancellationToken cancellationToken = default)
    {
        return TryAcquireLockAsync(resource, timeout, throwIfNotAcquired, cancellationToken).GetAwaiter().GetResult();
    }

    private async Task<LockStatus> TryAcquireLockInternalAsync(IRedisDatabase database, string resource, string lockId, string queueKey, string queueItemTimeoutKey, TimeSpan timeout)
    {
        var timeoutInMilliseconds = (long)timeout.TotalMilliseconds;
        
        var code = (int)(await database.Database.ScriptEvaluateAsync(_lockTakeScript,
            new RedisKey[] { resource, queueKey, queueItemTimeoutKey },
            new RedisValue[] { _expiryInMilliseconds, lockId, timeoutInMilliseconds, RedisLockUtils.GetNowInMilliseconds() }));

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
                    new RedisKey[] { resource }, new RedisValue[] { _expiryInMilliseconds, lockId });
            }
        }), cancellationToken);

        return timer;
    }
    
    private static string GetLockId()
    {
        return $"{_lockIdPrefix}_{Guid.NewGuid():n}";
    }

    private static readonly string _lockTakeScript = RedisLockUtils.RemoveExtraneousWhitespace(
        """
        while true do
            local firstLockId2 = redis.call('lindex', KEYS[2], 0);
            if firstLockId2 == false then
                break;
            end;
        
            local timeoutKey = KEYS[3] .. ':' .. firstLockId2;
            local timeout = redis.call('get', timeoutKey);
        
            if timeout ~= false then
                if tonumber(timeout) <= tonumber(ARGV[4]) then
                    redis.call('del', timeoutKey);
                    redis.call('lpop', KEYS[2]);
                else
                    break;
                end;
            elseif timeout == false then
                redis.call('lpop', KEYS[2]);
            end;
        end;
        
        local timeoutKey1 = KEYS[3] .. ':' .. ARGV[2];
        
        if (redis.call('exists', KEYS[1]) == 0)
            and ((redis.call('exists', KEYS[2]) == 0)
                or (redis.call('lindex', KEYS[2], 0) == ARGV[2])) then
            redis.call('lpop', KEYS[2]);
            redis.call('del', timeoutKey1);
            redis.call('set', KEYS[1], ARGV[2], 'px', ARGV[1]);
            return 0;
        end;
        
        local timeout1 = redis.call('get', timeoutKey1);
        
        if timeout1 ~= false then
            if tonumber(timeout1) <= tonumber(ARGV[4]) then
                redis.call('del', timeoutKey1);
                return 2;
            end;
        else
            redis.call('rpush', KEYS[2], ARGV[2]);
            redis.call('set', timeoutKey1, tonumber(ARGV[4]) + tonumber(ARGV[3]));
        end;
        
        return 1;
        """);
    
    private static readonly string _lockExtendScript = RedisLockUtils.RemoveExtraneousWhitespace(
        """
            local currentLockId = redis.call('get', KEYS[1]);
            if currentLockId == false then
                return redis.call('set', KEYS[1], ARGV[2], 'px', ARGV[1]);
            elseif currentLockId == ARGV[2] then
                return redis.call('pexpire', KEYS[1], ARGV[2]);
            end;
            
            return -1;
        """);
}