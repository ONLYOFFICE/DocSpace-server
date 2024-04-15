// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.ApiSystem.Classes;

[Scope]
public class QuotaUsageManager(
    TenantManager tenantManager,
    CoreBaseSettings coreBaseSettings,
    CoreSettings coreSettings,
    CountPaidUserStatistic countPaidUserStatistic,
    CountUserStatistic activeUsersStatistic,
    CountRoomCheckerStatistic countRoomCheckerStatistic)
{
    public async Task<QuotaUsageDto> Get(Tenant tenant)
    {
        tenantManager.SetCurrentTenant(tenant);

        var quota = await tenantManager.GetCurrentTenantQuotaAsync();

        var usedSize = (await tenantManager.FindTenantQuotaRowsAsync(tenant.Id))
            .Where(r => !string.IsNullOrEmpty(r.Tag) && new Guid(r.Tag) != Guid.Empty)
            .Sum(r => r.Counter);

        var roomsCount = await countRoomCheckerStatistic.GetValueAsync();

        var roomAdminCount = await countPaidUserStatistic.GetValueAsync();

        var usersCount = await activeUsersStatistic.GetValueAsync();

        var result = new QuotaUsageDto
        {
            TenantId = tenant.Id,
            TenantAlias = tenant.Alias,
            TenantDomain = tenant.GetTenantDomain(coreSettings),

            StorageSize = (ulong)Math.Max(0, quota.MaxTotalSize),
            UsedSize = (ulong)Math.Max(0, usedSize),
            MaxRoomAdminsCount = quota.CountRoomAdmin,
            RoomAdminCount = roomAdminCount,
            MaxUsers = coreBaseSettings.Standalone ? -1 : quota.CountUser,
            UsersCount = usersCount,
            MaxRoomsCount = coreBaseSettings.Standalone ? -1 : quota.CountRoom,
            RoomsCount = roomsCount
        };

        return result;
    }
}


public class QuotaUsageDto
{
    public int TenantId { get; set; }
    public string TenantAlias { get; set; }
    public string TenantDomain { get; set; }

    public ulong StorageSize { get; set; }
    public ulong UsedSize { get; set; }
    public int MaxRoomAdminsCount { get; set; }
    public int RoomAdminCount { get; set; }
    public long MaxUsers { get; set; }
    public long UsersCount { get; set; }
    public int MaxRoomsCount { get; set; }
    public int RoomsCount { get; set; }
}

public class TenantOwnerDto
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }
}
