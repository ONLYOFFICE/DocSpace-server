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
/// The request parameters for setting the backup schedule.
/// </summary>
public class BackupScheduleDto
{
    /// <summary>
    /// The storage the scheduled archives are written to. It defaults to `Documents`, and it decides which
    /// keys `storageParams` has to carry.
    /// </summary>
    /// <example>Documents</example>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BackupStorageType? StorageType { get; set; }

    /// <summary>
    /// The settings of the chosen storage, as an array of key and value pairs. `Documents` and
    /// `ThridpartyDocuments` need `folderId`, `Local` needs `filePath`, `ThirdPartyConsumer` needs `module`
    /// plus the settings of that consumer, and `DataStore` needs none.
    /// </summary>
    /// <example>[{"key": "folderId", "value": "1234"}]</example>
    public IEnumerable<ItemKeyValuePair<object, object>> StorageParams { get; set; }

    /// <summary>
    /// The number of scheduled copies to keep, from 1 to 30. It defaults to 1, and only the copies this
    /// schedule creates are counted and removed - archives started by hand are left alone.
    /// </summary>
    /// <example>5</example>
    public int? BackupsStored { get; set; }

    /// <summary>
    /// When the backup runs. It is required: a request without it fails rather than falling back to a
    /// default.
    /// </summary>
    /// <example>{"period": "EveryDay", "hour": 2}</example>
    public Cron CronParams { get; set; }

    /// <summary>
    /// Schedules a backup of the whole server rather than of this one portal. It requires the space access
    /// permission and works on a standalone installation only.
    /// </summary>
    /// <example>false</example>
    public bool Dump { get; set; }
}

/// <summary>
/// The request parameters for the time the scheduled backup runs.
/// </summary>
public class Cron
{
    /// <summary>
    /// How often the backup runs: `EveryDay`, `EveryWeek` or `EveryMonth`. It defaults to `EveryDay`.
    /// </summary>
    /// <example>EveryDay</example>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BackupPeriod? Period { get; set; }

    /// <summary>
    /// The hour of the day the backup starts at, from 0 to 23. Minutes cannot be chosen - it always starts
    /// on the hour.
    /// </summary>
    /// <example>2</example>
    public int Hour { get; set; }

    /// <summary>
    /// The day the backup runs on: the day of the week from 1 to 7, Sunday being 1, for `EveryWeek`, and the
    /// day of the month from 1 to 31 for `EveryMonth`. Leave it out for `EveryDay` only - an omitted value is
    /// stored as 0, which neither of the other two periods accepts, so a weekly or monthly schedule sent
    /// without it fails.
    /// </summary>
    /// <example>1</example>
    public int? Day { get; set; }
}