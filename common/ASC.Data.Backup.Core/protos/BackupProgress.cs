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
/// The backup progress type
/// </summary>
public enum BackupProgressEnum
{
    [Description("Backup")]
    Backup,

    [Description("Restore")]
    Restore,

    [Description("Transfer")]
    Transfer
}

/// <summary>
/// The backup progress parameters.
/// </summary>
public record BackupProgress
{
    /// <summary>
    /// Specifies if the backup is completed or not.
    /// </summary>
    /// <example>false</example>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// The backup progress in percentage.
    /// </summary>
    /// <example>50</example>
    public int Progress { get; set; }

    /// <summary>
    /// The backup error message.
    /// </summary>
    /// <example>null</example>
    public string Error { get; set; }

    /// <summary>
    /// The backup warning message.
    /// </summary>
    /// <example>null</example>
    public string Warning { get; set; }

    /// <summary>
    /// The backup link.
    /// </summary>
    /// <example>https://example.com/backup/task_123</example>
    public string Link { get; set; }

    /// <summary>
    /// The tenant ID.
    /// </summary>
    /// <example>1</example>
    public int TenantId { get; set; }

    /// <summary>
    /// The backup progress type.
    /// </summary>
    /// <example>Backup</example>
    public BackupProgressEnum BackupProgressEnum { get; set; }

    /// <summary>
    /// The backup progress status.
    /// </summary>
    /// <example>Running</example>
    public DistributedTaskStatus Status { get; set; }

    /// <summary>
    /// The task ID.
    /// </summary>
    /// <example>task_123</example>
    public string TaskId { get; set; }
}