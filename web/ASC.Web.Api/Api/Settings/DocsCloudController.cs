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

using ASC.Files.Core.ApiModels.ResponseDto;
using ASC.Files.Core.IntegrationEvents.Events;
using ASC.Files.Core.Services.DocumentBuilderService;

namespace ASC.Web.Api.Controllers.Settings;

[ApiEndpoint(Template = "docscloud")]
public class DocsCloudController(
    PermissionContext permissionContext,
    TenantManager tenantManager,
    CoreSettings coreSettings,
    DocsCloudClient docsCloudClient,
    ITariffService tariffService,
    IQuotaService quotaService,
    SecurityContext securityContext,
    UserManager userManager,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    PaymentHelper paymentHelper,
    CspSettingsHelper cspSettingsHelper,
    WebItemManager webItemManager,
    IFusionCache fusionCache,
    IConfiguration configuration,
    IDistributedLockProvider distributedLockProvider,
    CommonLinkUtility commonLinkUtility,
    IEventBus eventBus,
    DocumentBuilderTaskManager<CustomerOperationsReportTask, int, CustomerOperationsReportTaskData> documentBuilderTaskManager,
    IServiceProvider serviceProvider)
    : BaseSettingsController(fusionCache, webItemManager)
{
    /// <remarks>
    /// Starts the free Docs Connect trial for the current portal. If the trial is granted, the call adds the assigned
    /// Docs Connect server to the Content Security Policy settings. To check the subscriptions and the tariff state
    /// first, use `GET api/2.0/portal/tariff`. Requires the EditPortalSettings permission, which the portal owner and
    /// DocSpace admins have. Other callers get 403. The call changes data, and a repeated call returns 400.
    /// It returns `true` if the trial is granted. It returns `false` if the billing service declines it, for example
    /// after an earlier trial. Then nothing changes. The call never buys a paid plan. To move a paid Docs Connect
    /// subscription to Docs Connect Dev Pack, use `POST api/2.0/settings/docscloud/switchtodevpack`. The call returns
    /// 400 if the portal already has a Docs Connect trial, Docs Connect, or Docs Connect Dev Pack. It returns 402 if
    /// the tariff is delayed or not paid. It returns 403 if the billing service is not configured. If the installation
    /// has no Docs Connect trial quota, the call returns 404. After a granted trial, the call can still return 403 or
    /// 500, and the trial stays granted. It returns 403 if the server address makes the Content Security Policy header
    /// too long. It returns 500 if reading the Docs Connect tenant fails.
    /// </remarks>
    /// <summary>
    /// Start the Docs Connect trial
    /// </summary>
    /// <path>api/2.0/settings/docscloud/trial</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "Boolean value: true if the trial subscription is activated, false if the billing service declines it", typeof(bool))]
    [SwaggerResponse(400, "The portal already has a Docs Connect trial, Docs Connect or Docs Connect Dev Pack subscription")]
    [SwaggerResponse(402, "The portal tariff is delayed or not paid, so the trial cannot be started")]
    [SwaggerResponse(403, "The caller has no portal-settings right, the billing service is not configured, or the trial was granted but the address of the assigned Docs Connect server would push the Content Security Policy header over its size limit")]
    [SwaggerResponse(404, "The Docs Connect trial quota is not available on this installation")]
    [SwaggerResponse(500, "The trial was granted, but reading the Docs Connect tenant afterwards failed: the Docs Connect service is not configured on this installation, has no tenant for the portal yet, is unreachable, or answered with an error")]
    [HttpPost("trial")]
    public async Task<bool> StartDocsCloudTrial()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        paymentHelper.DemandConfigured();

        var tenant = tenantManager.GetCurrentTenant();

        var docsCloudTrialQuota = (await quotaService.GetTenantQuotasAsync())
            .FirstOrDefault(q => q.Name == "docscloudtrial");

        if (docsCloudTrialQuota == null)
        {
            throw new ItemNotFoundException("Quota could not be found");
        }

        var tariff = await tariffService.GetTariffAsync(tenant.Id);

        if (tariff.State > TariffState.Paid)
        {
            throw new BillingException("Tariff is not paid");
        }

        if (tariff.Quotas.Concat(tariff.OverdueQuotas ?? []).Any(q =>
                q.Id == docsCloudTrialQuota.TenantId ||
                q.Id == (int)TenantWalletService.DocsCloud ||
                q.Id == (int)TenantWalletService.DocsCloudDevPack))
        {
            throw new ArgumentException("Quota is already set");
        }

        var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);

        var customer = new DocsCloudTrialCustomer(
            user.Email,
            commonLinkUtility.ServerRootPath,
            user.DisplayUserName(false, displayUserSettingsHelper),
            user.MobilePhone,
            CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            tenant.AffiliateId);

        var result = await paymentHelper.GetDocsCloudTrialAsync(tenant.Id, docsCloudTrialQuota.Name, customer);

        if (result)
        {
            var docsCloudTenant = await docsCloudClient.GetTenantAsync(await GetPortalIdAsync(), true);

            await ChangeCspSettingsAsync(docsCloudTenant);
        }

        return result;
    }

    /// <remarks>
    /// Switches the paid Docs Connect subscription of the current portal to Docs Connect Dev Pack for `quantity` users.
    /// The call charges the price difference to the portal wallet and moves the license to the new product. To check
    /// the subscriptions and the tariff state first, use `GET api/2.0/portal/tariff`. To get the price, use
    /// `POST api/2.0/settings/docscloud/calculatedevpack` with the same `quantity`. Requires a DocSpace admin. Other
    /// callers get 403. The call spends money, and a repeated call returns 400. Concurrent calls run one after
    /// another, so the portal charges the wallet once. It returns `true` if the subscription is switched. It returns
    /// `false` if the billing service declines or fails the switch. Then the portal charges nothing and stays on Docs
    /// Connect. To change the number of users, or to return to Docs Connect at the next billing period, use
    /// `PUT api/2.0/portal/payment/updatewallet`. The call returns 400 if the body cannot be read. It also returns 400
    /// without an active Docs Connect subscription. It also returns 400 if the portal already has Docs Connect Dev Pack, or
    /// `quantity` is below its minimum. It returns 402 if the tariff is delayed or not paid. It returns 403 if the
    /// billing service is not configured. It returns 404 for a portal without a billing customer, or an installation
    /// without these wallet products.
    /// </remarks>
    /// <summary>
    /// Switch Docs Connect to Docs Connect Dev Pack
    /// </summary>
    /// <path>api/2.0/settings/docscloud/switchtodevpack</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "Boolean value: true if the subscription is switched to Docs Connect Dev Pack, false if the billing service declines it", typeof(bool))]
    [SwaggerResponse(400, "The request body cannot be read, the quantity is below the allowed minimum, the portal has no active Docs Connect subscription, or it already has a Docs Connect Dev Pack subscription")]
    [SwaggerResponse(402, "The portal tariff is delayed or not paid, so the subscription cannot be switched")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the billing service is not configured")]
    [SwaggerResponse(404, "The portal is not registered as a billing customer, or the Docs Connect and Docs Connect Dev Pack wallet products are not configured on this installation")]
    [HttpPost("switchtodevpack")]
    public async Task<bool> SwitchToDevPack(DocsCloudDevPackRequestDto inDto)
    {
        var tenant = tenantManager.GetCurrentTenant();

        // Serialize concurrent switch requests per tenant so the check-then-switch sequence in
        // PrepareSwitchAsync cannot run twice in parallel (which would double-charge the wallet).
        // A second request waits, then re-runs the check and hits the "already set" guard.
        await using (await distributedLockProvider.TryAcquireFairLockAsync($"docscloud_switchtodevpack_{tenant.Id}"))
        {
            var (fromQuota, toQuota) = await PrepareSwitchAsync(inDto.Quantity);

            return await paymentHelper.SwitchSubscriptionAsync(tenant.Id, fromQuota.GetPaymentId(), toQuota.GetPaymentId(), inDto.Quantity, securityContext.CurrentAccount.ID.ToString(), toQuota.Name);
        }
    }

    /// <remarks>
    /// Calculates the price of switching the portal from Docs Connect to Docs Connect Dev Pack for `quantity` users.
    /// The call changes nothing and charges nothing. To check the subscriptions and the tariff state first, use
    /// `GET api/2.0/portal/tariff`. Requires a DocSpace admin. Other callers get 403. The call is read-only, so you
    /// can try several values of `quantity` before the switch. An empty result means that the billing service cannot
    /// price the switch. In that case, do not start the switch. To switch, use
    /// `POST api/2.0/settings/docscloud/switchtodevpack` with the same `quantity`. It needs no value from this
    /// response. To price a new number of users for an existing subscription, use
    /// `PUT api/2.0/portal/payment/calculatewallet`. The call returns 400 if the body cannot be read, or the portal
    /// has no active Docs Connect subscription. It also returns 400 if the portal already has Docs Connect Dev Pack,
    /// or `quantity` is below its minimum. It returns 402 if the tariff is delayed or not paid. It returns 403 if the
    /// billing service is not configured. It returns 404 for a portal without a billing customer, or an installation
    /// without these wallet products.
    /// </remarks>
    /// <summary>
    /// Calculate the Docs Connect Dev Pack switch cost
    /// </summary>
    /// <path>api/2.0/settings/docscloud/calculatedevpack</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The cost of switching to Docs Connect Dev Pack for the requested quantity, or an empty result if the billing service could not price it", typeof(PaymentCalculationDto))]
    [SwaggerResponse(400, "The request body cannot be read, the quantity is below the allowed minimum, the portal has no active Docs Connect subscription, or it already has a Docs Connect Dev Pack subscription")]
    [SwaggerResponse(402, "The portal tariff is delayed or not paid, so the switch cannot be priced")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the billing service is not configured")]
    [SwaggerResponse(404, "The portal is not registered as a billing customer, or the Docs Connect and Docs Connect Dev Pack wallet products are not configured on this installation")]
    [HttpPost("calculatedevpack")]
    public async Task<PaymentCalculationDto> CalculateDevPack(DocsCloudDevPackRequestDto inDto)
    {
        var (fromQuota, toQuota) = await PrepareSwitchAsync(inDto.Quantity);

        var tenant = tenantManager.GetCurrentTenant();

        return (await tariffService.CalculateSwitchSubscriptionAsync(tenant.Id, fromQuota.GetPaymentId(), toQuota.GetPaymentId(), inDto.Quantity))?.Map();
    }

    /// <remarks>
    /// Returns the Docs Connect tenant of the current portal. It describes the Docs Connect server assigned to the
    /// portal. It also gives the subscription end date and the payment. A tenant exists only after a Docs Connect
    /// trial from `POST api/2.0/settings/docscloud/trial` or a Docs Connect purchase. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. The call is read-only. The
    /// portal caches the tenant for an hour, and the absence of a tenant for a minute. So after a subscription change,
    /// pass `refresh=true` to read the current state from Docs Connect. The `address` field is the full address of
    /// the server. The dates in the result are in UTC. An empty result means that the portal has no Docs Connect
    /// tenant yet. This is a normal state, not an error. So use this call to check if Docs Connect is active. To read
    /// the license, the configuration, the user quota, or the usage, use `GET api/2.0/settings/docscloud/tenant/info`,
    /// `GET api/2.0/settings/docscloud/tenant/config`, `GET api/2.0/settings/docscloud/tenant/quota`, or
    /// `GET api/2.0/settings/docscloud/tenant/usage`. Each of them returns 400 while the portal has no tenant. The
    /// call returns 500 if the Docs Connect service is not configured or fails.
    /// </remarks>
    /// <summary>
    /// Get the Docs Connect tenant
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant</path>
    /// <param name="refresh">Pass `true` to skip the cached copy and request the tenant from Docs Connect again, replacing the cached one; with the default `false` the answer may be up to an hour old, or up to a minute old while the portal has no tenant.</param>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The Docs Connect tenant of the portal, or an empty result if no Docs Connect tenant is assigned to it", typeof(DocsCloudTenantDto))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [SwaggerResponse(500, "The Docs Connect service is not configured on this installation, is unreachable, or answered with an error other than not found")]
    [HttpGet("tenant")]
    public async Task<DocsCloudTenantDto> GetTenant(bool refresh = false)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return (await docsCloudClient.GetTenantAsync(await GetPortalIdAsync(), refresh))?.Map();
    }

    /// <remarks>
    /// Returns the Docs Connect license of the current portal, its server, and the user limits of the license. It
    /// also returns the editor and viewer usage of the current period. The portal needs a Docs Connect tenant from
    /// `POST api/2.0/settings/docscloud/trial` or a Docs Connect purchase. Without one, the call returns 400, and
    /// `GET api/2.0/settings/docscloud/tenant` returns an empty result. Requires the EditPortalSettings permission,
    /// which the portal owner and DocSpace admins have. Other callers get 403. The call is read-only. The portal
    /// caches the answer for a minute. So after a subscription change, pass
    /// `refresh=true` to read the current state from Docs Connect. If the portal has a paid Docs Connect or Docs
    /// Connect Dev Pack subscription, `license.trial` is `false`. This holds even when the license itself is a trial.
    /// The dates in the result are in UTC. For one total of active users without the split by role, use
    /// `GET api/2.0/settings/docscloud/tenant/usage`. To read the configuration, the users counted, or the server
    /// address, use `GET api/2.0/settings/docscloud/tenant/config`, `GET api/2.0/settings/docscloud/tenant/quota`,
    /// or `GET api/2.0/settings/docscloud/tenant`. The call returns 500 if the Docs Connect service is not configured
    /// or fails.
    /// </remarks>
    /// <summary>
    /// Get the Docs Connect tenant information
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant/info</path>
    /// <param name="refresh">Pass `true` to skip the cached copy and request the license, server and usage information from Docs Connect again, replacing the cached one; with the default `false` the answer may be up to a minute old.</param>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The Docs Connect license and server information of the portal, with the user limits of the license and the usage statistics for the current period", typeof(DocsCloudTenantInfoDto))]
    [SwaggerResponse(400, "The portal has no activated Docs Connect tenant, so there is no license information to return")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [SwaggerResponse(500, "The Docs Connect service is not configured on this installation, is unreachable, or answered with an error other than not found")]
    [HttpGet("tenant/info")]
    public async Task<DocsCloudTenantInfoDto> GetTenantInfo(bool refresh = false)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var info = await docsCloudClient.GetTenantInfoAsync(await GetPortalIdAsync(), refresh);

        if (!info.License.Trial)
        {
            return info?.Map();
        }

        var tenant = tenantManager.GetCurrentTenant();

        var tariff = await tariffService.GetTariffAsync(tenant.Id);

        if (tariff.Quotas.Concat(tariff.OverdueQuotas ?? []).Any(q =>
                q.Id == (int)TenantWalletService.DocsCloud ||
                q.Id == (int)TenantWalletService.DocsCloudDevPack))
        {
            // paid tenants shouldn't show as trial
            info.License.Trial = false;
        }

        return info?.Map();
    }

    /// <remarks>
    /// Returns the configuration of the Docs Connect tenant of the current portal. It includes the tenant name, the
    /// security secret and header, the server file size limit, and anonymous access. It also includes the WOPI switch
    /// and the IP filter rules. The portal needs a Docs Connect tenant from `POST api/2.0/settings/docscloud/trial`
    /// or a Docs Connect purchase. Without one, the call returns 400, and `GET api/2.0/settings/docscloud/tenant`
    /// returns an empty result. Requires the EditPortalSettings permission, which the portal owner and DocSpace
    /// admins have. Other callers get 403. The call is read-only. The portal caches the answer for an hour. To read
    /// the current state from Docs Connect, pass `refresh=true`. An update with
    /// `PUT api/2.0/settings/docscloud/tenant/config` clears the cached copy, so the next read needs no refresh. The
    /// `security.secret` field is a credential, so treat the response as sensitive. An empty or missing
    /// `ipFilter.rules` means that no address is restricted. To read the license, the server address, the users
    /// counted, or the usage, use `GET api/2.0/settings/docscloud/tenant/info`, `GET api/2.0/settings/docscloud/tenant`,
    /// `GET api/2.0/settings/docscloud/tenant/quota`, or `GET api/2.0/settings/docscloud/tenant/usage`. The call
    /// returns 500 if the Docs Connect service is not configured or fails.
    /// </remarks>
    /// <summary>
    /// Get the Docs Connect tenant configuration
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant/config</path>
    /// <param name="refresh">Pass `true` to skip the cached copy and request the configuration from Docs Connect again, replacing the cached one; with the default `false` the answer may be up to an hour old.</param>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The configuration of the Docs Connect tenant of the portal, with its security, server, WOPI and IP filter settings", typeof(DocsCloudConfigDto))]
    [SwaggerResponse(400, "The portal has no activated Docs Connect tenant, so there is no configuration to return")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [SwaggerResponse(500, "The Docs Connect service is not configured on this installation, is unreachable, or answered with an error other than not found")]
    [HttpGet("tenant/config")]
    public async Task<DocsCloudConfigDto> GetTenantConfig(bool refresh = false)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return (await docsCloudClient.GetTenantConfigAsync(await GetPortalIdAsync(), refresh))?.Map();
    }

    /// <remarks>
    /// Replaces the configuration of the Docs Connect tenant of the current portal. It returns the configuration as
    /// Docs Connect stored it. The portal needs a Docs Connect tenant from `POST api/2.0/settings/docscloud/trial` or
    /// a Docs Connect purchase. Without one, the call returns 400, and `GET api/2.0/settings/docscloud/tenant`
    /// returns an empty result. First read the current values with `GET api/2.0/settings/docscloud/tenant/config`.
    /// Then send back whole sections: `security`, `server`, `wopi`, and `ipFilter`. Sections you leave out do not
    /// reach Docs Connect. In a section you send, an unset field becomes `0`, `false`, or empty. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. The call
    /// changes data, and you can repeat it with the same body. The next read returns the new values without
    /// `refresh=true`. The call changes only these settings, never the subscription, the user quota, or the license.
    /// It returns 400 if the body cannot be read, a text is too long, or `server.fileSizeLimit` is out of range. In
    /// that case, nothing reaches Docs Connect. The call returns 500 if the Docs Connect service is not configured,
    /// fails, or rejects the values.
    /// </remarks>
    /// <summary>
    /// Update the Docs Connect tenant configuration
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant/config</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The configuration of the Docs Connect tenant as Docs Connect stored it after the update", typeof(DocsCloudConfigDto))]
    [SwaggerResponse(400, "The request body cannot be read, a text field is longer than 255 characters, the file size limit is outside 0-209715200 bytes, or the portal has no activated Docs Connect tenant")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [SwaggerResponse(500, "The Docs Connect service is not configured on this installation, is unreachable, or answered with an error other than not found, including a rejection of the new values")]
    [HttpPut("tenant/config")]
    public async Task<DocsCloudConfigDto> UpdateTenantConfig(DocsCloudConfigRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return (await paymentHelper.UpdateTenantConfigAsync(await GetPortalIdAsync(), inDto.Map()))?.Map();
    }

    /// <remarks>
    /// Returns the Docs Connect user quota of the current portal. It lists the users who count as Docs Connect editors
    /// and as viewers, and when each entry expires. The portal needs a Docs Connect tenant from
    /// `POST api/2.0/settings/docscloud/trial` or a Docs Connect purchase. Without one, the call returns 400, and
    /// `GET api/2.0/settings/docscloud/tenant` returns an empty result. Requires the EditPortalSettings permission,
    /// which the portal owner and DocSpace admins have. Other callers get 403. The call is read-only, and the portal
    /// caches the answer for a minute. To read the current state from Docs Connect, pass `refresh=true`. The `users`
    /// list holds the editors, and `usersView` holds the viewers, in no particular order. For a portal member,
    /// `userId` is the DocSpace user ID. For anyone else, it is an ID of Docs Connect. The `expire` value is a UTC
    /// date and time as a string. Empty lists mean that no user is counted yet. For the license limits and the totals
    /// by role, use `GET api/2.0/settings/docscloud/tenant/info`. For one total of active users, use
    /// `GET api/2.0/settings/docscloud/tenant/usage`. To get the same lists as an "xlsx" file, use
    /// `POST api/2.0/settings/docscloud/tenant/quota/report`. The call returns 500 if the Docs Connect service is not
    /// configured or fails.
    /// </remarks>
    /// <summary>
    /// Get the Docs Connect tenant quota
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant/quota</path>
    /// <param name="refresh">Pass `true` to skip the cached copy and request the user quota from Docs Connect again, replacing the cached one; with the default `false` the answer may be up to a minute old.</param>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The editor and viewer users of the Docs Connect tenant of the portal, with the expiration date of each entry", typeof(DocsCloudQuotaDto))]
    [SwaggerResponse(400, "The portal has no activated Docs Connect tenant, so there is no user quota to return")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [SwaggerResponse(500, "The Docs Connect service is not configured on this installation, is unreachable, or answered with an error other than not found")]
    [HttpGet("tenant/quota")]
    public async Task<DocsCloudQuotaDto> GetTenantQuota(bool refresh = false)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return (await docsCloudClient.GetTenantQuotaAsync(await GetPortalIdAsync(), refresh))?.Map();
    }

    /// <remarks>
    /// Starts a background job that saves the Docs Connect user quota of the current portal as an "xlsx" file. The
    /// portal saves the file in the "My documents" folder of the caller. The report lists the editors and viewers with
    /// the type and expiration date of each. It also sums up the internal, external, and remaining users against the
    /// license limits. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have.
    /// Other callers get 403. The call does not check for a Docs Connect tenant. Without one, the job fails and puts
    /// the reason in `error`. The call queues work. Repeat `GET api/2.0/settings/docscloud/tenant/quota/report` until
    /// `isCompleted` is true. Then take the file from `resultFileId` or `resultFileUrl`. To cancel a running job, use
    /// `DELETE api/2.0/settings/docscloud/tenant/quota/report`. Each caller has one report job at a time. While it
    /// runs, a repeated call returns that job and starts no second one. So you can repeat the call. The response is
    /// the initial job state, with `percentage` 0, not the report. The report shows the quota at the moment of the
    /// build, and its file name has the build date. To read the same data as JSON, use
    /// `GET api/2.0/settings/docscloud/tenant/quota`.
    /// </remarks>
    /// <summary>
    /// Start the Docs Connect quota report
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant/quota/report</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The initial state of the queued report generation job, with zero progress and an uncompleted status", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpPost("tenant/quota/report")]
    public async Task<DocumentBuilderTaskDto> CreateTenantQuotaReport()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantId = tenantManager.GetCurrentTenant().Id;
        var userId = securityContext.CurrentAccount.ID;

        var task = serviceProvider.GetRequiredService<CustomerOperationsReportTask>();

        var baseUri = commonLinkUtility.ServerRootPath;

        task.Init(baseUri, tenantId, userId, null, DocumentBuilderTaskManager.GetTaskId(tenantId, userId, (int)ReportType.DocsCloudUserQuota));

        var taskProgress = await documentBuilderTaskManager.StartTask(task, false);

        var headers = MessageSettings.GetHttpHeaders(Request)?
            .ToDictionary(x => x.Key, x => x.Value.ToString()) ?? [];

        // The quota is a point-in-time snapshot; pass the current date so the report file name reflects today.
        var evt = new CustomerOperationsReportIntegrationEvent(
            userId,
            tenantId,
            baseUri,
            ReportType.DocsCloudUserQuota,
            startDate: DateTime.UtcNow,
            headers: headers);

        await eventBus.PublishAsync(evt);

        return DocumentBuilderTaskDto.Get(taskProgress);
    }

    /// <remarks>
    /// Returns the state of the Docs Connect user quota report that the caller started with
    /// `POST api/2.0/settings/docscloud/tenant/quota/report`. It shows only the caller's own job, never a report of
    /// another admin. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have.
    /// Other callers get 403. The call is read-only. This is the call to poll. Repeat it until `isCompleted` is true.
    /// The job also completes on failure, so then check `error`. It is empty if the report was built, and holds the
    /// failure message otherwise. The report file also stays in the "My documents" folder of the caller. An empty
    /// result means that the caller has no job. Either none was started, it was cancelled, or the portal deleted its
    /// state. The portal keeps the state of a finished job for a day, and a new report deletes it. This is a normal
    /// state, not an error. To cancel a report, use `DELETE api/2.0/settings/docscloud/tenant/quota/report`.
    /// </remarks>
    /// <summary>
    /// Get the Docs Connect quota report status
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant/quota/report</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The state of the Docs Connect quota report job of the caller, or an empty result if there is no such job", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("tenant/quota/report")]
    public async Task<DocumentBuilderTaskDto> GetTenantQuotaReport()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantId = tenantManager.GetCurrentTenant().Id;

        var task = await documentBuilderTaskManager.GetTask(tenantId, securityContext.CurrentAccount.ID, (int)ReportType.DocsCloudUserQuota);

        return DocumentBuilderTaskDto.Get(task);
    }

    /// <remarks>
    /// Cancels the Docs Connect user quota report of the caller and deletes its job. After this call, the caller can
    /// start a new report with `POST api/2.0/settings/docscloud/tenant/quota/report`. The call affects only the
    /// caller's own job, never a report of another admin. It succeeds even when the caller has no report job.
    /// Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers
    /// get 403. The call queues the cancellation, and you can repeat it. A 200 response means that the request is
    /// queued, not that the job has stopped. Repeat `GET api/2.0/settings/docscloud/tenant/quota/report` until it returns an
    /// empty result. The response has no body. A report file that is already saved in the "My documents" folder of
    /// the caller stays there. To delete it, use `DELETE api/2.0/files/file/{fileId}`.
    /// </remarks>
    /// <summary>
    /// Terminate the Docs Connect quota report
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant/quota/report</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The termination request has been queued for the report worker; the response has no body")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpDelete("tenant/quota/report")]
    public async Task TerminateTenantQuotaReport()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantId = tenantManager.GetCurrentTenant().Id;

        var evt = new CustomerOperationsReportIntegrationEvent(securityContext.CurrentAccount.ID, tenantId, null, ReportType.DocsCloudUserQuota, terminate: true);

        await eventBus.PublishAsync(evt);
    }

    /// <remarks>
    /// Returns the number of active Docs Connect users of the current portal in the current period. The `since` field
    /// is the start of that period, in UTC. The portal needs a Docs Connect tenant from
    /// `POST api/2.0/settings/docscloud/trial` or a Docs Connect purchase. Without one, the call returns 400, and
    /// `GET api/2.0/settings/docscloud/tenant` returns an empty result. Requires the EditPortalSettings permission,
    /// which the portal owner and DocSpace admins have. Other callers get 403. The call is read-only. The portal
    /// caches the answer for a minute. To read the current state from Docs Connect, pass `refresh=true`. The
    /// `activeCount` field is one total for the whole tenant, with no split by role and no limit. For the editor and
    /// viewer split with the license limits, use `GET api/2.0/settings/docscloud/tenant/info`. To list the counted
    /// users one by one, use `GET api/2.0/settings/docscloud/tenant/quota`. The call returns 500 if the Docs Connect
    /// service is not configured or fails.
    /// </remarks>
    /// <summary>
    /// Get the Docs Connect tenant usage
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant/usage</path>
    /// <param name="refresh">Pass `true` to skip the cached copy and request the usage statistics from Docs Connect again, replacing the cached one; with the default `false` the answer may be up to a minute old.</param>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The number of active Docs Connect users of the portal and the date the count starts from", typeof(DocsCloudUsageDto))]
    [SwaggerResponse(400, "The portal has no activated Docs Connect tenant, so there is no usage information to return")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [SwaggerResponse(500, "The Docs Connect service is not configured on this installation, is unreachable, or answered with an error other than not found")]
    [HttpGet("tenant/usage")]
    public async Task<DocsCloudUsageDto> GetTenantUsage(bool refresh = false)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return (await docsCloudClient.GetTenantUsageAsync(await GetPortalIdAsync(), refresh))?.Map();
    }

    private async Task ChangeCspSettingsAsync(DocsCloudTenant docsCloudTenant)
    {
        if (docsCloudTenant.IsDefault() || !Uri.IsWellFormedUriString(docsCloudTenant.Address, UriKind.Absolute))
        {
            return;
        }

        var settings = await cspSettingsHelper.LoadAsync();

        var currentDomains = settings.Domains?.ToList() ?? [];

        currentDomains.Add(docsCloudTenant.Address);

        _ = await cspSettingsHelper.SaveAsync(currentDomains.Distinct());
    }

    // DocsCloud identifies a portal by its Customer.UID, which maps to the core key of the current tenant.
    private async Task<string> GetPortalIdAsync()
    {
        var tenant = tenantManager.GetCurrentTenant();

        return await coreSettings.GetKeyAsync(tenant.Id);
    }

    private async Task<(TenantQuota FromQuota, TenantQuota ToQuota)> PrepareSwitchAsync(int quantity)
    {
        // Only the DocsCloud to DocsCloudDevPack transition is supported.
        const TenantWalletService from = TenantWalletService.DocsCloud;
        const TenantWalletService to = TenantWalletService.DocsCloudDevPack;

        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync(refresh: true);

        var tariff = await tariffService.GetTariffAsync(tenantId);
        if (tariff.State > TariffState.Paid)
        {
            throw new BillingException("Tariff is not paid");
        }

        var currentQuota = tariff.Quotas.FirstOrDefault(q => q.Id == (int)from);
        if (currentQuota == null)
        {
            throw new ArgumentException("DocsCloud subscription is not active");
        }

        var minValue = Math.Max(currentQuota.Quantity, configuration.GetValue<int?>("core:docscloud:minDevPackQuantity") ?? 10);
        if (quantity < minValue)
        {
            throw new ArgumentException($"Invalid quantity: must be greater than or equal to {minValue}");
        }

        if (tariff.Quotas.Any(q => q.Id == (int)to))
        {
            throw new ArgumentException("DocsCloudDevPack subscription is already set");
        }

        var quotaList = (await quotaService.GetTenantQuotasAsync()).Where(q => q.Wallet).ToList();

        var fromQuota = quotaList.FirstOrDefault(q => q.TenantId == (int)from);
        var toQuota = quotaList.FirstOrDefault(q => q.TenantId == (int)to);

        if (string.IsNullOrEmpty(fromQuota?.GetPaymentId()) || string.IsNullOrEmpty(toQuota?.GetPaymentId()))
        {
            throw new ItemNotFoundException("Service could not be found");
        }

        return (fromQuota, toQuota);
    }
}
