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
/// The rooms and files to hand over, together with the account that takes them.
/// </summary>
public class ChangeOwnerRequestDto
{
    /// <summary>
    /// The rooms to hand over, identified as `GET api/2.0/files/rooms` returns them - a number for a room stored on
    /// the portal and a string for one that lives on a connected third-party account. Only rooms belong here; a
    /// folder inside a room is refused.
    /// </summary>
    /// <example>[1, 2, 3]</example>
    public IEnumerable<JsonElement> FolderIds { get; set; } = new List<JsonElement>();

    /// <summary>
    /// The files to hand over, identified as a listing operation returns them - a number for a file stored on the
    /// portal and a string for one on a connected third-party account. Only a file kept in the portal's common
    /// section is accepted.
    /// </summary>
    /// <example>[7, 8]</example>
    public IEnumerable<JsonElement> FileIds { get; set; } = new List<JsonElement>();

    /// <summary>
    /// The account that becomes the owner of every listed entry. It has to be an active member allowed to manage
    /// rooms, so a deactivated account, a guest or a plain member is rejected, and for a private room the account
    /// must have set up its encryption keys beforehand.
    /// </summary>
    /// <example>9924256a-739c-462b-af15-e652a3b1b6eb</example>
    public required Guid UserId { get; set; }
}