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


namespace ASC.Web.Studio.Wallet;

internal static class WalletPayerExtensions
{
    /// <summary>
    /// The portal user whose e-mail is the billing customer's, when that user can still act on the portal, or null.
    /// A disabled user cannot be signed in as - the account resolves to a guest and authentication throws - and
    /// can no longer act on what the wallet letters ask for, so the wallet services treat them like a billing
    /// e-mail that matches nobody: they run as the portal owner and write to the owner alone.
    /// </summary>
    public static async Task<UserInfo> GetActivePayerAsync(this UserManager userManager, CustomerInfo customerInfo)
    {
        if (string.IsNullOrEmpty(customerInfo?.Email))
        {
            return null;
        }

        var payer = await userManager.GetUserByEmailAsync(customerInfo.Email);

        return payer.Id != ASC.Core.Users.Constants.LostUser.Id && payer.Status == EmployeeStatus.Active ? payer : null;
    }
}
