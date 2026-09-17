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
/// The answer to a report generation request: the queued task, the form whose answers are collected, and whether the
/// report file is being created or refreshed.
/// </summary>
public class XlsxReportResponseDto
{
    /// <summary>
    /// The original form the answers are collected from. It is not the produced spreadsheet - that one arrives with
    /// the task, once the task reports completion.
    /// </summary>
    public FileDto<int> Form { get; set; }

    /// <summary>
    /// The queued generation. Poll it with `GET api/2.0/files/file/{fileId}/xlsx` until it reports completion, and
    /// take the produced file from it then.
    /// </summary>
    public DocumentBuilderTaskDto Task { get; set; }

    /// <summary>
    /// True when this run creates the report file, false when an existing report is rewritten in place, which means
    /// it keeps its id and the links already shared for it.
    /// </summary>
    /// <example>true</example>
    public bool IsNewFile { get; set; }
}
