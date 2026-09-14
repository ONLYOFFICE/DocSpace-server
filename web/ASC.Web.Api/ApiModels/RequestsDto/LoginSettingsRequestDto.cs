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

namespace ASC.Web.Api.ApiModels.RequestsDto;

/// <summary>
/// The brute-force protection of the sign-in form: how many failures, over how long, cost how long a block.
/// </summary>
/// <example>
/// {
///   "attemptCount": 1,
///   "blockTime": 1,
///   "checkPeriod": 1
/// }
/// </example>
public class LoginSettingsRequestDto
{
    /// <summary>
    /// How many failed sign-in attempts inside one window are tolerated before the offender is blocked. Attempts are
    /// counted per user name and client address together, so one member being blocked leaves the rest of the portal
    /// signing in normally.
    /// </summary>
    /// <example>1</example>
    [Range(1, 9999)]
    public int AttemptCount { get; set; }

    /// <summary>
    /// How long, in seconds, a blocked user name and address pair stays refused. While the block lasts the sign-in
    /// is refused even when the password is finally correct.
    /// </summary>
    /// <example>1</example>
    [Range(1, 9999)]
    public int BlockTime { get; set; }

    /// <summary>
    /// The length, in seconds, of the rolling window the failed attempts are counted over. A wider window makes the
    /// same `attemptCount` stricter, because failures further apart still add up.
    /// </summary>
    /// <example>1</example>
    [Range(1, 9999)]
    public int CheckPeriod { get; set; }
}
