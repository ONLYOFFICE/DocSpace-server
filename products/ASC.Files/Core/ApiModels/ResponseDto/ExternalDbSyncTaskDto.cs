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
/// The state of the job that exports the collected form data of a form filling room into the external database of the
/// portal.
/// </summary>
public class ExternalDbSyncTaskDto
{
    /// <summary>
    /// The identifier of the job, which stays the same while a job for this room exists and is worth quoting when a
    /// failure has to be traced in the portal logs. Polling is done by room, so the value is not needed to read the
    /// state again.
    /// </summary>
    /// <example>ExternalDbSyncTask_1_42</example>
    public required string Id { get; set; }

    /// <summary>
    /// The message of a failure that stopped the whole job. It is empty while the job is running and after a job that
    /// ended without such a failure; a job that finished with individual forms rejected reports those in `forms` and
    /// leaves this field empty.
    /// </summary>
    /// <example>Connection refused</example>
    public string? Error { get; set; }

    /// <summary>
    /// How much of the work is done, from 0 to 100. It advances as the forms of the room are processed one by one, so
    /// it is a usable progress indicator for a room with many forms and jumps straight to the end for a room with
    /// one.
    /// </summary>
    /// <example>75</example>
    public required int Percentage { get; set; }

    /// <summary>
    /// Whether the job has ended. It is set both for a job that finished its work and for one that stopped on an
    /// error, so this is the flag to poll for, and `status` and `error` are what tell the two apart.
    /// </summary>
    /// <example>false</example>
    public required bool IsCompleted { get; set; }

    /// <summary>
    /// How the job ended, or how far it has got: queued, running, finished, cancelled or failed. It is the only field
    /// that separates a successful end from a failed one once `isCompleted` is set.
    /// </summary>
    /// <example>2</example>
    public required DistributedTaskStatus Status { get; set; }

    /// <summary>
    /// The outcome for every original form of the room, one entry each. The list is empty while the job is running
    /// and is filled in only when the job ends, so it is what to read after `isCompleted` turns true; it stays empty
    /// for a room that holds no forms at all.
    /// </summary>
    /// <example>[{"id": 42, "title": "Application.pdf", "success": true, "error": null}]</example>
    public required List<ExternalDbSyncFormResultDto> Forms { get; set; }

    public static ExternalDbSyncTaskDto? Get(ExternalDbSyncTask task)
    {
        return task == null
            ? null
            : new ExternalDbSyncTaskDto
            {
                Id = task.Id,
                Error = task.Exception?.Message,
                Percentage = (int)task.Percentage,
                IsCompleted = task.IsCompleted,
                Status = task.Status,
                Forms = task.FinalForms?.ToList() ?? []
            };
    }
}
