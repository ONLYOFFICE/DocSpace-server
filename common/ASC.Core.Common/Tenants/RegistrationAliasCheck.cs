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

namespace ASC.Core.Tenants;

/// <summary>
/// Tells, after a registration failed, whether the portal found under the requested alias belongs to another
/// registration or to this one.
/// </summary>
public static class RegistrationAliasCheck
{
    /// <summary>
    /// The tenant, its owner and the password hash are inserted in one transaction, and work after that insert (the
    /// settings of the new portal, a commit the execution strategy repeats after a lost answer) can still fail. Then
    /// the alias is found with the owner this registration asked for, and the failure is the server's, not a taken
    /// name. A portal under the alias with another owner is a name another registration took. When the owner of the
    /// found portal is not known, nothing shows that another registration took the name, so it is not reported as
    /// taken either. A second request of the same registration (same alias, same owner email) that loses the race to
    /// the insert is not told apart from this registration's own portal and is not reported as taken.
    /// </summary>
    /// <param name="found">The portal found under the alias, or null.</param>
    /// <param name="alias">The alias this registration asked for.</param>
    /// <param name="foundOwnerEmail">The email of the owner of <paramref name="found"/>, or null when it is not known.</param>
    /// <param name="requestedEmail">The owner email this registration asked for.</param>
    public static bool IsTakenByAnother(Tenant found, string alias, string foundOwnerEmail, string requestedEmail)
    {
        if (found == null || !string.Equals(found.Alias, alias?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(foundOwnerEmail))
        {
            return false;
        }

        return !string.Equals(foundOwnerEmail.Trim(), requestedEmail?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
