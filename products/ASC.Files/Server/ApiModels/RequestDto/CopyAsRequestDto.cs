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
/// The parameters of a file copy that may change the format on the way.
/// </summary>
public class CopyAs<T>
{
    /// <summary>
    /// The title of the copy, extension included. That extension decides the format: the same one as the source
    /// copies the content as it is, a different one has it converted first.
    /// </summary>
    /// <example>Document Copy.docx</example>
    public required string DestTitle { get; set; }

    /// <summary>
    /// The folder the copy is placed in, as a number for a folder inside the portal and as a string for one in a
    /// connected third-party storage; obtain it from `GET api/2.0/files/@root`. Anything else is answered with an
    /// empty body and nothing is copied.
    /// </summary>
    /// <example>1</example>
    public required T DestFolderId { get; set; }

    /// <summary>
    /// Whether the extension of the new title may be one the portal does not edit itself.
    /// </summary>
    /// <example>false</example>
    public bool EnableExternalExt { get; set; }

    /// <summary>
    /// The password that opens the source document, for a file that is protected by one.
    /// </summary>
    /// <example>password123</example>
    public string Password { get; set; }

    /// <summary>
    /// Whether the copy is to become a PDF form rather than a plain document, which the conversion supports for the
    /// text formats it can read.
    /// </summary>
    /// <example>false</example>
    public bool ToForm { get; set; }
}

/// <summary>
/// The request that copies a file under a new title.
/// </summary>
public class CopyAsRequestDto<T>
{
    /// <summary>
    /// The file to copy.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The title, the destination and the conversion options of the copy.
    /// </summary>
    /// <example>
    /// {"destTitle": "Contract copy.pdf", "destFolderId": 1, "enableExternalExt": false, "toForm": true}
    /// </example>
    [FromBody]
    public required CopyAs<JsonElement> File { get; set; }
}