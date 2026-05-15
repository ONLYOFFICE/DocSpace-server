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

using ASC.Data.Backup.Services;

namespace ASC.Data.Backup.ApiModels;

/// <summary>
/// The backup schedule parameters.
/// </summary>
public class BackupScheduleDto
{
    /// <summary>
    /// The backup storage type.
    /// </summary>
    /// <example>Documents</example>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BackupStorageType? StorageType { get; set; }

    /// <summary>
    /// The backup storage parameters.
    /// </summary>
    /// <example>[{"key": "path", "value": "/backup"}]</example>
    public IEnumerable<ItemKeyValuePair<object, object>> StorageParams { get; set; }

    /// <summary>
    /// The maximum number of the stored backup copies.
    /// </summary>
    /// <example>5</example>
    public int? BackupsStored { get; set; }

    /// <summary>
    /// The backup cron parameters.
    /// </summary>
    /// <example>{"period": "EveryDay", "hour": 2, "day": 0}</example>
    public Cron CronParams { get; set; }

    /// <summary>
    /// Specifies if a dump will be created or not.
    /// </summary>
    /// <example>false</example>
    public bool Dump { get; set; }
}

/// <summary>
/// The backup cron parameters.
/// </summary>
public class Cron
{
    /// <summary>
    /// The backup period type.
    /// </summary>
    /// <example>EveryDay</example>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BackupPeriod? Period { get; set; }

    /// <summary>
    /// The time of the day to start the backup process.
    /// </summary>
    /// <example>0</example>
    public int Hour { get; set; }

    /// <summary>
    /// The day of the week to start the backup process.
    /// </summary>
    /// <example>0</example>
    public int? Day { get; set; }
}