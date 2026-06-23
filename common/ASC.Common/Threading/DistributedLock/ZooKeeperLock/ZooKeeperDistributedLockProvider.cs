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

namespace ASC.Common.Threading.DistributedLock.ZooKeeperLock;

public class ZooKeeperDistributedLockProvider : Abstractions.IDistributedLockProvider
{
    private readonly Medallion.Threading.IDistributedLockProvider _distributedLockProvider;
    private readonly ILogger<ZooKeeperDistributedLockProvider> _logger;
    private readonly TimeSpan _minTimeout;

    private static readonly TimeSpan _defaultMinTimeout = TimeSpan.FromSeconds(30);
    private static readonly IDistributedLockHandle _emptyHandle = new DefaultHandle();

    public ZooKeeperDistributedLockProvider(
        Medallion.Threading.IDistributedLockProvider distributedLockProvider,
        ILogger<ZooKeeperDistributedLockProvider> logger,
        TimeSpan? minTimeout = null)
    {
        _distributedLockProvider = distributedLockProvider;
        _logger = logger;

        if (!minTimeout.HasValue || minTimeout.Value < _defaultMinTimeout || minTimeout.Value == TimeSpan.MaxValue || minTimeout.Value == Timeout.InfiniteTimeSpan)
        {
            _minTimeout = _defaultMinTimeout;
        }
        else
        {
            _minTimeout = minTimeout.Value;
        }
    }

    public async Task<IDistributedLockHandle> TryAcquireFairLockAsync(string resource, TimeSpan timeout = default, bool throwIfNotAcquired = true,
        CancellationToken cancellationToken = default)
    {
        if (timeout < _minTimeout || timeout == Timeout.InfiniteTimeSpan || timeout == TimeSpan.MaxValue)
        {
            timeout = _minTimeout;
        }

        var timestamp = TimeProvider.System.GetTimestamp();

        var handle = await _distributedLockProvider.TryAcquireLockAsync(resource, timeout, cancellationToken);

        return GetHandle(handle, resource, (long)TimeProvider.System.GetElapsedTime(timestamp).TotalMilliseconds, throwIfNotAcquired);
    }

    public Task<IDistributedLockHandle> TryAcquireLockAsync(string resource, TimeSpan timeout = default, bool throwIfNotAcquired = true, CancellationToken cancellationToken = default)
    {
        return TryAcquireFairLockAsync(resource, timeout, throwIfNotAcquired, cancellationToken);
    }

    private IDistributedLockHandle GetHandle(IDistributedSynchronizationHandle handle, string resource, long elapsedMilliseconds, bool throwIfNotAcquired)
    {
        if (handle != null)
        {
            _logger.DebugTryAcquireLock(resource, elapsedMilliseconds);

            return new ZooKeeperLockHandle(handle);
        }

        if (throwIfNotAcquired)
        {
            throw new DistributedLockException(LockStatus.NotAcquired, resource, elapsedMilliseconds);
        }

        _logger.ErrorTryAcquireLock(resource, elapsedMilliseconds);

        return _emptyHandle;
    }
}