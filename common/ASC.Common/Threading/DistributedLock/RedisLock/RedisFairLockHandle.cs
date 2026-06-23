// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

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