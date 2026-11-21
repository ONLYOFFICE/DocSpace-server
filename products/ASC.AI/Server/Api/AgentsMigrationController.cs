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


namespace ASC.AI.Api;

[Scope]
[ApiEndpoint("ai", Internal = true)]
[ApiExplorerSettings(IgnoreApi = true)]
public class AgentsMigrationController(
    AiAgentsPortalMigration portalMigration,
    UserManager userManager,
    AuthContext authContext,
    SecurityContext securityContext) : ControllerBase
{
    /// <remarks>
    /// Converts the AI agents of the portal of the caller into AI projects before it answers, and returns how many
    /// were converted and how many failed. An internal entry point of the integration tests: no user interface calls
    /// it. Requires the portal owner or a DocSpace admin. Other callers get 403. It works while the AI switch is off.
    /// A repeated call converts nothing again. It answers 409 while another node holds the lock of the legacy AI
    /// providers migration.
    /// </remarks>
    /// <summary>Convert the AI agents of the portal</summary>
    [HttpPost("agents/migration")]
    public async Task<AgentsMigrationDto> MigrateAgentsAsync()
    {
        var callerId = authContext.CurrentAccount.ID;
        if (!await userManager.IsDocSpaceAdminAsync(callerId))
        {
            throw new SecurityException();
        }

        AiAgentMigrationResult? result;

        await securityContext.AuthenticateMeWithoutCookieAsync(ASC.Core.Configuration.Constants.CoreSystem);
        try
        {
            result = await portalMigration.RunAsync(HttpContext.RequestAborted);
        }
        finally
        {
            await securityContext.AuthenticateMeWithoutCookieAsync(callerId);
        }

        if (result is not { } migration)
        {
            throw new CustomHttpException(HttpStatusCode.Conflict, "The legacy AI providers migration is running");
        }

        return new AgentsMigrationDto { Migrated = migration.Migrated, Failed = migration.Failed };
    }
}
