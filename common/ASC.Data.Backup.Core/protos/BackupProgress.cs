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

namespace ASC.Data.Backup.Contracts;

/// <summary>
/// What kind of job a progress report belongs to. `Transfer` describes moving a portal between regions and
/// is never reported by this API, because no operation of it starts such a job.
/// </summary>
public enum BackupProgressEnum
{
    [Description("Backup")]
    Backup,

    [Description("Restore")]
    Restore,

    [Description("Transfer")]
    Transfer
}

/// <summary>
/// The state of one backup or restoring job.
/// </summary>
public record BackupProgress
{
    /// <summary>
    /// Specifies whether the job has stopped running. This is the field to poll: true means the job will not
    /// change any more, whether it succeeded, failed or was cancelled, and `status` tells which of the three
    /// it is.
    /// </summary>
    /// <example>false</example>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// The share of the job that is already done, from 0 to 100. A job that has only been queued reports 0,
    /// because the work starts when a separate worker service picks it up.
    /// </summary>
    /// <example>50</example>
    public int Progress { get; set; }

    /// <summary>
    /// The message of the error that stopped the job. It is an empty string, not null, while the job runs
    /// and after a job that succeeded, so the sign of a failure is a non-empty value - and this is the only
    /// place where the reason is reported.
    /// </summary>
    /// <example>An error occurred during processing</example>
    public string Error { get; set; }

    /// <summary>
    /// A message about a job that stopped without failing: it names the entry inside the archive that lists
    /// the files which could not be read, when a backup finished without some of them, and it says so when
    /// the job was cancelled. It is an empty string otherwise, and it is only ever filled in for a backup
    /// job - a cancelled restoring job leaves it empty.
    /// </summary>
    /// <example>Some files were not included in the backup. For more details, please check storage/missing_info</example>
    public string Warning { get; set; }

    /// <summary>
    /// The link to download the stored archive. It is an empty string until the archive has been uploaded,
    /// and it is only ever filled in for a backup job, never for a restoring one.
    /// </summary>
    /// <example>https://example.com/products/files/httphandlers/filehandler.ashx?action=download&amp;fileid=1234</example>
    public string Link { get; set; }

    /// <summary>
    /// The ID of the portal the job belongs to, or -1 for a job that covers the whole server.
    /// </summary>
    /// <example>1</example>
    public int TenantId { get; set; }

    /// <summary>
    /// Whether this is a backup or a restoring job, reported as a number rather than as a name.
    /// </summary>
    /// <example>0</example>
    public BackupProgressEnum BackupProgressEnum { get; set; }

    /// <summary>
    /// The state of the job: `Created` while it waits for a worker to pick it up, `Running` while it works,
    /// `Completed` once it has finished on its own, `Canceled` after it was cancelled, and `Failted` when it
    /// stopped on an error, in which case `error` carries the reason. Reported as a number rather than as a
    /// name.
    /// </summary>
    /// <example>1</example>
    public DistributedTaskStatus Status { get; set; }

    /// <summary>
    /// The ID of the job. It is the handle to poll this operation with, and for a backup job it also becomes
    /// the `id` of the record in `GET api/2.0/backup/getbackuphistory`.
    /// </summary>
    /// <example>11111111-1111-1111-1111-111111111111</example>
    public string TaskId { get; set; }
}