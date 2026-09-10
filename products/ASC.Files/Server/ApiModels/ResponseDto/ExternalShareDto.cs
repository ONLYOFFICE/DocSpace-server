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

using Status = ASC.Files.Core.Security.Status;

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The outcome of validating an external share link and the entry it points at.
/// </summary>
public class ExternalShareDto
{
    /// <summary>
    /// How validating the link went. It is the first field to read: a refused link is reported here with the answer
    /// still arriving as a success. A link that resolved describes both the entry and the link, one that is waiting
    /// for its password describes only the entry, and one that failed outright leaves the rest of the object empty.
    /// </summary>
    /// <example>0</example>
    public required Status Status { get; set; }

    /// <summary>
    /// The identifier of the room, folder or file the link points at, always rendered as a string even where the
    /// portal stores it as a number. It is null when the link could not be resolved.
    /// </summary>
    /// <example>42</example>
    public string Id { get; set; }

    /// <summary>
    /// The title of the entry the link points at, suitable for showing to the visitor before they are let in. It is
    /// null when the link could not be resolved.
    /// </summary>
    /// <example>Project documents</example>
    public string Title { get; set; }

    /// <summary>
    /// Whether the link points at a folder - a room counts as one - or at a single file. It is null when the link
    /// could not be resolved.
    /// </summary>
    /// <example>1</example>
    public FileEntryType? Type { get; set; }

    /// <summary>
    /// The portal the link belongs to, which matters for a client that works with more than one. It stays 0 for a
    /// link that did not resolve.
    /// </summary>
    /// <example>1</example>
    public required int TenantId { get; set; }

    /// <summary>
    /// The identifier of the entry that was asked about through the request's file or folder parameter, echoed back
    /// once it was found under the link's target. It is null when nothing was asked about, or when the entry lies
    /// outside what the link opens.
    /// </summary>
    /// <example>9</example>
    public string EntityId { get; set; }

    /// <summary>
    /// The title of that entry, null under the same conditions as its identifier.
    /// </summary>
    /// <example>Contract.docx</example>
    public string EntityTitle { get; set; }

    /// <summary>
    /// Whether that entry is a folder or a file, null under the same conditions as its identifier.
    /// </summary>
    /// <example>2</example>
    public FileEntryType? EntityType { get; set; }

    /// <summary>
    /// True when the link opens a whole room rather than one entry inside it. It is null for a link to a file and for
    /// a link that did not resolve.
    /// </summary>
    /// <example>true</example>
    public bool? IsRoom { get; set; }

    /// <summary>
    /// True when the entry now sits in the calling account's own lists - it was already shared with that account, or
    /// resolving the link has just put it there. It stays false for a visitor browsing without an account, who
    /// reaches the entry through the link alone.
    /// </summary>
    /// <example>true</example>
    public required bool Shared { get; set; }

    /// <summary>
    /// The link the token belongs to, which is also the subject under which the link appears among the sharing rights
    /// of the entry. It is an empty identifier when the link did not resolve.
    /// </summary>
    /// <example>b3a1f0c7-5d2e-4a19-9f38-71c6e0d4b852</example>
    public required Guid LinkId { get; set; }

    /// <summary>
    /// Whether the request carried a signed-in account. It says nothing about that account's rights on the entry, so
    /// it must not be read as permission - it is false for every anonymous visitor and true for any member, even one
    /// who is a stranger to the room.
    /// </summary>
    /// <example>true</example>
    public required bool IsAuthenticated { get; set; }

    /// <summary>
    /// Whether the signed-in caller already has rights of their own on the room that holds the entry, as opposed to
    /// reaching it through this link. It is false for an anonymous visitor and for a member who has never been
    /// invited.
    /// </summary>
    /// <example>false</example>
    public bool IsRoomMember { get; set; }
}


[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class ExternalShareDtoMapper
{
    public static partial ExternalShareDto Map(this ValidationInfo source);
}