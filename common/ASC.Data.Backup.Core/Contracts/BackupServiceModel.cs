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
/// Where a backup archive is stored. The value decides which keys the storage parameters of an operation
/// have to carry: `Documents` and `ThridpartyDocuments` need a folder ID, `Local` needs a file path and
/// works on a standalone installation only, `ThirdPartyConsumer` needs the module of the consumer plus its
/// settings, and `DataStore` needs none. `CustomCloud` is not implemented and no storage can be built for
/// it.
/// </summary>
public enum BackupStorageType
{
    [Description("Documents")]
    Documents = 0,

    [Description("Thridparty documents")]
    ThridpartyDocuments = 1,

    [Description("Custom cloud")]
    CustomCloud = 2,

    [Description("Local")]
    Local = 3,

    [Description("Data store")]
    DataStore = 4,

    [Description("Thirdparty consumer")]
    ThirdPartyConsumer = 5
}

public class StartBackupRequest
{
    public int TenantId { get; init; }
    public Guid UserId { get; init; }
    public BackupStorageType StorageType { get; init; }
    public string StorageBasePath { get; set; }
    public Dictionary<string, string> StorageParams { get; init; }
    public string ServerBaseUri { get; init; }
    public bool Dump { get; init; }
    public IDictionary<string, string> Headers { get; init; }
}

/// <summary>
/// One stored backup of a portal.
/// </summary>
public class BackupHistoryRecord
{
    /// <summary>
    /// The ID of the backup, which is the same value as the `taskId` the backup was started with. Pass it to
    /// `DELETE api/2.0/backup/deletebackup/{id}` or as the `backupId` of
    /// `POST api/2.0/backup/startrestore`.
    /// </summary>
    /// <example>5f4b2c1a-9d3e-4f8a-b7c6-1e2d3f4a5b6c</example>
    public required Guid Id { get; set; }

    /// <summary>
    /// The name of the stored archive. It is built from the portal alias and the moment the backup started,
    /// or from `workspace` instead of the alias for a backup of the whole server.
    /// </summary>
    /// <example>myportal_2026-03-01_02-15-00.tar.gz</example>
    public required string FileName { get; set; }

    /// <summary>
    /// The storage the archive was written to, reported as a number rather than as a name.
    /// </summary>
    /// <example>0</example>
    public required BackupStorageType StorageType { get; set; }

    /// <summary>
    /// The date and time the backup was stored at, in UTC.
    /// </summary>
    /// <example>2026-03-01T02:15:00Z</example>
    public required DateTime CreatedOn { get; set; }

    /// <summary>
    /// The date and time a background cleaner removes this backup at. Only a backup written to `DataStore`
    /// expires, one day after it was stored; for every other storage type this is `0001-01-01T00:00:00`,
    /// which means the backup is kept until it is deleted by hand or pushed out by the stored-copies limit
    /// of a schedule.
    /// </summary>
    /// <example>0001-01-01T00:00:00Z</example>
    public required DateTime ExpiresOn { get; set; }
}

public class StartTransferRequest
{
    public int TenantId { get; init; }
    public string TargetRegion { get; init; }
    public bool NotifyUsers { get; init; }
}

public class TransferRegion
{
    public string Name { get; set; }
    public string BaseDomain { get; set; }
    public bool IsCurrentRegion { get; set; }
}

public class StartRestoreRequest
{
    public int TenantId { get; set; }
    public Guid BackupId { get; set; }
    public BackupStorageType StorageType { get; set; }
    public string FilePathOrId { get; set; }
    public bool NotifyAfterCompletion { get; init; }
    public Dictionary<string, string> StorageParams { get; set; }
    public string ServerBaseUri { get; init; }
    public bool Dump { get; init; }
}

public class CreateScheduleRequest : StartBackupRequest
{
    public string Cron { get; init; }
    public int NumberOfBackupsStored { get; init; }
}

public class ScheduleResponse
{
    public BackupStorageType StorageType { get; init; }
    public string StorageBasePath { get; init; }
    public int NumberOfBackupsStored { get; init; }
    public string Cron { get; init; }
    public DateTime LastBackupTime { get; init; }
    public Dictionary<string, string> StorageParams { get; init; }
    public bool Dump { get; set; }
}