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
/// The request parameters for generating a folder history report.
/// </summary>
public class FolderHistoryReportRequestDto
{
    /// <summary>
    /// The folder ID whose history is exported.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "folderId")]
    public required int FolderId { get; set; }

    /// <summary>
    /// The output file format of the report. Defaults to XLSX.
    /// </summary>
    /// <example>Xlsx</example>
    [FromQuery(Name = "format")]
    public AuditReportFormat Format { get; set; } = AuditReportFormat.Xlsx;

    /// <summary>
    /// The start date of the history period to export.
    /// </summary>
    /// <example>2025-01-01T00:00:00.0000000Z</example>
    [FromQuery(Name = "from")]
    public ApiDateTime From { get; set; }

    /// <summary>
    /// The end date of the history period to export.
    /// </summary>
    /// <example>2025-12-31T23:59:59.0000000Z</example>
    [FromQuery(Name = "to")]
    public ApiDateTime To { get; set; }
}
