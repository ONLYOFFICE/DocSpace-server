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

namespace ASC.ActiveDirectory.ComplexOperations.Data;

[Scope]
public class LdapChangeCollection(UserFormatter userFormatter) : List<LdapChange>
{
    #region User

    public void SetSkipUserChange(UserInfo user)
    {
        var change = new LdapChange(user.Sid,
            userFormatter.GetUserName(user),
            user.Email,
            LdapChangeType.User, LdapChangeAction.Skip);

        Add(change);
    }

    public void SetSaveAsPortalUserChange(UserInfo user)
    {
        var fieldChanges = new List<LdapItemChange>
            {
                new(LdapItemChangeKey.Sid, user.Sid, null)
            };

        var change = new LdapChange(user.Sid,
            userFormatter.GetUserName(user),
            user.Email, LdapChangeType.User, LdapChangeAction.SaveAsPortal, fieldChanges);

        Add(change);
    }

    public void SetNoneUserChange(UserInfo user)
    {
        var change = new LdapChange(user.Sid,
                    userFormatter.GetUserName(user), user.Email,
                    LdapChangeType.User, LdapChangeAction.None);

        Add(change);
    }

    public void SetUpdateUserChange(UserInfo beforeUserInfo, UserInfo afterUserInfo, ILogger log = null)
    {
        var fieldChanges =
                        LdapUserMapping.Fields.Select(field => GetPropChange(field, beforeUserInfo, afterUserInfo, log))
                            .Where(pch => pch != null)
                            .ToList();

        var change = new LdapChange(beforeUserInfo.Sid,
            userFormatter.GetUserName(afterUserInfo), afterUserInfo.Email,
            LdapChangeType.User, LdapChangeAction.Update, fieldChanges);

        Add(change);
    }

    public void SetAddUserChange(UserInfo user, ILogger log = null)
    {
        var fieldChanges =
                    LdapUserMapping.Fields.Select(field => GetPropChange(field, after: user, log: log))
                        .Where(pch => pch != null)
                        .ToList();

        var change = new LdapChange(user.Sid,
            userFormatter.GetUserName(user), user.Email,
            LdapChangeType.User, LdapChangeAction.Add, fieldChanges);

        Add(change);
    }

    public void SetRemoveUserChange(UserInfo user)
    {
        var change = new LdapChange(user.Sid,
                            userFormatter.GetUserName(user), user.Email,
                            LdapChangeType.User, LdapChangeAction.Remove);

        Add(change);
    }
    #endregion

    #region Group

    public void SetAddGroupChange(GroupInfo group, ILogger log = null)
    {
        var fieldChanges = new List<LdapItemChange>
                                    {
                                        new(LdapItemChangeKey.Name, null, group.Name),
                                        new(LdapItemChangeKey.Sid, null, group.Sid)
                                    };

        var change = new LdapChange(group.Sid, group.Name,
            LdapChangeType.Group, LdapChangeAction.Add, fieldChanges);

        Add(change);
    }

    public void SetAddGroupMembersChange(GroupInfo group, IEnumerable<UserInfo> members)
    {
        var fieldChanges = members.Select(member => new LdapItemChange(LdapItemChangeKey.Member, null, userFormatter.GetUserName(member))).ToList();

        var change = new LdapChange(group.Sid, group.Name, LdapChangeType.Group, LdapChangeAction.AddMember, fieldChanges);

        Add(change);
    }

    public void SetSkipGroupChange(GroupInfo group)
    {
        var change = new LdapChange(group.Sid, group.Name, LdapChangeType.Group, LdapChangeAction.Skip);

        Add(change);
    }

    public void SetUpdateGroupChange(GroupInfo group)
    {
        var fieldChanges = new List<LdapItemChange>
                                {
                                    new(LdapItemChangeKey.Name, group.Name, group.Name)
                                };

        var change = new LdapChange(group.Sid, group.Name,
            LdapChangeType.Group, LdapChangeAction.Update, fieldChanges);

        Add(change);
    }

    public void SetRemoveGroupChange(GroupInfo group, ILogger log = null)
    {
        var change = new LdapChange(group.Sid, group.Name,
                        LdapChangeType.Group, LdapChangeAction.Remove);

        Add(change);
    }

    public void SetRemoveGroupMembersChange(GroupInfo group, IEnumerable<UserInfo> members)
    {
        var fieldChanges = members.Select(member => new LdapItemChange(LdapItemChangeKey.Member, null, userFormatter.GetUserName(member))).ToList();

        var change = new LdapChange(group.Sid, group.Name, LdapChangeType.Group, LdapChangeAction.RemoveMember, fieldChanges);

        Add(change);
    }

    #endregion

    private static LdapItemChange GetPropChange(string propName, UserInfo before = null, UserInfo after = null, ILogger log = null)
    {
        try
        {
            var valueSrc = before != null ? before.GetType().GetProperty(propName).GetValue(before, null) as string : "";
            var valueDst = after != null ? after.GetType().GetProperty(propName).GetValue(before, null) as string : "";

            if (!Enum.TryParse(propName, out LdapItemChangeKey key))
            {
                throw new InvalidEnumArgumentException(propName);
            }

            var change = new LdapItemChange(key, valueSrc, valueDst);

            return change;
        }
        catch (Exception ex)
        {
            log?.ErrorCanNotGetSidProperty(propName, ex);
        }

        return null;
    }
}