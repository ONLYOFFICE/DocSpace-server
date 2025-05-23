﻿// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Files.Core;

/// <summary>
/// The period when the trash bin will be cleared.
/// </summary>
public enum DateToAutoCleanUp
{
    [SwaggerEnum(Description = "One week")]
    OneWeek = 1,

    [SwaggerEnum(Description = "Two weeks")]
    TwoWeeks,

    [SwaggerEnum(Description = "One month")]
    OneMonth,

    [SwaggerEnum(Description = "Thirty days")]
    ThirtyDays,

    [SwaggerEnum(Description = "Two months")]
    TwoMonths,

    [SwaggerEnum(Description = "Three months")]
    ThreeMonths
}

/// <summary>
/// The auto-clearing setting parameters.
/// </summary>
public class AutoCleanUpData
{
    /// <summary>
    /// Specifies whether to permanently delete files in the Trash folder.
    /// </summary>
    public bool IsAutoCleanUp { get; init; }

    /// <summary>
    /// The period when the trash bin will be cleared.
    /// </summary>
    public DateToAutoCleanUp Gap { get; init; }

    public static AutoCleanUpData GetDefault()
    {
        return new AutoCleanUpData {
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