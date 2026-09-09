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
/// The body of an editing session request.
/// </summary>
public class StartEdit
{
    /// <summary>
    /// Claims the file for this caller alone: the session is opened without asking the document service to track
    /// co-editing, and the call is refused when anybody else already has the file open. Left off, an ordinary
    /// co-editing session is opened and others may join it.
    /// </summary>
    /// <example>false</example>
    public bool EditingAlone { get; set; }
}

/// <summary>
/// The parameters of an editing session request: the file in the route and the session options in the body.
/// </summary>
public class StartEditRequestDto<T>
{
    /// <summary>
    /// The file to open the editing session on. The caller needs edit access to it.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The session options. The body is required even when it only carries the default, so send an empty object to
    /// open an ordinary co-editing session.
    /// </summary>
    [FromBody]
    public required StartEdit File { get; set; }
}