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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The state of a background document building task: how far it has got, how it ended, and the file it produced.
/// </summary>
public class DocumentBuilderTaskDto
{
    /// <summary>
    /// The identifier of the task. It is derived from the portal, the account and the kind of report, so starting the
    /// same report again while it runs returns this same value, which is how a resumed poll is told from a newly
    /// queued build.
    /// </summary>
    /// <example>DocumentBuilderTask_1_c2b0e3a4-1f6c-4c2e-9c4f-3a5d8b7e1c22</example>
    public required string Id { get; set; }

    /// <summary>
    /// The message of the failure that stopped the build. It is filled in only for a task that ended in the failed
    /// state, and stays empty while the task runs and after it succeeds.
    /// </summary>
    /// <example>The document service is unavailable</example>
    public required string Error { get; set; }

    /// <summary>
    /// How far the build has got, from 0 to 100. It advances in a few coarse steps rather than smoothly, so it is a
    /// progress hint and not a measure of the time left; wait on the completion flag instead.
    /// </summary>
    /// <example>60</example>
    public required int Percentage { get; set; }

    /// <summary>
    /// True once the task has stopped for any reason, a failure and a cancellation included. It is the field to poll
    /// on, and the status tells those outcomes apart.
    /// </summary>
    /// <example>false</example>
    public required bool IsCompleted { get; set; }

    /// <summary>
    /// How the task ended, or that it has not started yet. Read it together with the completion flag: a stopped task
    /// can be a finished build, a cancelled one or a failure, and only this field separates them.
    /// </summary>
    /// <example>2</example>
    public required DistributedTaskStatus Status { get; set; }

    /// <summary>
    /// The identifier of the produced file, set only after the build succeeds. Pass it to the file operations to
    /// download, move or delete the report, which is saved as an ordinary file in the portal.
    /// </summary>
    /// <example>1234</example>
    public required object ResultFileId { get; set; }

    /// <summary>
    /// The name the produced file was saved with, extension included. The name is built from the subject of the
    /// report and is not unique: a second build adds another file instead of replacing the first.
    /// </summary>
    /// <example>usage_report.xlsx</example>
    public required string ResultFileName { get; set; }

    /// <summary>
    /// The address of the produced file in the document editor, relative to the portal root, so prefix it with the
    /// portal address to open it. It stays empty until the build succeeds.
    /// </summary>
    /// <example>/doceditor?fileid=1234</example>
    public required string ResultFileUrl { get; set; }

    public static DocumentBuilderTaskDto Get<TId, TData>(DocumentBuilderTask<TId, TData> task)
    {
        return task == null
            ? null
            : new DocumentBuilderTaskDto
            {
                Id = task.Id,
                Error = task.Exception?.Message,
                Percentage = (int)task.Percentage,
                IsCompleted = task.IsCompleted,
                Status = task.Status,
                ResultFileId = task.ResultFileId,
                ResultFileName = task.ResultFileName,
                ResultFileUrl = task.ResultFileUrl
            };
    }
}