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

namespace ASC.AI.Migration;

internal static partial class LegacyAiProvidersMigrationLogger
{
    [LoggerMessage(LogLevel.Information, "Legacy AI providers migration for tenant {tenantId}: {profiles} profiles in use, {boundAgents} agents bound")]
    public static partial void InfoTenantMigrated(this ILogger<LegacyAiProvidersMigrator> logger, int tenantId, int profiles, int boundAgents);

    [LoggerMessage(LogLevel.Warning, "Legacy AI providers migration for tenant {tenantId}: default provider {providerId} is missing or not migratable, skipped")]
    public static partial void WarningDefaultProviderMissing(this ILogger<LegacyAiProvidersMigrator> logger, int tenantId, int providerId);

    [LoggerMessage(LogLevel.Warning, "Legacy AI providers migration for tenant {tenantId}: agent {roomId} references unknown provider {providerId}, skipped")]
    public static partial void WarningAgentProviderMissing(this ILogger<LegacyAiProvidersMigrator> logger, int tenantId, int roomId, int providerId);

    [LoggerMessage(LogLevel.Warning, "Legacy AI providers migration for tenant {tenantId}: agent {roomId} has no provider binding and the tenant has no default provider, skipped")]
    public static partial void WarningAgentUnbound(this ILogger<LegacyAiProvidersMigrator> logger, int tenantId, int roomId);

    [LoggerMessage(LogLevel.Warning, "Legacy AI providers migration for tenant {tenantId}: provider {providerId} has unsupported type {type}, skipped")]
    public static partial void WarningUnsupportedProviderType(this ILogger<LegacyAiProvidersMigrator> logger, int tenantId, int providerId, ProviderType type);

    [LoggerMessage(LogLevel.Warning, "Legacy AI providers migration for tenant {tenantId}: key of provider {providerId} ({title}) cannot be decrypted, skipped")]
    public static partial void WarningKeyDecryptFailed(this ILogger<LegacyAiProvidersMigrator> logger, int tenantId, int providerId, string title, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Legacy AI providers migration for tenant {tenantId}: completion flag was not saved, the tenant will be re-checked on the next start")]
    public static partial void WarningFlagNotSaved(this ILogger<LegacyAiProvidersMigrator> logger, int tenantId);

    [LoggerMessage(LogLevel.Debug, "Legacy AI providers migration skipped: the lock is held by another node")]
    public static partial void DebugLockNotAcquired(this ILogger<LegacyAiProvidersMigrationStartupTask> logger);

    [LoggerMessage(LogLevel.Error, "Legacy AI providers migration failed for tenant {tenantId}")]
    public static partial void ErrorTenantFailed(this ILogger<LegacyAiProvidersMigrationStartupTask> logger, int tenantId, Exception exception);

    [LoggerMessage(LogLevel.Error, "Legacy AI providers migration failed")]
    public static partial void ErrorFailed(this ILogger<LegacyAiProvidersMigrationStartupTask> logger, Exception exception);
}
