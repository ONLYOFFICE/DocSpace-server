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
/// The body of a spreadsheet reference request: the source spreadsheet, and the three ways of naming the document it
/// refers to, which are tried in the order they are described.
/// </summary>
public class GetReferenceDataDto<T>
{
    /// <summary>
    /// The id of the referenced file as the document service recorded it in the formula. It is tried first, and only
    /// when `instanceId` names this portal.
    /// </summary>
    /// <example>512</example>
    public required string FileKey { get; set; }

    /// <summary>
    /// The portal the reference was made on, as the document service recorded it. Only the id of this portal makes
    /// the file key resolvable; any other value falls through to the path and the link.
    /// </summary>
    /// <example>1</example>
    public required string InstanceId { get; set; }

    /// <summary>
    /// The spreadsheet the formula sits in. The path is resolved against it - the referenced file is looked for among
    /// the files lying next to it - and it is the file whose read access is checked.
    /// </summary>
    /// <example>1</example>
    public T SourceFileId { get; set; }

    /// <summary>
    /// The title of the referenced file exactly as the formula spells it, matched against the files lying next to the
    /// source file. It is tried after the file key, and only when no link is given.
    /// </summary>
    /// <example>Budget 2026.xlsx</example>
    public string Path { get; set; }

    /// <summary>
    /// The web address the formula points at, an editor link of this portal or one of its short links. It is tried
    /// last, and an address belonging to another site is not resolved at all but handed back for the client to follow
    /// as it is.
    /// </summary>
    /// <example>https://portal.example.com/doc/512</example>
    public string Link { get; set; }
}