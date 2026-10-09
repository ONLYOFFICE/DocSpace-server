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
    /// The portals whose activity is read on every run, set by <see cref="BeginRunAsync"/>: those keeping their
    /// login history shorter than the default, where a sign-in could come and go between two of the days the
    /// activity is otherwise read on.
    /// </summary>
    private HashSet<int> _activityReadDaily = [];

    /// <summary>The balance requests that failed in a row in this run; see <see cref="HasPositiveBalanceAsync"/>.</summary>
    private int _balanceFailures;

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

        // Portals on the default audit settings keep no row, so this reads the few that changed them.
        _activityReadDaily = (await settingsManager.LoadOfAllTenantsAsync<TenantAuditSettings>())
            .Where(s => s.Value.LoginHistoryLifeTime < TenantAuditSettings.MaxLifeTime)
            .Select(s => s.Key)
            .ToHashSet();

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
        var blocked = context.Tenant.Status == TenantStatus.Blocked;
        var formerPaying = !context.Quota.Free && context.Tariff.State == TariffState.NotPaid && context.DueDateIsNotMax;

        if (!context.Quota.Free && !formerPaying)
        {
            // Paid, in its grace period or on a trial: not the policy's business.
            return blocked;
        }

        var kept = await settingsManager.LoadAsync<PortalRetentionSettings>(context.Tenant.Id);

        if (blocked)
        {
            await ApplyBlockedAsync(context, formerPaying, kept, client, senderName, lastRunOn);

            // A blocked portal gets none of the ordinary letters, whatever today brought it.
            return true;
        }

        return await ApplyActiveAsync(context, formerPaying, kept, policyStart, client, senderName, lastRunOn);
    }

    /// <summary>A blocked portal: reminded of its deletion, or deleted.</summary>
    private async Task ApplyBlockedAsync(PeriodicLetterContext context, bool formerPaying, PortalRetentionSettings kept, INotifyClient client, string senderName, DateTime? lastRunOn)
    {
        var blockedOn = context.Tenant.StatusChangeDate;

        PortalRetentionDecision Decide(PortalRetentionCategory category) =>
            PortalRetentionSchedule.DecideBlocked(Options.For(category), blockedOn, context.NowDate, lastRunOn, kept.FinalNoticeSentOn);

        // A blocked portal keeps the category it was blocked under: the letter about the block named the
        // deletion date of that category, and a balance that changes afterwards must not bring it forward.
        // One blocked without a category on record - not by this job - is placed by its wallet instead.
        var due = kept.Category is { } category
            ? await DueAsync(context.Tenant, category, Decide(category))
            : await ChooseByWalletAsync(context.Tenant, formerPaying, Decide);

        if (due is { } step)
        {
            await ActAsync(context, step.Category, step.Decision, client, senderName, kept);
        }
    }

    /// <summary>An active portal: warned, or blocked. Returns true when it has just been blocked.</summary>
    private async Task<bool> ApplyActiveAsync(PeriodicLetterContext context, bool formerPaying, PortalRetentionSettings kept, DateTime policyStart, INotifyClient client, string senderName, DateTime? lastRunOn)
    {
        var tenant = context.Tenant;

        // A lapsed tariff is the policy's only from its first unpaid day, once the grace period is over; a
        // warning due while it was still in that period goes out on that day instead of never.
        var noticesFrom = formerPaying ? context.DueDate.Date.AddDays(tariffService.GetPaymentDelay() + 1) : DateTime.MinValue;
        var lastWarning = kept.LastWarning;

        // The count starts no earlier than the last change of status, which is what an unblocked portal starts
        // again from.
        Func<PortalRetentionCategory, PortalRetentionDecision> CountedFrom(DateTime day) => category =>
            PortalRetentionSchedule.DecideActive(Options.For(category), Later(day, tenant.StatusChangeDate), policyStart, context.NowDate, noticesFrom, lastRunOn, lastWarning);

        DateTime anchor;

        if (formerPaying)
        {
            // Counted from the end of the subscription: activity after it changes nothing.
            anchor = context.DueDate;
        }
        else
        {
            // Activity only moves the count forward, so the latest activity already seen bounds it from below:
            // with nothing due counted from there, nothing is due counted from the real one either. The
            // database is asked only on a day the schedule may act on - about once a month for a portal in use.
            var seen = kept.LastActivityOn ?? DateTime.MinValue;

            if (!_activityReadDaily.Contains(tenant.Id) && NothingDue(formerPaying, CountedFrom(seen)))
            {
                return false;
            }

            var activity = await context.GetLastActivityAsync();

            kept = await KeepLastActivityAsync(tenant.Id, activity.LastOn, kept);
            anchor = kept.LastActivityOn ?? activity.LastOn;
        }

        if (await ChooseByWalletAsync(tenant, formerPaying, CountedFrom(anchor)) is not { } step)
        {
            return false;
        }

        await ActAsync(context, step.Category, step.Decision, client, senderName, kept);

        return step.Decision.Step == PortalRetentionStep.Block;
    }

    /// <summary>
    /// What today brings a portal whose wallet decides its category, or null when nothing does. Both
    /// schedules are asked first, so the accounting service is only asked on a day one of them has
    /// something.
    /// </summary>
    private async Task<(PortalRetentionCategory Category, PortalRetentionDecision Decision)?> ChooseByWalletAsync(Tenant tenant, bool formerPaying, Func<PortalRetentionCategory, PortalRetentionDecision> decide)
    {
        var (plain, withBalance) = Categories(formerPaying);

        var plainDecision = decide(plain);
        var balanceDecision = decide(withBalance);

        if ((plainDecision.Step == PortalRetentionStep.None && balanceDecision.Step == PortalRetentionStep.None) || await IsForbiddenDomainAsync(tenant))
        {
            return null;
        }

        if (await HasPositiveBalanceAsync(tenant) is not { } hasBalance)
        {
            // Not knowing whether money is left is not knowing which schedule applies: wait for tomorrow.
            return null;
        }

        return hasBalance ? DueOrNull(withBalance, balanceDecision) : DueOrNull(plain, plainDecision);
    }

    /// <summary>
    /// Asks the accounting service whether money is left on the portal's wallet, waiting no longer than
    /// <see cref="PortalRetentionOptions.BalanceTimeoutSeconds"/>; null when it could not tell. The run walks the
    /// portals one by one, and every letter after the policy waits for it, so once
    /// <see cref="PortalRetentionOptions.BalanceFailuresBeforeStop"/> requests in a row have failed the service is
    /// not asked again until the next run.
    /// </summary>
    private async Task<bool?> HasPositiveBalanceAsync(Tenant tenant)
    {
        if (_balanceFailures >= Options.BalanceFailuresBeforeStop)
        {
            return null;
        }

        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(Options.BalanceTimeoutSeconds));

        if (await tariffService.HasPositiveBalanceAsync(tenant.Id, deadline.Token) is { } hasBalance)
        {
            _balanceFailures = 0;

            return hasBalance;
        }

        logger.WarningBalanceUnknown(tenant.Id);

        if (++_balanceFailures == Options.BalanceFailuresBeforeStop)
        {
            logger.WarningAccountingUnavailable(_balanceFailures);
        }

        return null;
    }

    /// <summary>The two categories a portal may be in, by whether money is left on its wallet.</summary>
    private static (PortalRetentionCategory Plain, PortalRetentionCategory WithBalance) Categories(bool formerPaying)
    {
        return formerPaying
            ? (PortalRetentionCategory.FormerPaying, PortalRetentionCategory.FormerPayingWithBalance)
            : (PortalRetentionCategory.Free, PortalRetentionCategory.FreeWithBalance);
    }

    /// <summary>Whether neither schedule the wallet may choose has anything for the portal today.</summary>
    private static bool NothingDue(bool formerPaying, Func<PortalRetentionCategory, PortalRetentionDecision> decide)
    {
        var (plain, withBalance) = Categories(formerPaying);

        return decide(plain).Step == PortalRetentionStep.None && decide(withBalance).Step == PortalRetentionStep.None;
    }

    /// <summary>What today brings a portal of a known category, or null when nothing does.</summary>
    private async Task<(PortalRetentionCategory Category, PortalRetentionDecision Decision)?> DueAsync(Tenant tenant, PortalRetentionCategory category, PortalRetentionDecision decision)
    {
        return decision.Step == PortalRetentionStep.None || await IsForbiddenDomainAsync(tenant) ? null : (category, decision);
    }

    private static (PortalRetentionCategory Category, PortalRetentionDecision Decision)? DueOrNull(PortalRetentionCategory category, PortalRetentionDecision decision)
    {
        return decision.Step == PortalRetentionStep.None ? null : (category, decision);
    }

    /// <summary>
    /// Carries out the decision of the day, and keeps with the portal what the next days depend on: the
    /// day a warning named for the block, the day the last reminder before the deletion went out.
    /// </summary>
    private async Task ActAsync(PeriodicLetterContext context, PortalRetentionCategory category, PortalRetentionDecision decision, INotifyClient client, string senderName, PortalRetentionSettings kept)
    {
        var tenant = context.Tenant;

        logger.InformationDecision(tenant.Id, tenant.GetTenantDomain(coreSettings), category, decision.Step, decision.Letter, decision.BlockOn, decision.DeleteOn);

        switch (decision.Step)
        {
            case PortalRetentionStep.Notify:
                await SendAsync(LetterFor(category, decision.Letter), context, category, decision, client, senderName);

                var keep = decision.Letter switch
                {
                    // The block waits for the day this warning names.
                    PortalRetentionLetter.FirstNotice or PortalRetentionLetter.SecondNotice or PortalRetentionLetter.MonthlyNotice =>
                        kept with { LastWarning = new PortalRetentionWarning(context.NowDate, decision.BlockOn) },

                    // The deletion waits a full notice period after this reminder.
                    PortalRetentionLetter.FinalDeletionNotice => kept with { Category = category, FinalNoticeSentOn = context.NowDate },

                    _ => null
                };

                if (keep is not null)
                {
                    await settingsManager.SaveAsync(keep, tenant.Id);
                }

                break;

            case PortalRetentionStep.Block:
                await BlockAsync(context, category, decision, client, senderName);
                break;

            case PortalRetentionStep.Delete:
                await RemoveAsync(context, category, decision, client, senderName);
                break;
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
    /// Keeps the latest activity with the portal when it is later than the one kept. It is the lower bound the
    /// next runs count from without asking the database, and it outlives the login history: the portal's audit
    /// settings purge that, down to a day, and a count started from what is left would jump back and bring the
    /// block at once.
    /// </summary>
    private async Task<PortalRetentionSettings> KeepLastActivityAsync(int tenantId, DateTime lastActivityOn, PortalRetentionSettings kept)
    {
        if (lastActivityOn <= kept.LastActivityOn)
        {
            return kept;
        }

        kept = kept with { LastActivityOn = lastActivityOn };

        await settingsManager.SaveAsync(kept, tenantId);

        return kept;
    }

    /// <summary>The later of two days.</summary>
    private static DateTime Later(DateTime day, DateTime? other)
    {
        return other?.Date > day.Date ? other.Value.Date : day.Date;
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
