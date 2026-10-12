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

#nullable enable

/// <summary>
/// What happened to one original form while the room was being exported to the external database.
/// </summary>
public class ExternalDbSyncFormResultDto
{
    /// <summary>
    /// The file of the original form whose collected data was exported. It is the form itself, not one of the filled
    /// copies, so the same id can be read with the file operations of the portal.
    /// </summary>
    /// <example>42</example>
    public int Id { get; set; }

    /// <summary>
    /// The name of that form file at the moment of the export. It is empty when the form file no longer exists, which
    /// is also the case in which the export of that entry fails.
    /// </summary>
    /// <example>Application.pdf</example>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Whether the data of this form reached the external database. One rejected form does not stop the others, so a
    /// finished job can hold both successful and failed entries.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// Why this form was not exported. It is empty for a successful entry, and for a failed one it carries either the
    /// message of the underlying failure or the generic export error of the portal.
    /// </summary>
    /// <example>Connection refused</example>
    public string? Error { get; set; }
}
