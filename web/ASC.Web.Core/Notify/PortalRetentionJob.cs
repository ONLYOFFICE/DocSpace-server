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
/// <see cref="PortalRetentionSchedule"/> what today brings, and tells the owner, blocks the portal or
/// removes it when that is the answer. Stored per portal (<see cref="PortalRetentionSettings"/>) is
/// only what the schedule cannot read anywhere else: while it is active, its last warning and its last
/// sign-in; once it is blocked, the category it was blocked under and the day of its last reminder.
/// </summary>
[Scope]
public class PortalRetentionJob(
    ILogger<PortalRetentionJob> logger,
    PortalRetentionConfiguration configuration,
    SettingsManager settingsManager,
    ITariffService tariffService,
    TenantManager tenantManager,
    CoreSettings coreSettings,
    UserManager userManager,
    StudioNotifyService studioNotifyService,
    MessageService messageService,
    PortalRemovalService portalRemovalService,
    SecurityContext securityContext,
    IServiceProvider serviceProvider)
{
    /// <summary>
    /// How many days back a run looks for letters it has not sent. After a longer stop the older warnings
    /// are not worth a pile of letters at once; the last reminder before a deletion is never lost to it.
    /// </summary>
    private const int MaxCatchUpDays = 7;

    public PortalRetentionOptions Options => configuration.Options;

    /// <summary>
    /// Opens the daily run: the day the policy started counting in this installation, stamped on the
    /// first run, and the last day a run got through, after which this one sends the letters.
    /// </summary>
    public async Task<(DateTime PolicyStart, DateTime? LastRunOn)> BeginRunAsync(DateTime today)
    {
        today = today.Date;

        var settings = await settingsManager.LoadForDefaultTenantAsync<PortalRetentionPolicyStartSettings>();

        if (settings.StartedOn is not { } startedOn)
        {
            startedOn = today;
            settings.StartedOn = startedOn;
            await settingsManager.SaveForDefaultTenantAsync(settings);
        }

        var lastRunOn = settings.LastRunOn?.Date;
        var earliest = today.AddDays(-MaxCatchUpDays);

        if (lastRunOn < earliest)
        {
            lastRunOn = earliest;
        }

        logger.InformationPolicyStart(startedOn);
        logger.InformationRunCovers(lastRunOn ?? today.AddDays(-1), today);

        return (startedOn, lastRunOn);
    }

    /// <summary>
    /// Closes the daily run once every portal has been through it. A run that stops halfway leaves the
    /// mark where it was, so the next one sends again what it could not.
    /// </summary>
    public async Task EndRunAsync(DateTime today)
    {
        var settings = await settingsManager.LoadForDefaultTenantAsync<PortalRetentionPolicyStartSettings>();

        settings.LastRunOn = today.Date;
        await settingsManager.SaveForDefaultTenantAsync(settings);
    }

    /// <summary>
    /// Applies the policy to the portal of <paramref name="context"/>, sending its letters through
    /// <paramref name="client"/>. Returns true when the caller must leave the portal alone for the rest of
    /// the run: it is blocked, it was just blocked, or it is gone. The letters of the days after
    /// <paramref name="lastRunOn"/> (<see cref="BeginRunAsync"/>) are this run's; null means yesterday.
    /// </summary>
    public async Task<bool> ApplyAsync(PeriodicLetterContext context, DateTime policyStart, INotifyClient client, string senderName, DateTime? lastRunOn = null)
    {
        var tenant = context.Tenant;
        var blocked = tenant.Status == TenantStatus.Blocked;

        var formerPaying = !context.Quota.Free && context.Tariff.State == TariffState.NotPaid && context.DueDateIsNotMax;

        if (!context.Quota.Free && !formerPaying)
        {
            // Paid, in its grace period or on a trial: not the policy's business.
            return blocked;
        }

        var kept = await settingsManager.LoadAsync<PortalRetentionSettings>(tenant.Id);

        // A blocked portal keeps the category it was blocked under: the letter about the block named the
        // deletion date of that category, and a balance that changes afterwards must not bring it forward.
        var blockSettings = blocked ? kept : null;

        if (blockSettings?.Category is { } blockedCategory)
        {
            var blockedDecision = PortalRetentionSchedule.Decide(Options.For(blockedCategory), tenant.StatusChangeDate, policyStart, tenant.StatusChangeDate.Date, context.NowDate,
                lastRunOn: lastRunOn, finalNoticeSentOn: blockSettings.FinalNoticeSentOn);

            if (blockedDecision.Step == PortalRetentionStep.None || await IsForbiddenDomainAsync(tenant))
            {
                return true;
            }

            return await ActAsync(context, blockedCategory, blockedDecision, client, senderName, blocked);
        }

        (var anchor, kept) = await GetAnchorAsync(context, formerPaying, kept);
        DateTime? blockedOn = blocked ? tenant.StatusChangeDate.Date : null;

        PortalRetentionWarning? lastWarning = kept.WarnedOn is { } warnedOn && kept.WarnedBlockOn is { } warnedBlockOn
            ? new PortalRetentionWarning(warnedOn, warnedBlockOn)
            : null;

        var plain = formerPaying ? PortalRetentionCategory.FormerPaying : PortalRetentionCategory.Free;
        var withBalance = formerPaying ? PortalRetentionCategory.FormerPayingWithBalance : PortalRetentionCategory.FreeWithBalance;

        // Both schedules first, so the accounting service is only asked on a day one of them has something.
        // A lapsed tariff is the policy's only from its first unpaid day, once the grace period is over; a
        // warning due while it was still in that period goes out on that day instead of never.
        var noticesFrom = formerPaying ? context.DueDate.Date.AddDays(tariffService.GetPaymentDelay() + 1) : DateTime.MinValue;

        var finalNoticeSentOn = blockSettings?.FinalNoticeSentOn;

        var plainDecision = PortalRetentionSchedule.Decide(Options.For(plain), anchor, policyStart, blockedOn, context.NowDate, noticesFrom, lastRunOn, finalNoticeSentOn, lastWarning);
        var balanceDecision = PortalRetentionSchedule.Decide(Options.For(withBalance), anchor, policyStart, blockedOn, context.NowDate, noticesFrom, lastRunOn, finalNoticeSentOn, lastWarning);

        if (plainDecision.Step == PortalRetentionStep.None && balanceDecision.Step == PortalRetentionStep.None)
        {
            return blocked;
        }

        if (await IsForbiddenDomainAsync(tenant))
        {
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

        return await ActAsync(context, category, decision, client, senderName, blocked, kept);
    }

    /// <summary>
    /// Carries out the decision of the day. Returns what <see cref="ApplyAsync"/> returns: true when the
    /// portal is to be left alone for the rest of the run. <paramref name="kept"/> is what is kept about the
    /// portal, null for a portal blocked under a recorded category.
    /// </summary>
    private async Task<bool> ActAsync(PeriodicLetterContext context, PortalRetentionCategory category, PortalRetentionDecision decision, INotifyClient client, string senderName, bool blocked, PortalRetentionSettings kept = null)
    {
        var tenant = context.Tenant;

        logger.InformationDecision(tenant.Id, tenant.GetTenantDomain(coreSettings), category, decision.Step, decision.Letter, decision.BlockOn, decision.DeleteOn);

        switch (decision.Step)
        {
            case PortalRetentionStep.Notify:
                await SendAsync(LetterFor(category, decision.Letter), context, category, decision, client, senderName);

                if (decision.Letter == PortalRetentionLetter.FinalDeletionNotice)
                {
                    // The deletion waits for this letter, so the day it went out is kept with the block.
                    await settingsManager.SaveAsync(new PortalRetentionSettings { Category = category, FinalNoticeSentOn = context.NowDate }, tenant.Id);
                }
                else if (!blocked && decision.Letter is PortalRetentionLetter.FirstNotice or PortalRetentionLetter.SecondNotice or PortalRetentionLetter.MonthlyNotice)
                {
                    // The block waits for the day this letter names, so the letter is kept with the portal.
                    await settingsManager.SaveAsync(new PortalRetentionSettings
                    {
                        LastLoginOn = kept?.LastLoginOn,
                        WarnedOn = context.NowDate,
                        WarnedBlockOn = decision.BlockOn
                    }, tenant.Id);
                }

                return blocked;

            case PortalRetentionStep.Block:
                await BlockAsync(context, category, decision, client, senderName);

                return true;

            case PortalRetentionStep.Delete:
                await RemoveAsync(context, category, decision, client, senderName);

                return true;

            default:
                return blocked;
        }
    }

    /// <summary>A portal on a forbidden domain is kept alive on purpose and never touched by the policy.</summary>
    private async Task<bool> IsForbiddenDomainAsync(Tenant tenant)
    {
        if (!await tenantManager.IsForbiddenDomainAsync(tenant.Alias))
        {
            return false;
        }

        logger.InformationForbiddenDomain(tenant.Id, tenant.GetTenantDomain(coreSettings));

        return true;
    }

    /// <summary>
    /// The letter that carries a decision. The warnings before the block differ by category - the free
    /// portal is told to sign in, the lapsed one to renew, the one with money left that the money goes with
    /// it - while everything from the block on reads the same, the way back aside.
    /// </summary>
    private static Type LetterFor(PortalRetentionCategory category, PortalRetentionLetter? letter)
    {
        return letter switch
        {
            PortalRetentionLetter.Blocked => typeof(SaasOwnerRetentionBlockedNotifyAction),
            PortalRetentionLetter.EarlyDeletionNotice => typeof(SaasOwnerRetentionDeletionReminderNotifyAction),
            PortalRetentionLetter.FinalDeletionNotice => typeof(SaasOwnerRetentionFinalReminderNotifyAction),
            _ => category switch
            {
                PortalRetentionCategory.Free => typeof(SaasOwnerRetentionInactivityWarningNotifyAction),
                PortalRetentionCategory.FormerPaying => typeof(SaasOwnerRetentionUnpaidWarningNotifyAction),
                _ => typeof(SaasOwnerRetentionWalletWarningNotifyAction)
            }
        };
    }

    private async Task SendAsync(Type letter, PeriodicLetterContext context, PortalRetentionCategory category, PortalRetentionDecision decision, INotifyClient client, string senderName, string portalDomain = null)
    {
        var action = (PortalRetentionNotifyAction)serviceProvider.GetRequiredService(letter);

        action.Init(category, decision, portalDomain);

        await action.SendAsync(context, client, senderName);
    }

    /// <summary>
    /// Blocks the portal and says so - to the owner, and, for a portal that has paid or still has money on
    /// its wallet, to support, where a manager can step in.
    /// </summary>
    private async Task BlockAsync(PeriodicLetterContext context, PortalRetentionCategory category, PortalRetentionDecision decision, INotifyClient client, string senderName)
    {
        // Read again rather than saved as the run found it: the run takes hours, and saving the whole portal
        // as it was then would undo a rename, a change of owner or a deactivation made in the meantime - or
        // block a portal that is no longer active at all.
        var tenant = await tenantManager.GetTenantAsync(context.Tenant.Id);

        if (tenant?.Status != TenantStatus.Active)
        {
            return;
        }

        // Before the status, so a blocked portal always finds the category its letter was written for.
        await settingsManager.SaveAsync(new PortalRetentionSettings { Category = category }, tenant.Id);

        tenant.SetStatus(TenantStatus.Blocked);
        await tenantManager.SaveTenantAsync(tenant);

        // The letters name the portal and link back to it as it is now, with the status date that ties the
        // unblocking link to this block.
        context = context with { Tenant = tenant };

        // Written by the system: there is no request behind the daily job to take a user and an address from.
        messageService.Send(MessageInitiator.System, MessageAction.PortalBlocked);

        await SendAsync(typeof(SaasOwnerRetentionBlockedNotifyAction), context, category, decision, client, senderName);

        // Support steps in only for a portal that has paid or still has money on its wallet.
        if (category == PortalRetentionCategory.Free)
        {
            return;
        }

        var owner = await userManager.GetUsersAsync(tenant.OwnerId);
        await studioNotifyService.SendMsgPortalBlockedToSupportAsync(tenant.GetTenantDomain(coreSettings), owner, category, decision.DeleteOn);
    }

    /// <summary>
    /// The day the count starts: the last activity for a free portal, the due date for a former paying
    /// one - and in both cases no earlier than the last change of status, which is what an unblocked
    /// portal starts again from.
    /// </summary>
    /// <remarks>
    /// The portal's audit settings purge its login history, down to a day, while the audit trail stays. A
    /// portal whose last sign of life is a sign-in would see its count jump back, and its block come at
    /// once, the day that row goes - so the last sign-in is kept with the portal while it is the later of
    /// the two, and the count never starts earlier than it.
    /// </remarks>
    private async Task<(DateTime Anchor, PortalRetentionSettings Kept)> GetAnchorAsync(PeriodicLetterContext context, bool formerPaying, PortalRetentionSettings kept)
    {
        DateTime anchor;

        if (formerPaying)
        {
            anchor = context.DueDate;
        }
        else
        {
            var activity = await context.GetLastActivityAsync();
            var keptLoginOn = kept.LastLoginOn?.Date ?? DateTime.MinValue;

            // Nobody signs in to a blocked portal, and its record belongs to the block.
            if (context.Tenant.Status != TenantStatus.Blocked && activity.LastLoginOn > activity.LastEventOn && activity.LastLoginOn > keptLoginOn)
            {
                keptLoginOn = activity.LastLoginOn;
                kept = new PortalRetentionSettings
                {
                    LastLoginOn = keptLoginOn,
                    WarnedOn = kept.WarnedOn,
                    WarnedBlockOn = kept.WarnedBlockOn
                };

                await settingsManager.SaveAsync(kept, context.Tenant.Id);
            }

            anchor = keptLoginOn > activity.LastOn ? keptLoginOn : activity.LastOn;
        }

        var statusChanged = context.Tenant.StatusChangeDate.Date;

        return (statusChanged > anchor ? statusChanged : anchor, kept);
    }

    /// <summary>
    /// Removes the portal. The letters go while its rows still exist: to the owner, and - for a portal that
    /// has paid - to support, who make sure nothing is billed for it any more.
    /// </summary>
    private async Task RemoveAsync(PeriodicLetterContext context, PortalRetentionCategory category, PortalRetentionDecision decision, INotifyClient client, string senderName)
    {
        var tenant = context.Tenant;

        // Before the removal: it renames the alias, and the letter to support names the portal.
        var tenantDomain = tenant.GetTenantDomain(coreSettings);
        var formerPaying = category is PortalRetentionCategory.FormerPaying or PortalRetentionCategory.FormerPayingWithBalance;

        try
        {
            // The request to the identity service carries a token issued for the current account.
            await securityContext.AuthenticateMeWithoutCookieAsync(tenant.OwnerId);
            await portalRemovalService.RemoveAsync(tenant, Guid.Empty, auto: true, async () =>
            {
                await SendAsync(typeof(SaasOwnerRetentionDeletedNotifyAction), context, category, decision, client, senderName, tenantDomain);

                if (formerPaying)
                {
                    var owner = await userManager.GetUsersAsync(tenant.OwnerId);
                    var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);

                    await studioNotifyService.SendMsgPaidPortalDeletedToSupportAsync(tenantDomain, owner, customerInfo);
                }
            });
        }
        finally
        {
            // the owner was authenticated only to remove the portal: keep that identity
            // out of the tenants processed after this one
            securityContext.Logout();
        }
    }
}
