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

namespace ASC.People.ApiModels.RequestDto;

/// <summary>
/// The filter request parameters.
/// </summary>
public class SimpleByFilterRequestDto
{
    /// <summary>
    /// The user status.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "employeeStatus")]
    public EmployeeStatus? EmployeeStatus { get; set; }

    /// <summary>
    /// The group ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "groupId")]
    public Guid? GroupId { get; set; }

    /// <summary>
    /// The user activation status.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "activationStatus")]
    public EmployeeActivationStatus? ActivationStatus { get; set; }

    /// <summary>
    /// The user type.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "employeeType")]
    public EmployeeType? EmployeeType { get; set; }

    /// <summary>
    /// The list of user types.
    /// </summary>
    /// <example>[1, 2]</example>
    [FromQuery(Name = "employeeTypes")]
    public EmployeeType[] EmployeeTypes { get; set; }

    /// <summary>
    /// Specifies if the user is an administrator or not.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "isAdministrator")]
    public bool? IsAdministrator { get; set; }

    /// <summary>
    /// The user payment status.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "payments")]
    public Payments? Payments { get; set; }

    /// <summary>
    /// The account login type.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "accountLoginType")]
    public AccountLoginType? AccountLoginType { get; set; }

    /// <summary>
    /// The quota filter (All - 0, Default - 1, Custom - 2).
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "quotaFilter")]
    public QuotaFilter? QuotaFilter { get; set; }

    /// <summary>
    /// Specifies whether the user should be a member of a group or not.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "withoutGroup")]
    public bool? WithoutGroup { get; set; }

    /// <summary>
    /// Specifies whether the user should be a member of the group with the specified ID.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "excludeGroup")]
    public bool? ExcludeGroup { get; set; }

    /// <summary>
    /// Specifies whether the user is invited by the current user or not.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "invitedByMe")]
    public bool? InvitedByMe { get; set; }

    /// <summary>
    /// The inviter ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "inviterId")]
    public Guid? InviterId { get; set; }

    /// <summary>
    /// The filter area.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "area")]
    public Area Area { get; set; } = Area.All;

    /// <summary>
    /// The maximum number of items to be retrieved in the response.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The zero-based index of the first item to be retrieved in a filtered result set.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// Specifies the property or field name by which the results should be sorted.
    /// </summary>
    /// <example>displayName</example>
    [FromQuery(Name = "sortBy")]
    public string SortBy { get; set; }

    /// <summary>
    /// The order in which the results are sorted.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "sortOrder")]
    public SortOrder SortOrder { get; set; }

    /// <summary>
    /// Represents the separator used to split filter criteria in query parameters.
    /// </summary>
    /// <example>,</example>
    [FromQuery(Name = "filterSeparator")]
    public string FilterSeparator { get; set; }

    /// <summary>
    /// The search text used to filter results based on user input.
    /// </summary>
    /// <example>John</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}