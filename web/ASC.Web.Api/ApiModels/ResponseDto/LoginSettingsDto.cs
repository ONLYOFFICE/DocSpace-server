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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The brute-force protection of the sign-in form: how many failures, over how long, cost how long a block.
/// </summary>
public class LoginSettingsDto
{
    /// <summary>
    /// How many failed attempts inside one window are tolerated before the offender is blocked. Attempts are
    /// counted per user name and client address together, so one member being blocked leaves the rest of the
    /// portal signing in normally.
    /// </summary>
    /// <example>5</example>
    public required int AttemptCount { get; set; }

    /// <summary>
    /// How long, in seconds, a blocked user name and address pair stays refused. While the block lasts the
    /// sign-in is refused even once the password is correct.
    /// </summary>
    /// <example>15</example>
    public required int BlockTime { get; set; }

    /// <summary>
    /// The length, in seconds, of the rolling window the failures are counted over. It is not a request timeout: a
    /// wider window makes the same `attemptCount` stricter, because failures further apart still add up.
    /// </summary>
    /// <example>60</example>
    public required int CheckPeriod { get; set; }

    /// <summary>
    /// Whether the three numbers above still match the ones the installation ships with. It turns `false` as soon
    /// as any of them is saved differently, and `true` again after
    /// `DELETE api/2.0/settings/security/loginsettings`.
    /// </summary>
    /// <example>false</example>
    public required bool IsDefault { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class LoginSettingsDtoMapper
{
    public static partial LoginSettingsDto Map(this LoginSettings source);
}