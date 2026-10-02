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
/// <example>
/// {
///   "script": "builder.OpenFile(\"1234\"); Api.GetDocument().GetElement(0).AddText(Argument.title); builder.SaveFile(\"docx\", \"result.docx\"); builder.SaveFile(\"pdf\", \"result.pdf\"); builder.CloseFile();",
///   "folderId": 5678,
///   "outputs": {
///     "result.docx": { "fileId": 1234 },
///     "result.pdf": { "folderId": 5678, "title": "Quarterly report.pdf" }
///   },
///   "argument": { "title": "Quarterly report" }
/// }
/// </example>
public class DocsBuilderRequestDto
{
    /// <summary>
    /// The document builder script. It names a portal file by its id where the document builder documentation
    /// writes an address - `builder.OpenFile("1234")`; an address written out in the script is refused.
    /// </summary>
    /// <example>builder.OpenFile("1234"); Api.GetDocument().GetElement(0).AddText(Argument.title); builder.SaveFile("docx", "result.docx"); builder.SaveFile("pdf", "result.pdf"); builder.CloseFile();</example>
    [StringLength(MaxScriptLength)]
    public required string Script { get; set; }

    /// <summary>
    /// The id of the folder for the produced files that `outputs` does not list, as reported by a folder listing
    /// such as `GET api/2.0/files/{folderId}`.
    /// </summary>
    /// <example>5678</example>
    public int? FolderId { get; set; }

    /// <summary>
    /// Destinations of the produced files, keyed by the name the script saves each one under - the second argument of
    /// `builder.SaveFile("docx", "result.docx")`.
    /// </summary>
    /// <example>{"result.docx": {"fileId": 1234}, "result.pdf": {"folderId": 5678, "title": "Quarterly report.pdf"}}</example>
    public Dictionary<string, DocsBuilderOutputDto> Outputs { get; set; }

    /// <summary>
    /// Values the script reads through the global `Argument` object, such as `Argument.title`. A JSON object without
    /// http or https addresses.
    /// </summary>
    /// <example>{"title": "Quarterly report"}</example>
    public JsonElement? Argument { get; set; }

    // the script and the argument travel in an event bus message, which the broker caps
    private const int MaxScriptLength = 2 * 1024 * 1024;
}

/// <summary>
/// Where one produced file goes.
/// </summary>
/// <example>
/// {
///   "folderId": 5678,
///   "title": "Quarterly report.pdf"
/// }
/// </example>
public class DocsBuilderOutputDto
{
    /// <summary>
    /// The id of a portal file to store the result in as a new version, as reported by a folder listing such as
    /// `GET api/2.0/files/{folderId}`.
    /// </summary>
    /// <example>1234</example>
    public int? FileId { get; set; }

    /// <summary>
    /// The id of a folder to save the result in as a new file, as reported by a folder listing such as
    /// `GET api/2.0/files/{folderId}`.
    /// </summary>
    /// <example>5678</example>
    public int? FolderId { get; set; }

    /// <summary>
    /// The title of the new file, when it has to differ from the name the script saved the result under.
    /// </summary>
    /// <example>Quarterly report.pdf</example>
    public string Title { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class DocsBuilderOutputDtoMapper
{
    public static partial FileBuilderOutputData MapToFileBuilderOutputData(this DocsBuilderOutputDto source);
}
