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

namespace ASC.People.ApiModels.ResponseDto;

/// <summary>
/// The task progress response parameters.
/// </summary>
public class TaskProgressResponseDto
{
    /// <summary>
    /// The ID of the queued job. It identifies this run of the job and changes every time the job is started again.
    /// </summary>
    /// <example>task-123456</example>
    public required string Id { get; set; }

    /// <summary>
    /// The message of the error that stopped the job. It is empty while the job is running and after a job that
    /// succeeded, and it is the only place where the reason for a failure is reported.
    /// </summary>
    /// <example>An error occurred during processing</example>
    public string Error { get; set; }

    /// <summary>
    /// The share of the job that is already done, from 0 to 100.
    /// </summary>
    /// <example>75</example>
    public required int Percentage { get; set; }

    /// <summary>
    /// Specifies whether the job has stopped running. This is the field to poll: true means the job will not change
    /// any more, whether it succeeded, failed or was cancelled, and `status` tells which of the three it is.
    /// </summary>
    /// <example>false</example>
    public required bool IsCompleted { get; set; }

    /// <summary>
    /// The state of the job: `Created` while it waits in the queue, `Running` while it works, `Completed` once it has
    /// finished on its own, `Canceled` after a terminate operation, and `Failted` when it stopped on an error, in
    /// which case `error` carries the reason.
    /// </summary>
    /// <example>Running</example>
    public required DistributedTaskStatus Status { get; set; }

    public static TaskProgressResponseDto Get(DistributedTaskProgress progressItem)
    {
        return progressItem == null
            ? null
            : new TaskProgressResponseDto
            {
                Id = progressItem.Id,
                Error = progressItem.Exception?.Message,
                Percentage = (int)progressItem.Percentage,
                IsCompleted = progressItem.IsCompleted,
                Status = progressItem.Status
            };
    }
}