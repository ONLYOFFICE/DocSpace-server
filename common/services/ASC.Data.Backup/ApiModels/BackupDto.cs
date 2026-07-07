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
/// The backup parameters.
/// </summary>
public class BackupDto
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
    /// Specifies if a dump will be created or not.
    /// </summary>
    /// <example>false</example>
    public bool Dump { get; set; }
}


/// <summary>
/// Parameters for calculating the number of backups.
/// </summary>
public class BackupsCountDto
{
    /// <summary>
    /// The from date.
    /// </summary>
    /// <example>2025-01-01T00:00:00Z</example>
    [FromQuery(Name = "from")]
    public DateTime? From { get; set; }

    /// <summary>
    /// The to date.
    /// </summary>
    /// <example>2025-12-31T23:59:59Z</example>
    [FromQuery(Name = "to")]
    public DateTime? To { get; set; }

    /// <summary>
    /// Specifies if the backups are paid or not.
    /// </summary>
    /// <example>false</example>
    [FromQuery(Name = "paid")]
    public bool Paid { get; set; }
}

/// <summary>
/// The number of backups.
/// </summary>
public class BackupsCountResultDto
{
    /// <summary>
    /// The number of free backups.
    /// </summary>
    /// <example>3</example>
    public int Free { get; set; }

    /// <summary>
    /// The number of paid backups.
    /// </summary>
    /// <example>5</example>
    public int Paid { get; set; }
}

/// <summary>
/// Backup service state.
/// </summary>
public class BackupServiceStateDto
{
    /// <summary>
    /// Specifies if the backup service is enabled or not.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }
}