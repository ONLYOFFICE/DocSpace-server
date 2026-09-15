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
/// The fields of a room that a partial update changes.
/// </summary>
/// <remarks>
/// An undocumented field in the payload is a caller mistake, not something to ignore: a typo'd
/// property name would otherwise be reported as a successful update that changed nothing.
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class UpdateRoomRequest
{
    /// <summary>
    /// The new name of the room. It is trimmed and sanitised the way a room title is at creation, and a blank or
    /// missing value leaves the current name alone rather than clearing it.
    /// </summary>
    /// <example>Project Alpha</example>
    [StringLength(170)]
    public string Title { get; set; }

    /// <summary>
    /// The new storage limit of the room, in bytes. A value of -1 leaves the room with no limit of its own, any other
    /// negative value puts it back on the portal default, and a positive one is accepted only while the per-room
    /// quota feature is on.
    /// </summary>
    /// <example>1073741824</example>
    public long? Quota { get; set; }

    /// <summary>
    /// Whether the room keeps a manual order of its contents. With it on every file and folder carries a position
    /// that listings follow and that `PUT api/2.0/files/rooms/{id}/reorder` compacts; with it off the contents are
    /// ordered by the sorting of the request. Turning it on renumbers the existing contents at once.
    /// </summary>
    /// <example>true</example>
    public bool? Indexing { get; set; }

    /// <summary>
    /// Whether members without editing rights are stopped from downloading and printing the contents of the room.
    /// They can still open the documents in the editor.
    /// </summary>
    /// <example>true</example>
    public bool? DenyDownload { get; set; }

    /// <summary>
    /// How long files may stay in the room before they are deleted automatically. The countdown starts when the
    /// setting is saved, and leaving the field out keeps the files forever. Sending it with the switch off stops the
    /// automatic deletion.
    /// </summary>
    /// <example>{"deletePermanently": false, "period": 1, "value": 6, "enabled": true}</example>
    public RoomDataLifetimeDto Lifetime { get; set; }

    /// <summary>
    /// The watermark drawn over documents opened in the room. Leaving the field out adds no watermark, and sending it
    /// with the switch turned off removes the one the room has.
    /// </summary>
    /// <example>{"enabled": true, "text": "Confidential", "rotate": -45, "imageScale": 100}</example>
    public WatermarkRequestDto Watermark { get; set; }

    /// <summary>
    /// The picture to use as the room logo, named by the path that `POST api/2.0/files/logos` returned for an image
    /// uploaded beforehand, plus the crop to take from it. Leaving the field out keeps the room on its cover and
    /// colour.
    /// </summary>
    /// <example>{"tmpFile": "/temp/logo_a1b2c3.png", "x": 0, "y": 0, "width": 200, "height": 200}</example>
    public LogoRequest Logo { get; set; }

    /// <summary>
    /// The labels the room is to carry from now on. The list replaces the whole tag set rather than adding to it, an
    /// empty list clears it, and names the portal catalogue does not hold yet are added to it.
    /// </summary>
    /// <example>["Finance", "2026"]</example>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// The background colour the room is drawn with while it has no logo, as six hexadecimal digits with no leading
    /// number sign. An empty value restores the default colour of the room type.
    /// </summary>
    /// <example>FF5733</example>
    [RegularExpression("^[0-9a-fA-F]{6}$")]
    public string Color { get; set; }

    /// <summary>
    /// The picture drawn on the room while it has no logo, named by an identifier from
    /// `GET api/2.0/files/rooms/covers`. Any other value is rejected, and an empty value leaves the room without a
    /// cover.
    /// </summary>
    /// <example>bookmark</example>
    [StringLength(50)]
    public string Cover { get; set; }
    
    /// <summary>
    /// The model and the prompt an AI room answers with. It belongs to AI rooms only and is rejected for a room of
    /// any other kind.
    /// </summary>
    /// <example>{"providerId": 1, "modelId": "gpt-4", "prompt": "Answer using the documents of this room"}</example>
    public ChatSettings ChatSettings { get; set; }

    /// <summary>
    /// For a form filling room, whether the data of every completed submission is also pushed to the external
    /// database configured for the portal. It is what `POST api/2.0/files/rooms/{id}/externaldbsync` re-runs for the
    /// forms already collected.
    /// </summary>
    /// <example>false</example>
    public bool? SendFormToExternalDB { get; set; }

    /// <summary>
    /// For a form filling room, whether the collected submissions are also gathered into a spreadsheet stored next to
    /// the completed forms. With it off the submissions are kept only as the filled documents themselves.
    /// </summary>
    /// <example>false</example>
    public bool? SaveFormAsXLSX { get; set; }
}

/// <summary>
/// The request parameters for updating a room.
/// </summary>
public class UpdateRoomRequestDto<T>
{
    /// <summary>
    /// The room to update, named by the identifier that `GET api/2.0/files/rooms` reports for it.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// The fields to change. Only the properties present in the object are applied, and a property that the object
    /// does not define is rejected instead of being ignored.
    /// </summary>
    [FromBody]
    public required UpdateRoomRequest UpdateRoom { get; set; }
}