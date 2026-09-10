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

namespace ASC.Files.Core.ApiModels.RequestDto;

/// <summary>
/// The complete external sharing policy of the portal. Every field is written, so an omitted one is stored as
/// false.
/// </summary>
public class ExternalSharingSettingsRequestDto
{
    /// <summary>
    /// Whether links that open a file or a room without a portal account may be created at all. This is the master
    /// switch of the policy: while it is false the portal keeps the default link type internal, turns sharing on
    /// social networks off, and applies the three restriction fields below.
    /// </summary>
    /// <example>true</example>
    public bool ExternalShare { get; set; }

    /// <summary>
    /// The kind of link offered first when a new one is created: true offers a link only accounts of this portal can
    /// open, false one that anyone holding it can open. The portal keeps it at true while external sharing is
    /// switched off.
    /// </summary>
    /// <example>false</example>
    public bool DefaultShareLinkInternal { get; set; }

    /// <summary>
    /// Whether the restriction reaches personal documents: with true, no external link can be created for an entry in
    /// the caller's own documents while external sharing is off. It has no effect while external sharing is allowed.
    /// </summary>
    /// <example>true</example>
    public bool ExternalShareApplyToDocuments { get; set; }

    /// <summary>
    /// Whether the restriction reaches rooms: with true, no external link can be created for a room or its content
    /// while external sharing is off, and a new room cannot be made public. It has no effect while external sharing
    /// is allowed.
    /// </summary>
    /// <example>true</example>
    public bool ExternalShareApplyToRooms { get; set; }

    /// <summary>
    /// What happens to the links that already exist once external sharing is switched off: with true they stop
    /// opening for the sections named above, with false they keep working and only new ones are refused. This is the
    /// field that changes access to data that is already shared.
    /// </summary>
    /// <example>true</example>
    public bool BlockExistingLinksOnRestrict { get; set; }
}
