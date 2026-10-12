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

using ASC.Files.Core.Services.DocumentBuilderService;

namespace ASC.Web.Api.ApiModels.RequestsDto;

/// <summary>
/// The file format the queued audit report is built in and the period it covers.
/// </summary>
public class AuditReportRequestDto
{
    /// <summary>
    /// The format the report file is written in: a spreadsheet workbook, which is the default, or a comma-separated
    /// text file.
    /// </summary>
    /// <example>Xlsx</example>
    [FromQuery(Name = "format")]
    public AuditReportFormat Format { get; set; } = AuditReportFormat.Xlsx;

    /// <summary>
    /// The earliest moment a reported event may have been recorded at, read as a UTC instant.
    /// </summary>
    /// <example>2026-09-01T00:00:00Z</example>
    [FromQuery(Name = "from")]
    public ApiDateTime From { get; set; }

    /// <summary>
    /// The latest moment a reported event may have been recorded at, read as a UTC instant in the same way as `from`.
    /// </summary>
    /// <example>2026-09-30T23:59:59Z</example>
    [FromQuery(Name = "to")]
    public ApiDateTime To { get; set; }
}
