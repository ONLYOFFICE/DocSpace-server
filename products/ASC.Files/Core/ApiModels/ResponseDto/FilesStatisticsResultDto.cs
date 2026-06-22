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
/// The file statistics result parameters.
/// </summary>
public class FilesStatisticsResultDto
{
    /// <summary>
    /// The used space of files in the \"My Documents\" section.
    /// </summary>
    /// <example>{"title": "My Documents", "usedSpace": 10240}</example>
    public FilesStatisticsFolder MyDocumentsUsedSpace { get; set; }

    /// <summary>
    /// The used space of files in the \"Trash\" section.
    /// </summary>
    /// <example>{"title": "My Documents", "usedSpace": 512}</example>
    public FilesStatisticsFolder TrashUsedSpace { get; set; }

    /// <summary>
    /// The used space of files in the \"Archive\" section.
    /// </summary>
    /// <example>{"title": "My Documents", "usedSpace": 2048}</example>
    public FilesStatisticsFolder ArchiveUsedSpace { get; set; }

    /// <summary>
    /// The used space of files in the \"Rooms\" section.
    /// </summary>
    /// <example>{"title": "My Documents", "usedSpace": 5120}</example>
    public FilesStatisticsFolder RoomsUsedSpace { get; set; }

    /// <summary>
    /// The used space of files in the \"AI agents\" section.
    /// </summary>
    /// <example>{"title": "My Documents", "usedSpace": 1024}</example>
    public FilesStatisticsFolder AiAgentsUsedSpace { get; set; }

    /// <summary>
    /// The used space of files in the \"Forms\" section.
    /// </summary>
    /// <example>{"title": "My Documents", "usedSpace": 1024}</example>
    public FilesStatisticsFolder FormsUsedSpace { get; set; }
}

/// <summary>
/// The file statictics folder parameters.
/// </summary>
public class FilesStatisticsFolder
{
    /// <summary>
    /// The folder title.
    /// </summary>
    /// <example>My Documents</example>
    public string Title { get; set; }

    /// <summary>
    /// The used space in the folder.
    /// </summary>
    /// <example>1048576</example>
    public long UsedSpace { get; set; }
}