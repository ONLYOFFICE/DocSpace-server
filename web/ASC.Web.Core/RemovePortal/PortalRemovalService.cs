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

namespace ASC.Web.Core.RemovePortal;

/// <summary>
/// Removes a portal: the one sequence both the owner's own deletion and the retention policy go
/// through, so the two cannot drift apart. What differs between them - who is told and what is written
/// to the audit trail - stays with the caller.
/// </summary>
[Scope]
public class PortalRemovalService(
    TenantManager tenantManager,
    IdentityClient identityClient,
    ApiSystemHelper apiSystemHelper,
    CoreBaseSettings coreBaseSettings,
    CoreSettings coreSettings,
    IFusionCache hybridCache,
    IEventBus eventBus)
{
    /// <summary>
    /// Takes the portal down at once - its alias is released and it stops answering - and starts the
    /// purge of its content.
    /// </summary>
    /// <param name="tenant">The portal to remove.</param>
    /// <param name="initiatorId">Who asked for it; <see cref="Guid.Empty"/> when the policy did.</param>
    /// <param name="auto">Whether the policy removes it, which only changes the suffix the alias gets.</param>
    /// <param name="beforePurgeAsync">
    /// What has to happen while the portal's rows still exist, the letters above all: the notify queue
    /// goes with the tenant, so a letter queued after the purge started may never be sent.
    /// </param>
    public async Task RemoveAsync(Tenant tenant, Guid initiatorId, bool auto, Func<Task> beforePurgeAsync = null)
    {
        var tenantDomain = tenant.GetTenantDomain(coreSettings);

        // The owner asked for it, so a failure is theirs to see; the policy cannot ask anyone, and the
        // clients are not worth keeping a portal for.
        await identityClient.DeleteTenantClientsAsync(throwIfNotSuccess: !auto);
        await tenantManager.RemoveTenantAsync(tenant, auto);

        if (!coreBaseSettings.Standalone && apiSystemHelper.ApiCacheEnable)
        {
            await apiSystemHelper.RemoveTenantFromCacheAsync(tenantDomain);
        }

        await hybridCache.RemoveAsync(GetCspKey(tenantDomain));

        if (beforePurgeAsync != null)
        {
            await beforePurgeAsync();
        }

        await eventBus.PublishAsync(new RemovePortalIntegrationEvent(initiatorId, tenant.Id));
    }

    private static string GetCspKey(string domain) => $"csp:{domain}";
}
