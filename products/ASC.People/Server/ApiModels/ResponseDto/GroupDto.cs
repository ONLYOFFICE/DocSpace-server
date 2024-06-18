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

namespace ASC.People.ApiModels.ResponseDto;

/// <summary>
/// </summary>
public class GroupDto
{
    /// <summary>Name</summary>
    /// <type>System.String, System</type>
    public string Name { get; set; }

    /// <summary>Parent</summary>
    /// <type>System.Nullable{System.Guid}, System</type>
    public Guid? Parent { get; set; }

    /// <summary>Category</summary>
    /// <type>System.Guid, System</type>
    public Guid Category { get; set; }

    /// <summary>ID</summary>
    /// <type>System.Guid, System</type>
    public Guid Id { get; set; }

    /// <summary>Specifies if the LDAP settings are enabled for the group or not</summary>
    /// <type>System.Boolean, System</type>
    public bool IsLDAP { get; set; }

    /// <summary>Manager</summary>
    /// <type>ASC.Web.Api.Models.EmployeeDto, ASC.Api.Core</type>
    public EmployeeFullDto Manager { get; set; }

    /// <summary>List of members</summary>
    /// <type>System.Collections.Generic.List{ASC.Web.Api.Models.EmployeeDto,}, System.Collections.Generic</type>
    public List<EmployeeFullDto> Members { get; set; }
    
    public bool? Shared { get; set; }
    public int MembersCount { get; set; }
}

[Scope]
public class GroupFullDtoHelper(UserManager userManager, EmployeeFullDtoHelper employeeFullDtoHelper)
{
    public async Task<GroupDto> Get(GroupInfo group, bool includeMembers, bool? shared = null)
    {
        var result = new GroupDto
        {
            Id = group.ID,
            Category = group.CategoryID,
            Parent = group.Parent?.ID ?? Guid.Empty,
            Name = group.Name,
            Shared = shared,
            IsLDAP = !string.IsNullOrEmpty(group.Sid)
        };
        
        var manager = await userManager.GetUsersAsync(await userManager.GetDepartmentManagerAsync(group.ID));
        if (manager != null && !manager.Equals(Constants.LostUser))
        {
            result.Manager = await employeeFullDtoHelper.GetFullAsync(manager);
        }

        var members = await userManager.GetUsersByGroupAsync(group.ID);
        result.MembersCount = members.Length;

        if (!includeMembers)
        {
            return result;
        }

        result.Members = [];
        foreach (var m in members)
        { 
            result.Members.Add(await employeeFullDtoHelper.GetFullAsync(m));
        }

        return result;
    }
}
