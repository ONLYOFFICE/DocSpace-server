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
/// The backup schedule of a portal.
/// </summary>
public class ScheduleDto
{
    /// <summary>
    /// The storage the scheduled archives are written to, reported as a number rather than as the name the
    /// schedule was created with.
    /// </summary>
    /// <example>0</example>
    public required BackupStorageType StorageType { get; set; }

    /// <summary>
    /// The settings of the storage, as an object keyed by parameter name - not as the array of key and value
    /// pairs the schedule was created with, so it cannot be sent back unchanged. For every storage type
    /// except `ThirdPartyConsumer` the `folderId` key is built from the stored base path.
    /// </summary>
    /// <example>{"folderId": "1234"}</example>
    public required Dictionary<string, string> StorageParams { get; set; }

    /// <summary>
    /// When the backup runs, read back from the stored cron expression. `day` is 0 for a daily schedule,
    /// because a daily one has no day.
    /// </summary>
    /// <example>{"period": 0, "hour": 2, "day": 0}</example>
    public required CronParamsDto CronParams { get; init; }

    /// <summary>
    /// The number of scheduled copies kept. It is null, not 0, when the schedule keeps an unlimited number.
    /// </summary>
    /// <example>5</example>
    public int? BackupsStored { get; init; }

    /// <summary>
    /// The date and time the schedule last ran at. It is `0001-01-01T00:00:00` until the schedule has run
    /// for the first time.
    /// </summary>
    /// <example>2026-01-01T00:00:00Z</example>
    public required DateTime LastBackupTime { get; set; }

    /// <summary>
    /// Specifies whether this schedule backs up the whole server instead of one portal.
    /// </summary>
    /// <example>false</example>
    public required bool Dump { get; set; }
}

/// <summary>
/// The time a scheduled backup runs at.
/// </summary>
public class CronParamsDto
{
    /// <summary>
    /// How often the backup runs: 0 for every day, 1 for every week and 2 for every month.
    /// </summary>
    /// <example>0</example>
    public BackupPeriod Period { get; init; }

    /// <summary>
    /// The hour of the day the backup starts at, from 0 to 23.
    /// </summary>
    /// <example>2</example>
    public int Hour { get; init; }

    /// <summary>
    /// The day the backup runs on: the day of the week from 1 to 7, Sunday being 1, for a weekly schedule,
    /// and the day of the month from 1 to 31 for a monthly one. It is 0 for a daily schedule.
    /// </summary>
    /// <example>1</example>
    public int Day { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class CronParamsDtoMapper
{
    public static partial CronParamsDto Map(this CronParams source);
}
