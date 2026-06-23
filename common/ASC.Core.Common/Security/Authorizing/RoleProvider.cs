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

using AuthConstants = ASC.Common.Security.Authorizing.AuthConstants;

namespace ASC.Core.Security.Authorizing;

[Scope(typeof(IRoleProvider))]
internal class RoleProvider(IServiceProvider serviceProvider) : IRoleProvider
{
    //circ dep

    public async Task<List<IRole>> GetRolesAsync(ISubject account)
    {
        var roles = new List<IRole>();
        if (account is not ISystemAccount)
        {
            if (account is IRole)
            {
                roles = (await GetParentRolesAsync(account.ID)).ToList();
            }
            else if (account is IUserAccount)
            {
                roles = (await serviceProvider.GetService<UserManager>()
                                   .GetUserGroupsAsync(account.ID, IncludeType.Distinct | IncludeType.InParent))
                                   .Select(IRole (g) => g)
                                   .ToList();
            }
        }

        if (roles.Any(r => r.ID == AuthConstants.User.ID || r.ID == AuthConstants.Guest.ID))
        {
            roles = roles.Where(r => r.ID != AuthConstants.RoomAdmin.ID).ToList();
        }

        return roles;
    }

    public async Task<bool> IsSubjectInRoleAsync(ISubject account, IRole role)
    {
        return await serviceProvider.GetService<UserManager>().IsUserInGroupAsync(account.ID, role.ID);
    }

    private async Task<List<IRole>> GetParentRolesAsync(Guid roleID)
    {
        var roles = new List<IRole>();
        var gi = await serviceProvider.GetService<UserManager>().GetGroupInfoAsync(roleID);
        if (gi != null)
        {
            var parent = gi.Parent;
            while (parent != null)
            {
                roles.Add(parent);
                parent = parent.Parent;
            }
        }

        return roles;
    }
}