// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using ASC.Core.Common.Identity;

using Constants = ASC.Core.Users.Constants;

namespace ASC.Web.Studio.Core.Notify;

[Scope]
public class StudioPeriodicNotify(ILoggerProvider log,
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
        DisplayUserSettingsHelper displayUserSettingsHelper,
        CoreSettings coreSettings,
        IServiceProvider serviceProvider,
        AuditEventsRepository auditEventsRepository,
        LoginEventsRepository loginEventsRepository,
        IFusionCache hybridCache,
        IEventBus eventBus,
        IdentityClient identityClient,
        SecurityContext securityContext)
{
    private readonly ILogger _log = log.CreateLogger("ASC.Notify");

    private const string CacheKey = "notification_date_for_unused_portals";

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

                INotifyAction action = null;
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

                if (quota.Free)
                {
                    #region After registration letters

                    #region 1 days after registration to admins SAAS Free

                    if (createdDate.AddDays(1) == nowDate)
                    {
                        action = Actions.SaasAdminModulesV1;
                        paymentMessage = false;
                        toadmins = true;

                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonConfigureRightNow", c);
                        orangeButtonUrl = c => commonLinkUtility.GetFullAbsolutePath("~/portal-settings/");
                        topGif = studioNotifyHelper.GetNotificationImageUrl("configure_docspace.gif");

                        url1 = c => externalResourceSettingsHelper.Helpcenter.GetRegionalFullEntry("administrationguides", c);

                        trulyYoursAsTebleRow = true;
                    }

                    #endregion

                    #region 4 days after registration to admins SAAS Free

                    if (createdDate.AddDays(4) == nowDate)
                    {
                        action = Actions.SaasAdminVideoGuides;
                        paymentMessage = false;
                        toadmins = true;

                        img1 = studioNotifyHelper.GetNotificationImageUrl("cover_1.png");
                        img2 = studioNotifyHelper.GetNotificationImageUrl("cover_2.png");
                        img3 = studioNotifyHelper.GetNotificationImageUrl("settings.png");
                        img4 = studioNotifyHelper.GetNotificationImageUrl("management.png");
                        img5 = studioNotifyHelper.GetNotificationImageUrl("administration.png");

                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonWatchFullPlaylist", c);
                        orangeButtonUrl = c => externalResourceSettingsHelper.Videoguides.GetRegionalFullEntry("playlist", c);

                        url1 = c => externalResourceSettingsHelper.Videoguides.GetRegionalFullEntry("full", c);
                        url2 = c => externalResourceSettingsHelper.Videoguides.GetRegionalFullEntry("rooms", c);
                        url3 = c => externalResourceSettingsHelper.Videoguides.GetRegionalFullEntry("roles", c);
                        url4 = c => externalResourceSettingsHelper.Videoguides.GetRegionalFullEntry("security", c);
                        url5 = c => externalResourceSettingsHelper.Videoguides.GetRegionalFullEntry("createfiles", c);
                        url6 = c => externalResourceSettingsHelper.Videoguides.GetRegionalFullEntry("profile", c);
                        url7 = c => externalResourceSettingsHelper.Videoguides.GetRegionalFullEntry("backup", c);
                        url8 = c => externalResourceSettingsHelper.Videoguides.GetRegionalFullEntry("whatis", c);
                        url9 = c => externalResourceSettingsHelper.Videoguides.GetRegionalFullEntry("operationswithfiles", c);
                        url10 = c => externalResourceSettingsHelper.Videoguides.GetRegionalFullEntry("activesessions", c);
                        url11 = c => externalResourceSettingsHelper.Videoguides.GetRegionalFullEntry("archive", c);
                        url12 = c => externalResourceSettingsHelper.Videoguides.GetRegionalFullEntry("filterfiles", c);
                        url13 = c => externalResourceSettingsHelper.Videoguides.GetRegionalFullEntry("fileversions", c);
                        url14 = c => externalResourceSettingsHelper.Videoguides.GetRegionalFullEntry("hotkeys", c);

                        topGif = studioNotifyHelper.GetNotificationImageUrl("video_guides.gif");

                        trulyYoursAsTebleRow = true;
                    }

                    #endregion

                    #region 7 days after registration to admins and users SAAS Free

                    else if (createdDate.AddDays(7) == nowDate)
                    {
                        action = Actions.DocsTips;
                        paymentMessage = false;
                        toadmins = true;
                        tousers = true;

                        img1 = studioNotifyHelper.GetNotificationImageUrl("docs_tips1.png");
                        img2 = studioNotifyHelper.GetNotificationImageUrl("docs_tips2.png");
                        img3 = studioNotifyHelper.GetNotificationImageUrl("docs_tips3.png");
                        img4 = studioNotifyHelper.GetNotificationImageUrl("docs_tips4.png");
                        img5 = studioNotifyHelper.GetNotificationImageUrl("docs_tips5.png");

                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonCollaborate", c);
                        orangeButtonUrl = c => commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/');

                        url1 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("collaborationrooms", c);
                        url2 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("publicrooms", c);
                        url3 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("customrooms", c);
                        url4 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("formfillingrooms", c);
                        url5 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("seamlesscollaboration", c);
                        url6 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("openai", c);

                        topGif = studioNotifyHelper.GetNotificationImageUrl("five_tips.gif");
                    }

                    #endregion

                    #region 10 days after registration to admins SAAS Free

                    else if (createdDate.AddDays(10) == nowDate)
                    {
                        action = Actions.SaasAdminIntegrations;
                        paymentMessage = false;
                        toadmins = true;

                        img1 = studioNotifyHelper.GetNotificationImageUrl("onlyoffice.png");
                        img2 = studioNotifyHelper.GetNotificationImageUrl("connect.png");
                        img3 = studioNotifyHelper.GetNotificationImageUrl("zoom.png");
                        img4 = studioNotifyHelper.GetNotificationImageUrl("zapier.png");
                        img5 = studioNotifyHelper.GetNotificationImageUrl("wordpress.png");
                        img6 = studioNotifyHelper.GetNotificationImageUrl("drupal.png");
                        img7 = studioNotifyHelper.GetNotificationImageUrl("pipedrive.png");

                        orangeButtonText1 = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetFreeApp", c);
                        orangeButtonUrl1 = c => externalResourceSettingsHelper.Integrations.GetRegionalFullEntry("zoom", c);
                        orangeButtonText2 = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetStarted", c);
                        orangeButtonUrl2 = c => externalResourceSettingsHelper.Integrations.GetRegionalFullEntry("zapier", c);
                        orangeButtonText3 = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetFreeApp", c);
                        orangeButtonUrl3 = c => externalResourceSettingsHelper.Integrations.GetRegionalFullEntry("wordpress", c);
                        orangeButtonText4 = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetFreeApp", c);
                        orangeButtonUrl4 = c => externalResourceSettingsHelper.Integrations.GetRegionalFullEntry("drupal", c);
                        orangeButtonText5 = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonGetFreeApp", c);
                        orangeButtonUrl5 = c => externalResourceSettingsHelper.Integrations.GetRegionalFullEntry("pipedrive", c);

                        url1 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("officeforzoom", c);
                        url2 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("officeforzapier", c);
                        url3 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("officeforwordpress", c);
                        url4 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("officefordrupal", c);

                        topGif = studioNotifyHelper.GetNotificationImageUrl("integration.gif");

                        trulyYoursAsTebleRow = true;
                    }

                    #endregion

                    #region 14 days after registration to admins and users SAAS Free

                    else if (createdDate.AddDays(14) == nowDate)
                    {
                        action = Actions.SaasAdminUserAppsTipsV1;
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

                    #endregion

                    #region 1 year whithout activity to owner SAAS Free

                    else if (nowDate.Day == tenant.CreationDateTime.Day || nowDate.AddDays(-7).Day == tenant.CreationDateTime.Day)
                    {
                        var lastAuditEvent = await auditEventsRepository.GetLastEventAsync(tenant.Id);
                        var lastAuditEventDate = lastAuditEvent != null ? lastAuditEvent.Date.Date : tenant.CreationDateTime.Date;

                        if (lastAuditEventDate.AddYears(1) > nowDate)
                        {
                            continue;
                        }

                        var lastLoginEvent = await loginEventsRepository.GetLastSuccessEventAsync(tenant.Id);
                        var lastLoginEventDate = lastLoginEvent != null ? lastLoginEvent.Date.Date : tenant.CreationDateTime.Date;

                        if (lastLoginEventDate.AddYears(1) > nowDate)
                        {
                            continue;
                        }

                        if (nowDate >= startDateToNotifyUnusedPortals && nowDate.Day == tenant.CreationDateTime.Day)
                        {
                            action = Actions.SaasAdminStartupWarningAfterYearV1;
                            toowner = true;

                            orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonLeaveFeedback", c);
                            orangeButtonUrl = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("registrationcanceled", c);

                            url1 = c => externalResourceSettingsHelper.Common.GetRegionalFullEntry("legalterms", c);

                            topGif = studioNotifyHelper.GetNotificationImageUrl("docspace_deleted.gif");

                            trulyYoursAsTebleRow = true;
                        }

                        if (nowDate >= startDateToRemoveUnusedPortals && nowDate.AddDays(-7).Day == tenant.CreationDateTime.Day)
                        {
                            _log.InformationStartRemovingUnusedFreeTenant(tenant.Id);

                            await securityContext.AuthenticateMeWithoutCookieAsync(tenant.OwnerId);
                            await identityClient.DeleteTenantClientsAsync(false);
                            await tenantManager.RemoveTenantAsync(tenant, true);

                            if (!coreBaseSettings.Standalone && apiSystemHelper.ApiCacheEnable)
                            {
                                await apiSystemHelper.RemoveTenantFromCacheAsync(tenant.GetTenantDomain(coreSettings));
                            }
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
                        action = Actions.SaasOwnerPaymentWarningGracePeriodBeforeActivation;
                        toowner = true;
                        topayer = true;
                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonVisitPaymentsSection", c);
                        orangeButtonUrl = c => commonLinkUtility.GetFullAbsolutePath("~/portal-settings/payments/portal-payments");
                    }

                    #endregion

                    #region grace period activation

                    else if (dueDateIsNotMax && dueDate == nowDate)
                    {
                        action = Actions.SaasOwnerPaymentWarningGracePeriodActivation;
                        toowner = true;
                        topayer = true;
                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonVisitPaymentsSection", c);
                        orangeButtonUrl = c => commonLinkUtility.GetFullAbsolutePath("~/portal-settings/payments/portal-payments");
                    }

                    #endregion

                    #region grace period last day

                    else if (tariff.State == TariffState.Delay && delayDueDateIsNotMax && delayDueDate.AddDays(-1) == nowDate)
                    {
                        action = Actions.SaasOwnerPaymentWarningGracePeriodLastDay;
                        toowner = true;
                        topayer = true;
                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonVisitPaymentsSection", c);
                        orangeButtonUrl = c => commonLinkUtility.GetFullAbsolutePath("~/portal-settings/payments/portal-payments");
                    }

                    #endregion

                    #region grace period expired

                    else if (tariff.State == TariffState.Delay && delayDueDateIsNotMax && delayDueDate == nowDate)
                    {
                        action = Actions.SaasOwnerPaymentWarningGracePeriodExpired;
                        toowner = true;
                        topayer = true;
                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonVisitPaymentsSection", c);
                        orangeButtonUrl = c => commonLinkUtility.GetFullAbsolutePath("~/portal-settings/payments/portal-payments");
                    }

                    #endregion

                    #region 6 months after SAAS PAID expired

                    else if (tariff.State == TariffState.NotPaid && dueDateIsNotMax && dueDate.AddMonths(6) == nowDate)
                    {
                        action = Actions.SaasAdminTrialWarningAfterHalfYearV1;
                        toowner = true;

                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonLeaveFeedback", c);
                        orangeButtonUrl = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("registrationcanceled", c);

                        url1 = c => externalResourceSettingsHelper.Common.GetRegionalFullEntry("legalterms", c);

                        topGif = studioNotifyHelper.GetNotificationImageUrl("docspace_deleted.gif");

                        trulyYoursAsTebleRow = true;
                    }
                    else if (tariff.State == TariffState.NotPaid && dueDateIsNotMax && dueDate.AddMonths(6).AddDays(7) <= nowDate)
                    {
                        await securityContext.AuthenticateMeWithoutCookieAsync(tenant.OwnerId);
                        await identityClient.DeleteTenantClientsAsync(false);
                        await tenantManager.RemoveTenantAsync(tenant, true);

                        if (!coreBaseSettings.Standalone && apiSystemHelper.ApiCacheEnable)
                        {
                            await apiSystemHelper.RemoveTenantFromCacheAsync(tenant.GetTenantDomain(coreSettings));
                        }
                        await eventBus.PublishAsync(new RemovePortalIntegrationEvent(Guid.Empty, tenant.Id));
                    }

                    #endregion

                    #endregion
                }


                if (action == null)
                {
                    continue;
                }

                var users = toowner
                                    ? new List<UserInfo> { await userManager.GetUsersAsync(tenant.OwnerId) }
                                    : await studioNotifyHelper.GetRecipientsAsync(toadmins, tousers, false);

                if (topayer)
                {
                    var payer = await userManager.GetUserByEmailAsync(tariff.CustomerId);

                    if (payer.Id != Constants.LostUser.Id && !users.Any(u => u.Id == payer.Id))
                    {
                        users = users.Concat([payer]);
                    }
                }
                var asyncUsers = users.ToAsyncEnumerable();
                await foreach (var u in asyncUsers.WhereAwait(async u => paymentMessage || await studioNotifyHelper.IsSubscribedToNotifyAsync(u, Actions.PeriodicNotify)))
                {
                    var culture = string.IsNullOrEmpty(u.CultureName) ? tenant.GetCulture() : u.GetCulture();
                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;
                    var rquota = await tenantExtra.GetRightQuota() ?? TenantQuota.Default;

                    await client.SendNoticeToAsync(
                        action,
                        u,
                        senderName,
                        new TagValue(CommonTags.Culture, culture.Name),
                        new TagValue(Tags.UserName, u.FirstName.HtmlEncode()),
                        new TagValue(Tags.ActiveUsers, (await userManager.GetUsersAsync()).Length),
                        new TagValue(Tags.Price, rquota.Price),
                        new TagValue(Tags.PricePeriod, rquota.Year ? UserControlsCommonResource.TariffPerYear : UserControlsCommonResource.TariffPerMonth),
                        //new TagValue(Tags.DueDate, dueDate.ToLongDateString()),
                        //new TagValue(Tags.DelayDueDate, (delayDueDateIsNotMax ? delayDueDate : dueDate).ToLongDateString()),
                        TagValues.OrangeButton(orangeButtonText(culture), orangeButtonUrl(culture)),
                        TagValues.OrangeButton(orangeButtonText1(culture), orangeButtonUrl1(culture), "OrangeButton1"),
                        TagValues.OrangeButton(orangeButtonText2(culture), orangeButtonUrl2(culture), "OrangeButton2"),
                        TagValues.OrangeButton(orangeButtonText3(culture), orangeButtonUrl3(culture), "OrangeButton3"),
                        TagValues.OrangeButton(orangeButtonText4(culture), orangeButtonUrl4(culture), "OrangeButton4"),
                        TagValues.OrangeButton(orangeButtonText5(culture), orangeButtonUrl5(culture), "OrangeButton5"),
                        TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours(culture), trulyYoursAsTebleRow),
                        new TagValue("IMG1", img1),
                        new TagValue("IMG2", img2),
                        new TagValue("IMG3", img3),
                        new TagValue("IMG4", img4),
                        new TagValue("IMG5", img5),
                        new TagValue("IMG6", img6),
                        new TagValue("IMG7", img7),
                        new TagValue("URL1", url1(culture)),
                        new TagValue("URL2", url2(culture)),
                        new TagValue("URL3", url3(culture)),
                        new TagValue("URL4", url4(culture)),
                        new TagValue("URL5", url5(culture)),
                        new TagValue("URL6", url6(culture)),
                        new TagValue("URL7", url7(culture)),
                        new TagValue("URL8", url8(culture)),
                        new TagValue("URL9", url9(culture)),
                        new TagValue("URL10", url10(culture)),
                        new TagValue("URL11", url11(culture)),
                        new TagValue("URL12", url12(culture)),
                        new TagValue("URL13", url13(culture)),
                        new TagValue("URL14", url14(culture)),
                        new TagValue(CommonTags.TopGif, topGif),
                        new TagValue(Tags.PaymentDelay, tariffService.GetPaymentDelay()),
                        new TagValue(CommonTags.Footer, await userManager.IsDocSpaceAdminAsync(u) ? "common" : "social"));
                }
            }
            catch (Exception err)
            {
                _log.ErrorSendSaasLettersAsync(err);
            }
        }

        _log.InformationEndSendSaasTariffLetters();
    }

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

                INotifyAction action = null;
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

                    #region 7 days after registration to admins and users ENTERPRISE TRIAL + defaultRebranding

                    if (createdDate.AddDays(7) == nowDate)
                    {
                        action = Actions.DocsTips;
                        paymentMessage = false;
                        toadmins = true;
                        tousers = true;

                        img1 = studioNotifyHelper.GetNotificationImageUrl("docs_tips1.png");
                        img2 = studioNotifyHelper.GetNotificationImageUrl("docs_tips2.png");
                        img3 = studioNotifyHelper.GetNotificationImageUrl("docs_tips3.png");
                        img4 = studioNotifyHelper.GetNotificationImageUrl("docs_tips4.png");
                        img5 = studioNotifyHelper.GetNotificationImageUrl("docs_tips5.png");

                        url1 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("collaborationrooms", c);
                        url2 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("publicrooms", c);
                        url3 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("customrooms", c);
                        url4 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("formfillingrooms", c);
                        url5 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("seamlesscollaboration", c);
                        url6 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("openai", c);

                        topGif = studioNotifyHelper.GetNotificationImageUrl("five_tips.gif");

                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonCollaborate", c);
                        orangeButtonUrl = c => commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/');

                        trulyYoursAsTableRow = true;
                    }

                    #endregion

                    #region 14 days after registration to admins and users ENTERPRISE TRIAL + defaultRebranding

                    else if (createdDate.AddDays(14) == nowDate)
                    {
                        action = Actions.EnterpriseAdminUserAppsTipsV1;
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
                            ? Actions.EnterpriseAdminPaymentWarningLifetimeBeforeExpiration
                            : quota.Customization
                                ? Actions.DeveloperAdminPaymentWarningGracePeriodBeforeActivation
                                : Actions.EnterpriseAdminPaymentWarningGracePeriodBeforeActivation;

                        toadmins = true;

                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonPurchaseNow", c);
                        orangeButtonUrl = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("docspaceprices", c) + "?utm_source=billing&utm_medium=email&utm_campaign=ee_docspace_expire_7_days";
                    }

                    #endregion

                    #region ENTERPRISE PAID expires today to admins

                    else if (dueDate == nowDate)
                    {
                        action = quota.Lifetime
                            ? Actions.EnterpriseAdminPaymentWarningLifetimeExpiration
                            : quota.Customization
                                ? Actions.DeveloperAdminPaymentWarningGracePeriodActivation
                                : Actions.EnterpriseAdminPaymentWarningGracePeriodActivation;

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
                                ? Actions.DeveloperAdminPaymentWarningGracePeriodBeforeExpiration
                                : Actions.EnterpriseAdminPaymentWarningGracePeriodBeforeExpiration;

                        toadmins = true;

                        orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonPurchaseNow", c);
                        orangeButtonUrl = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("docspaceprices", c) + "?utm_source=billing&utm_medium=email&utm_campaign=ee_docspace_grace_period_expire_soon";
                    }

                    #endregion

                    #region ENTERPRISE GRACE PERIOD expires today to admins

                    else if (delayDueDate == nowDate)
                    {
                        action = quota.Customization
                                ? Actions.DeveloperAdminPaymentWarningGracePeriodExpiration
                                : Actions.EnterpriseAdminPaymentWarningGracePeriodExpiration;

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

                await foreach (var u in users.ToAsyncEnumerable().WhereAwait(async u => paymentMessage || await studioNotifyHelper.IsSubscribedToNotifyAsync(u, Actions.PeriodicNotify)))
                {
                    var culture = string.IsNullOrEmpty(u.CultureName) ? tenant.GetCulture() : u.GetCulture();
                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;

                    var rquota = await tenantExtra.GetRightQuota() ?? TenantQuota.Default;

                    await client.SendNoticeToAsync(
                        action,
                        u,
                        senderName,
                        new TagValue(CommonTags.Culture, culture.Name),
                        new TagValue(Tags.UserName, u.FirstName.HtmlEncode()), 
                        new TagValue(Tags.ActiveUsers, (await userManager.GetUsersAsync()).Length),
                        new TagValue(Tags.Price, rquota.Price),
                        new TagValue(Tags.PricePeriod, rquota.Year ? UserControlsCommonResource.TariffPerYear : UserControlsCommonResource.TariffPerMonth),
                        new TagValue(Tags.PaymentDelay, tariffService.GetPaymentDelay()),
                        //new TagValue(Tags.DueDate, dueDate.ToLongDateString()),
                        //new TagValue(Tags.DelayDueDate, (delayDueDateIsNotMax ? delayDueDate : dueDate).ToLongDateString()),
                        TagValues.OrangeButton(orangeButtonText(culture), orangeButtonUrl(culture)),
                        TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours(culture), trulyYoursAsTableRow),
                        new TagValue("IMG1", img1),
                        new TagValue("IMG2", img2),
                        new TagValue("IMG3", img3),
                        new TagValue("IMG4", img4),
                        new TagValue("IMG5", img5),
                        new TagValue("URL1", url1(culture)),
                        new TagValue("URL2", url2(culture)),
                        new TagValue("URL3", url3(culture)),
                        new TagValue("URL4", url4(culture)),
                        new TagValue("URL5", url5(culture)),
                        new TagValue("URL6", url6(culture)),
                        new TagValue(CommonTags.TopGif, topGif));
                }
            }
            catch (Exception err)
            {
                _log.ErrorSendEnterpriseLetters(err);
            }
        }

        _log.InformationEndSendTariffEnterpriseLetters();
    }

    public async Task SendOpensourceLettersAsync(string senderName, DateTime scheduleDate)
    {
        var nowDate = scheduleDate.Date;

        _log.InformationStartSendOpensourceTariffLetters();

        var activeTenants = await tenantManager.GetTenantsAsync();

        if (activeTenants.Count <= 0)
        {
            _log.InformationEndSendOpensourceTariffLetters();
            return;
        }

        foreach (var tenant in activeTenants)
        {
            try
            {
                await tenantManager.SetCurrentTenantAsync(tenant.Id);
                var client = workContext.RegisterClient(serviceProvider, studioNotifyHelper.NotifySource);

                var createdDate = tenant.CreationDateTime.Date;


                #region After registration letters

                #region 7 days after registration to admins

                if (createdDate.AddDays(7) == nowDate)
                {
                    var users = await studioNotifyHelper.GetRecipientsAsync(true, true, false);

                    var orangeButtonUrl = commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/');

                    Func<CultureInfo, string> orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonCollaborate", c);
                    Func<CultureInfo, string> txtTrulyYours = c => WebstudioNotifyPatternResource.ResourceManager.GetString("TrulyYoursText", c);

                    var img1 = studioNotifyHelper.GetNotificationImageUrl("docs_tips1.png");
                    var img2 = studioNotifyHelper.GetNotificationImageUrl("docs_tips2.png");
                    var img3 = studioNotifyHelper.GetNotificationImageUrl("docs_tips3.png");
                    var img4 = studioNotifyHelper.GetNotificationImageUrl("docs_tips4.png");
                    var img5 = studioNotifyHelper.GetNotificationImageUrl("docs_tips5.png");

                    Func<CultureInfo, string> url1 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("collaborationrooms", c);
                    Func<CultureInfo, string> url2 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("publicrooms", c);
                    Func<CultureInfo, string> url3 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("customrooms", c);
                    Func<CultureInfo, string> url4 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("formfillingrooms", c);
                    Func<CultureInfo, string> url5 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("seamlesscollaboration", c);
                    Func<CultureInfo, string> url6 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("openai", c);

                    var topGif = studioNotifyHelper.GetNotificationImageUrl("five_tips.gif");

                    await foreach (var u in users.ToAsyncEnumerable().WhereAwait(async u => await studioNotifyHelper.IsSubscribedToNotifyAsync(u, Actions.PeriodicNotify)))
                    {
                        var culture = string.IsNullOrEmpty(u.CultureName) ? tenant.GetCulture() : u.GetCulture();
                        Thread.CurrentThread.CurrentCulture = culture;
                        Thread.CurrentThread.CurrentUICulture = culture;

                        await client.SendNoticeToAsync(
                            Actions.DocsTips,
                            u,
                            senderName,
                            new TagValue(CommonTags.Culture, culture.Name),
                            new TagValue(Tags.UserName, u.DisplayUserName(displayUserSettingsHelper)),
                            new TagValue(CommonTags.Footer, "opensource"),
                            TagValues.OrangeButton(orangeButtonText(culture), orangeButtonUrl),
                            TagValues.TrulyYours(studioNotifyHelper, txtTrulyYours(culture), true),
                            new TagValue("IMG1", img1),
                            new TagValue("IMG2", img2),
                            new TagValue("IMG3", img3),
                            new TagValue("IMG4", img4),
                            new TagValue("IMG5", img5),
                            new TagValue("URL1", url1(culture)),
                            new TagValue("URL2", url2(culture)),
                            new TagValue("URL3", url3(culture)),
                            new TagValue("URL4", url4(culture)),
                            new TagValue("URL5", url5(culture)),
                            new TagValue("URL6", url6(culture)),
                            new TagValue(CommonTags.TopGif, topGif));
                    }
                }
                #endregion

                #endregion
            }
            catch (Exception err)
            {
                _log.ErrorSendOpensourceLetters(err);
            }
        }

        _log.InformationEndSendOpensourceTariffLetters();
    }

    public static async Task<bool> ChangeSubscriptionAsync(Guid userId, StudioNotifyHelper studioNotifyHelper)
    {
        var recipient = await studioNotifyHelper.ToRecipientAsync(userId);

        var isSubscribe = await studioNotifyHelper.IsSubscribedToNotifyAsync(recipient, Actions.PeriodicNotify);

        await studioNotifyHelper.SubscribeToNotifyAsync(recipient, Actions.PeriodicNotify, !isSubscribe);

        return !isSubscribe;
    }
}
