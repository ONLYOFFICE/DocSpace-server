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
/// The parameters of a text or HTML file created from content sent in the request.
/// </summary>
public class CreateTextOrHtmlFile
{
    /// <summary>
    /// The title of the file. The extension the operation stands for is appended unless the title already ends with
    /// it, so "Notes" becomes "Notes.txt" or "Notes.html".
    /// </summary>
    /// <example>Document.txt</example>
    [StringLength(165, MinimumLength = 1)]
    public required string Title { get; set; }

    /// <summary>
    /// The content of the file, as plain text or as HTML markup. A request carrying none is rejected as an invalid
    /// request, and for a text file content that looks like markup makes the portal store it as HTML instead.
    /// </summary>
    /// <example>This is the file content</example>
    public string Content { get; set; }

    /// <summary>
    /// What to do when the folder already holds a file of this title, the other way round than the name reads: `true`
    /// updates that file and adds a version to its history, `false` creates another file and makes its title unique,
    /// as in "Notes (1).txt".
    /// </summary>
    /// <example>false</example>
    public bool CreateNewIfExist { get; set; }
}

/// <summary>
/// The request that creates a text or HTML file in a folder.
/// </summary>
public class CreateTextOrHtmlFileRequestDto<T>
{
    /// <summary>
    /// The folder the file is created in.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }

    /// <summary>
    /// The title, the content and the collision behaviour of the new file.
    /// </summary>
    /// <example>{"title": "Document.txt", "content": "This is the file content", "createNewIfExist": false}</example>
    [FromBody]
    public required CreateTextOrHtmlFile File { get; set; }
}