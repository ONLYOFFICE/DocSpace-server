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

namespace ASC.Data.Backup.Core.IntegrationEvents.Events;

[ProtoContract]
public record BackupRequestIntegrationEvent : IntegrationEvent
{
    private BackupRequestIntegrationEvent()
    {
        StorageParams = new Dictionary<string, string>();
    }

    public BackupRequestIntegrationEvent(BackupStorageType storageType,
                                  int tenantId,
                                  Guid createBy,
                                  Dictionary<string, string> storageParams,
                                  bool isScheduled = false,
                                  int backupsStored = 0,
                                  string storageBasePath = "",
                                  string serverBaseUri = null,
                                  bool dump = false,
                                  string taskId = null,
                                  int billingSessionId = 0,
                                  DateTime billingSessionExpire = default,
                                  IDictionary<string, string> headers = null) : base(createBy, tenantId)
    {
        StorageType = storageType;
        StorageParams = storageParams;
        IsScheduled = isScheduled;
        BackupsStored = backupsStored;
        StorageBasePath = storageBasePath;
        ServerBaseUri = serverBaseUri;
        Dump = dump;
        TaskId = taskId;
        BillingSessionId = billingSessionId;
        BillingSessionExpire = billingSessionExpire;
        Headers = headers;
    }

    [ProtoMember(1)]
    public BackupStorageType StorageType { get; private init; }

    [ProtoMember(2)]
    public Dictionary<string, string> StorageParams { get; private init; }

    [ProtoMember(4)]
    public bool IsScheduled { get; private init; }

    [ProtoMember(5)]
    public int BackupsStored { get; private init; }

    [ProtoMember(6)]
    public string StorageBasePath { get; private init; }

    [ProtoMember(7)]
    public string ServerBaseUri { get; private init; }

    [ProtoMember(8)]
    public bool Dump { get; private init; }

    [ProtoMember(9)]
    public string TaskId { get; private init; }

    [ProtoMember(10)]
    public int BillingSessionId { get; private init; }

    [ProtoMember(11)]
    public DateTime BillingSessionExpire { get; private init; }

    [ProtoMember(12)]
    public IDictionary<string, string> Headers { get; private init; }
}