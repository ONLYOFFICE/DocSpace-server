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

using ASC.AI.Integration.Profiles;
using ASC.AI.Integration.Threads;
using ASC.Common.Threading.DistributedLock.Abstractions;
using ASC.Core.Users;

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
        await AssertUserHasAccessAsync(_allowedTypes);

        int? entryId = entityId == null ? null : int.Parse(entityId);

        if (entryId.HasValue)
        {
            await AssertEntryAccessAsync(entryId.Value);
        }

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
        await AssertUserHasAccessAsync(_allowedTypes);

        int? entryId = entityId == null ? null : int.Parse(entityId);

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
