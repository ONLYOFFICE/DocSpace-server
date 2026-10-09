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

/// <summary>
/// A portal member the editor can offer when the author types a mention: who they are, where the notification goes
/// and how to show them in the suggestion list.
/// </summary>
public class MentionDto
{
    /// <summary>
    /// The account itself, as the portal stores it.
    /// </summary>
    /// <example>{"id": "00000000-0000-0000-0000-000000000000", "firstName": "John", "lastName": "Doe"}</example>
    public PortalUserDto User { get; init; }

    /// <summary>
    /// Where a mention notification for this user is delivered.
    /// </summary>
    /// <example>user@example.com</example>
    [EmailAddress]
    public string Email { get; init; }

    /// <summary>
    /// The account id as text, the same value the account object carries; it is what identifies the user in a sharing
    /// request built from this list.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public string Id { get; init; }

    /// <summary>
    /// An absolute address of the medium-sized avatar. A generated default avatar is reported when the user never
    /// uploaded one, so the field is never empty.
    /// </summary>
    /// <example>https://portal.example.com/avatar/user_0001.png</example>
    public string Image { get; init; }

    /// <summary>
    /// Not filled in by the operations that return this list: it always comes back false. Whether a user can already
    /// open the document has to be read from the sharing settings of the file.
    /// </summary>
    /// <example>false</example>
    public bool HasAccess { get; init; }

    /// <summary>
    /// The name to display, assembled the way the portal is configured to show names.
    /// </summary>
    /// <example>John Doe</example>
    public string Name { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
[UseStaticMapper(typeof(PortalUserDtoMapper))]
public static partial class MentionDtoMapper
{
    public static partial MentionDto Map(this MentionWrapper source);
}
