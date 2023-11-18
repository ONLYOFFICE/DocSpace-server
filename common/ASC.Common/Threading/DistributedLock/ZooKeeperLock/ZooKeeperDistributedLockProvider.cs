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


namespace ASC.Common.Threading.DistributedLock.ZooKeeperLock;

public class ZooKeeperDistributedLockProvider : Abstractions.IDistributedLockProvider
{
    private readonly Medallion.Threading.IDistributedLockProvider _distributedLockProvider;
    private readonly ILogger<ZooKeeperDistributedLockProvider> _logger;
    
    private static readonly IDistributedLockHandle _emptyHandle = new DefaultHandle();

    public ZooKeeperDistributedLockProvider(Medallion.Threading.IDistributedLockProvider distributedLockProvider, ILogger<ZooKeeperDistributedLockProvider> logger)
    {
        _distributedLockProvider = distributedLockProvider;
        _logger = logger;
    }

    public async Task<IDistributedLockHandle> TryAcquireFairLockAsync(string resource, TimeSpan timeout = default, bool throwIfNotAcquired = false, 
        CancellationToken cancellationToken = default)
    {
        var stopWatch = Stopwatch.StartNew();
        
        var handle = await _distributedLockProvider.TryAcquireLockAsync(resource, timeout, cancellationToken);

        return GetHandle(handle, resource, stopWatch.ElapsedMilliseconds, throwIfNotAcquired);
    }

    public IDistributedLockHandle TryAcquireFairLock(string resource, TimeSpan timeout = default, bool throwIfNotAcquired = false, CancellationToken cancellationToken = default)
    {
        var stopWatch = Stopwatch.StartNew();
        
        var handle = _distributedLockProvider.TryAcquireLock(resource, timeout, cancellationToken);

        return GetHandle(handle, resource, stopWatch.ElapsedMilliseconds, throwIfNotAcquired);
    }

    public Task<IDistributedLockHandle> TryAcquireLockAsync(string resource, TimeSpan timeout = default, bool throwIfNotAcquired = false, CancellationToken cancellationToken = default)
    {
        return TryAcquireFairLockAsync(resource, timeout, throwIfNotAcquired, cancellationToken);
    }

    public IDistributedLockHandle TryAcquireLock(string resource, TimeSpan timeout = default, bool throwIfNotAcquired = false, CancellationToken cancellationToken = default)
    {
        return TryAcquireFairLock(resource, timeout, throwIfNotAcquired, cancellationToken);
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