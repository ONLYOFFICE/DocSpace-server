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

namespace ASC.ApiSystem.Classes;

[Scope]
public class QuotaUsageManager(
    TenantManager tenantManager,
    CoreBaseSettings coreBaseSettings,
    CoreSettings coreSettings,
    CountPaidUserStatistic countPaidUserStatistic,
    CountUserStatistic activeUsersStatistic,
    CountRoomCheckerStatistic countRoomCheckerStatistic,
    CountAIAgentCheckerStatistic countAIAgentCheckerStatistic)
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

        var aiAgentsCount = await countAIAgentCheckerStatistic.GetValueAsync();

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
            RoomsCount = roomsCount,
            MaxAIAgentsCount = coreBaseSettings.Standalone ? -1 : quota.CountAIAgent,
            AIAgentsCount = aiAgentsCount
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
    public int MaxAIAgentsCount { get; set; }
    public int AIAgentsCount { get; set; }
}

public class TenantOwnerDto
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }
}