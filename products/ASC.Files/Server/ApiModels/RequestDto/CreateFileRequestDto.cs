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
/// The parameters of a file that the portal creates from a template or a blank document.
/// </summary>
public class CreateFile<T>
{
    /// <summary>
    /// The title of the new file. The extension in it decides the format, and one of a known text, spreadsheet or
    /// presentation format is rewritten to the DOCX, XLSX or PPTX of the portal unless `enableExternalExt` says
    /// otherwise; a title with no extension gets DOCX added.
    /// </summary>
    /// <example>New Document.docx</example>
    [StringLength(165)]
    public required string Title { get; set; }

    /// <summary>
    /// An existing file the new one copies its content from, as a number for a file in the portal and as a string for
    /// one in a connected third-party storage; the caller has to be able to read it. Left out, a blank template for
    /// the format and the language of the caller is used.
    /// </summary>
    /// <example>1</example>
    public T TemplateId { get; set; }

    /// <summary>
    /// Whether the extension of the title is kept as it is: `true` stores the title verbatim, `false` rewrites a
    /// known foreign format to the format the portal edits itself.
    /// </summary>
    /// <example>false</example>
    public bool EnableExternalExt { get; set; }

    /// <summary>
    /// A ready form from the form gallery of the portal to copy instead of a template, named by the identifier the
    /// gallery reports for it. It takes precedence over `templateId`; 0 means no form.
    /// </summary>
    /// <example>0</example>
    public int FormId { get; set; }
}

/// <summary>
/// The request that creates a file in a folder.
/// </summary>
public class CreateFileRequestDto<T>
{
    /// <summary>
    /// The folder the file is created in.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }

    /// <summary>
    /// The title of the new file and the source of its content.
    /// </summary>
    /// <example>{"title": "New Document.docx", "templateId": "1", "enableExternalExt": false, "formId": 0}</example>
    [FromBody]
    public required CreateFile<JsonElement> File { get; set; }
}