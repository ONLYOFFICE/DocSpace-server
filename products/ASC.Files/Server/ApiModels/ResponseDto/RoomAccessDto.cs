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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>The outcome of a change of the room membership.</summary>
public class RoomSecurityDto
{
    /// <summary>
    /// The access entries of the subjects named in the request, read back after the change was applied. A subject the
    /// caller may not see is missing from it, so comparing this list with the request is the way to learn who was
    /// skipped; it is null when nothing was applied at all.
    /// </summary>
    /// <example>[{"access": 10, "isOwner": false, "subjectType": 0}]</example>
    public IEnumerable<FileShareDto> Members { get; set; }

    /// <summary>
    /// The reason the first subject that could not be handled was skipped, in the language of the request, while the
    /// rest of the list was still applied. Null when every named subject went through. The text is meant to be shown
    /// to a person, not matched against.
    /// </summary>
    /// <example>The maximum number of links is 10</example>
    public string Warning { get; set; }

    /// <summary>
    /// Reports the one case in which nothing at all was changed: a member being removed still holds a role in a form
    /// of the room, and the request did not ask to remove them anyway. Repeat the call with `force` to remove them
    /// together with the role.
    /// </summary>
    /// <example>1</example>
    public RoomSecurityError Error { get; set; }
}

/// <summary>
/// The error type.
/// </summary>
public enum RoomSecurityError
{
    [Description("None")]
    None,

    [Description("Form role blocking deletion")]
    FormRoleBlockingDeletion
}