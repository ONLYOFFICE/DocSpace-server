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

namespace ASC.People.ApiModels.ResponseDto;

/// <summary>
/// The file upload result.
/// </summary>
public class FileUploadResultDto
{
    /// <summary>
    /// Whether the upload succeeded. This is the field to check: the operation answers 200 even when it fails, and
    /// reports the reason in `message` instead of in the status code.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// The result of a successful upload, whose shape depends on `autosave`. With `autosave` on it holds the URLs of
    /// the stored avatar in every size - `main`, `retina`, `max`, `big`, `medium` and `small` - each carrying a
    /// `hash` query parameter that changes with the avatar. With `autosave` off it holds the name of the temporary
    /// file to pass to `POST api/2.0/people/{userid}/photo/thumbnails`. It is empty when the upload failed.
    /// </summary>
    /// <example>{"main": "/storage/userphotos/photo.png?hash=123456", "retina": "/storage/userphotos/photo_retina.png?hash=123456"}</example>
    public object Data { get; set; }

    /// <summary>
    /// The reason the upload failed, ready to be shown to a person. It is empty for a successful upload, and it is
    /// the only place where a failure is described, because the status code stays 200.
    /// </summary>
    /// <example>The image size is too large</example>
    public string Message { get; set; }
}