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
/// The filters that narrow the portal login history, and the window of the page returned from it.
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
    /// The user whose sign-in attempts are kept, given by portal user ID. Leave it at the empty GUID to keep the
    /// events of every user.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "userId")]
    public Guid UserId { get; set; }

    /// <summary>
    /// The sign-in action recorded, spelled as `GET api/2.0/security/audit/types` lists it under `actions` - a
    /// successful login, a failed one, a logout. The default value keeps every action.
    /// </summary>
    /// <example>FileCreated</example>
    [FromQuery(Name = "action")]
    public MessageAction Action { get; set; }

    /// <summary>
    /// The earliest moment an event may have been recorded at, read as a UTC instant. The `date` of the events that
    /// come back is in the portal time zone instead, so the two do not line up on a portal that is not on UTC.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    [FromQuery(Name = "from")]
    public ApiDateTime From { get; set; }

    /// <summary>
    /// The latest moment an event may have been recorded at, read as a UTC instant in the same way as `from`.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    [FromQuery(Name = "to")]
    public ApiDateTime To { get; set; }

    /// <summary>
    /// How many events one page may hold. The maximum is also the default, so a client that wants shorter pages has
    /// to ask for them.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// How many events to skip before the page begins, counting from the newest. It is applied to the log before
    /// the filters, so a page can hold fewer events than `count` while older matches still exist.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }
}
