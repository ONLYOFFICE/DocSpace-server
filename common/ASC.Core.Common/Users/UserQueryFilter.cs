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

namespace ASC.Core.Common.Users;

public record UserQueryFilter
{
    public bool IsDocSpaceAdmin { get; set; }
    public EmployeeStatus? EmployeeStatus { get; set; }
    public List<List<Guid>> IncludeGroups { get; set; }
    public List<Guid> ExcludeGroups { get; set; }
    public List<Tuple<List<List<Guid>>, List<Guid>>> CombinedGroups { get; set; }
    public EmployeeActivationStatus? ActivationStatus { get; set; }
    public AccountLoginType? AccountLoginType { get; set; }
    public QuotaFilter? QuotaFilter { get; set; }
    public Area? Area { get; set; }
    public string Text { get; set; }
    public string Separator { get; set; }
    public bool WithoutGroup { get; set; }
    public UserSortType SortType { get; set; }
    public bool SortOrderAsc { get; set; }
    public bool IncludeStrangers { get; set; }
    public long Limit { get; set; }
    public long Offset { get; set; }
    public int TenantId { get; set; }
    public Guid OwnerId { get; set; }
    public bool? InvitedByMe { get; set; }
    public Guid? InviterId { get; set; }

    public UserQueryFilter() { }

    public UserQueryFilter(
        bool isDocSpaceAdmin,
        EmployeeStatus? employeeStatus,
        List<List<Guid>> includeGroups,
        List<Guid> excludeGroups,
        List<Tuple<List<List<Guid>>, List<Guid>>> combinedGroups,
        EmployeeActivationStatus? activationStatus,
        AccountLoginType? accountLoginType,
        QuotaFilter? quotaFilter,
        Area? area,
        bool? invitedByMe,
        Guid? inviterId,
        string text,
        string separator,
        bool withoutGroup,
        string sortBy,
        bool sortOrderAsc,
        bool includeStrangers,
        long limit,
        long offset)
    {
        IsDocSpaceAdmin = isDocSpaceAdmin;
        EmployeeStatus = employeeStatus;
        IncludeGroups = includeGroups;
        ExcludeGroups = excludeGroups;
        CombinedGroups = combinedGroups;
        ActivationStatus = activationStatus;
        AccountLoginType = accountLoginType;
        QuotaFilter = quotaFilter;
        Area = area;
        Text = text;
        Separator = separator;
        WithoutGroup = withoutGroup;
        SortOrderAsc = sortOrderAsc;
        IncludeStrangers = includeStrangers;
        Limit = limit;
        Offset = offset;
        InvitedByMe = invitedByMe;
        InviterId = inviterId;

        SortType = !UserSortTypeExtensions.TryParse(sortBy, true, out var sortType)
            ? UserSortType.FirstName
            : sortType;

        if (sortType == UserSortType.DisplayName)
        {
            SortType = UserFormatter.GetUserDisplayDefaultOrder() == DisplayUserNameFormat.FirstLast
                ? UserSortType.FirstName
                : UserSortType.LastName;
        }
    }
}