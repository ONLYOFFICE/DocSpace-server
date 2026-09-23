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
/// The request parameters for restoring a portal from a backup.
/// </summary>
public class BackupRestoreDto
{
    /// <summary>
    /// The ID of the backup to restore from, as listed by `GET api/2.0/backup/getbackuphistory`. Send
    /// anything that is not a GUID to restore from a file given by `storageParams` instead; an all-zero GUID
    /// selects neither, because it parses as a GUID and then matches no record.
    /// </summary>
    /// <example>11111111-1111-1111-1111-111111111111</example>
    public required string BackupId { get; set; }

    /// <summary>
    /// The storage the archive is read from. It defaults to `Documents` and is only used when `backupId` is
    /// not a GUID, because a known backup carries the storage of its own record.
    /// </summary>
    /// <example>Documents</example>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BackupStorageType? StorageType { get; set; }

    /// <summary>
    /// The location of the archive, as an array of key and value pairs. The key read here is `filePath` -
    /// not the `folderId` a backup is started with - and it holds a file ID for `Documents`, a
    /// provider-specific file ID for `ThridpartyDocuments` and a path on the server for `Local`. It is only
    /// used when `backupId` is not a GUID.
    /// </summary>
    /// <example>[{"key": "filePath", "value": "1234"}]</example>
    public IEnumerable<ItemKeyValuePair<object, object>> StorageParams { get; set; }

    /// <summary>
    /// Chooses who is emailed when the restoring starts and when it finishes: every active user of the
    /// portal when true, and its owner alone when false. Mail goes only to accounts that have been
    /// activated, so this decides the audience rather than whether anybody is notified at all.
    /// </summary>
    /// <example>true</example>
    public bool Notify { get; set; }

    /// <summary>
    /// Restores the whole server rather than this one portal. It requires the space access permission.
    /// </summary>
    /// <example>false</example>
    public bool Dump { get; set; }
}