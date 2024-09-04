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

namespace ASC.Common.Threading.DistributedLock.RedisLock;

public class RedisFairLockHandle : LockHandleBase
{
    private readonly string _id, _resource, _channelName, _queueKey, _queueItemTimeoutKey;
    private readonly IRedisDatabase _database;
    private PeriodicTimer _timer;
    private byte[] Message => _database.Serializer.Serialize<byte>(0);
    
    internal RedisFairLockHandle(
        IRedisDatabase database, 
        string resource, 
        string id,
        string channelName, 
        string queueKey, 
        string queueItemTimeoutKey,
        PeriodicTimer timer)
    {
        _database = database;
        _resource = resource;
        _id = id;
        _channelName = channelName;
        _queueKey = queueKey;
        _timer = timer;
        _queueItemTimeoutKey = queueItemTimeoutKey;
    }

    public override async ValueTask DisposeAsync()
    {
        CheckDispose();
        
        _timer?.Dispose();
        _timer = null;
        
        await _database.Database.ScriptEvaluateAsync(_lockReleaseScript, new
        {
            lockKey = _resource,
            queue = _queueKey,
            channel = _channelName,
            queueTimeout = _queueItemTimeoutKey,
            id = _id,
            currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            message = Message
        });

        _disposed = true;
    }

    public override void Dispose()
    {
        CheckDispose();
        
        _timer?.Dispose();
        _timer = null;
        
        _database.Database.ScriptEvaluate(_lockReleaseScript, new
        {
            lockKey = _resource,
            queue = _queueKey,
            channel = _channelName,
            queueTimeout = _queueItemTimeoutKey,
            id = _id,
            currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            message = Message
        });
        
        _disposed = true;
    }

    private static readonly LuaScript _lockReleaseScript = LuaScript.Prepare(RedisLockUtils.RemoveExtraneousWhitespace(
        """
        while true do
            local firstLockId = redis.call('lindex', @queue, 0);
            if firstLockId == false then
                break;
            end;
        
            local timeoutKey = @queueTimeout .. ':' .. firstLockId;
            local timeout = redis.call('get', timeoutKey);
        
            if timeout ~= false then
                if tonumber(timeout) <= tonumber(@currentTime) then
                    redis.call('del', timeoutKey);
                    redis.call('lpop', @queue);
                else
                    break;
                end;
            elseif timeout == false then
                redis.call('lpop', @queue);
            end;
        end;
        
        if (redis.call('exists', @lockKey) == 0) then
            local nextLockId = redis.call('lindex', @queue, 0);
            if nextLockId ~= false then
                redis.call('publish', @channel .. ':' .. nextLockId, @message);
            end;
            return 1;
        end;
        
        if redis.call('get', @lockKey) == @id then
            redis.call('del', @lockKey)
            local nextLockId = redis.call('lindex', @queue, 0);
            if nextLockId ~= false then
                redis.call('publish', @channel .. ':' .. nextLockId, @message);
            end;
            return 1;
        end
        
        return 0;
        """));
}