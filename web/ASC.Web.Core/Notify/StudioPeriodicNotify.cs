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

using ASC.Core.Common.Identity;

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Studio.Core.Notify;

[Scope]
public class StudioPeriodicNotify(
    ILoggerFactory loggerFactory,
    WorkContext workContext,
    TenantManager tenantManager,
    TenantLogoManager tenantLogoManager,
    UserManager userManager,
    StudioNotifyHelper studioNotifyHelper,
    ITariffService tariffService,
    TenantExtra tenantExtra,
    CommonLinkUtility commonLinkUtility,
    ApiSystemHelper apiSystemHelper,
    ExternalResourceSettingsHelper externalResourceSettingsHelper,
    CoreBaseSettings coreBaseSettings,
    CoreSettings coreSettings,
    IServiceProvider serviceProvider,
    AuditEventsRepository auditEventsRepository,
    LoginEventsRepository loginEventsRepository,
    IFusionCache hybridCache,
    IEventBus eventBus,
    IdentityClient identityClient,
    SecurityContext securityContext)
{
    private readonly ILogger _log = loggerFactory.CreateLogger("ASC.Notify");

    private const string CacheKey = "notification_date_for_unused_portals";

    private static string GetCspKey(string domain) => $"csp:{domain}";

    public async ValueTask SendSaasLettersAsync(string senderName, DateTime scheduleDate)
    {
        _log.InformationStartSendSaasTariffLetters();

        var activeTenants = await tenantManager.GetTenantsAsync();

        if (activeTenants.Count <= 0)
        {
            _log.InformationEndSendSaasTariffLetters();
        }

        var nowDate = scheduleDate.Date;
        var startDateToNotifyUnusedPortals = nowDate;

        var cacheValue = await hybridCache.GetOrDefaultAsync<string>(CacheKey);
        if (string.IsNullOrEmpty(cacheValue))
        {
            await hybridCache.SetAsync(CacheKey, JsonSerializer.Serialize(startDateToNotifyUnusedPortals));
        }
        else
        {
            startDateToNotifyUnusedPortals = JsonSerializer.Deserialize<DateTime>(cacheValue);
        }

        var startDateToRemoveUnusedPortals = startDateToNotifyUnusedPortals.AddDays(7);

        // The paid add-ons the wallet is charged for, by quota id: their titles are what the upcoming
        // payment letter lists. Global and cached, so they are read once for all tenants.
        var walletQuotas = (await tenantManager.GetTenantQuotasAsync(all: true, wallet: true)).ToDictionary(q => q.TenantId);

        foreach (var tenant in activeTenants)
        {
            try
            {
                await tenantManager.SetCurrentTenantAsync(tenant.Id);
                var client = workContext.RegisterClient(serviceProvider, studioNotifyHelper.NotifySource);

                var tariff = await tariffService.GetTariffAsync(tenant.Id);
                var quota = await tenantManager.GetTenantQuotaAsync(tenant.Id);
                var createdDate = tenant.CreationDateTime.Date;

                var dueDateIsNotMax = tariff.DueDate != DateTime.MaxValue;
                var dueDate = tariff.DueDate.Date;

                var delayDueDateIsNotMax = tariff.DelayDueDate != DateTime.MaxValue;
                var delayDueDate = tariff.DelayDueDate.Date;

                #region 3 days before the wallet is charged for an add-on subscription

                // Every add-on renews on its own due date, whatever the tariff state is, so this reminder
                // is sent on its own and takes no part in the one-letter-per-run chain below.
                await SendUpcomingSubscriptionPaymentAsync(tenant, tariff, nowDate, walletQuotas);

                #endregion

                BasePeriodicNotifyAction action = null;
                var paymentMessage = true;

                var toadmins = false;
                var tousers = false;
                var toowner = false;
                var topayer = false;

                Func<CultureInfo, string> orangeButtonText = _ => string.Empty;
                Func<CultureInfo, string> orangeButtonUrl = _ => string.Empty;
                Func<CultureInfo, string> orangeButtonText1 = _ => string.Empty;
                Func<CultureInfo, string> orangeButtonUrl1 = _ => string.Empty;
                Func<CultureInfo, string> orangeButtonText2 = _ => string.Empty;
                Func<CultureInfo, string> orangeButtonUrl2 = _ => string.Empty;
                Func<CultureInfo, string> orangeButtonText3 = _ => string.Empty;
                Func<CultureInfo, string> orangeButtonUrl3 = _ => string.Empty;
                Func<CultureInfo, string> orangeButtonText4 = _ => string.Empty;
                Func<CultureInfo, string> orangeButtonUrl4 = _ => string.Empty;
                Func<CultureInfo, string> orangeButtonText5 = _ => string.Empty;
                Func<CultureInfo, string> orangeButtonUrl5 = _ => string.Empty;

                var img1 = string.Empty;
                var img2 = string.Empty;
                var img3 = string.Empty;
                var img4 = string.Empty;
                var img5 = string.Empty;
                var img6 = string.Empty;
                var img7 = string.Empty;

                Func<CultureInfo, string> url1 = _ => string.Empty;
                Func<CultureInfo, string> url2 = _ => string.Empty;
                Func<CultureInfo, string> url3 = _ => string.Empty;
                Func<CultureInfo, string> url4 = _ => string.Empty;
                Func<CultureInfo, string> url5 = _ => string.Empty;
                Func<CultureInfo, string> url6 = _ => string.Empty;
                Func<CultureInfo, string> url7 = _ => string.Empty;
                Func<CultureInfo, string> url8 = _ => string.Empty;
                Func<CultureInfo, string> url9 = _ => string.Empty;
                Func<CultureInfo, string> url10 = _ => string.Empty;
                Func<CultureInfo, string> url11 = _ => string.Empty;
                Func<CultureInfo, string> url12 = _ => string.Empty;
                Func<CultureInfo, string> url13 = _ => string.Empty;
                Func<CultureInfo, string> url14 = _ => string.Empty;

                string txtTrulyYours(CultureInfo c) => WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", c);
                var topGif = string.Empty;

                var trulyYoursAsTebleRow = false;

                #region 2 days after registration to owner and admins SAAS (any tariff)

                if (createdDate.AddDays(2) == nowDate)
                {
                    action = serviceProvider.GetService<SaasAdminHandyAppsV1NotifyAction>();
                    paymentMessage = false;
                    toowner = true;
                    toadmins = true;

                    orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGoToDocSpace", c);
                    orangeButtonUrl = _ => commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/');

                    trulyYoursAsTebleRow = true;
                }

                #endregion

                #region 3 days after registration to owner and admins SAAS (any tariff)

                else if (createdDate.AddDays(3) == nowDate)
                {
                    action = serviceProvider.GetService<SaasAdminConfigureV1NotifyAction>();
                    paymentMessage = false;
                    toowner = true;
                    toadmins = true;

                    orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonConfigureRightNow", c);
                    orangeButtonUrl = _ => commonLinkUtility.GetFullAbsolutePath("~/portal-settings");
                    topGif = studioNotifyHelper.GetNotificationImageUrl("configure_docspace.gif");

                    url1 = c => externalResourceSettingsHelper.Helpcenter.GetRegionalDomain(c);
                    url2 = _ => commonLinkUtility.GetFullAbsolutePath("~/billing/tariff-plan");

                    trulyYoursAsTebleRow = true;
                }

                #endregion

                #region 4 days after registration to owner and admins SAAS (any tariff)

                else if (createdDate.AddDays(4) == nowDate)
                {
                    action = serviceProvider.GetService<SaasAdminAddonsV1NotifyAction>();
                    paymentMessage = false;
                    toowner = true;
                    toadmins = true;

                    orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetStarted", c);
                    orangeButtonUrl = _ => commonLinkUtility.GetFullAbsolutePath("~/billing/overview");

                    url1 = _ => commonLinkUtility.GetFullAbsolutePath("~/billing/overview");
                    url2 = _ => commonLinkUtility.GetFullAbsolutePath("~/billing/wallet");

                    trulyYoursAsTebleRow = true;
                }

                #endregion

                #region 7 days after registration to owner and admins SAAS (any tariff)

                else if (createdDate.AddDays(7) == nowDate)
                {
                    action = serviceProvider.GetService<SaasAdminAiAgentsV1NotifyAction>();
                    paymentMessage = false;
                    toowner = true;
                    toadmins = true;

                    orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonActivateAiFeatures", c);
                    orangeButtonUrl = _ => commonLinkUtility.GetFullAbsolutePath("~/portal-settings/ai-settings/ai-models");

                    trulyYoursAsTebleRow = true;
                }

                #endregion

                #region 10 days after registration to owner and admins SAAS (any tariff)

                else if (createdDate.AddDays(10) == nowDate)
                {
                    action = serviceProvider.GetService<SaasAdminDeveloperToolsV1NotifyAction>();
                    paymentMessage = false;
                    toowner = true;
                    toadmins = true;

                    orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetStarted", c);
                    orangeButtonUrl = _ => commonLinkUtility.GetFullAbsolutePath("~/developer-tools/overview");

                    url1 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("allconnectors", c);
                    url2 = c => externalResourceSettingsHelper.Api.GetRegionalDomain(c);

                    trulyYoursAsTebleRow = true;
                }

                #endregion

                #region 14 days after registration to admins and users SAAS (any tariff)

                else if (createdDate.AddDays(14) == nowDate)
                {
                    action = serviceProvider.GetService<SaasAdminUserAppsTipsV1NotifyAction>();
                    paymentMessage = false;
                    toadmins = true;
                    tousers = true;

                    topGif = studioNotifyHelper.GetNotificationImageUrl("free_apps.gif");

                    img1 = studioNotifyHelper.GetNotificationImageUrl("windows.png");
                    img2 = studioNotifyHelper.GetNotificationImageUrl("apple.png");
                    img3 = studioNotifyHelper.GetNotificationImageUrl("linux.png");
                    img4 = studioNotifyHelper.GetNotificationImageUrl("android.png");

                    url1 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("downloaddesktop", c);
                    url2 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("downloadmobile", c);

                    trulyYoursAsTebleRow = true;
                }

                #endregion

                else if (quota.Free)
                {
                    #region without activity to owner SAAS Free

                    if (nowDate.Day == tenant.CreationDateTime.Day || nowDate.AddDays(-7).Day == tenant.CreationDateTime.Day)
                    {
                        var lastAuditEvent = await auditEventsRepository.GetLastEventAsync(tenant.Id);
                        var lastAuditEventDate = lastAuditEvent != null ? lastAuditEvent.Date.Date : tenant.CreationDateTime.Date;

                        if (lastAuditEventDate.AddMonths(3) > nowDate)
                        {
                            continue;
                        }

                        var lastLoginEvent = await loginEventsRepository.GetLastSuccessEventAsync(tenant.Id);
                        var lastLoginEventDate = lastLoginEvent != null ? lastLoginEvent.Date.Date : tenant.CreationDateTime.Date;

                        if (lastLoginEventDate.AddMonths(3) > nowDate)
                        {
                            continue;
                        }

                        var lastActivityDate = lastAuditEventDate > lastLoginEventDate ? lastAuditEventDate : lastLoginEventDate;

                        if (nowDate >= startDateToNotifyUnusedPortals && nowDate.Day == tenant.CreationDateTime.Day)
                        {
                            // This runs once a month, so each one-month-wide window warns the owner exactly
                            // once and an idle portal is not spammed on the following checks.
                            if (lastActivityDate.AddMonths(4) > nowDate)
                            {
                                action = serviceProvider.GetService<SaasAdminStartupWarningAfterThreeMonthsV1NotifyAction>();
                                toowner = true;

                                orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonLogIn", c);
                                orangeButtonUrl = _ => commonLinkUtility.GetFullAbsolutePath("~/dashboard");

                                topGif = studioNotifyHelper.GetNotificationImageUrl("docspace_deleted.gif");

                                trulyYoursAsTebleRow = true;
                            }
                            else if (lastActivityDate.AddMonths(6) <= nowDate && lastActivityDate.AddMonths(7) > nowDate)
                            {
                                action = serviceProvider.GetService<SaasAdminStartupWarningAfterHalfYearV1NotifyAction>();
                                toowner = true;

                                orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonLeaveFeedback", c);
                                orangeButtonUrl = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("registrationcanceled", c);

                                url1 = c => externalResourceSettingsHelper.Common.GetRegionalFullEntry("legalterms", c);

                                topGif = studioNotifyHelper.GetNotificationImageUrl("docspace_deleted.gif");

                                trulyYoursAsTebleRow = true;
                            }
                        }

                        if (nowDate >= startDateToRemoveUnusedPortals && nowDate.AddDays(-7).Day == tenant.CreationDateTime.Day
                            && lastActivityDate.AddMonths(6).AddDays(7) <= nowDate)
                        {
                            if (await tenantManager.IsForbiddenDomainAsync(tenant.Alias))
                            {
                                continue;
                            }

                            var tenantDomain = tenant.GetTenantDomain(coreSettings);

                            _log.InformationStartRemovingInactiveTenant(tenant.Id, tenantDomain);

                            await securityContext.AuthenticateMeWithoutCookieAsync(tenant.OwnerId);
                            await identityClient.DeleteTenantClientsAsync(false);
                            await tenantManager.RemoveTenantAsync(tenant, true);

                            if (!coreBaseSettings.Standalone && apiSystemHelper.ApiCacheEnable)
                            {
                                await apiSystemHelper.RemoveTenantFromCacheAsync(tenantDomain);
                            }

                            await hybridCache.RemoveAsync(GetCspKey(tenantDomain));

                            await eventBus.PublishAsync(new RemovePortalIntegrationEvent(Guid.Empty, tenant.Id));
                        }
                    }

                    #endregion

                }

                else if (tariff.State >= TariffState.Paid)
                {
                    #region Payment warning letters

                    #region 3 days before grace period

                    if (dueDateIsNotMax && dueDate.AddDays(-3) == nowDate)
                    {
                        action = serviceProvider.GetService<SaasOwnerPaymentWarningGracePeriodBeforeActivationNotifyAction>();
                        toowner = true;
                        topayer = true;

                        url1 = _ => commonLinkUtility.GetFullAbsolutePath("~/billing/payment-method");
                    }

                    #endregion

                    #region grace period activation

                    else if (dueDateIsNotMax && dueDate.AddDays(1) == nowDate && delayDueDateIsNotMax)
                    {
                        action = serviceProvider.GetService<SaasOwnerPaymentWarningGracePeriodActivationNotifyAction>();
                        toowner = true;
                        topayer = true;
                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonVisitBillingSection", c);
                        orangeButtonUrl = _ => commonLinkUtility.GetFullAbsolutePath("~/billing/overview");
                    }

                    #endregion

                    #region grace period last day

                    else if (tariff.State == TariffState.Delay && delayDueDateIsNotMax && delayDueDate.AddDays(-1) == nowDate)
                    {
                        action = serviceProvider.GetService<SaasOwnerPaymentWarningGracePeriodLastDayNotifyAction>();
                        toowner = true;
                        topayer = true;
                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonVisitBillingSection", c);
                        orangeButtonUrl = _ => commonLinkUtility.GetFullAbsolutePath("~/billing/overview");
                    }

                    #endregion

                    #region grace period expired

                    else if (tariff.State == TariffState.Delay && delayDueDateIsNotMax && delayDueDate == nowDate)
                    {
                        action = serviceProvider.GetService<SaasOwnerPaymentWarningGracePeriodExpiredNotifyAction>();
                        toowner = true;
                        topayer = true;
                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonVisitBillingSection", c);
                        orangeButtonUrl = _ => commonLinkUtility.GetFullAbsolutePath("~/billing/overview");
                    }

                    #endregion

                    #region 3 months after SAAS PAID expired

                    else if (tariff.State == TariffState.NotPaid && dueDateIsNotMax && dueDate.AddMonths(3) == nowDate)
                    {
                        action = serviceProvider.GetService<SaasAdminWarningAfterThreeMonthsV1NotifyAction>();
                        toowner = true;

                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonLogIn", c);
                        orangeButtonUrl = _ => commonLinkUtility.GetFullAbsolutePath("~/dashboard");

                        url1 = c => externalResourceSettingsHelper.Common.GetRegionalFullEntry("legalterms", c);

                        topGif = studioNotifyHelper.GetNotificationImageUrl("docspace_deleted.gif");

                        trulyYoursAsTebleRow = true;
                    }

                    #endregion

                    #region 6 months after SAAS PAID expired

                    else if (tariff.State == TariffState.NotPaid && dueDateIsNotMax && dueDate.AddMonths(6) == nowDate)
                    {
                        action = serviceProvider.GetService<SaasAdminWarningAfterHalfYearV1NotifyAction>();
                        toowner = true;

                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonLeaveFeedback", c);
                        orangeButtonUrl = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("registrationcanceled", c);

                        url1 = c => externalResourceSettingsHelper.Common.GetRegionalFullEntry("legalterms", c);

                        topGif = studioNotifyHelper.GetNotificationImageUrl("docspace_deleted.gif");

                        trulyYoursAsTebleRow = true;
                    }
                    else if (tariff.State == TariffState.NotPaid && dueDateIsNotMax && dueDate.AddMonths(6).AddDays(7) <= nowDate)
                    {
                        if (await tenantManager.IsForbiddenDomainAsync(tenant.Alias))
                        {
                            continue;
                        }

                        var tenantDomain = tenant.GetTenantDomain(coreSettings);

                        _log.InformationStartRemovingUnpaidTenant(tenant.Id, tenantDomain);

                        await securityContext.AuthenticateMeWithoutCookieAsync(tenant.OwnerId);
                        await identityClient.DeleteTenantClientsAsync(false);
                        await tenantManager.RemoveTenantAsync(tenant, true);

                        if (!coreBaseSettings.Standalone && apiSystemHelper.ApiCacheEnable)
                        {
                            await apiSystemHelper.RemoveTenantFromCacheAsync(tenantDomain);
                        }

                        await hybridCache.RemoveAsync(GetCspKey(tenantDomain));

                        await eventBus.PublishAsync(new RemovePortalIntegrationEvent(Guid.Empty, tenant.Id));
                    }

                    #endregion

                    #endregion
                }


                if (action == null)
                {
                    continue;
                }

                var users = await studioNotifyHelper.GetRecipientsAsync(toadmins, tousers, false);

                if (toowner)
                {
                    users = users.Append(await userManager.GetUsersAsync(tenant.OwnerId)).DistinctBy(u => u.Id);
                }

                if (topayer)
                {
                    var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
                    var payer = await userManager.GetUserByEmailAsync(customerInfo?.Email);

                    if (payer.Id != Constants.LostUser.Id && !users.Any(u => u.Id == payer.Id))
                    {
                        users = users.Concat([payer]);
                    }
                }
                var asyncUsers = users.ToAsyncEnumerable();
                await foreach (var u in asyncUsers.Where(async (u, _) => paymentMessage || await studioNotifyHelper.IsSubscribedToNotifyAsync(u,  serviceProvider.GetService<PeriodicNotifyAction>())))
                {
                    var culture = string.IsNullOrEmpty(u.CultureName) ? tenant.GetCulture() : u.GetCulture();
                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;
                    var rquota = await tenantExtra.GetRightQuota() ?? TenantQuota.Default;
                    await action.Init(culture, u, rquota, orangeButtonText, orangeButtonText1, orangeButtonText2, orangeButtonText3, orangeButtonText4, orangeButtonText5, orangeButtonUrl, orangeButtonUrl1, orangeButtonUrl2, orangeButtonUrl3, orangeButtonUrl4, orangeButtonUrl5, txtTrulyYours, trulyYoursAsTebleRow, img1, img2, img3, img4, img5, img6, img7, url1, url2, url3, url4, url5, url6, url7, url8, url9, url10, url11, url12, url13, url14, topGif);
                    await client.SendNoticeToAsync(action, u, senderName);
                }
            }
            catch (Exception err)
            {
                _log.ErrorSendSaasLettersAsync(tenant.Id, err);
            }
        }

        _log.InformationEndSendSaasTariffLetters();
    }

    /// <summary>
    /// Warns the owner and the payer three days before the wallet is charged for the add-ons that renew
    /// then - one letter per portal, listing every add-on due on that day.
    /// </summary>
    private async Task SendUpcomingSubscriptionPaymentAsync(Tenant tenant, Tariff tariff, DateTime nowDate, Dictionary<int, TenantQuota> walletQuotas)
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

        var owner = await userManager.GetUsersAsync(tenant.OwnerId);

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        var payer = await userManager.GetUserByEmailAsync(customerInfo?.Email);

        // The add-on titles are the ones the billing page shows, resolved in the recipient's culture.
        Func<CultureInfo, string> subscriptionName = c =>
            string.Join(", ", features.Select(f => Resource.ResourceManager.GetString(FeatureTitleKey(f), c)));

        await serviceProvider.GetService<StudioNotifyService>()
            .SendUpcomingSubscriptionPaymentAsync(payer.Id == Constants.LostUser.Id ? null : payer, owner, subscriptionName);
    }

    /// <summary>The title of a wallet add-on, the same key <c>QuotaHelper.GetFeatures</c> resolves.</summary>
    private static string FeatureTitleKey(string featureName) => $"TariffsFeature_{featureName}_wallet";

    public async Task SendEnterpriseLettersAsync(string senderName, DateTime scheduleDate)
    {
        var nowDate = scheduleDate.Date;

        _log.InformationStartSendTariffEnterpriseLetters();

        var activeTenants = await tenantManager.GetTenantsAsync();

        if (activeTenants.Count <= 0)
        {
            _log.InformationEndSendTariffEnterpriseLetters();
            return;
        }

        foreach (var tenant in activeTenants)
        {
            try
            {
                await tenantManager.SetCurrentTenantAsync(tenant.Id);
                var defaultRebranding = await tenantLogoManager.IsDefaultLogoSettingsAsync();
                var client = workContext.RegisterClient(serviceProvider, studioNotifyHelper.NotifySource);

                var tariff = await tariffService.GetTariffAsync(tenant.Id);
                var quota = await tenantManager.GetTenantQuotaAsync(tenant.Id);
                var createdDate = tenant.CreationDateTime.Date;

                var actualEndDate = tariff.DueDate != DateTime.MaxValue ? tariff.DueDate : tariff.LicenseDate;
                var dueDate = actualEndDate.Date;
                var delayDueDate = tariff.DelayDueDate.Date;

                BasePeriodicNotifyAction action = null;
                var paymentMessage = true;

                var toadmins = false;
                var tousers = false;

                Func<CultureInfo, string> orangeButtonText = _ => string.Empty;
                Func<CultureInfo, string> orangeButtonUrl = _ => string.Empty;

                Func<CultureInfo, string> txtTrulyYours = c => WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", c);
                var topGif = string.Empty;
                var img1 = string.Empty;
                var img2 = string.Empty;
                var img3 = string.Empty;
                var img4 = string.Empty;
                var img5 = string.Empty;

                Func<CultureInfo, string> url1 = _ => string.Empty;
                Func<CultureInfo, string> url2 = _ => string.Empty;
                Func<CultureInfo, string> url3 = _ => string.Empty;
                Func<CultureInfo, string> url4 = _ => string.Empty;
                Func<CultureInfo, string> url5 = _ => string.Empty;
                Func<CultureInfo, string> url6 = _ => string.Empty;

                var trulyYoursAsTableRow = false;

                if (quota.Trial && defaultRebranding)
                {
                    #region After registration letters

                    #region 14 days after registration to admins and users ENTERPRISE TRIAL + defaultRebranding

                    if (createdDate.AddDays(14) == nowDate)
                    {
                        action = serviceProvider.GetService<EnterpriseAdminUserAppsTipsV1NotifyAction>();
                        paymentMessage = false;
                        toadmins = true;
                        tousers = true;

                        topGif = studioNotifyHelper.GetNotificationImageUrl("free_apps.gif");

                        img1 = studioNotifyHelper.GetNotificationImageUrl("windows.png");
                        img2 = studioNotifyHelper.GetNotificationImageUrl("apple.png");
                        img3 = studioNotifyHelper.GetNotificationImageUrl("linux.png");
                        img4 = studioNotifyHelper.GetNotificationImageUrl("android.png");

                        url1 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("downloaddesktop", c);
                        url2 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("downloadmobile", c);

                        trulyYoursAsTableRow = true;
                    }

                    #endregion

                    #endregion
                }

                if (tariff.State == TariffState.Paid)
                {
                    #region Payment warning letters

                    #region 7 days before ENTERPRISE PAID expired to admins

                    if (dueDate.AddDays(-7) == nowDate)
                    {
                        action = quota.Lifetime
                            ? serviceProvider.GetService<EnterpriseAdminPaymentWarningLifetimeBeforeExpirationNotifyAction>()
                            : quota.Customization
                                ? serviceProvider.GetService<DeveloperAdminPaymentWarningGracePeriodBeforeActivationNotifyAction>()
                                : serviceProvider.GetService<EnterpriseAdminPaymentWarningGracePeriodBeforeActivationNotifyAction>();

                        toadmins = true;

                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonPurchaseNow", c);
                        orangeButtonUrl = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("docspaceprices", c) + "?utm_source=billing&utm_medium=email&utm_campaign=ee_docspace_expire_7_days";
                    }

                    #endregion

                    #region ENTERPRISE PAID expires today to admins

                    else if (dueDate == nowDate)
                    {
                        action = quota.Lifetime
                            ? serviceProvider.GetService<EnterpriseAdminPaymentWarningLifetimeExpirationNotifyAction>()
                            : quota.Customization
                                ? serviceProvider.GetService<DeveloperAdminPaymentWarningGracePeriodActivationNotifyAction>()
                                : serviceProvider.GetService<EnterpriseAdminPaymentWarningGracePeriodActivationNotifyAction>();

                        toadmins = true;

                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonPurchaseNow", c);
                        orangeButtonUrl = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("docspaceprices", c) + "?utm_source=billing&utm_medium=email&utm_campaign=ee_docspace_grace_period";
                    }

                    #endregion

                    #endregion
                }
                else if (tariff.State == TariffState.Delay)
                {
                    #region Payment warning letters

                    #region 7 days before ENTERPRISE GRACE PERIOD expired to admins

                    if (delayDueDate.AddDays(-7) == nowDate)
                    {
                        action = quota.Customization
                                ? serviceProvider.GetService<DeveloperAdminPaymentWarningGracePeriodBeforeExpirationNotifyAction>()
                                : serviceProvider.GetService<EnterpriseAdminPaymentWarningGracePeriodBeforeExpirationNotifyAction>();

                        toadmins = true;

                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonPurchaseNow", c);
                        orangeButtonUrl = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("docspaceprices", c) + "?utm_source=billing&utm_medium=email&utm_campaign=ee_docspace_grace_period_expire_soon";
                    }

                    #endregion

                    #region ENTERPRISE GRACE PERIOD expires today to admins

                    else if (delayDueDate == nowDate)
                    {
                        action = quota.Customization
                                ? serviceProvider.GetService<DeveloperAdminPaymentWarningGracePeriodExpirationNotifyAction>()
                                : serviceProvider.GetService<EnterpriseAdminPaymentWarningGracePeriodExpirationNotifyAction>();

                        toadmins = true;

                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonPurchaseNow", c);
                        orangeButtonUrl = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("docspaceprices", c) + "?utm_source=billing&utm_medium=email&utm_campaign=ee_docspace_no_available";
                    }

                    #endregion

                    #endregion
                }


                if (action == null)
                {
                    continue;
                }

                var users = await studioNotifyHelper.GetRecipientsAsync(toadmins, tousers, false);

                await foreach (var u in users.ToAsyncEnumerable().Where(async (u, _) => paymentMessage || await studioNotifyHelper.IsSubscribedToNotifyAsync(u, serviceProvider.GetService<PeriodicNotifyAction>())))
                {
                    var culture = string.IsNullOrEmpty(u.CultureName) ? tenant.GetCulture() : u.GetCulture();
                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;

                    var rquota = await tenantExtra.GetRightQuota() ?? TenantQuota.Default;
                    await action.Init(culture, u, rquota, orangeButtonText, orangeButtonUrl, txtTrulyYours, trulyYoursAsTableRow, img1, img2, img3, img4, img5, url1, url2, url3, url4, url5, url6, topGif);

                    await client.SendNoticeToAsync(action, u, senderName);
                }
            }
            catch (Exception err)
            {
                _log.ErrorSendEnterpriseLetters(err);
            }
        }

        _log.InformationEndSendTariffEnterpriseLetters();
    }
}
