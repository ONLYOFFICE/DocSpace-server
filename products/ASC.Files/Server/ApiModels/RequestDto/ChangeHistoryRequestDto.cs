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
/// The change to make to a revision group of a file.
/// </summary>
public class ChangeHistory
{
    /// <summary>
    /// The version the change applies to; 0 means the current version of the file.
    /// </summary>
    /// <example>1</example>
    public required int Version { get; set; }

    /// <summary>
    /// What to do with the revision group: `false` completes the named version, storing its content again as a fresh
    /// version that opens a new group, while `true` folds the last group back into the group before it, so the next
    /// save continues that revision.
    /// </summary>
    /// <example>false</example>
    public bool ContinueVersion { get; set; }
}

/// <summary>
/// The request that closes or reopens a revision group of a file.
/// </summary>
public class ChangeHistoryRequestDto<T>
{
    /// <summary>
    /// The file whose version history is changed.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The change to make to the revision group.
    /// </summary>
    /// <example>{"version": 1, "continueVersion": false}</example>
    [FromBody]
    public required ChangeHistory File { get; set; }
}