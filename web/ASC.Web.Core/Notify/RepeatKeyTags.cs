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

namespace ASC.Web.Studio.Core.Notify;

/// <summary>
/// The values letters put into the tags their repeat check keys on. Init fills the tags with them, and the sending
/// code passes the same values when it asks beforehand whether the letter would be a repeat, so the two keys cannot
/// drift apart.
/// </summary>
public static class RepeatKeyTags
{
    /// <summary>The account type an invitation link lets the invitee join with.</summary>
    public static string InviteType(EmployeeType inviteType)
    {
        return inviteType.ToStringFast();
    }

    /// <summary>The address the confirmation links of a letter to the portal owner are checked against.</summary>
    public static string LinkAddress(UserInfo owner)
    {
        return owner.Email?.ToLowerInvariant();
    }
}
