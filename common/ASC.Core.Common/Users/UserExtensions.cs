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

namespace ASC.Core.Users;

public static class UserExtensions
{
    public static bool IsOwner(this UserInfo ui, Tenant tenant)
    {
        if (ui == null)
        {
            return false;
        }

        return IsOwner(ui.Id, tenant);
    }
    public static bool IsOwner(this Guid ui, Tenant tenant)
    {
        return tenant != null && tenant.OwnerId.Equals(ui);
    }

    extension(UserInfo ui)
    {
        public bool IsMe(AuthContext authContext)
        {
            return IsMe(ui, authContext.CurrentAccount.ID);
        }

        public bool IsMe(Guid id)
        {
            return ui != null && ui.Id == id;
        }
    }

    extension(UserManager userManager)
    {
        public async Task<bool> IsDocSpaceAdminAsync(Guid id)
        {
            return await userManager.IsUserInGroupAsync(id, Constants.GroupAdmin.ID);
        }

        public Task<bool> IsDocSpaceAdminAsync(UserInfo ui)
        {
            return userManager.IsDocSpaceAdminAsync(ui.Id);
        }

        public async Task<bool> IsGuestAsync(Guid id)
        {
            return await userManager.IsUserInGroupAsync(id, Constants.GroupGuest.ID);
        }

        public Task<bool> IsGuestAsync(UserInfo ui)
        {
            return userManager.IsGuestAsync(ui.Id);
        }

        public async Task<bool> IsSystemGroup(Guid groupId)
        {
            var group = await userManager.GetGroupInfoAsync(groupId);
            return group.ID == Constants.LostGroupInfo.ID || group.CategoryID == Constants.SysGroupCategoryId;
        }

        public Task<bool> IsUserAsync(UserInfo userInfo)
        {
            return userManager.IsUserAsync(userInfo.Id);
        }

        public async Task<bool> IsUserAsync(Guid id)
        {
            return await userManager.IsUserInGroupAsync(id, Constants.GroupUser.ID);
        }

        public async Task<bool> IsOutsiderAsync(Guid id)
        {
            return await userManager.IsGuestAsync(id) && id == Constants.OutsideUser.Id;
        }

        public Task<bool> IsOutsiderAsync(UserInfo ui)
        {
            return userManager.IsOutsiderAsync(ui.Id);
        }
    }

    extension(UserInfo ui)
    {
        public bool IsLDAP()
        {
            if (ui == null)
            {
                return false;
            }

            return !string.IsNullOrEmpty(ui.Sid);
        }

        public bool IsSSO()
        {
            if (ui == null)
            {
                return false;
            }

            return !string.IsNullOrEmpty(ui.SsoNameId);
        }
    }

    // ReSharper disable once InconsistentNaming

    extension(UserManager userManager)
    {
        public async Task<EmployeeType> GetUserTypeAsync(Guid id)
        {
            if (id.Equals(Constants.LostUser.Id))
            {
                return EmployeeType.Guest;
            }

            return
                await userManager.IsDocSpaceAdminAsync(id) ? EmployeeType.DocSpaceAdmin :
                await userManager.IsGuestAsync(id) ? EmployeeType.Guest :
                await userManager.IsUserAsync(id) ? EmployeeType.User :
                EmployeeType.RoomAdmin;
        }

        public Task<EmployeeType> GetUserTypeAsync(UserInfo user)
        {
            return userManager.GetUserTypeAsync(user.Id);
        }

        public async Task<bool> CanUserViewAnotherUserAsync(Guid sourceUserId, Guid targetUserId)
        {
            if (sourceUserId == targetUserId)
            {
                return true;
            }

            var sourceUserType = await userManager.GetUserTypeAsync(sourceUserId);

            if (sourceUserType is EmployeeType.DocSpaceAdmin)
            {
                return true;
            }

            if (sourceUserType is EmployeeType.User or EmployeeType.Guest)
            {
                return false;
            }

            var targetUserType = await userManager.GetUserTypeAsync(targetUserId);

            if (targetUserType is EmployeeType.Guest)
            {
                var userRelations = await userManager.GetUserRelationsAsync(sourceUserId);

                return userRelations.ContainsKey(targetUserId);
            }

            return true;
        }

        public async Task<bool> CanUserShareAnotherUserAsync(Guid sourceUserId, Guid targetUserId)
        {
            if (sourceUserId == targetUserId)
            {
                return true;
            }

            var sourceUserType = await userManager.GetUserTypeAsync(sourceUserId);

            if (sourceUserType is EmployeeType.DocSpaceAdmin)
            {
                return true;
            }

            if (sourceUserType is EmployeeType.Guest)
            {
                return false;
            }

            var targetUserType = await userManager.GetUserTypeAsync(targetUserId);

            if (targetUserType is EmployeeType.Guest)
            {
                var userRelations = await userManager.GetUserRelationsAsync(sourceUserId);

                return userRelations.ContainsKey(targetUserId);
            }

            return true;
        }
    }

    private const string _extMobPhone = "extmobphone";
    private const string _mobPhone = "mobphone";
    private const string _extMail = "extmail";
    private const string _mail = "mail";

    public static void ConvertExternalContactsToOrdinary(this UserInfo ui)
    {
        var ldapUserContacts = ui.ContactsList;

        if (ui.ContactsList == null)
        {
            return;
        }

        var newContacts = new List<string>();

        for (int i = 0, m = ldapUserContacts.Count; i < m; i += 2)
        {
            if (i + 1 >= ldapUserContacts.Count)
            {
                continue;
            }

            var type = ldapUserContacts[i];
            var value = ldapUserContacts[i + 1];

            switch (type)
            {
                case _extMobPhone:
                    newContacts.Add(_mobPhone);
                    break;
                case _extMail:
                    newContacts.Add(_mail);
                    break;
                default:
                    newContacts.Add(type);
                    break;
            }

            newContacts.Add(value);
        }

        ui.ContactsList = newContacts;
    }
}
