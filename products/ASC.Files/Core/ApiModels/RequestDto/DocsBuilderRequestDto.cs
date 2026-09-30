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
    /// Where each produced file goes, keyed by the name the script saves it under - the second argument of
    /// `builder.SaveFile("docx", "report.docx")`. An entry either replaces an existing file with a new version or
    /// puts a new file in a folder. A produced file with no entry falls back to `folderId`.
    /// </summary>
    /// <example>{"result.docx": {"fileId": 1234}, "result.pdf": {"folderId": 1234}}</example>
    public Dictionary<string, DocsBuilderOutputDto> Outputs { get; set; }

    /// <summary>
    /// Values the script reads through the global `Argument` object, such as `Argument.title`. A JSON object without
    /// http or https addresses.
    /// </summary>
    /// <example>{"title": "Quarterly report"}</example>
    public JsonElement? Argument { get; set; }
}

/// <summary>
/// Where one produced file goes. Exactly one of the two destinations is given.
/// </summary>
public class DocsBuilderOutputDto
{
    /// <summary>
    /// The file this result replaces, as a new version of it. The caller has to be allowed to edit it, and the
    /// result has to carry the same format.
    /// </summary>
    /// <example>1234</example>
    public int? FileId { get; set; }

    /// <summary>
    /// The folder this result is saved into as a new file. The caller has to be allowed to create files there.
    /// </summary>
    /// <example>1234</example>
    public int? FolderId { get; set; }

    /// <summary>
    /// The title to save under, when the name the script used is not the one the portal should show. Only together
    /// with `folderId`: a file being replaced keeps its own title.
    /// </summary>
    /// <example>result.docx</example>
    public string Title { get; set; }
}
