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
/// The request parameters for the user with the room/folder/file sharing settings.
/// </summary>
public class UsersWithFileEntitySharedRequestDto<T>
{
    /// <summary>
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// The user status.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "employeeStatus")]
    public EmployeeStatus? EmployeeStatus { get; set; }

    /// <summary>
    /// The user activation status.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "activationStatus")]
    public EmployeeActivationStatus? ActivationStatus { get; set; }

    /// <summary>
    /// Specifies whether to exclude the user sharing settings or not.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "excludeShared")]
    public bool? ExcludeShared { get; set; }

    /// <summary>
    /// Specifies whether to include the user sharing settings or not.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "includeShared")]
    public bool? IncludeShared { get; set; }

    /// <summary>
    /// Specifies whether the user was invited by the current user or not.
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
    /// The user area.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "area")]
    public Area Area { get; set; } = Area.All;

    /// <summary>
    /// The list of user types.
    /// </summary>
    /// <example>[1, 2]</example>
    [FromQuery(Name = "employeeTypes")]
    public IEnumerable<EmployeeType> EmployeeTypes { get; set; } = new List<EmployeeType>();

    /// <summary>
    /// The maximum number of users to be retrieved in the request.
    /// </summary>
    /// <example>25</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The zero-based index of the first record to retrieve in a paged query.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }

    /// <summary>
    /// The character or string used to separate multiple filter values in a filtering query.
    /// </summary>
    /// <example>,</example>
    /// <remarks>
    /// This property defines the delimiter applied when multiple filter criteria are provided.
    /// It allows the request to parse and handle multiple filtering values effectively.
    /// </remarks>
    [FromQuery(Name = "filterSeparator")]
    public string FilterSeparator { get; set; }

    /// <summary>
    /// The filter text value used for searching or filtering user results.
    /// </summary>
    /// <example>John</example>
    [FromQuery(Name = "filterValue")]
    public string Text { get; set; }
}