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
/// The request parameters for filtering and retrieving audit event records.
/// </summary>
public class AuditEventRequestDto
{
    /// <summary>
    /// The ID of the user who triggered the audit event.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    [FromQuery(Name = "userId")]
    public Guid UserId { get; set; }

    /// <summary>
    /// The location where the audit event occurred.
    /// </summary>
    /// <example>Files</example>
    [FromQuery(Name = "moduleType")]
    public LocationType LocationType { get; set; }

    /// <summary>
    /// The type of action performed in the audit event (e.g., Create, Update, Delete).
    /// </summary>
    /// <example>Create</example>
    [FromQuery(Name = "actionType")]
    public ActionType ActionType { get; set; }

    /// <summary>
    /// The specific action that occurred within the audit event.
    /// </summary>
    /// <example>FileCreated</example>
    [FromQuery(Name = "action")]
    public MessageAction Action { get; set; }

    /// <summary>
    /// The type of audit entry (e.g., Folder, User, File).
    /// </summary>
    /// <example>File</example>
    [FromQuery(Name = "entryType")]
    public EntryType EntryType { get; set; }

    /// <summary>
    /// The target object affected by the audit event (e.g., document ID, user account).
    /// </summary>
    /// <example>document.docx</example>
    [FromQuery(Name = "target")]
    public string Target { get; set; }

    /// <summary>
    /// The starting date and time for filtering audit events.
    /// </summary>
    /// <example>2024-01-01T00:00:00Z</example>
    [FromQuery(Name = "from")]
    public ApiDateTime From { get; set; }

    /// <summary>
    /// The ending date and time for filtering audit events.
    /// </summary>
    /// <example>2024-01-31T23:59:59Z</example>
    [FromQuery(Name = "to")]
    public ApiDateTime To { get; set; }

    /// <summary>
    /// The maximum number of audit event records to retrieve.
    /// </summary>
    /// <example>100</example>
    [FromQuery(Name = "count")]
    [Range(1, ApiContext.MaxCount)]
    public int Count { get; set; } = ApiContext.DefaultCount;

    /// <summary>
    /// The index of the first audit event record to retrieve in a paged query.
    /// </summary>
    /// <example>0</example>
    [FromQuery(Name = "startIndex")]
    public int StartIndex { get; set; }
}