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
/// The parameters of a room template built from an existing room.
/// </summary>
public class RoomTemplateDto
{
    /// <summary>
    /// The identifier of the room the template is built from. Take it from the room listing of
    /// `GET api/2.0/files/rooms`; a folder identifier is not accepted.
    /// </summary>
    /// <example>1234</example>
    public required int RoomId { get; set; }

    /// <summary>
    /// The title the template is saved under in the Templates section. Characters that a folder name cannot contain
    /// are replaced with an underscore on save, and two templates may share a title.
    /// </summary>
    /// <example>Sales agreement room</example>
    [Required]
    [StringLength(400)]
    public string Title { get; set; }

    /// <summary>
    /// A picture of the caller's own for the template, cropped out of an image already placed in the temporary
    /// storage.
    /// </summary>
    /// <example>{"tmpFile": "temp_logo.png", "x": 0, "y": 0, "width": 100, "height": 100}</example>
    public LogoRequest Logo { get; set; }

    /// <summary>
    /// Whether the template takes over the picture already set on the source room. When false the template gets no
    /// picture from that room.
    /// </summary>
    /// <example>true</example>
    public bool CopyLogo { get; set; }

    /// <summary>
    /// The email addresses of the portal members who are granted read access to the finished template.
    /// </summary>
    /// <example>["user1@example.com", "user2@example.com"]</example>
    public List<string> Share { get; set; }

    /// <summary>
    /// The identifiers of the portal groups whose members are granted read access to the finished template.
    /// </summary>
    /// <example>["9924256a-739c-462b-af15-e652a3b1b6eb"]</example>
    public List<Guid> Groups { get; set; }

    /// <summary>
    /// Whether the finished template is shared with everyone allowed to create rooms. When false it stays reachable
    /// only for the recipients named for it.
    /// </summary>
    /// <example>true</example>
    public bool Public { get; set; }

    /// <summary>
    /// The labels attached to the template and shown next to it in listings.
    /// </summary>
    /// <example>["Contracts", "Sales"]</example>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// The accent colour of the generated cover, written as six hexadecimal digits with no leading hash sign. When it
    /// is left empty a colour is picked at random.
    /// </summary>
    /// <example>FF5733</example>
    [StringLength(6)]
    public string Color { get; set; }

    /// <summary>
    /// The identifier of a built-in cover picture, as listed by `GET api/2.0/files/rooms/covers`. When it is left
    /// empty the template gets no cover.
    /// </summary>
    /// <example>bookmark</example>
    [StringLength(50)]
    public string Cover { get; set; }

    /// <summary>
    /// The storage limit assigned to the template, in bytes. When it is not set the template keeps the limit of the
    /// source room.
    /// </summary>
    /// <example>10485760</example>
    public long? Quota { get; set; }
}
