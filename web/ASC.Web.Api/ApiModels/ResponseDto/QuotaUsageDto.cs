// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Web.Api.ApiModel.ResponseDto;

[Scope]
public class QuotaUsageManager(
    TenantManager tenantManager,
    CoreBaseSettings coreBaseSettings,
    CountPaidUserStatistic countPaidUserStatistic,
    CountUserStatistic activeUsersStatistic)
{
    public async Task<QuotaUsageDto> Get()
    {
        var tenant = tenantManager.GetCurrentTenant();
        var quota = await tenantManager.GetCurrentTenantQuotaAsync();
        var quotaRows = (await tenantManager.FindTenantQuotaRowsAsync(tenant.Id))
            .Where(r => !string.IsNullOrEmpty(r.Tag) && new Guid(r.Tag) != Guid.Empty)
            .ToList();

        var result = new QuotaUsageDto
        {
            StorageSize = (ulong)Math.Max(0, quota.MaxTotalSize),
            UsedSize = (ulong)Math.Max(0, quotaRows.Sum(r => r.Counter)),
            MaxRoomAdminsCount = quota.CountRoomAdmin,
            RoomAdminCount = await countPaidUserStatistic.GetValueAsync(),
            MaxUsers = coreBaseSettings.Standalone ? -1 : quota.CountUser,
            UsersCount = await activeUsersStatistic.GetValueAsync(),

            StorageUsage = quotaRows
                .Select(x => new QuotaUsage { Path = x.Path.TrimStart('/').TrimEnd('/'), Size = x.Counter })
                .ToList()
        };

        result.MaxFileSize = Math.Min(result.AvailableSize, (ulong)quota.MaxFileSize);

        return result;
    }
}

/// <summary>
/// The quota usage parameters.
/// </summary>
public class QuotaUsageDto
{
    /// <summary>
    /// The storage size.
    /// </summary>
    public ulong StorageSize { get; init; }

    /// <summary>
    /// The maximum file size.
    /// </summary>
    public ulong MaxFileSize { get; set; }

    /// <summary>
    /// The portal used space.
    /// </summary>
    public ulong UsedSize { get; init; }

    /// <summary>
    /// The maximum number of room administrators.
    /// </summary>
    public int MaxRoomAdminsCount { get; init; }

    /// <summary>
    /// The number of room administrators.
    /// </summary>
    public int RoomAdminCount { get; init; }

    /// <summary>
    /// The portal available space.
    /// </summary>
    public ulong AvailableSize
    {
        get { return Math.Max(0, StorageSize > UsedSize ? StorageSize - UsedSize : 0); }
        set { throw new NotImplementedException(); }
    }

    /// <summary>
    /// The available number of users.
    /// </summary>
    public int AvailableUsersCount
    {
        get { return Math.Max(0, MaxRoomAdminsCount - RoomAdminCount); }
        set { throw new NotImplementedException(); }
    }

    /// <summary>
    /// The list of quota storage usage.
    /// </summary>
    public IList<QuotaUsage> StorageUsage { get; set; }

    /// <summary>
    /// The user storage size.
    /// </summary>
    public long UserStorageSize { get; set; }

    /// <summary>
    /// The user used space.
    /// </summary>
    public long UserUsedSize { get; set; }

    /// <summary>
    /// The user available space.
    /// </summary>
    public long UserAvailableSize
    {
        get { return Math.Max(0, UserStorageSize - UserUsedSize); }
        set { throw new NotImplementedException(); }
    }

    /// <summary>
    /// The maximum number of users.
    /// </summary>
    public long MaxUsers { get; set; }

    /// <summary>
    /// The number of users.
    /// </summary>
    public long UsersCount { get; set; }
}

/// <summary>
/// The quota storage usage.
/// </summary>
public class QuotaUsage
{
    /// <summary>
    /// The path to the storage.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// The storage size.
    /// </summary>
    public long Size { get; set; }
}