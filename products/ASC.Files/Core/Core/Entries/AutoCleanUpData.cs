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

namespace ASC.Files.Core;

/// <summary>
/// How long an item may stay in the trash before it is cleared.
/// </summary>
public enum DateToAutoCleanUp
{
    [Description("One week")]
    OneWeek = 1,

    [Description("Two weeks")]
    TwoWeeks,

    [Description("One month")]
    OneMonth,

    [Description("Thirty days")]
    ThirtyDays,

    [Description("Two months")]
    TwoMonths,

    [Description("Three months")]
    ThreeMonths
}

/// <summary>
/// The trash auto-clearing setting of an account.
/// </summary>
public class AutoCleanUpData
{
    /// <summary>
    /// Whether the trash of the account is cleared automatically. While it is false nothing is removed by the portal
    /// and the interval below is kept but unused.
    /// </summary>
    /// <example>false</example>
    public bool IsAutoCleanUp { get; init; }

    /// <summary>
    /// How long an item may stay in the trash before it is removed for good. It is reported even while clearing is
    /// off, and it is what the moment in the `autoDelete` field of a trashed entry is computed from.
    /// </summary>
    /// <example>4</example>
    public DateToAutoCleanUp Gap { get; init; }

    public static AutoCleanUpData GetDefault()
    {
        return new AutoCleanUpData
        {
            Gap = DateToAutoCleanUp.ThirtyDays,
            IsAutoCleanUp = true
        };
    }
}

[Scope]
public class FileDateTime(TenantUtil tenantUtil)
{
    public DateTime GetModifiedOnWithAutoCleanUp(DateTime modifiedOn, DateToAutoCleanUp date, bool utc = false)
    {
        var dateTime = modifiedOn;
        dateTime = date switch
        {
            DateToAutoCleanUp.OneWeek => dateTime.AddDays(7),
            DateToAutoCleanUp.TwoWeeks => dateTime.AddDays(14),
            DateToAutoCleanUp.OneMonth => dateTime.AddMonths(1),
            DateToAutoCleanUp.ThirtyDays => dateTime.AddDays(30),
            DateToAutoCleanUp.TwoMonths => dateTime.AddMonths(2),
            DateToAutoCleanUp.ThreeMonths => dateTime.AddMonths(3),
            _ => dateTime
        };

        return utc ? tenantUtil.DateTimeToUtc(dateTime) : dateTime;
    }
}