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

using GroupInfo = ASC.Core.Users.GroupInfo;

namespace ASC.People.ApiModels.ResponseDto;

/// <summary>
/// The group parameters.
/// </summary>
public class GroupDto
{
    /// <summary>
    /// The group name.
    /// </summary>
    /// <example>Marketing Team</example>
    public required string Name { get; set; }

    /// <summary>
    /// The parent group ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid? Parent { get; set; }

    /// <summary>
    /// The group category ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid Category { get; set; }

    /// <summary>
    /// The group ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid Id { get; set; }

    /// <summary>
    /// Specifies if the LDAP settings are enabled for the group or not.
    /// </summary>
    /// <example>false</example>
    public required bool IsLDAP { get; set; }

    /// <summary>
    /// Indicates whether the group is a system group.
    /// </summary>
    /// <example>false</example>
    public bool? IsSystem { get; set; }

    /// <summary>
    /// The group manager full information.
    /// </summary>
    /// <example>{"displayName": "John Doe"}</example>
    public EmployeeFullDto Manager { get; set; }

    /// <summary>
    /// The list of group members.
    /// </summary>
    /// <example>[{"displayName": "John Doe"}]</example>
    public List<EmployeeFullDto> Members { get; set; }

    /// <summary>
    /// Specifies whether the group can be shared or not.
    /// </summary>
    /// <example>false</example>
    public bool? Shared { get; set; }

    /// <summary>
    /// The number of group members.
    /// </summary>
    /// <example>0</example>
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
            IsLDAP = !string.IsNullOrEmpty(group.Sid),
            IsSystem = await userManager.IsSystemGroup(group.ID) ? true : null
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