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

namespace ASC.AI.Models.RequestDto;

/// <summary>
/// Request to change the enabled/disabled state of an MCP server.
/// </summary>
public class SetServerStatusRequestDto
{
    /// <summary>
    /// Unique identifier of the MCP server whose status is being changed.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "id")]
    public Guid Id { get; set; }

    /// <summary>
    /// New status value.
    /// </summary>
    /// <example>{"enabled": true}</example>
    [FromBody]
    public required SetServerStatusRequestBody Body { get; set; }
}

/// <summary>
/// Parameters for toggling the MCP server status.
/// </summary>
public class SetServerStatusRequestBody
{
    /// <summary>
    /// Set to true to enable the server (making it available for room assignment), or false to disable it.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }
}