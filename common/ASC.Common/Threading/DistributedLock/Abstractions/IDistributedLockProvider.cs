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