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
/// The parameters of a room built from a room template.
/// </summary>
public class CreateRoomFromTemplateDto : IValidatableObject
{
    /// <summary>
    /// The room template to copy. Templates live in their own section and are listed by `GET api/2.0/files/rooms`
    /// with a search area of 4; an ordinary room id is rejected here.
    /// </summary>
    /// <example>42</example>
    public required int TemplateId { get; set; }

    /// <summary>
    /// The name of the room to create. It is sanitised and truncated the way a room title is, and a blank value is
    /// rejected; the title of the template is not reused.
    /// </summary>
    /// <example>Project Alpha</example>
    [StringLength(RoomTitleMaxLength)]
    public required string Title { get; set; }

    /// <summary>
    /// The picture to use as the room logo, named by the path that `POST api/2.0/files/logos` returned for an image
    /// uploaded beforehand, plus the crop to take from it. Leaving the field out keeps the room on its cover and
    /// colour. It is ignored when the logo of the template is copied instead.
    /// </summary>
    /// <example>{"tmpFile": "/temp/logo_a1b2c3.png", "x": 0, "y": 0, "width": 200, "height": 200}</example>
    public LogoRequest Logo { get; set; }

    /// <summary>
    /// Whether the new room keeps the logo of the template. With it on the uploaded picture is ignored; with it off
    /// the room starts with no logo unless one is supplied.
    /// </summary>
    /// <example>false</example>
    public bool CopyLogo { get; set; }

    /// <summary>
    /// The labels to attach to the room, by name. Names the portal tag catalogue does not hold yet are added to it,
    /// and `GET api/2.0/files/tags` lists what already exists. Leaving the field out keeps the tags of the template.
    /// </summary>
    /// <example>["Finance", "2026"]</example>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// The background colour the room is drawn with while it has no logo, as six hexadecimal digits with no leading
    /// number sign. An empty value restores the default colour of the room type.
    /// </summary>
    /// <example>FF5733</example>
    [StringLength(6)]
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
    /// The storage the room may take, in bytes. It is accepted only while the per-room quota feature is on for the
    /// portal and must stay inside the portal own limit; leaving it out lets the room follow the portal default.
    /// </summary>
    /// <example>1073741824</example>
    public long? Quota { get; set; }

    /// <summary>
    /// Whether the room keeps a manual order of its contents. With it on every file and folder carries a position
    /// that listings follow and that `PUT api/2.0/files/rooms/{id}/reorder` compacts; with it off the contents are
    /// ordered by the sorting of the request. Leaving it out keeps the setting of the template.
    /// </summary>
    /// <example>true</example>
    public bool? Indexing { get; set; }

    /// <summary>
    /// Whether members without editing rights are stopped from downloading and printing the contents of the room.
    /// They can still open the documents in the editor. Leaving it out keeps the setting of the template.
    /// </summary>
    /// <example>false</example>
    public bool? DenyDownload { get; set; }

    /// <summary>
    /// How long files may stay in the room before they are deleted automatically. The countdown starts when the
    /// setting is saved, and leaving the field out keeps the files forever. Leaving the field out keeps the setting
    /// of the template.
    /// </summary>
    /// <example>{"deletePermanently": false, "period": 1, "value": 6, "enabled": true}</example>
    public RoomDataLifetimeDto Lifetime { get; set; }

    /// <summary>
    /// The watermark drawn over documents opened in the room. Leaving the field out adds no watermark, and sending it
    /// with the switch turned off removes the one the room has. Leaving the field out keeps the setting of the
    /// template.
    /// </summary>
    /// <example>{"enabled": true, "text": "Confidential", "rotate": -45, "imageScale": 100}</example>
    public WatermarkRequestDto Watermark { get; set; }

    /// <summary>
    /// Whether the room is end-to-end encrypted. Its files can then be opened only in the desktop application by
    /// members whose encryption keys are set up, and the flag cannot be changed after the room is created.
    /// </summary>
    /// <example>false</example>
    public bool? Private { get; set; }

    /// <summary>
    /// The room this creates is an ordinary room, so its title obeys the ordinary room title limit
    /// (<c>CreateRoomRequestDto.Title</c>) and, like it, is never blank.
    /// </summary>
    private const int RoomTitleMaxLength = 170;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return new ValidationResult("A room title cannot be empty or consist of whitespace only.", [nameof(Title)]);
        }
    }
}
