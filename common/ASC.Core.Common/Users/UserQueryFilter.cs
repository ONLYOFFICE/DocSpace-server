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
        
        if (!UserSortTypeExtensions.TryParse(sortBy, true, out var sortType))
        {
            SortType = UserSortType.FirstName;
        }

        if (sortType == UserSortType.DisplayName)
        {
            SortType = UserFormatter.GetUserDisplayDefaultOrder() == DisplayUserNameFormat.FirstLast 
                ? UserSortType.FirstName 
                : UserSortType.LastName;
        }
    }
}