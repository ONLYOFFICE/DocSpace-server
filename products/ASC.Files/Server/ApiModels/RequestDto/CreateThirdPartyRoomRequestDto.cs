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

/// <summary>The room to be created out of a folder of a connected third-party storage account.</summary>
public class CreateThirdPartyRoom
{
    /// <summary>
    /// Creates a new folder named after `title` inside the folder named in the path and turns that subfolder into the
    /// room, leaving the named folder itself untouched. When omitted, the named folder becomes the room and keeps
    /// everything it already holds.
    /// </summary>
    /// <example>false</example>
    public bool CreateAsNewFolder { get; set; }

    /// <summary>
    /// The name the room is shown under. It is stored on the connected account, so it does not have to match the name
    /// of the folder in the storage; with `createAsNewFolder` it is also the name given to the created subfolder.
    /// </summary>
    /// <example>Third-party project room</example>
    public required string Title { get; set; }

    /// <summary>
    /// The kind of room the folder becomes, which decides the default access rules of its members and cannot be
    /// changed afterwards.
    /// </summary>
    /// <example>2</example>
    public required RoomType RoomType { get; set; }

    /// <summary>
    /// Restricts the room to the members explicitly invited into it. The flag is kept on the connected storage
    /// account rather than on the folder, so every folder read through that account reports the same value.
    /// </summary>
    /// <example>false</example>
    public bool Private { get; set; }

    /// <summary>
    /// Keeps the contents of the room in an explicit numbered order, the one reported as `order` on every entry,
    /// instead of leaving the order to the reader.
    /// </summary>
    /// <example>true</example>
    public bool Indexing { get; set; }

    /// <summary>
    /// Forbids downloading and printing the contents of the room, which leaves the members with viewing and editing
    /// in the editor only.
    /// </summary>
    /// <example>false</example>
    public bool DenyDownload { get; set; }

    /// <summary>
    /// The background colour drawn behind the cover of the room, as six hexadecimal digits without a leading number
    /// sign. An empty value restores the colour the portal picks by default.
    /// </summary>
    /// <example>FF5733</example>
    public string Color { get; set; }

    /// <summary>
    /// The drawing shown on the room tile, named by one of the built-in cover identifiers returned by
    /// `GET api/2.0/files/rooms/covers`. An empty value leaves the room without a cover, and any other unknown value
    /// is rejected as an invalid request.
    /// </summary>
    /// <example>bookmark</example>
    public string Cover { get; set; }

    /// <summary>
    /// The tags to attach to the room, named by their text. A name that is not in the portal tag catalogue yet is
    /// added to it, and `GET api/2.0/files/tags` lists the names already there.
    /// </summary>
    /// <example>["Marketing", "Q3"]</example>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// The picture to use as the room logo, which has to be uploaded with `POST api/2.0/files/logos` first; leaving
    /// it out keeps the room on its cover and colour.
    /// </summary>
    /// <example>{"tmpFile": "/temp/logo.png", "x": 0, "y": 0, "width": 100, "height": 100}</example>
    public LogoRequest Logo { get; set; }
}


/// <summary>
/// The request parameters for creating a third-party room.
/// </summary>
public class CreateThirdPartyRoomRequestDto
{
    /// <summary>
    /// The identifier of the folder in the connected third-party storage that becomes the room, or receives it as a
    /// subfolder. Folder identifiers of a connected account are strings and are returned by the folder listings of
    /// that account.
    /// </summary>
    /// <example>box-12-|280143035119</example>
    [FromRoute(Name = "id")]
    public required string Id { get; set; }

    /// <summary>The settings of the room to be created out of the folder.</summary>
    /// <example>
    /// {"createAsNewFolder": false, "title": "Third-party project room", "roomType": 2, "indexing": true, "color":
    /// "FF5733", "cover": "bookmark", "tags": ["Marketing"]}
    /// </example>
    [FromBody]
    public required CreateThirdPartyRoom Room { get; set; }
}