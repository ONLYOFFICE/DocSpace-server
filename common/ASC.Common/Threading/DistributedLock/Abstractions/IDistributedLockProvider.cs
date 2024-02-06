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

namespace ASC.Common.Threading.DistributedLock.Abstractions;

public interface IDistributedLockProvider
{
    /// <summary>
    /// Asynchronously waits for distributed blocking to be acquired. Observes the order for obtaining a lock.
    /// Use in areas with potential high loads.
    /// </summary>
    /// <param name="resource">locking resource</param>
    /// <param name="timeout">lockout waiting time</param>
    /// <param name="throwIfNotAcquired">throw an exception if a lock is not received</param>
    /// <param name="cancellationToken">token to observe</param>
    /// <exception cref="DistributedLockException">lock not acquired</exception>
    /// <returns>A task that will complete with distributed lock handle</returns>
    Task<IDistributedLockHandle> TryAcquireFairLockAsync(string resource, TimeSpan timeout = default, bool throwIfNotAcquired = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously waits for distributed blocking to be acquired. Does not comply with the order for obtaining a blocking.
    /// Use in a long operation.
    /// </summary>
    /// <param name="resource">locking resource</param>
    /// <param name="timeout">lockout waiting time</param>
    /// <param name="throwIfNotAcquired">throw an exception if a lock is not received</param>
    /// <param name="cancellationToken">token to observe</param>
    /// <exception cref="DistributedLockException">lock not acquired</exception>
    /// <returns>A task that will complete with distributed lock handle</returns>
    Task<IDistributedLockHandle> TryAcquireLockAsync(string resource, TimeSpan timeout = default, bool throwIfNotAcquired = true,
        CancellationToken cancellationToken = default);
}