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
/// The active connections parameters.
/// </summary>

public class ActiveConnectionsDto
{
    /// <summary>
    /// The login event.
    /// </summary>
    /// <example>1</example>
    public required int LoginEvent { get; set; }

    /// <summary>
    /// The list of active connection items.
    /// </summary>
    /// <example>[{"id": "conn1", "ip": "192.168.1.1"}]</example>
    public List<ActiveConnectionsItemDto> Items { get; set; }
}

/// <summary>
/// The active connection item parameters.
/// </summary>
public class ActiveConnectionsItemDto
{
    /// <summary>
    /// The active connection ID.
    /// </summary>
    /// <example>1</example>
    public required int Id { get; set; }

    /// <summary>
    /// The tenant ID.
    /// </summary>
    /// <example>1</example>
    public required int TenantId { get; set; }

    /// <summary>
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Specifies if the active connection has a mobile phone or not.
    /// </summary>
    /// <example>true</example>
    public bool Mobile { get; set; }

    /// <summary>
    /// The IP address of the active connection.
    /// </summary>
    /// <example>192.0.2.1</example>
    public string Ip { get; set; }

    /// <summary>
    /// The active connection country.
    /// </summary>
    /// <example>United States</example>
    public string Country { get; set; }

    /// <summary>
    /// The active connection city.
    /// </summary>
    /// <example>New York</example>
    public string City { get; set; }

    /// <summary>
    /// The active connection browser.
    /// </summary>
    /// <example>Chrome 120.0</example>
    public string Browser { get; set; }

    /// <summary>
    /// The active connection platform.
    /// </summary>
    /// <example>Windows</example>
    public string Platform { get; set; }

    /// <summary>
    /// The active connection date.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime Date { get; set; }

    /// <summary>
    /// The active connection page.
    /// </summary>
    /// <example>/rooms/shared</example>
    public string Page { get; set; }
}