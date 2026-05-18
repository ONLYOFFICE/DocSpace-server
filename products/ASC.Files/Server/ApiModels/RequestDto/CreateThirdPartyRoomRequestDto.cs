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
/// The parameters for creating a third-party room.
/// </summary>
public class CreateThirdPartyRoom
{
    /// <summary>
    /// Specifies whether to create a third-party room as a new folder or not.
    /// </summary>
    /// <example>false</example>
    public bool CreateAsNewFolder { get; set; }

    /// <summary>
    /// The third-party room name to be created.
    /// </summary>
    /// <example>My Third-Party Room</example>
    public required string Title { get; set; }

    /// <summary>
    /// The third-party room type to be created.
    /// </summary>
    /// <example>2</example>
    public required RoomType RoomType { get; set; }

    /// <summary>
    /// Specifies whether to create the private third-party room or not.
    /// </summary>
    /// <example>false</example>
    public bool Private { get; set; }

    /// <summary>
    /// Specifies whether to create the third-party room with indexing.
    /// </summary>
    /// <example>true</example>
    public bool Indexing { get; set; }

    /// <summary>
    /// Specifies whether to deny downloads from the third-party room.
    /// </summary>
    /// <example>false</example>
    public bool DenyDownload { get; set; }

    /// <summary>
    /// The color of the third-party room.
    /// </summary>
    /// <example>#FF0000</example>
    public string Color { get; set; }

    /// <summary>
    /// The cover of the third-party room.
    /// </summary>
    /// <example>cover1.jpg</example>
    public string Cover { get; set; }

    /// <summary>
    /// The list of tags of the third-party room.
    /// </summary>
    /// <example>["tag1", "tag2", "tag3"]</example>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// The logo request parameters of the third-party room.
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
    /// The ID of the folder in the third-party storage in which the contents of the room will be stored.
    /// </summary>
    /// <example>folder-123-abc</example>
    [FromRoute(Name = "id")]
    public required string Id { get; set; }

    /// <summary>
    /// The third-party room information.
    /// </summary>
    /// <example>{"createAsNewFolder": false, "title": "My Third-Party Room", "roomType": 2, "private": false, "indexing": true, "denyDownload": false, "color": "FF0000", "cover": "cover1.jpg", "tags": ["tag1", "tag2", "tag3"]}</example>
    [FromBody]
    public required CreateThirdPartyRoom Room { get; set; }
}