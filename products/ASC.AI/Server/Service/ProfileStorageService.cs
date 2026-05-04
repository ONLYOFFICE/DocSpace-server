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
using ASC.Common.Threading.DistributedLock.Abstractions;
using ASC.Core.Users;

namespace ASC.AI.Service;

[Scope]
public class ProfileStorageService(
    UserManager userManager,
    AuthContext authContext,
    TenantManager tenantManager,
    ProfileStorage storage,
    IDistributedLockProvider distributedLockProvider) : IntegrationServiceBase(userManager, authContext)
{
    public async Task<Profile> CreateAsync(ProfileData profile)
    {
        await AssertUserHasAccessAsync([EmployeeType.DocSpaceAdmin]);

        return await storage.CreateAsync(tenantManager.GetCurrentTenantId(), profile);
    }

    public async Task<IReadOnlyList<Profile>> CreateManyAsync(IReadOnlyList<ProfileData> profiles)
    {
        await AssertUserHasAccessAsync([EmployeeType.DocSpaceAdmin]);

        return await storage.CreateManyAsync(tenantManager.GetCurrentTenantId(), profiles);
    }

    public async Task<Profile> ReadByIdAsync(string id)
    {
        await AssertUserHasAccessAsync([EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin]);

        var profile = await storage.ReadByIdAsync(tenantManager.GetCurrentTenantId(), int.Parse(id));

        return profile ?? throw new ItemNotFoundException();
    }

    public async Task<List<Profile>> ReadAllAsync()
    {
        await AssertUserHasAccessAsync([EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin]);

        return await storage.ReadAllAsync(tenantManager.GetCurrentTenantId());
    }

    public async Task<Profile> UpdateAsync(Profile profile)
    {
        await AssertUserHasAccessAsync([EmployeeType.DocSpaceAdmin]);

        return await storage.UpdateAsync(tenantManager.GetCurrentTenantId(), profile);
    }

    public async Task DeleteAsync(string id)
    {
        await AssertUserHasAccessAsync([EmployeeType.DocSpaceAdmin]);

        var tenantId = tenantManager.GetCurrentTenantId();
        var parsedId = int.Parse(id);

        await using (await distributedLockProvider.TryAcquireFairLockAsync(ProfileStorage.GetLockKey(tenantId, parsedId)))
        {
            await storage.DeleteAsync(tenantId, parsedId);
        }
    }
}
