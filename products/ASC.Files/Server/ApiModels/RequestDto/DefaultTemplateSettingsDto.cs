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
/// The document to use as the blank the portal creates for one extension.
/// </summary>
public class DefaultTemplateSettingsRequestDto
{
    /// <summary>
    /// The document to copy as the blank: a number for a file stored in the portal, a string for one in a connected
    /// third-party storage. Take the identifier from a folder listing such as `GET api/2.0/files/{folderId}`; the
    /// caller must be allowed to copy that file, and its extension must be the one named below.
    /// </summary>
    /// <example>1</example>
    public required JsonElement SelectedFile { get; set; }
    /// <summary>
    /// The extension the blank is set for, written in lower case with the leading dot. Only the extensions the
    /// portal's built-in template set covers are accepted, and `GET api/2.0/files/settings/defaulttemplate` returns
    /// exactly that list; an extension outside it leaves the settings unchanged instead of failing.
    /// </summary>
    /// <example>.docx</example>
    public required string FileExtension { get; set; }
}

/// <summary>
/// The extension whose custom blank is dropped in favour of the built-in one.
/// </summary>
public class DefaultTemplateSettingsResetRequestDto
{
    /// <summary>
    /// The extension whose custom blank is dropped, written in lower case with the leading dot. Only the extensions
    /// the portal's built-in template set covers are accepted, and `GET api/2.0/files/settings/defaulttemplate`
    /// returns exactly that list; an extension outside it leaves the settings unchanged instead of failing.
    /// </summary>
    /// <example>.docx</example>
    public required string FileExtension { get; set; }
}

/// <summary>
/// The document uploaded as the blank for one extension, sent as multipart form data.
/// </summary>
public class DefaultTemplateSettingsUploadRequestDto
{
    /// <summary>
    /// The extension the uploaded blank is set for, written in lower case with the leading dot, and travelling in the
    /// query string rather than in the form. It must match the extension of the uploaded file name. Only the
    /// extensions the portal's built-in template set covers are accepted, and
    /// `GET api/2.0/files/settings/defaulttemplate` returns exactly that list; an extension outside it leaves the
    /// settings unchanged instead of failing.
    /// </summary>
    /// <example>.docx</example>
    public required string FileExtension { get; set; }

    /// <summary>
    /// The template document itself. Its file name must end with the extension named above, a PDF must be a fillable
    /// form, and the body is capped at 100 MB - a larger one is refused while it is still streaming in.
    /// </summary>
    /// <example>binary file data</example>
    public required IFormFile File { get; set; }
}