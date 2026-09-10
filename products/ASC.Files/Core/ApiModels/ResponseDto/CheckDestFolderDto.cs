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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The verdict on placing the requested files in the destination folder.
/// </summary>
public class CheckDestFolderDto
{
    /// <summary>
    /// Whether the destination folder accepts all of the requested files, only some of them or none at all.
    /// </summary>
    /// <example>0</example>
    public CheckDestFolderResult Result { get; set; }

    /// <summary>
    /// The requested files the destination accepts, each with the information it was listed under. The files it
    /// rejects are absent, so an empty list means that none of them is accepted.
    /// </summary>
    /// <example>[{"title": "document.docx", "fileEntryType": 2}]</example>
    public List<FileEntryBaseDto> Files { get; set; }
}

/// <summary>
/// Whether the destination folder accepts the requested files.
/// </summary>
public enum CheckDestFolderResult
{
    /// <summary>
    /// Every requested file may be placed in the destination folder.
    /// </summary>
    [Description("All allowed")]
    AllAllowed,

    /// <summary>
    /// Only some of the requested files may be placed in the destination folder; the rest are absent from the answer.
    /// </summary>
    [Description("Part allowed")]
    PartAllowed,

    /// <summary>
    /// None of the requested files may be placed in the destination folder.
    /// </summary>
    [Description("None allowed")]
    NoneAllowed
}