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
/// The document builder script to run and where to put what it produces.
/// </summary>
public class DocsBuilderRequestDto
{
    /// <summary>
    /// The document builder script. It addresses a portal file by writing the identifier of that file where the
    /// document builder documentation writes an address - `builder.OpenFile("1234")` - and the portal resolves it
    /// after checking that the caller may read it. Addresses written out by hand are refused.
    /// </summary>
    /// <example>builder.OpenFile("1234"); Api.GetDocument().GetElement(0).AddText("done"); builder.SaveFile("docx", "result.docx"); builder.CloseFile();</example>
    public required string Script { get; set; }

    /// <summary>
    /// Where the produced files are saved. It defaults to the folder of the file the script opened, and is required
    /// when the script opens none.
    /// </summary>
    /// <example>5678</example>
    public int? FolderId { get; set; }

    /// <summary>
    /// Whether the result replaces the file the script opened, as a new version of it, instead of being saved as a new
    /// file. A script that does this has to open a file it may edit and produce exactly one result.
    /// </summary>
    /// <example>false</example>
    public bool Overwrite { get; set; }
}
