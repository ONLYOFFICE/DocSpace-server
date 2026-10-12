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
/// The link of a room to create, change or revoke.
/// </summary>
public class RoomLinkRequest
{
    /// <summary>
    /// Which link to change, taken from `GET api/2.0/files/rooms/{id}/links`. Leaving it out creates a link, and an
    /// identifier the room does not know creates a link carrying that identifier.
    /// </summary>
    /// <example>b3f1c8de-5a64-4d1e-9f27-6c0a8d5b7e41</example>
    public Guid LinkId { get; set; }

    /// <summary>
    /// What whoever opens the link may do in the room. The value 0 revokes the link instead of changing it, and the
    /// levels a room accepts depend on its kind.
    /// </summary>
    /// <example>2</example>
    [EnumDataType(typeof(FileShare))]
    public FileShare Access { get; set; }

    /// <summary>
    /// When the link stops working, written with the offset of the portal time zone. A date already past is dropped
    /// silently for an external link and refused for an invitation link, and a date further ahead than the portal
    /// allows is refused as well; leaving it out means the link does not expire.
    /// </summary>
    /// <example>2026-12-31T23:59:59.0000000+03:00</example>
    public ApiDateTime ExpirationDate { get; set; }

    /// <summary>
    /// Whether the external link works only for people already signed in to the portal. With it off the link opens
    /// the room for anyone who has the address, subject to the password.
    /// </summary>
    /// <example>false</example>
    public bool Internal { get; set; }

    /// <summary>
    /// The name the link is shown under in the room. An empty value is accepted and the portal names the link itself,
    /// so the answer is what tells the caller the name in use.
    /// </summary>
    /// <example>Read-only access for auditors</example>
    [StringLength(255)]
    public string Title { get; set; }

    /// <summary>
    /// Which kind of link to create: an invitation link makes whoever opens it a member of the room, while an
    /// external link opens the room without an account. It is fixed when the link is created and is ignored on later
    /// changes.
    /// </summary>
    /// <example>1</example>
    [EnumDataType(typeof(LinkType))]
    public LinkType LinkType { get; set; }

    /// <summary>
    /// The password an external link asks for before it opens the room. An empty value leaves the link open to anyone
    /// who has the address, and the password is never returned when links are listed.
    /// </summary>
    /// <example>S3cret-Phrase</example>
    [StringLength(255)]
    public string Password { get; set; }

    /// <summary>
    /// Whether people arriving through the link are stopped from downloading and printing what they open. They can
    /// still read the documents in the editor.
    /// </summary>
    /// <example>false</example>
    public bool DenyDownload { get; set; }

    /// <summary>
    /// How many people an invitation link may still let in before it stops working. A value below the number of
    /// people who already used it is refused, and leaving it out puts no ceiling on the link.
    /// </summary>
    /// <example>25</example>
    [Range(1, 1000)]
    public int? MaxUseCount { get; set; }

    /// <summary>
    /// How many people have already joined through this invitation link. The value is kept by the portal: it is
    /// reported back when links are listed and anything sent here is ignored.
    /// </summary>
    /// <example>0</example>
    public int CurrentUseCount { get; set; }
}

/// <summary>
/// The generic room link request parameters.
/// </summary>
public class RoomLinkRequestDto<T>
{
    /// <summary>
    /// The room the link belongs to, named by the identifier that `GET api/2.0/files/rooms` reports for it.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// The link to create, change or revoke.
    /// </summary>
    [FromBody]
    public required RoomLinkRequest RoomLink { get; set; }
}
