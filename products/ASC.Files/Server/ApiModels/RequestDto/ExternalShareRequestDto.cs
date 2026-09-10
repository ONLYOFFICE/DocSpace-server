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

namespace ASC.Files.ApiModels.RequestDto;

/// <summary>
/// The password that unlocks a protected external share link.
/// </summary>
public class ExternalShareRequestParam
{
    /// <summary>
    /// The password chosen by the member who shared the entry, spelled exactly as they typed it. It is compared
    /// against the stored value and never returned back; a mismatch is reported through the answer's status instead
    /// of an error.
    /// </summary>
    /// <example>p@ssw0rd</example>
    public string Password { get; set; }
}

/// <summary>
/// The token of a protected external share link and the password to check against it.
/// </summary>
public class ExternalShareRequestDto
{
    /// <summary>
    /// The token of the external share link, taken verbatim from the `requestToken` of a link returned by the link
    /// operations of an entry, such as `GET api/2.0/files/rooms/{id}/link`. It is an opaque URL-safe string that
    /// carries the link's own identifier, so it cannot be assembled by hand.
    /// </summary>
    /// <example>q7Ry8cQ1lZ0dP3sK2mXfA9tBnV6hJ4uE8wCz5oLg</example>
    [FromRoute(Name = "key")]
    public required string Key { get; set; }

    /// <summary>
    /// The body of the request, holding the password to check.
    /// </summary>
    [FromBody]
    public required ExternalShareRequestParam RequestParam { get; set; }
}