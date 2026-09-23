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
/// The space that stored documents take in each section of the portal, in bytes. The figures cover every account of
/// the portal rather than the caller alone, and a section the portal does not have comes back as null instead of a
/// zero figure.
/// </summary>
public class FilesStatisticsResultDto
{
    /// <summary>
    /// The space taken by the personal "Files" sections of all accounts of the portal added together. An item deleted
    /// to the trash keeps taking space and is counted in `trashUsedSpace` until the trash is emptied.
    /// </summary>
    /// <example>{"title": "Files", "usedSpace": 10240}</example>
    public FilesStatisticsFolder MyDocumentsUsedSpace { get; set; }

    /// <summary>
    /// The space held by the items deleted to the trash from any section, which is given back only when the trash is
    /// emptied or the items are erased for good.
    /// </summary>
    /// <example>{"title": "Trash", "usedSpace": 512}</example>
    public FilesStatisticsFolder TrashUsedSpace { get; set; }

    /// <summary>
    /// The space taken by the content of the archived rooms, the archived form filling rooms included. Restoring a
    /// room moves its space back to `roomsUsedSpace` or `formsUsedSpace`.
    /// </summary>
    /// <example>{"title": "Archive", "usedSpace": 2048}</example>
    public FilesStatisticsFolder ArchiveUsedSpace { get; set; }

    /// <summary>
    /// The space taken by the content of the active rooms, except the form filling rooms, whose content is reported
    /// in `formsUsedSpace`. Archiving a room moves its space to `archiveUsedSpace`.
    /// </summary>
    /// <example>{"title": "Rooms", "usedSpace": 5120}</example>
    public FilesStatisticsFolder RoomsUsedSpace { get; set; }

    /// <summary>
    /// The space taken by the content of the "AI agents" section, which exists only in a portal where the AI agents
    /// feature is active; creating an AI room is not enough to bring the section into being.
    /// </summary>
    /// <example>{"title": "AI agents", "usedSpace": 1024}</example>
    public FilesStatisticsFolder AiAgentsUsedSpace { get; set; }

    /// <summary>
    /// The space taken by the content of the active form filling rooms, which is kept apart from `roomsUsedSpace`
    /// even though those rooms are listed among the rooms.
    /// </summary>
    /// <example>{"title": "Forms", "usedSpace": 1024}</example>
    public FilesStatisticsFolder FormsUsedSpace { get; set; }
}

/// <summary>
/// One section of the portal and the space its documents take.
/// </summary>
public class FilesStatisticsFolder
{
    /// <summary>
    /// The name of the section as the interface shows it, translated into the language used by the caller, so it
    /// suits display but not matching - which section an entry describes is told by the field that carries it.
    /// </summary>
    /// <example>Files</example>
    public string Title { get; set; }

    /// <summary>
    /// The size of the files kept in the section, in bytes, counting every folder and room inside it; 0 means the
    /// section holds nothing. The counter is brought up to date as an operation finishes, so a reading taken right
    /// after an upload or a delete can still show the previous value.
    /// </summary>
    /// <example>1048576</example>
    public long UsedSpace { get; set; }
}