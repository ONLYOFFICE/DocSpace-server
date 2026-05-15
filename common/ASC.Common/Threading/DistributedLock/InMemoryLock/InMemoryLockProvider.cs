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

namespace ASC.Common.Threading.DistributedLock.InMemoryLock;

/// <summary>
/// In-process distributed lock provider using SemaphoreSlim per resource.
/// Designed for single-process (monolith/standalone) deployment mode.
/// </summary>
public class InMemoryLockProvider : Abstractions.IDistributedLockProvider
{
    private static readonly ConcurrentDictionary<string, LockEntry> _locks = new();
    private static readonly TimeSpan _defaultMinTimeout = TimeSpan.FromSeconds(30);

    public async Task<IDistributedLockHandle> TryAcquireFairLockAsync(
        string resource,
        TimeSpan timeout = default,
        bool throwIfNotAcquired = true,
        CancellationToken cancellationToken = default)
    {
        return await TryAcquireLockAsync(resource, timeout, throwIfNotAcquired, cancellationToken);
    }

    public async Task<IDistributedLockHandle> TryAcquireLockAsync(
        string resource,
        TimeSpan timeout = default,
        bool throwIfNotAcquired = true,
        CancellationToken cancellationToken = default)
    {
        if (timeout < _defaultMinTimeout || timeout == Timeout.InfiniteTimeSpan || timeout == TimeSpan.MaxValue)
        {
            timeout = _defaultMinTimeout;
        }

        var entry = AcquireEntry(resource);

        var sw = Stopwatch.StartNew();
        var acquired = await entry.Semaphore.WaitAsync(timeout, cancellationToken);
        sw.Stop();

        if (!acquired)
        {
            ReleaseEntry(resource, entry);

            if (throwIfNotAcquired)
            {
                throw new DistributedLockException(LockStatus.NotAcquired, resource, sw.ElapsedMilliseconds);
            }

            return new DefaultHandle();
        }

        return new InMemoryLockHandle(resource, entry, this);
    }

    public void ReleaseEntry(string resource, LockEntry entry)
    {
        lock (entry)
        {
            entry.RefCount--;

            if (entry.RefCount == 0)
            {
                _locks.TryRemove(new KeyValuePair<string, LockEntry>(resource, entry));
            }
        }
    }

    private static LockEntry AcquireEntry(string resource)
    {
        while (true)
        {
            var entry = _locks.GetOrAdd(resource, _ => new LockEntry());

            lock (entry)
            {
                if (_locks.TryGetValue(resource, out var current) && ReferenceEquals(current, entry))
                {
                    entry.RefCount++;

                    return entry;
                }
            }
        }
    }

    public sealed class LockEntry : IDisposable
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public int RefCount;

        public void Dispose()
        {
            Semaphore.Dispose();
        }
    }
}
