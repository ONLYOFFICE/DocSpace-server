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

namespace ASC.People.ApiModels.RequestDto;

/// <summary>
/// The request parameters for uploading a user photo.
/// </summary>
public class UploadMemberPhotoRequestDto
{
    /// <summary>
    /// The profile whose avatar is uploaded, taken from the route. Either the ID of the account or its user name is
    /// accepted, and it has to be the calling account, because a profile photo can only be changed by its owner.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userid")]
    public required string UserId { get; set; }

    /// <summary>
    /// The image itself, sent as a multipart form field. It has to be a raster format the portal can read and stay
    /// within the portal limit on image size; sending no file makes the operation answer with `success` false rather
    /// than an error status.
    /// </summary>
    /// <example>photo.png</example>
    [FromForm]
    public required IFormFile File { get; set; }

    /// <summary>
    /// Set it to true to make the uploaded image the avatar right away. With the default false the image is only
    /// stored as a temporary file whose name comes back in `data`, and it has to be passed to
    /// `POST api/2.0/people/{userid}/photo/thumbnails` to take effect.
    /// </summary>
    /// <example>true</example>
    [FromForm(Name = "Autosave")]
    public bool Autosave { get; set; }
}
