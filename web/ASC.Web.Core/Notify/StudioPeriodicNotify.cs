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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Studio.Core.Notify;

/// <summary>
/// The daily tariff job. It walks every portal, applies the retention policy to it, and then asks each
/// periodic letter whether today is its day for that portal.
///
/// It used to decide that itself, in one <c>else if</c> chain per edition that filled forty shared
/// locals; the letters now answer for themselves (<see cref="BasePeriodicNotifyAction"/>), so adding one
/// no longer means editing shared control flow.
/// </summary>
[Scope]
public class StudioPeriodicNotify(
    ILoggerFactory loggerFactory,
    WorkContext workContext,
    TenantManager tenantManager,
    TenantLogoManager tenantLogoManager,
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    IServiceProvider serviceProvider,
    AuditEventsRepository auditEventsRepository,
    LoginEventsRepository loginEventsRepository,
    PortalRetentionJob portalRetentionJob)
{
    private readonly ILogger _log = loggerFactory.CreateLogger("ASC.Notify");

    /// <summary>
    /// The SaaS letters, and the only list of them. Order carries no meaning: every letter judges itself,
    /// so two may go out on the same day if a portal genuinely qualifies for both.
    /// </summary>
    private static readonly Type[] _saasLetters =
    [
        typeof(SaasAdminHandyAppsV1NotifyAction),
        typeof(SaasAdminConfigureV1NotifyAction),
        typeof(SaasAdminAddonsV1NotifyAction),
        typeof(SaasAdminAiAgentsV1NotifyAction),
        typeof(SaasAdminDeveloperToolsV1NotifyAction),
        typeof(SaasAdminUserAppsTipsV1NotifyAction),
        typeof(SaasOwnerPaymentWarningGracePeriodBeforeActivationNotifyAction),
        typeof(SaasOwnerPaymentWarningGracePeriodActivationNotifyAction),
        typeof(SaasOwnerPaymentWarningGracePeriodLastDayNotifyAction),
        typeof(SaasOwnerPaymentWarningGracePeriodExpiredNotifyAction)
    ];

    /// <summary>The Enterprise and Developer letters. Never runs in the same installation as the list above.</summary>
    private static readonly Type[] _enterpriseLetters =
    [
        typeof(EnterpriseAdminUserAppsTipsV1NotifyAction),
        typeof(EnterpriseAdminPaymentWarningLifetimeBeforeExpirationNotifyAction),
        typeof(EnterpriseAdminPaymentWarningLifetimeExpirationNotifyAction),
        typeof(EnterpriseAdminPaymentWarningGracePeriodBeforeActivationNotifyAction),
        typeof(EnterpriseAdminPaymentWarningGracePeriodActivationNotifyAction),
        typeof(EnterpriseAdminPaymentWarningGracePeriodBeforeExpirationNotifyAction),
        typeof(EnterpriseAdminPaymentWarningGracePeriodExpirationNotifyAction),
        typeof(DeveloperAdminPaymentWarningGracePeriodBeforeActivationNotifyAction),
        typeof(DeveloperAdminPaymentWarningGracePeriodActivationNotifyAction),
        typeof(DeveloperAdminPaymentWarningGracePeriodBeforeExpirationNotifyAction),
        typeof(DeveloperAdminPaymentWarningGracePeriodExpirationNotifyAction)
    ];

    public async ValueTask SendSaasLettersAsync(string senderName, DateTime scheduleDate)
    {
        _log.InformationStartSendSaasTariffLetters();

        // The retention policy also walks the portals it has blocked: they are reminded, or deleted, from here.
        var tenants = await tenantManager.GetTenantsByStatusAsync(TenantStatus.Active, TenantStatus.Blocked);

        if (tenants.Count <= 0)
        {
            _log.InformationEndSendSaasTariffLetters();
            return;
        }

        var nowDate = scheduleDate.Date;
        var retentionStart = await portalRetentionJob.GetPolicyStartAsync(nowDate);

        // The paid add-ons the wallet is charged for, by quota id: their titles are what the upcoming
        // payment letter lists. Global and cached, so they are read once for all tenants.
        var walletQuotas = (await tenantManager.GetTenantQuotasAsync(all: true, wallet: true)).ToDictionary(q => q.TenantId);

        foreach (var tenant in tenants)
        {
            try
            {
                await tenantManager.SetCurrentTenantAsync(tenant.Id);

                var context = await BuildContextAsync(tenant, nowDate);
                var client = workContext.RegisterClient(serviceProvider, studioNotifyHelper.NotifySource);

                // Before any letter: a portal blocked or removed here must not be written to afterwards.
                if (await portalRetentionJob.ApplyAsync(context, retentionStart, client, senderName))
                {
                    continue;
                }

                await SendLettersAsync(_saasLetters, context, client, senderName);

                // Every add-on renews on its own due date, whatever the tariff state is, so this reminder
                // is sent on its own and takes no part in the letters above.
                await SendUpcomingSubscriptionPaymentAsync(tenant, context.Tariff, nowDate, walletQuotas, client, senderName);
            }
            catch (Exception err)
            {
                _log.ErrorSendSaasLettersAsync(tenant.Id, err);
            }
        }

        _log.InformationEndSendSaasTariffLetters();
    }

    public async Task SendEnterpriseLettersAsync(string senderName, DateTime scheduleDate)
    {
        _log.InformationStartSendTariffEnterpriseLetters();

        var activeTenants = await tenantManager.GetTenantsAsync();

        if (activeTenants.Count <= 0)
        {
            _log.InformationEndSendTariffEnterpriseLetters();
            return;
        }

        var nowDate = scheduleDate.Date;

        foreach (var tenant in activeTenants)
        {
            try
            {
                await tenantManager.SetCurrentTenantAsync(tenant.Id);

                var context = await BuildContextAsync(tenant, nowDate, enterprise: true);
                var client = workContext.RegisterClient(serviceProvider, studioNotifyHelper.NotifySource);

                await SendLettersAsync(_enterpriseLetters, context, client, senderName);
            }
            catch (Exception err)
            {
                _log.ErrorSendEnterpriseLetters(err);
            }
        }

        _log.InformationEndSendTariffEnterpriseLetters();
    }

    /// <summary>Asks every letter in the list whether today is its day, and sends the ones that say yes.</summary>
    private async Task SendLettersAsync(Type[] letters, PeriodicLetterContext context, INotifyClient client, string senderName)
    {
        foreach (var type in letters)
        {
            var letter = (BasePeriodicNotifyAction)serviceProvider.GetRequiredService(type);

            if (await letter.ShouldSendAsync(context))
            {
                await letter.SendAsync(context, client, senderName);
            }
        }
    }

    /// <summary>
    /// Everything the letters need to judge this portal, read once. The tariff and the quota are cached
    /// but not free, and the letters would otherwise fetch them twenty-five times over.
    /// </summary>
    private async Task<PeriodicLetterContext> BuildContextAsync(Tenant tenant, DateTime nowDate, bool enterprise = false)
    {
        var tariff = await tariffService.GetTariffAsync(tenant.Id);
        var quota = await tenantManager.GetTenantQuotaAsync(tenant.Id);

        // Enterprise falls back to the licence date when there is no due date; SaaS has no licence.
        var actualEndDate = enterprise && tariff.DueDate == DateTime.MaxValue ? tariff.LicenseDate : tariff.DueDate;

        return new PeriodicLetterContext
        {
            Tenant = tenant,
            Tariff = tariff,
            Quota = quota,
            NowDate = nowDate,
            CreatedDate = tenant.CreationDateTime.Date,
            DueDate = actualEndDate.Date,
            DueDateIsNotMax = actualEndDate != DateTime.MaxValue,
            DelayDueDate = tariff.DelayDueDate.Date,
            DelayDueDateIsNotMax = tariff.DelayDueDate != DateTime.MaxValue,
            DefaultRebranding = !enterprise || await tenantLogoManager.IsDefaultLogoSettingsAsync(),
            LastActivity = new Lazy<Task<DateTime>>(() => GetLastActivityDateAsync(tenant))
        };
    }

    /// <summary>
    /// The later of the last audit event and the last successful login, or the creation date when the
    /// portal has neither. Two queries, so it is only run for the letters that ask.
    /// </summary>
    private async Task<DateTime> GetLastActivityDateAsync(Tenant tenant)
    {
        var lastAuditEvent = await auditEventsRepository.GetLastEventAsync(tenant.Id);
        var lastLoginEvent = await loginEventsRepository.GetLastSuccessEventAsync(tenant.Id);

        var lastAuditEventDate = lastAuditEvent?.Date.Date ?? tenant.CreationDateTime.Date;
        var lastLoginEventDate = lastLoginEvent?.Date.Date ?? tenant.CreationDateTime.Date;

        return lastAuditEventDate > lastLoginEventDate ? lastAuditEventDate : lastLoginEventDate;
    }

    /// <summary>
    /// Warns the owner and the payer three days before the wallet is charged for the add-ons that renew
    /// then - one letter per portal, listing every add-on due on that day.
    /// </summary>
    private async Task SendUpcomingSubscriptionPaymentAsync(Tenant tenant, Tariff tariff, DateTime nowDate, Dictionary<int, TenantQuota> walletQuotas, INotifyClient client, string senderName)
    {
        var features = tariff.Quotas
            // NextQuantity 0 means the subscription was cancelled: it is neither renewed nor charged for.
            .Where(q => q.Wallet && q.Additional && q.NextQuantity is not <= 0
                        && q.DueDate.HasValue && q.DueDate.Value.Date.AddDays(-3) == nowDate)
            // a scheduled switch to another add-on is bought outright instead of renewing the current one
            .Select(q => walletQuotas.GetValueOrDefault(q.NextQuota ?? q.Id))
            .Where(q => q != null)
            .Select(q => q.Features.Split(':')[0]) // a wallet add-on carries exactly one feature
            // an add-on with no title of its own would show up as a blank in the letter
            .Where(f => Resource.ResourceManager.GetString(FeatureTitleKey(f)) != null)
            .ToList();

        if (features.Count == 0)
        {
            return;
        }

        var users = new List<UserInfo> { await userManager.GetUsersAsync(tenant.OwnerId) };

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        var payer = await userManager.GetUserByEmailAsync(customerInfo?.Email);

        if (payer.Id != Constants.LostUser.Id && users.TrueForAll(u => u.Id != payer.Id))
        {
            users.Add(payer);
        }

        // The add-on titles are the ones the billing page shows, resolved in the recipient's culture.
        Func<CultureInfo, string> subscriptionName = c =>
            string.Join(", ", features.Select(f => Resource.ResourceManager.GetString(FeatureTitleKey(f), c)));

        var action = serviceProvider.GetService<UpcomingSubscriptionPaymentNotifyAction>();

        foreach (var u in users)
        {
            action.Init(u, subscriptionName);
            await client.SendNoticeToAsync(action, u, senderName);
        }
    }

    /// <summary>The title of a wallet add-on, the same key <c>QuotaHelper.GetFeatures</c> resolves.</summary>
    private static string FeatureTitleKey(string featureName) => $"TariffsFeature_{featureName}_wallet";
}
