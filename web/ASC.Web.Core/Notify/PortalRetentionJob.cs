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

namespace ASC.Web.Studio.Core.Notify;

/// <summary>
/// Applies the retention policy to one portal on the daily run: works out its category, asks
/// <see cref="PortalRetentionSchedule"/> what today brings, and blocks or removes it when that is the
/// answer. Nothing is stored per portal - see the schedule for why none of it is needed.
/// </summary>
[Scope]
public class PortalRetentionJob(
    ILogger<PortalRetentionJob> logger,
    PortalRetentionConfiguration configuration,
    SettingsManager settingsManager,
    ITariffService tariffService,
    TenantManager tenantManager,
    CoreSettings coreSettings,
    PortalRemovalService portalRemovalService,
    SecurityContext securityContext)
{
    public PortalRetentionOptions Options => configuration.Options;

    /// <summary>
    /// The day the policy started counting in this installation, stamped on the first real run. A dry run
    /// does not stamp it: it previews what the policy would do if it were switched on today.
    /// </summary>
    public async Task<DateTime> GetPolicyStartAsync(DateTime today)
    {
        var settings = await settingsManager.LoadForDefaultTenantAsync<PortalRetentionPolicyStartSettings>();

        if (settings.StartedOn is not { } startedOn)
        {
            startedOn = today.Date;

            if (!Options.DryRun)
            {
                settings.StartedOn = startedOn;
                await settingsManager.SaveForDefaultTenantAsync(settings);
            }
        }

        logger.InformationPolicyStart(startedOn);

        return startedOn;
    }

    /// <summary>
    /// Applies the policy to the portal of <paramref name="context"/>. Returns true when the caller must
    /// leave the portal alone for the rest of the run: it is blocked, it was just blocked, or it is gone.
    /// </summary>
    public async Task<bool> ApplyAsync(PeriodicLetterContext context, DateTime policyStart)
    {
        var tenant = context.Tenant;
        var blocked = tenant.Status == TenantStatus.Blocked;

        var formerPaying = !context.Quota.Free && context.Tariff.State == TariffState.NotPaid && context.DueDateIsNotMax;

        if (!context.Quota.Free && !formerPaying)
        {
            // Paid, in its grace period or on a trial: not the policy's business.
            return blocked;
        }

        var anchor = await GetAnchorAsync(context, formerPaying);
        DateTime? blockedOn = blocked ? tenant.StatusChangeDate.Date : null;

        var plain = formerPaying ? PortalRetentionCategory.FormerPaying : PortalRetentionCategory.Free;
        var withBalance = formerPaying ? PortalRetentionCategory.FormerPayingWithBalance : PortalRetentionCategory.FreeWithBalance;

        // Both schedules first, so the accounting service is only asked on a day one of them has something.
        var plainDecision = PortalRetentionSchedule.Decide(Options.For(plain), anchor, policyStart, blockedOn, context.NowDate);
        var balanceDecision = PortalRetentionSchedule.Decide(Options.For(withBalance), anchor, policyStart, blockedOn, context.NowDate);

        if (plainDecision.Step == PortalRetentionStep.None && balanceDecision.Step == PortalRetentionStep.None)
        {
            return blocked;
        }

        if (await tenantManager.IsForbiddenDomainAsync(tenant.Alias))
        {
            logger.InformationForbiddenDomain(tenant.Id, tenant.GetTenantDomain(coreSettings));

            return blocked;
        }

        if (await tariffService.HasPositiveBalanceAsync(tenant.Id) is not { } hasBalance)
        {
            // Not knowing whether money is left is not knowing which schedule applies: wait for tomorrow.
            logger.WarningBalanceUnknown(tenant.Id);

            return blocked;
        }

        var category = hasBalance ? withBalance : plain;
        var decision = hasBalance ? balanceDecision : plainDecision;

        if (decision.Step == PortalRetentionStep.None)
        {
            return blocked;
        }

        logger.InformationDecision(Options.DryRun ? "dry run" : "applied", tenant.Id, tenant.GetTenantDomain(coreSettings),
            category, decision.Step, decision.Letter, decision.BlockOn, decision.DeleteOn);

        if (Options.DryRun)
        {
            return blocked;
        }

        switch (decision.Step)
        {
            case PortalRetentionStep.Block:
                tenant.SetStatus(TenantStatus.Blocked);
                await tenantManager.SaveTenantAsync(tenant);

                return true;

            case PortalRetentionStep.Delete:
                await RemoveAsync(tenant);

                return true;

            default:
                return blocked;
        }
    }

    /// <summary>
    /// The day the count starts: the last activity for a free portal, the due date for a former paying
    /// one - and in both cases no earlier than the last change of status, which is what an unblocked
    /// portal starts again from.
    /// </summary>
    private static async Task<DateTime> GetAnchorAsync(PeriodicLetterContext context, bool formerPaying)
    {
        var anchor = formerPaying ? context.DueDate : await context.GetLastActivityDateAsync();
        var statusChanged = context.Tenant.StatusChangeDate.Date;

        return statusChanged > anchor ? statusChanged : anchor;
    }

    private async Task RemoveAsync(Tenant tenant)
    {
        try
        {
            // The request to the identity service carries a token issued for the current account.
            await securityContext.AuthenticateMeWithoutCookieAsync(tenant.OwnerId);
            await portalRemovalService.RemoveAsync(tenant, Guid.Empty, auto: true);
        }
        finally
        {
            // the owner was authenticated only to remove the portal: keep that identity
            // out of the tenants processed after this one
            securityContext.Logout();
        }
    }
}
