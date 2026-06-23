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
/// The request parameters for querying user login events within the specified time range.
/// </summary>
/// <example>
/// {
///   "userId": {},
///   "action": "EnumValue",
///   "from": "2024-01-15T10:30:00Z",
///   "to": "2024-01-15T10:30:00Z",
///   "count": 1,
///   "startIndex": 1
/// }
/// </example>
public class LoginEventRequestDto
{
    /// <summary>
    /// The ID of the user whose login events are being queried.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "userId")]
    public Guid UserId { get; set; }

    /// <summary>
    /// The login-related action to filter events by.
    /// </summary>
    /// <example>FileCreated</example>
    [FromQuery(Name = "action")]
    public MessageAction Action { get; set; }

    /// <summary>
    /// The starting date and time for filtering login events.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    [FromQuery(Name = "from")]
    public ApiDateTime From { get; set; }

    /// <summary>
    /// The ending date and time for filtering login events.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    [FromQuery(Name = "to")]
    public ApiDateTime To { get; set; }

    /// <summary>
    /// The number of login events to retrieve in the query.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The starting index for fetching a subset of login events from the query results.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }
}