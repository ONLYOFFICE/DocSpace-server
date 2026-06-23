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

namespace ASC.Api.Core.Core;
public class RedisPersistentConnection : IDisposable
{
    private long _lastReconnectTicks = DateTimeOffset.MinValue.UtcTicks;
    private DateTimeOffset _firstErrorTime = DateTimeOffset.MinValue;
    private DateTimeOffset _previousErrorTime = DateTimeOffset.MinValue;

    // StackExchange.Redis will also be trying to reconnect internally,
    // so limit how often we recreate the ConnectionMultiplexer instance
    // in an attempt to reconnect
    private readonly TimeSpan _reconnectMinInterval = TimeSpan.FromSeconds(60);

    // If errors occur for longer than this threshold, StackExchange.Redis
    // may be failing to reconnect internally, so we'll recreate the
    // ConnectionMultiplexer instance
    private readonly TimeSpan _reconnectErrorThreshold = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _restartConnectionTimeout = TimeSpan.FromSeconds(15);
    //private const int RetryMaxAttempts = 5;

    private readonly SemaphoreSlim _reconnectSemaphore = new(initialCount: 1, maxCount: 1);
    private readonly ConfigurationOptions _configurationOptions;
    private ConnectionMultiplexer _connection;
    private IDatabase _database;

    private RedisPersistentConnection(ConfigurationOptions configurationOptions)
    {
        _configurationOptions = configurationOptions;
    }

    public static async Task<RedisPersistentConnection> InitializeAsync(ConfigurationOptions configurationOptions)
    {
        var redisConnection = new RedisPersistentConnection(configurationOptions);
        await redisConnection.ForceReconnectAsync(initializing: true);

        return redisConnection;
    }

    public IConnectionMultiplexer GetConnection()
    {
        return _connection;
    }

    // public async Task<T> BasicRetryAsync<T>(Func<IDatabase, Task<T>> func)
    // {
    //     var policy = Policy.Handle<RedisConnectionException>()
    //       .Or<SocketException>()
    //       .Or<ObjectDisposedException>()
    //       .WaitAndRetry(RetryMaxAttempts, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
    //       onRetry: async (exception, sleepDuration, attemptNumber, context) =>
    //       {
    //           try
    //           {
    //               await ForceReconnectAsync();
    //           }
    //           catch (ObjectDisposedException) { }
    //       });
    //
    //      return await policy.Execute(async () => await func(_database));
    // }

    /// <summary>
    /// Force a new ConnectionMultiplexer to be created.
    /// NOTES:
    ///     1. Users of the ConnectionMultiplexer MUST handle ObjectDisposedExceptions, which can now happen as a result of calling ForceReconnectAsync().
    ///     2. Call ForceReconnectAsync() for RedisConnectionExceptions and RedisSocketExceptions. You can also call it for RedisTimeoutExceptions,
    ///         but only if you're using generous ReconnectMinInterval and ReconnectErrorThreshold. Otherwise, establishing new connections can cause
    ///         a cascade failure on a server that's timing out because it's already overloaded.
    ///     3. The code will:
    ///         a. wait to reconnect for at least the "ReconnectErrorThreshold" time of repeated errors before actually reconnecting
    ///         b. not reconnect more frequently than configured in "ReconnectMinInterval"
    /// </summary>
    /// <param name="initializing">Should only be true when ForceReconnect is running at startup.</param>
    private async Task ForceReconnectAsync(bool initializing = false)
    {
        var previousTicks = Interlocked.Read(ref _lastReconnectTicks);
        var previousReconnectTime = new DateTimeOffset(previousTicks, TimeSpan.Zero);
        var elapsedSinceLastReconnect = DateTimeOffset.UtcNow - previousReconnectTime;

        // We want to limit how often we perform this top-level reconnect, so we check how long it's been since our last attempt.
        if (elapsedSinceLastReconnect < _reconnectMinInterval)
        {
            return;
        }

        var lockTaken = await _reconnectSemaphore.WaitAsync(_restartConnectionTimeout);

        if (!lockTaken)
        {
            // If we fail to enter the semaphore, then it is possible that another thread has already done so.
            // ForceReconnectAsync() can be retried while connectivity problems persist.
            return;
        }

        try
        {
            var utcNow = DateTimeOffset.UtcNow;
            previousTicks = Interlocked.Read(ref _lastReconnectTicks);
            previousReconnectTime = new DateTimeOffset(previousTicks, TimeSpan.Zero);
            elapsedSinceLastReconnect = utcNow - previousReconnectTime;

            if (_firstErrorTime == DateTimeOffset.MinValue && !initializing)
            {
                // We haven't seen an error since last reconnect, so set initial values.
                _firstErrorTime = utcNow;
                _previousErrorTime = utcNow;
                return;
            }

            if (elapsedSinceLastReconnect < _reconnectMinInterval)
            {
                return; // Some other thread made it through the check and the lock, so nothing to do.
            }

            var elapsedSinceFirstError = utcNow - _firstErrorTime;
            var elapsedSinceMostRecentError = utcNow - _previousErrorTime;

            var shouldReconnect =
                elapsedSinceFirstError >= _reconnectErrorThreshold // Make sure we gave the multiplexer enough time to reconnect on its own if it could.
                && elapsedSinceMostRecentError <= _reconnectErrorThreshold; // Make sure we aren't working on stale data (e.g. if there was a gap in errors, don't reconnect yet).

            // Update the previousErrorTime timestamp to be now (e.g. this reconnect request).
            _previousErrorTime = utcNow;

            if (!shouldReconnect && !initializing)
            {
                return;
            }

            _firstErrorTime = DateTimeOffset.MinValue;
            _previousErrorTime = DateTimeOffset.MinValue;

            if (_connection != null)
            {
                try
                {
                    await _connection.CloseAsync();
                }
                catch
                {
                    // Ignore any errors from the old connection
                }
            }

            Interlocked.Exchange(ref _connection, null);
            var newConnection = await ConnectionMultiplexer.ConnectAsync(_configurationOptions);
            Interlocked.Exchange(ref _connection, newConnection);

            Interlocked.Exchange(ref _lastReconnectTicks, utcNow.UtcTicks);
            var newDatabase = _connection.GetDatabase();
            Interlocked.Exchange(ref _database, newDatabase);
        }
        finally
        {
            _reconnectSemaphore.Release();
        }
    }

    public void Dispose()
    {
        try { _connection?.Dispose(); } catch { }
    }
}