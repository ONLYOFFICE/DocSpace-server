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

namespace ASC.Data.Storage.Log;
internal static partial class EncryptionOperationLogger
{
    [LoggerMessage(LogLevel.Debug, "Storage already {status}")]
    public static partial void DebugStorageAlready(this ILogger logger, EncryprtionStatus status);

    [LoggerMessage(LogLevel.Debug, "Percentage: {tenantAlias} {module} {percentage}")]
    public static partial void DebugPercentage(this ILogger logger, string tenantAlias, string module, double percentage);

    [LoggerMessage(LogLevel.Debug, "Save new EncryptionSettings")]
    public static partial void DebugSaveNewEncryptionSettings(this ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Tenant {tenantAlias} SetStatus Active")]
    public static partial void DebugTenantSetStatusActive(this ILogger logger, string tenantAlias);

    [LoggerMessage(LogLevel.Debug, "Tenant {tenantAlias} SendStorageEncryptionSuccess")]
    public static partial void DebugTenantSendStorageEncryptionSuccess(this ILogger logger, string tenantAlias);

    [LoggerMessage(LogLevel.Debug, "Tenant {tenantAlias} SendStorageEncryptionError")]
    public static partial void DebugTenantSendStorageEncryptionError(this ILogger logger, string tenantAlias);

    [LoggerMessage(LogLevel.Error, "EncryptionOperation")]
    public static partial void ErrorEncryptionOperation(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "{logItem}")]
    public static partial void ErrorLogItem(this ILogger logger, string logItem, Exception exception);
}