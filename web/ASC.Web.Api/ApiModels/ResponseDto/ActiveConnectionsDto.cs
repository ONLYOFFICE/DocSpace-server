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
/// The connections the calling user currently has open, and which of them the request itself was made with.
/// </summary>

public class ActiveConnectionsDto
{
    /// <summary>
    /// The `id` of the item in `items` that the current request is authenticated by. It is `0` when the request
    /// carried a token in the `Authorization` header instead of the portal cookie, and in that case none of the
    /// items is the current connection.
    /// </summary>
    /// <example>1</example>
    public required int LoginEvent { get; set; }

    /// <summary>
    /// One item per sign-in of the caller that is still active, ordered newest sign-in first, with the connection
    /// the request itself uses moved to the front. Sign-ins older than a year are left out, and a caller with no
    /// stored connection gets a single item describing the current request rather than an empty list.
    /// </summary>
    /// <example>[{"id": 1234, "ip": "192.0.2.1"}]</example>
    public List<ActiveConnectionsItemDto> Items { get; set; }
}

/// <summary>
/// One open connection of a user: where the sign-in behind it came from, and the ID it can be closed by.
/// </summary>
public class ActiveConnectionsItemDto
{
    /// <summary>
    /// The ID of the sign-in this connection was opened by. Pass it as `loginEventId` to
    /// `PUT api/2.0/security/activeconnections/logout/{loginEventId}` to end this one connection; the item whose
    /// value equals `loginEvent` is the connection the current request uses.
    /// </summary>
    /// <example>1</example>
    public required int Id { get; set; }

    /// <summary>
    /// The portal the sign-in was made on. The operation never crosses portals, so it is the current one on every
    /// item.
    /// </summary>
    /// <example>1</example>
    public required int TenantId { get; set; }

    /// <summary>
    /// The user the connection belongs to, which is the calling user on every item - the operation cannot report
    /// anyone else's connections.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Whether the sign-in came from a mobile client. No mobile marker is stored with a connection, so the value
    /// is `false` on every item and tells a caller nothing about the device.
    /// </summary>
    /// <example>true</example>
    public bool Mobile { get; set; }

    /// <summary>
    /// The IP address the sign-in came from, with the port stripped off. On the item that matches `loginEvent` it
    /// is taken from the address the current request arrives from instead of the one stored at sign-in.
    /// </summary>
    /// <example>192.0.2.1</example>
    public string Ip { get; set; }

    /// <summary>
    /// The English name of the country the IP address is located in. It is empty when the address cannot be
    /// located, which is the normal outcome for private and loopback addresses.
    /// </summary>
    /// <example>United States</example>
    public string Country { get; set; }

    /// <summary>
    /// The city the IP address is located in, empty under the same conditions as `country`.
    /// </summary>
    /// <example>New York</example>
    public string City { get; set; }

    /// <summary>
    /// The browser and its version as parsed from the user agent of the sign-in, empty when the client sent no
    /// recognisable one. It is refreshed from the current request on the item that matches `loginEvent`.
    /// </summary>
    /// <example>Chrome 120.0</example>
    public string Browser { get; set; }

    /// <summary>
    /// The operating system as parsed from the user agent of the sign-in, refreshed and left empty under the same
    /// conditions as `browser`.
    /// </summary>
    /// <example>Windows</example>
    public string Platform { get; set; }

    /// <summary>
    /// When the sign-in happened, in the portal time zone rather than in UTC.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public ApiDateTime Date { get; set; }

    /// <summary>
    /// Where in the portal the sign-in was made from: the referrer of the request that created it, or that
    /// request's own path when it carried no referrer. Long values are cut off at 512 characters.
    /// </summary>
    /// <example>/rooms/shared</example>
    public string Page { get; set; }
}