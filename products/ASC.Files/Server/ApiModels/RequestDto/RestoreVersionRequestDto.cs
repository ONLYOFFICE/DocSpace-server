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
/// The request that brings an earlier version of a file back.
/// </summary>
public class RestoreVersionRequestDto<T>
{
    /// <summary>
    /// The file whose version is restored.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// The version to restore, as reported by `GET api/2.0/files/file/{fileId}/edit/history`. It has to name an
    /// existing version that is not the current one.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "version")]
    public int Version { get; set; } = 0;

    /// <summary>
    /// The address the content of the new version is fetched from instead of the stored version, which is how the
    /// document service hands back a document with a set of changes rolled back; left out, the stored version is
    /// used.
    /// </summary>
    /// <example>https://document-server.example.com/cache/files/conv_1_docx/output.docx</example>
    [FromQuery(Name = "url")]
    public string Url { get; set; } = null;
}