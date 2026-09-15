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
/// How a folder is to be deleted.
/// </summary>
public class DeleteFolder
{
    /// <summary>
    /// Whether the deletion waits for the editing sessions on the contents to end: with true a folder somebody is
    /// working in is removed once they are done, with false the deletion starts at once.
    /// </summary>
    /// <example>false</example>
    public bool DeleteAfter { get; set; }

    /// <summary>
    /// Whether the folder is discarded for good instead of being moved to the "Trash" section: with false it can be
    /// restored from Trash, with true it cannot be recovered. Inside a room there is no Trash and the deletion is
    /// final either way.
    /// </summary>
    /// <example>false</example>
    public bool Immediately { get; set; }
}

/// <summary>
/// The request that deletes one folder.
/// </summary>
public class DeleteFolder<T>
{
    /// <summary>
    /// The folder to delete, together with everything it holds.
    /// </summary>
    /// <example>10</example>
    [FromRoute(Name = "folderId")]
    public required T FolderId { get; set; }

    /// <summary>
    /// How the deletion is to be carried out.
    /// </summary>
    /// <example>{"deleteAfter": false, "immediately": false}</example>
    [FromBody]
    public required DeleteFolder Delete { get; set; }
}