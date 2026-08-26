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

namespace ASC.ClearEvents.Log;
internal static partial class ClearAuditEventsServiceLogger
{
    [LoggerMessage(LogLevel.Information, "Clear Audit Events Service running. Life time: {paidLifeTimeDays} day(s) for a paid tariff, {freeLifeTimeDays} day(s) otherwise.")]
    public static partial void InformationTimerRunning(this ILogger<ClearAuditEventsService> logger, int paidLifeTimeDays, int freeLifeTimeDays);

    [LoggerMessage(LogLevel.Information, "Clear Audit Events Service is disabled.")]
    public static partial void InformationDisabled(this ILogger<ClearAuditEventsService> logger);

    [LoggerMessage(LogLevel.Information, "Clear Audit Events Service is stopping.")]
    public static partial void InformationTimerStopping(this ILogger<ClearAuditEventsService> logger);

    [LoggerMessage(LogLevel.Warning, "Clear Audit Events Service will not run: life time {paidLifeTimeDays}/{freeLifeTimeDays} day(s), batch size {batchSize} and period {period} are not a usable configuration.")]
    public static partial void WarningInvalidConfiguration(this ILogger<ClearAuditEventsService> logger, int paidLifeTimeDays, int freeLifeTimeDays, int batchSize, TimeSpan period);

    [LoggerMessage(LogLevel.Debug, "Removed {count} audit event(s) older than {threshold} for tenant {tenantId} (paid tariff: {paid}).")]
    public static partial void DebugRemovedAuditEvents(this ILogger<ClearAuditEventsService> logger, int count, DateTime threshold, int tenantId, bool paid);

    [LoggerMessage(LogLevel.Information, "Removed {count} audit event(s) in {tenantCount} tenant(s).")]
    public static partial void InformationRemovedAuditEvents(this ILogger<ClearAuditEventsService> logger, int count, int tenantCount);

    [LoggerMessage(LogLevel.Warning, "Could not clear the audit trail of tenant {tenantId}.")]
    public static partial void WarningClearTenantFailed(this ILogger<ClearAuditEventsService> logger, int tenantId, Exception exception);
}
