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
internal static partial class RestoreDbModuleTaskLogger
{
    [LoggerMessage(LogLevel.Debug, "begin restore data for module {moduleName}")]
    public static partial void DebugBeginRestoreDataForModule(this ILogger<RestoreDbModuleTask> logger, ModuleName moduleName);

    [LoggerMessage(LogLevel.Debug, "begin restore table {tableName}")]
    public static partial void DebugBeginRestoreTable(this ILogger<RestoreDbModuleTask> logger, string tableName);

    [LoggerMessage(LogLevel.Debug, "{rows} rows inserted for table {tableName}")]
    public static partial void DebugRowsInserted(this ILogger<RestoreDbModuleTask> logger, int rows, string tableName);

    [LoggerMessage(LogLevel.Debug, "end restore data for module {moduleName}")]
    public static partial void DebugEndRestoreDataForModule(this ILogger<RestoreDbModuleTask> logger, ModuleName moduleName);

    [LoggerMessage(LogLevel.Warning, "Can't create command to insert row to {tableInfo} with values [{row}]")]
    public static partial void WarningCantCreateCommand(this ILogger<RestoreDbModuleTask> logger, TableInfo tableInfo, DataRowInfo row);

    [LoggerMessage(LogLevel.Warning, "Table {name} does not contain tenant id column. Can't apply low importance relations on such tables.")]
    public static partial void WarningTableDoesNotContainTenantIdColumn(this ILogger<RestoreDbModuleTask> logger, string name);
}