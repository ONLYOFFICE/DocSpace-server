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
/// The parameters of an editor configuration request: the file in the route, and the intended mode in the query.
/// </summary>
public class OpenEditRequestDto<T>
{
    /// <summary>
    /// The file the editor configuration is built for. Take the id from a folder listing such as
    /// `GET api/2.0/files/{folderId}`.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// Which entry of the file history to open, numbered the way the file versions are. Left out, the current
    /// revision is opened; naming a version requires access to the history of the file.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "version")]
    public int Version { get; set; }

    /// <summary>
    /// Asks for a read-only configuration. Left off, the configuration is built for editing as far as the caller's
    /// rights and the room the file lies in allow.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "view")]
    public bool View { get; set; }

    /// <summary>
    /// Which editor layout the configuration is built for: the full desktop interface, the reduced mobile one, or the
    /// embedded viewer meant to be framed inside another page.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "editorType")]
    public EditorType EditorType { get; set; }

    /// <summary>
    /// Asks for editing rather than viewing. On a form in a form-filling room this also records that the form is
    /// being edited; the room may still turn the request into viewing or into filling.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "edit")]
    public bool Edit { get; set; }

    /// <summary>
    /// Asks for a PDF form to open for filling out rather than for editing. It has no effect on a file that is not a
    /// form.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "fill")]
    public bool Fill { get; set; }
}