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
/// Whether a confirmation link may still be used, and what it leads to when it invites into a room.
/// </summary>
public class ConfirmDto
{
    /// <summary>
    /// The outcome of the check. Only `Ok` means the action behind the link may be carried out: `Invalid` and
    /// `Expired` fault the key itself, while `UserExisted`, `UserExcluded`, `TariffLimit` and `QuotaFailed` mean
    /// the key is sound but the invitation behind it cannot be accepted as it stands.
    /// </summary>
    /// <example>0</example>
    public required ValidationResult Result { get; set; }

    /// <summary>
    /// The room the invitation leads into - a numeric folder ID for a room of the portal, a provider-specific
    /// string for a third-party one. It is empty for an invitation to the portal as a whole, for a room that has
    /// been removed or that the invited account may not see, and whenever `result` is neither `Ok` nor
    /// `UserExisted`.
    /// </summary>
    /// <example>1</example>
    public string RoomId { get; set; }

    /// <summary>
    /// The title of that room, present exactly when `roomId` is and meant to be shown on the confirmation page.
    /// </summary>
    /// <example>Conference Room</example>
    public string Title { get; set; }

    /// <summary>
    /// The address the link was issued for, echoed back only when `result` is `Ok` so that a sign-up form can be
    /// prefilled with it. Every other outcome leaves it empty, `UserExisted` included.
    /// </summary>
    /// <example>user@example.com</example>
    public string Email { get; set; }

    /// <summary>
    /// Whether the room behind the link is an AI room rather than an ordinary one, which decides where the invited
    /// person is taken. It is `false` whenever `roomId` is empty.
    /// </summary>
    /// <example>true</example>
    public bool IsAgent { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class ConfirmDtoMapper
{
    public static partial ConfirmDto Map(this Validation source);
}