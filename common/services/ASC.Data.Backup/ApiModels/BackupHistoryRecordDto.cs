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

namespace ASC.Data.Backup.ApiModels;

/// <summary>
/// One stored backup of a portal.
/// </summary>
public class BackupHistoryRecordDto
{
    /// <summary>
    /// The ID of the backup, which is the same value as the `taskId` the backup was started with. Pass it to
    /// `DELETE api/2.0/backup/deletebackup/{id}` or as the `backupId` of
    /// `POST api/2.0/backup/startrestore`.
    /// </summary>
    /// <example>11111111-1111-1111-1111-111111111111</example>
    public required Guid Id { get; init; }

    /// <summary>
    /// The name of the stored archive. It is built from the portal alias and the moment the backup started,
    /// or from `workspace` instead of the alias for a backup of the whole server.
    /// </summary>
    /// <example>myportal_2026-03-01_02-15-00.tar.gz</example>
    public required string FileName { get; init; }

    /// <summary>
    /// The storage the archive was written to, reported as a number rather than as a name.
    /// </summary>
    /// <example>0</example>
    public required BackupStorageType StorageType { get; init; }

    /// <summary>
    /// The date and time the backup was stored at, in UTC.
    /// </summary>
    /// <example>2026-03-01T02:15:00Z</example>
    public required DateTime CreatedOn { get; init; }

    /// <summary>
    /// The date and time a background cleaner removes this backup at. Only a backup written to `DataStore`
    /// expires, one day after it was stored; for every other storage type this is `0001-01-01T00:00:00`,
    /// which means the backup is kept until it is deleted by hand or pushed out by the stored-copies limit
    /// of a schedule.
    /// </summary>
    /// <example>0001-01-01T00:00:00Z</example>
    public required DateTime ExpiresOn { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class BackupHistoryRecordDtoMapper
{
    [MapProperty(nameof(BackupRecord.Name), nameof(BackupHistoryRecordDto.FileName))]
    public static partial BackupHistoryRecordDto Map(this BackupRecord source);
}
