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