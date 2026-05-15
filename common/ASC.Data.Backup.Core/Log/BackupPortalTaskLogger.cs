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

namespace ASC.Data.Backup.Core.Log;
internal static partial class BackupPortalTaskLogger
{
    [LoggerMessage(LogLevel.Debug, "begin backup {tenantId}")]
    public static partial void DebugBeginBackup(this ILogger<BackupPortalTask> logger, int tenantId);

    [LoggerMessage(LogLevel.Debug, "end backup {tenantId}")]
    public static partial void DebugEndBackup(this ILogger<BackupPortalTask> logger, int tenantId);

    [LoggerMessage(LogLevel.Debug, "files: {count}")]
    public static partial void DebugFilesCount(this ILogger<BackupPortalTask> logger, int count);

    [LoggerMessage(LogLevel.Debug, "dir remove start {subDir}")]
    public static partial void DebugDirRemoveStart(this ILogger<BackupPortalTask> logger, string subDir);

    [LoggerMessage(LogLevel.Debug, "dir remove end {subDir}")]
    public static partial void DebugDirRemoveEnd(this ILogger<BackupPortalTask> logger, string subDir);

    [LoggerMessage(LogLevel.Debug, "dump table scheme start {table}")]
    public static partial void DebugDumpTableSchemeStart(this ILogger<BackupPortalTask> logger, string table);

    [LoggerMessage(LogLevel.Debug, "dump table scheme stop {table}")]
    public static partial void DebugDumpTableSchemeStop(this ILogger<BackupPortalTask> logger, string table);

    [LoggerMessage(LogLevel.Debug, "dump table data stop {table}")]
    public static partial void DebugDumpTableDataStop(this ILogger<BackupPortalTask> logger, string table);

    [LoggerMessage(LogLevel.Debug, "dump table data start {table}")]
    public static partial void DebugDumpTableDataStart(this ILogger<BackupPortalTask> logger, string table);

    [LoggerMessage(LogLevel.Debug, "save to file {table}")]
    public static partial void DebugSaveTable(this ILogger<BackupPortalTask> logger, string table);

    [LoggerMessage(LogLevel.Debug, "begin backup storage")]
    public static partial void DebugBeginBackupStorage(this ILogger<BackupPortalTask> logger);

    [LoggerMessage(LogLevel.Debug, "end backup storage")]
    public static partial void DebugEndBackupStorage(this ILogger<BackupPortalTask> logger);

    [LoggerMessage(LogLevel.Debug, "backup file {path}")]
    public static partial void DebugBackupFile(this ILogger<BackupPortalTask> logger, string path);

    [LoggerMessage(LogLevel.Debug, "archive dir start {subDir}")]
    public static partial void DebugArchiveDirStart(this ILogger<BackupPortalTask> logger, string subDir);

    [LoggerMessage(LogLevel.Debug, "archive dir end {subDir}")]
    public static partial void DebugArchiveDirEnd(this ILogger<BackupPortalTask> logger, string subDir);

    [LoggerMessage(LogLevel.Debug, "begin saving data for module {name}")]
    public static partial void DebugBeginSavingDataForModule(this ILogger<BackupPortalTask> logger, ModuleName name);

    [LoggerMessage(LogLevel.Debug, "begin load table {table}")]
    public static partial void DebugBeginLoadTable(this ILogger<BackupPortalTask> logger, string table);

    [LoggerMessage(LogLevel.Debug, "end load table {table}")]
    public static partial void DebugEndLoadTable(this ILogger<BackupPortalTask> logger, string table);

    [LoggerMessage(LogLevel.Debug, "begin saving table {table}")]
    public static partial void DebugBeginSavingTable(this ILogger<BackupPortalTask> logger, string table);

    [LoggerMessage(LogLevel.Debug, "end saving table {table}")]
    public static partial void DebugEndSavingTable(this ILogger<BackupPortalTask> logger, string table);

    [LoggerMessage(LogLevel.Debug, "end saving data for module {table}")]
    public static partial void DebugEndSavingDataForModule(this ILogger<BackupPortalTask> logger, ModuleName table);

    [LoggerMessage(LogLevel.Error, "DumpTableScheme")]
    public static partial void ErrorDumpTableScheme(this ILogger<BackupPortalTask> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "SelectCount")]
    public static partial void ErrorSelectCount(this ILogger<BackupPortalTask> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "DumpTableData")]
    public static partial void ErrorDumpTableData(this ILogger<BackupPortalTask> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "backup attempt failure")]
    public static partial void WarningBackupAttemptFailure(this ILogger<BackupPortalTask> logger, Exception exception);

    [LoggerMessage(LogLevel.Warning, "can't backup file ({module}:{path})")]
    public static partial void WarningCanNotBackupFile(this ILogger<BackupPortalTask> logger, string module, string path, Exception exception);
}