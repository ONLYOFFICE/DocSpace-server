// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.People;

internal class GroupBasedFilter 
{
    public List<Guid> ExcludeGroups { get; private init; }
    public List<List<Guid>> IncludeGroups { get; private init; }
    public List<Tuple<List<List<Guid>>, List<Guid>>> CombinedGroups { get; private init; }
    
    public static GroupBasedFilter Create(IEnumerable<Guid> groupsIds, EmployeeType? employeeType, IEnumerable<EmployeeType> employeeTypes, bool? isDocSpaceAdministrator, 
        Payments? payments, bool? withoutGroup, WebItemManager webItemManager)
    {
        var filter = new GroupBasedFilter { ExcludeGroups = [], IncludeGroups = [], CombinedGroups = [] };

        if ((!withoutGroup.HasValue || withoutGroup.Value) && groupsIds != null && groupsIds.Any())
        {
            foreach (var groupId in groupsIds.Distinct())
            {
                filter.IncludeGroups.Add([groupId]);
            }
        }

        if (employeeType.HasValue)
        {
            FilterByUserType(employeeType.Value, filter.IncludeGroups, filter.ExcludeGroups);
        }
        else if (employeeTypes != null && employeeTypes.Any())
        {
            foreach (var et in employeeTypes)
            {
                var combinedIncludeGroups = new List<List<Guid>>();
                var combinedExcludeGroups = new List<Guid>();
                FilterByUserType(et, combinedIncludeGroups, combinedExcludeGroups);
                filter.CombinedGroups.Add(new Tuple<List<List<Guid>>, List<Guid>>(combinedIncludeGroups, combinedExcludeGroups));
            }
        }

        if (payments != null)
        {
            switch (payments)
            {
                case Payments.Paid:
                    filter.ExcludeGroups.Add(Constants.GroupUser.ID);
                    break;
                case Payments.Free:
                    filter.IncludeGroups.Add([Constants.GroupUser.ID]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(payments), payments, null);
            }
        }

        if (!isDocSpaceAdministrator.HasValue || !isDocSpaceAdministrator.Value)
        {
            return filter;
        }

        var adminGroups = new List<Guid> { Constants.GroupAdmin.ID };
        var products = webItemManager.GetItemsAll().Where(i => i is IProduct || i.ID == WebItemManager.MailProductID);
            
        adminGroups.AddRange(products.Select(r => r.ID));
        filter.IncludeGroups.Add(adminGroups);

        return filter;

        void FilterByUserType(EmployeeType type, ICollection<List<Guid>> iGroups, ICollection<Guid> eGroups)
        {
            switch (type)
            {
                case EmployeeType.DocSpaceAdmin:
                    iGroups.Add([Constants.GroupAdmin.ID]);
                    break;
                case EmployeeType.RoomAdmin:
                    eGroups.Add(Constants.GroupUser.ID);
                    eGroups.Add(Constants.GroupAdmin.ID);
                    eGroups.Add(Constants.GroupCollaborator.ID);
                    break;
                case EmployeeType.Collaborator:
                    iGroups.Add([Constants.GroupCollaborator.ID]);
                    break;
                case EmployeeType.User:
                    iGroups.Add([Constants.GroupUser.ID]);
                    break;
                case EmployeeType.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}