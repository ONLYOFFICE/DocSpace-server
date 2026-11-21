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


using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace ASC.AI.Worker.Log;

internal static partial class AiAgentsMigrationServiceLogger
{
    [LoggerMessage(LogLevel.Information, "AI agents migration: the background pass is off by ai:agentsMigration:enabled")]
    public static partial void InformationPassDisabled(this ILogger<AiAgentsMigrationService> logger);

    [LoggerMessage(LogLevel.Information, "AI agents migration: {count} portals checked, the pass completed: {completed}")]
    public static partial void InformationPassEnded(this ILogger<AiAgentsMigrationService> logger, int count, bool completed);

    [LoggerMessage(LogLevel.Debug, "AI agents migration: tenant {tenantId} skipped for this pass, the legacy AI providers migration holds its lock")]
    public static partial void DebugLegacyMigrationBusy(this ILogger<AiAgentsMigrationService> logger, int tenantId);

    [LoggerMessage(LogLevel.Warning, "AI agents migration: the state of the pass was not saved, the next pass repeats this one")]
    public static partial void WarningPassStateNotSaved(this ILogger<AiAgentsMigrationService> logger);

    [LoggerMessage(LogLevel.Error, "AI agents migration failed for tenant {tenantId}")]
    public static partial void ErrorPortalFailed(this ILogger<AiAgentsMigrationService> logger, int tenantId, Exception exception);

    [LoggerMessage(LogLevel.Error, "AI agents migration: tenant {tenantId} reached {maxAttempts} failed passes and is left alone until its entry is removed from the state of the pass")]
    public static partial void ErrorPortalLeftAlone(this ILogger<AiAgentsMigrationService> logger, int tenantId, int maxAttempts);
}
