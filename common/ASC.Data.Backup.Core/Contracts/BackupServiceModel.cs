// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Data.Backup.Contracts;


public enum BackupStorageType
{
    [SwaggerEnum(Description = "Documents")]
    Documents = 0,

    [SwaggerEnum(Description = "Thridparty documents")]
    ThridpartyDocuments = 1,

    [SwaggerEnum(Description = "Custom cloud")]
    CustomCloud = 2,

    [SwaggerEnum(Description = "Local")]
    Local = 3,

    [SwaggerEnum(Description = "Data store")]
    DataStore = 4,

    [SwaggerEnum(Description = "Thirdparty consumer")]
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
}

public class BackupHistoryRecord
{
    [SwaggerSchemaCustom("Backup ID")]
    public Guid Id { get; set; }

    [SwaggerSchemaCustomString("File name")]
    public string FileName { get; set; }

    [SwaggerSchemaCustomString("Storage type", Example = "Documents")]
    public BackupStorageType StorageType { get; set; }

    [SwaggerSchemaCustom("Creation date")]
    public DateTime CreatedOn { get; set; }

    [SwaggerSchemaCustom("Expiration date")]
    public DateTime ExpiresOn { get; set; }
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
    public int TenantId { get; init; }
    public Guid BackupId { get; set; }
    public BackupStorageType StorageType { get; set; }
    public string FilePathOrId { get; set; }
    public bool NotifyAfterCompletion { get; init; }
    public Dictionary<string, string> StorageParams { get; set; }
    public string ServerBaseUri { get; init; }
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
