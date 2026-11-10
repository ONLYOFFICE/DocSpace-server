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
internal static partial class AuditTrailCleanerLogger
{
    [LoggerMessage(LogLevel.Warning, "The audit trail cleanup will not run: life time {paidLifeTimeDays}/{freeLifeTimeDays} day(s), paid grace period {paidGracePeriodDays} day(s), batch size {batchSize}, read page size {readPageSize} and period {period} are not a usable configuration. Both life times must be between {minLifeTimeDays} and {maxLifeTimeDays} day(s) with the free one not above the paid one, the grace period between 0 and {maxLifeTimeDays} day(s), the batch size between 1 and {maxBatchSize}, the read page size between 1 and {maxReadPageSize} and the period between {minPeriod} and {maxPeriod}.")]
    public static partial void WarningInvalidConfiguration(this ILogger<AuditTrailCleaner> logger, int paidLifeTimeDays, int freeLifeTimeDays, int paidGracePeriodDays, int batchSize, int readPageSize, TimeSpan period, int minLifeTimeDays, int maxLifeTimeDays, int maxBatchSize, int maxReadPageSize, TimeSpan minPeriod, TimeSpan maxPeriod);

    [LoggerMessage(LogLevel.Warning, "The audit trail cleanup will not run: the {sectionName} section could not be read. {error}")]
    public static partial void WarningUnreadableConfiguration(this ILogger<AuditTrailCleaner> logger, string sectionName, string error);

    [LoggerMessage(LogLevel.Information, "Removed {count} audit event(s) older than {threshold} for tenant {tenantId} (paid tariff: {paid}), {pageCount} page(s) read in {elapsed}.")]
    public static partial void InformationRemovedTenantAuditEvents(this ILogger<AuditTrailCleaner> logger, int count, DateTime threshold, int tenantId, bool paid, int pageCount, TimeSpan elapsed);

    [LoggerMessage(LogLevel.Information, "Audit trail cleanup pass finished (stopped: {stopped}) in {elapsed}: {walkedTenantCount} tenant(s) walked, {pageCount} page(s) and {readCount} audit event(s) read, {tariffLookupCount} tariff lookup(s); removed {count} audit event(s) in {tenantCount} tenant(s). Slowest tenant {slowestTenantId}: {slowestTenantElapsed} over {slowestTenantPageCount} page(s).")]
    public static partial void InformationPassFinished(this ILogger<AuditTrailCleaner> logger, bool stopped, TimeSpan elapsed, int walkedTenantCount, int pageCount, long readCount, int tariffLookupCount, int count, int tenantCount, int slowestTenantId, TimeSpan slowestTenantElapsed, int slowestTenantPageCount);

    [LoggerMessage(LogLevel.Warning, "Could not clear the audit trail of tenant {tenantId}.")]
    public static partial void WarningClearTenantFailed(this ILogger<AuditTrailCleaner> logger, int tenantId, Exception exception);
}
