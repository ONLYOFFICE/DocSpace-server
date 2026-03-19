// (c) Copyright Ascensio System SIA 2009-2026
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
