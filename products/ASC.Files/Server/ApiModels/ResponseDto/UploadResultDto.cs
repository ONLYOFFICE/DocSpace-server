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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The outcome of storing an image in temporary storage before it is used as a room logo.
/// </summary>
public class UploadResultDto
{
    /// <summary>
    /// True when the image was stored and its path is in the data field. A rejected image is reported with an error
    /// response rather than with a false here, so this field is true in every answer that carries a body.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// The path of the stored image, which is the value to send as the temporary file when the logo of a room is set.
    /// It is opaque: do not build or parse it, and expect a different path from every upload.
    /// </summary>
    /// <example>/storage/logos_temp/0f2e4b6a-8c1d-4e3f-9a5b-7c8d9e0f1a2b_...png</example>
    public object Data { get; set; }

    /// <summary>
    /// Left empty by this operation: nothing is reported here, and a refused image comes back as an error response
    /// instead.
    /// </summary>
    /// <example></example>
    public string Message { get; set; }
}