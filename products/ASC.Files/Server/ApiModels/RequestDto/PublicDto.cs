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
/// The address of the room template whose public access is read.
/// </summary>
public class PublicDto
{
    /// <summary>
    /// The identifier of the room template. Take it from `templateId` of `GET api/2.0/files/roomtemplate/status`, or
    /// from the folder list of `GET api/2.0/files/rooms` called with `searchArea` set to 4; an identifier of an
    /// ordinary room is not accepted.
    /// </summary>
    /// <example>1234</example>
    [FromRoute(Name = "id")]
    [Range(1, int.MaxValue)]
    public required int Id { get; set; }
}

/// <summary>
/// The public access to set on a room template.
/// </summary>
public class SetPublicDto
{
    /// <summary>
    /// The identifier of the room template. Take it from `templateId` of `GET api/2.0/files/roomtemplate/status`, or
    /// from the folder list of `GET api/2.0/files/rooms` called with `searchArea` set to 4; an identifier of an
    /// ordinary room is not accepted.
    /// </summary>
    /// <example>1234</example>
    [Range(1, int.MaxValue)]
    public required int Id { get; set; }

    /// <summary>
    /// Whether the Everyone group keeps read access to the template. True shares it with every member allowed to
    /// create rooms; false leaves it reachable only for its owner.
    /// </summary>
    /// <example>true</example>
    public bool Public { get; set; }
}