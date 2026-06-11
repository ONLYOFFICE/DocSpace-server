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

using ASC.AI.Integration.Threads;

using Thread = ASC.AI.Integration.Threads.Thread;

namespace ASC.AI.Service;

[Scope]
public class ThreadStorageService(
    UserManager userManager,
    AuthContext authContext,
    TenantManager tenantManager,
    ThreadsStorage storage,
    ProfileStorage profileStorage,
    IDistributedLockProvider distributedLockProvider,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity) : IntegrationServiceBase(userManager, authContext, daoFactory, fileSecurity)
{
    private static readonly EmployeeType[] _allowedTypes = [EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin];

    public async Task<Thread> CreateAsync(string title, Guid? profileId = null, string? entityId = null)
    {
        var entryId = await AssertUserHasAccessAsync(_allowedTypes, entityId);

        var tenantId = tenantManager.GetCurrentTenantId();

        if (!profileId.HasValue)
        {
            return await storage.CreateAsync(tenantId, CurrentUserId, title, entryId: entryId);
        }

        var id = profileId.Value;

        await using (await distributedLockProvider.TryAcquireFairLockAsync(ProfileStorage.GetLockKey(tenantId, id)))
        {
            await AssertProfileExistsAsync(tenantId, id);
            return await storage.CreateAsync(tenantId, CurrentUserId, title, id, entryId);
        }
    }

    public async Task<Thread> ReadByIdAsync(Guid id)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        var thread = await storage.ReadByIdAsync(tenantManager.GetCurrentTenantId(), id) ?? throw new ItemNotFoundException();

        AssertOwner(thread);

        return thread;
    }

    public async Task<IEnumerable<Thread>> ReadAllAsync(string? entityId = null)
    {
        var entryId = await AssertUserHasAccessAsync(_allowedTypes, entityId);

        return await storage.ReadAllAsync(tenantManager.GetCurrentTenantId(), CurrentUserId, entryId);
    }

    public async Task UpdateAsync(Guid id, string? title)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        var tenantId = tenantManager.GetCurrentTenantId();
        var thread = await storage.ReadByIdAsync(tenantId, id) ?? throw new ItemNotFoundException();

        AssertOwner(thread);

        await storage.UpdateAsync(tenantId, id, title);
    }

    public async Task TouchAsync(Guid id, long lastEditDate, Guid? profileId = null, bool clearProfile = false)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        var tenantId = tenantManager.GetCurrentTenantId();
        var thread = await storage.ReadByIdAsync(tenantId, id) ?? throw new ItemNotFoundException();

        AssertOwner(thread);

        var lastEditDateUtc = DateTimeOffset.FromUnixTimeMilliseconds(lastEditDate).UtcDateTime;

        if (clearProfile || !profileId.HasValue)
        {
            await storage.TouchAsync(tenantId, id, lastEditDateUtc, null, clearProfile);
            return;
        }

        var parsedProfileId = profileId.Value;

        await using (await distributedLockProvider.TryAcquireFairLockAsync(ProfileStorage.GetLockKey(tenantId, parsedProfileId)))
        {
            await AssertProfileExistsAsync(tenantId, parsedProfileId);
            await storage.TouchAsync(tenantId, id, lastEditDateUtc, parsedProfileId);
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        var tenantId = tenantManager.GetCurrentTenantId();
        var thread = await storage.ReadByIdAsync(tenantId, id) ?? throw new ItemNotFoundException();

        AssertOwner(thread);

        await storage.DeleteAsync(tenantId, id);
    }

    public async Task AssertAccessAsync(Thread thread)
    {
        await AssertUserHasAccessAsync(_allowedTypes);
        AssertOwner(thread);
    }

    private void AssertOwner(Thread thread)
    {
        if (thread.CreatedBy != CurrentUserId)
        {
            throw new SecurityException();
        }
    }

    private async Task AssertProfileExistsAsync(int tenantId, Guid profileId)
    {
        _ = await profileStorage.ReadByIdAsync(tenantId, profileId)
            ?? throw new ItemNotFoundException($"Profile with id '{profileId}' was not found");
    }
}
