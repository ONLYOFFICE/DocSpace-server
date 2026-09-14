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
    /// Activates the free DocsCloud trial subscription for the current portal, and, once a DocsCloud server is
    /// assigned to the portal, allows the address of that server in the Content Security Policy settings.
    /// The portal tariff must be in the trial or paid state (not delayed and not unpaid), and the portal must not
    /// already hold a DocsCloud trial, DocsCloud or DocsCloudDevPack subscription: the quotas of the current
    /// tariff are listed by `GET api/2.0/portal/tariff`. The caller must be a portal administrator allowed to edit
    /// the portal settings, on an installation where the billing service is configured. The operation changes the
    /// portal subscription and is not idempotent: repeating it after a successful activation fails with 400.
    /// It returns `true` when the trial has been granted, and `false` when the billing service declines it
    /// (for example, when this portal has already used its trial), in which case nothing is changed. It never buys
    /// a paid plan: an existing paid DocsCloud subscription is moved to DocsCloudDevPack by
    /// `POST api/2.0/settings/docscloud/switchtodevpack` instead.
    /// </remarks>
    /// <summary>
    /// Start the DocsCloud trial
    /// </summary>
    /// <path>api/2.0/settings/docscloud/trial</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "Boolean value: true if the trial subscription is activated, false if the billing service declines it", typeof(bool))]
    [SwaggerResponse(400, "The portal already has a DocsCloud trial, DocsCloud or DocsCloudDevPack subscription")]
    [SwaggerResponse(402, "The portal tariff is delayed or not paid, so the trial cannot be started")]
    [SwaggerResponse(403, "The caller is not allowed to edit the portal settings, or the billing service is not configured")]
    [SwaggerResponse(404, "The DocsCloud trial quota is not available on this installation")]
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

        var result = await paymentHelper.GetDocsCloudTrialAsync(tenant.Id, docsCloudTrialQuota.Name);

        if (result)
        {
            var docsCloudTenant = await docsCloudClient.GetTenantAsync(await GetPortalIdAsync(), true);

            await ChangeCspSettingsAsync(docsCloudTenant);
        }

        return result;
    }

    /// <remarks>
    /// Upgrades the paid DocsCloud subscription of the current portal to DocsCloudDevPack for the requested
    /// number of users, charging the price difference to the portal wallet and moving the DocsCloud license
    /// to the new product. The portal must hold an active DocsCloud subscription, must not already hold a
    /// DocsCloudDevPack one, and its tariff must not be delayed or unpaid: the quotas and the state of the
    /// current tariff are listed by `GET api/2.0/portal/tariff`, and the amount that will be charged is
    /// returned by `POST api/2.0/settings/docscloud/calculatedevpack` for the same `quantity`. The caller
    /// must be a DocSpace administrator of a portal registered with the billing service. The switch is
    /// synchronous, mutating and not idempotent: repeating it after a successful call fails with 400, and
    /// concurrent calls for one portal are serialized so that the wallet is charged only once. It returns
    /// `true` when the subscription has been switched, and `false` when the billing service declines or
    /// fails to perform the switch, in which case nothing is charged and the portal stays on DocsCloud.
    /// Only the DocsCloud to DocsCloudDevPack direction is supported: to change the number of users of a
    /// subscription the portal already has, or to schedule a reversion from DocsCloudDevPack back to
    /// DocsCloud at the next billing period, use `PUT api/2.0/portal/payment/updatewallet` instead.
    /// </remarks>
    /// <summary>
    /// Switch DocsCloud to DocsCloudDevPack
    /// </summary>
    /// <path>api/2.0/settings/docscloud/switchtodevpack</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "Boolean value: true if the subscription is switched to DocsCloudDevPack, false if the billing service declines it", typeof(bool))]
    [SwaggerResponse(400, "The quantity is below the allowed minimum, the portal has no active DocsCloud subscription, or it already has a DocsCloudDevPack subscription")]
    [SwaggerResponse(402, "The portal tariff is delayed or not paid, so the subscription cannot be switched")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the billing service is not configured")]
    [SwaggerResponse(404, "The portal is not registered as a billing customer, or the DocsCloud and DocsCloudDevPack wallet products are not configured on this installation")]
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
    /// Prices the upgrade of the paid DocsCloud subscription of the current portal to DocsCloudDevPack for
    /// the requested number of users, without changing the subscription or charging anything. It applies the
    /// same preconditions as the switch itself: the portal must hold an active DocsCloud subscription, must
    /// not already hold a DocsCloudDevPack one, and its tariff must not be delayed or unpaid; the quotas and
    /// the state of the current tariff are listed by `GET api/2.0/portal/tariff`. The caller must be a
    /// DocSpace administrator of a portal registered with the billing service. The call is read-only and
    /// idempotent, so it can be repeated for different quantities before any switch is made. It returns the
    /// amount that switching would cost, the three-letter ISO 4217 currency of that amount, the quantity the
    /// amount was calculated for, and the identifier of the billing operation; an empty result means the
    /// billing service could not price the switch, which should then not be attempted. The switch itself is
    /// performed by `POST api/2.0/settings/docscloud/switchtodevpack` with the same `quantity` and takes no
    /// identifier from this response; to price a change in the number of users of a subscription the portal
    /// already has, use `PUT api/2.0/portal/payment/calculatewallet` instead.
    /// </remarks>
    /// <summary>
    /// Calculate the DocsCloudDevPack switch cost
    /// </summary>
    /// <path>api/2.0/settings/docscloud/calculatedevpack</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The cost of switching to DocsCloudDevPack for the requested quantity, or an empty result if the billing service could not price it", typeof(PaymentCalculation))]
    [SwaggerResponse(400, "The quantity is below the allowed minimum, the portal has no active DocsCloud subscription, or it already has a DocsCloudDevPack subscription")]
    [SwaggerResponse(402, "The portal tariff is delayed or not paid, so the switch cannot be priced")]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator, or the billing service is not configured")]
    [SwaggerResponse(404, "The portal is not registered as a billing customer, or the DocsCloud and DocsCloudDevPack wallet products are not configured on this installation")]
    [HttpPost("calculatedevpack")]
    public async Task<PaymentCalculation> CalculateDevPack(DocsCloudDevPackRequestDto inDto)
    {
        var (fromQuota, toQuota) = await PrepareSwitchAsync(inDto.Quantity);

        var tenant = tenantManager.GetCurrentTenant();

        return await tariffService.CalculateSwitchSubscriptionAsync(tenant.Id, fromQuota.GetPaymentId(), toQuota.GetPaymentId(), inDto.Quantity);
    }

    /// <remarks>
    /// Returns the DocsCloud tenant of the current portal: the DocsCloud server assigned to the portal, with its
    /// address, the date the tenant subscription ends and the payment the tenant was created for. A tenant exists
    /// only after a DocsCloud subscription has been granted, by `POST api/2.0/settings/docscloud/trial` or by a
    /// DocsCloud purchase, and only on an installation where the DocsCloud service is configured. The caller must
    /// be a portal administrator allowed to edit the portal settings. The call is read-only and idempotent, and it
    /// is served from a cache that keeps the tenant for an hour and the absence of a tenant for a minute, so pass
    /// `refresh=true` right after a subscription change to read the current state from DocsCloud instead. In the
    /// result, `address` is the absolute URL of the assigned server, `isActive` tells whether `endDate` is still in
    /// the future, and the dates are in UTC. An empty result means the portal has no DocsCloud tenant yet, which is
    /// the normal state before a subscription and not an error, so this is the operation to call to find out whether
    /// DocsCloud is activated at all. The license and server details, the editing settings, the user quota and the
    /// usage statistics are not part of it: they live in `GET api/2.0/settings/docscloud/tenant/info`,
    /// `.../tenant/config`, `.../tenant/quota` and `.../tenant/usage`, each of which fails with 400 while the
    /// portal has no activated tenant.
    /// </remarks>
    /// <summary>
    /// Get the DocsCloud tenant
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant</path>
    /// <param name="refresh">Pass `true` to skip the cached copy and request the tenant from DocsCloud again, replacing the cached one; with the default `false` the answer may be up to an hour old, or up to a minute old while the portal has no tenant.</param>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The DocsCloud tenant of the portal, or an empty result if no DocsCloud tenant is assigned to it", typeof(DocsCloudTenant))]
    [SwaggerResponse(403, "The caller is not allowed to edit the portal settings")]
    [HttpGet("tenant")]
    public async Task<DocsCloudTenant> GetTenant(bool refresh = false)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await docsCloudClient.GetTenantAsync(await GetPortalIdAsync(), refresh);
    }

    /// <remarks>
    /// Returns the DocsCloud license of the current portal, the DocsCloud server serving it, the user limits of
    /// that license and the editor and viewer usage counted against them for the current period. The portal must
    /// have an activated DocsCloud tenant, granted by `POST api/2.0/settings/docscloud/trial` or by a DocsCloud
    /// purchase: an empty result from `GET api/2.0/settings/docscloud/tenant` means there is none and this call
    /// fails with 400. The caller must be a portal administrator allowed to edit the portal settings, on an
    /// installation where the DocsCloud service is configured. The call is read-only, idempotent and cached for a
    /// minute, so pass `refresh=true` right after a subscription change to read the current state from DocsCloud.
    /// In the result, `license.valid` is when the license expires and `license.trial` is reported as `false` once
    /// the portal holds a paid DocsCloud or DocsCloudDevPack subscription, even when the license itself still says
    /// trial; `usersLimit` caps the editors and the viewers allowed, `stats` counts the active, internal, external
    /// and remaining users of each of those two kinds over the last `stats.periodDay` days, and the dates are in
    /// UTC. The editing settings, the per-user quota lists and the address of the assigned server live in
    /// `.../tenant/config`, `.../tenant/quota` and `.../tenant`, while `.../tenant/usage` gives one active-user
    /// total instead of this per-role breakdown.
    /// </remarks>
    /// <summary>
    /// Get the DocsCloud tenant information
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant/info</path>
    /// <param name="refresh">Pass `true` to skip the cached copy and request the license, server and usage information from DocsCloud again, replacing the cached one; with the default `false` the answer may be up to a minute old.</param>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The DocsCloud license and server information of the portal, with the user limits of the license and the usage statistics for the current period", typeof(DocsCloudTenantInfo))]
    [SwaggerResponse(400, "The portal has no activated DocsCloud tenant, so there is no license information to return")]
    [SwaggerResponse(403, "The caller is not allowed to edit the portal settings")]
    [HttpGet("tenant/info")]
    public async Task<DocsCloudTenantInfo> GetTenantInfo(bool refresh = false)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var info = await docsCloudClient.GetTenantInfoAsync(await GetPortalIdAsync(), refresh);

        if (!info.License.Trial)
        {
            return info;
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

        return info;
    }

    /// <remarks>
    /// Returns the configuration of the DocsCloud tenant of the current portal: its name, the security secret and
    /// header name, the file size limit and anonymous access switch of the server, the WOPI switch and the IP filter
    /// rules. The portal must have an activated DocsCloud tenant, granted by `POST api/2.0/settings/docscloud/trial`
    /// or by a DocsCloud purchase: an empty result from `GET api/2.0/settings/docscloud/tenant` means there is none
    /// and this call fails with 400. The caller must be a portal administrator allowed to edit the portal settings,
    /// on an installation where the DocsCloud service is configured. The call is read-only, idempotent and cached for
    /// an hour, so pass `refresh=true` to read the current state from DocsCloud; the same values are changed by
    /// `PUT api/2.0/settings/docscloud/tenant/config`, which drops the cached copy itself, so no refresh is needed
    /// after an update. In the result, `security.secret` is a credential, so the response should be treated as
    /// sensitive; `server.fileSizeLimit` is in bytes and an update cannot raise it above 209715200 (200 MB); and an
    /// empty or absent `ipFilter.rules` means no address restriction is configured. The license and server version,
    /// the address of the assigned server, the per-user quota and the usage counters are not part of it: they live in
    /// `.../tenant/info`, `.../tenant`, `.../tenant/quota` and `.../tenant/usage`.
    /// </remarks>
    /// <summary>
    /// Get the DocsCloud tenant configuration
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant/config</path>
    /// <param name="refresh">Pass `true` to skip the cached copy and request the configuration from DocsCloud again, replacing the cached one; with the default `false` the answer may be up to an hour old.</param>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The configuration of the DocsCloud tenant of the portal, with its security, server, WOPI and IP filter settings", typeof(DocsCloudConfig))]
    [SwaggerResponse(400, "The portal has no activated DocsCloud tenant, so there is no configuration to return")]
    [SwaggerResponse(403, "The caller is not allowed to edit the portal settings")]
    [HttpGet("tenant/config")]
    public async Task<DocsCloudConfig> GetTenantConfig(bool refresh = false)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await docsCloudClient.GetTenantConfigAsync(await GetPortalIdAsync(), refresh);
    }

    /// <remarks>
    /// Replaces the configuration of the DocsCloud tenant of the current portal: its name, the security secret and
    /// header name, the file size limit and anonymous access switch of the server, the WOPI switch and the IP filter
    /// rules; it returns the configuration as DocsCloud stored it. The portal must have an activated DocsCloud tenant,
    /// granted by `POST api/2.0/settings/docscloud/trial` or by a DocsCloud purchase: an empty result from
    /// `GET api/2.0/settings/docscloud/tenant` means there is none and this call fails with 400. Read the current
    /// values with `GET api/2.0/settings/docscloud/tenant/config` first and send back whole sections: the sections
    /// left out of the request are not sent to DocsCloud at all, while a section that is present is sent with all of
    /// its fields, so a field left unset inside it goes out as `0`, `false` or empty. The caller must be a portal
    /// administrator allowed to edit the portal settings, on an installation where the DocsCloud service is
    /// configured. The call is mutating,
    /// synchronous and idempotent, it is recorded in the portal audit trail, and it drops the cached configuration
    /// itself, so the next read returns the new values without `refresh=true`. The `tenantName`, `security.secret`,
    /// `security.header` and every `ipFilter.rules` address are capped at 255 characters and `server.fileSizeLimit`
    /// at 209715200 bytes (200 MB); a value outside those bounds is rejected with 400 before anything reaches
    /// DocsCloud. It changes these settings only, never the subscription, the user quota or the license.
    /// </remarks>
    /// <summary>
    /// Update the DocsCloud tenant configuration
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant/config</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The configuration of the DocsCloud tenant as DocsCloud stored it after the update", typeof(DocsCloudConfig))]
    [SwaggerResponse(400, "A text field is longer than 255 characters, the file size limit is outside 0-209715200 bytes, or the portal has no activated DocsCloud tenant")]
    [SwaggerResponse(403, "The caller is not allowed to edit the portal settings")]
    [HttpPut("tenant/config")]
    public async Task<DocsCloudConfig> UpdateTenantConfig(DocsCloudConfig inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await paymentHelper.UpdateTenantConfigAsync(await GetPortalIdAsync(), inDto);
    }

    /// <remarks>
    /// Returns the DocsCloud user quota of the current portal: the users who currently count as DocsCloud editors and
    /// the users who count as viewers, each with the identifier DocsCloud knows them by and the date their quota entry
    /// expires. The portal must have an activated DocsCloud tenant, granted by `POST api/2.0/settings/docscloud/trial`
    /// or by a DocsCloud purchase: an empty result from `GET api/2.0/settings/docscloud/tenant` means there is none
    /// and this call fails with 400. The caller must be a portal administrator allowed to edit the portal settings,
    /// on an installation where the DocsCloud service is configured. The call is read-only, idempotent and cached for
    /// a minute, so pass `refresh=true` to read the current state from DocsCloud. In the result, `users` holds the
    /// editor entries and `usersView` the viewer entries, both unordered; `userId` is the DocSpace user ID for a
    /// portal member and an identifier of DocsCloud's own for anyone else; `expire` is the date and time the entry
    /// expires, as a UTC string; and empty lists mean no user has been counted yet. It lists the users themselves,
    /// not the counters: the license limits with the per-role totals are in
    /// `GET api/2.0/settings/docscloud/tenant/info`, a single active-user total is in `.../tenant/usage`, and the
    /// same lists as a downloadable "xlsx" file are produced by
    /// `POST api/2.0/settings/docscloud/tenant/quota/report`.
    /// </remarks>
    /// <summary>
    /// Get the DocsCloud tenant quota
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant/quota</path>
    /// <param name="refresh">Pass `true` to skip the cached copy and request the user quota from DocsCloud again, replacing the cached one; with the default `false` the answer may be up to a minute old.</param>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The editor and viewer users of the DocsCloud tenant of the portal, with the expiration date of each entry", typeof(DocsCloudQuota))]
    [SwaggerResponse(400, "The portal has no activated DocsCloud tenant, so there is no user quota to return")]
    [SwaggerResponse(403, "The caller is not allowed to edit the portal settings")]
    [HttpGet("tenant/quota")]
    public async Task<DocsCloudQuota> GetTenantQuota(bool refresh = false)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await docsCloudClient.GetTenantQuotaAsync(await GetPortalIdAsync(), refresh);
    }

    /// <remarks>
    /// Queues a background job that renders the current DocsCloud user quota of the portal into an "xlsx" file and
    /// saves that file in the "My documents" folder of the calling user; the report lists the editor and the viewer
    /// users with the type and the expiration date of each, and summarizes the internal, external and remaining users
    /// against the license limits. The file is not ready when the response arrives: poll
    /// `GET api/2.0/settings/docscloud/tenant/quota/report` until `isCompleted` is true, then take the file from
    /// `resultFileId` or `resultFileUrl`, and use `DELETE api/2.0/settings/docscloud/tenant/quota/report` to cancel a
    /// job that is still running. The caller must be a portal administrator allowed to edit the portal settings. The
    /// portal should have an activated DocsCloud tenant: this call does not check that, and without a tenant the job
    /// itself fails and reports the reason in the `error` of the status response. One report per caller runs at a
    /// time: while a report of this user is still being built, the call describes that running job and no second
    /// generation is started, so a repeated call is safe. What comes back is the initial state of the job, with
    /// `percentage` 0 and a created `status`, not the report; the report is a point-in-time snapshot and carries the
    /// generation date in its file name. To read the same data as JSON, without building a file, use
    /// `GET api/2.0/settings/docscloud/tenant/quota`.
    /// </remarks>
    /// <summary>
    /// Start the DocsCloud quota report
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant/quota/report</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The initial state of the queued report generation job, with zero progress and an uncompleted status", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "The caller is not allowed to edit the portal settings")]
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
    /// Returns the state of the DocsCloud user quota report that the current user started with
    /// `POST api/2.0/settings/docscloud/tenant/quota/report`, so that the caller can follow the generation and pick
    /// up the resulting file. It reports the caller's own job only: a report started by another administrator is not
    /// visible here, and an empty result means this user has no job, because none was started, because it was
    /// terminated, or because a finished one has already been cleared (a job state is kept for a day, and starting a
    /// new report drops the previous finished one); that is a normal state and not an error. The caller must be a
    /// portal administrator allowed to edit the portal settings. The call is read-only and idempotent, and it is
    /// meant to be polled while the job runs. In the result, `percentage` goes from 0 to 100 and `isCompleted`
    /// becomes true both on success and on failure, so check `error`: it is empty when the report was built and
    /// carries the failure message otherwise;
    /// `resultFileId`, `resultFileName` and `resultFileUrl` are filled in only once the file exists, and that file
    /// also stays in the "My documents" folder of the caller. Use the `POST` operation on this path to start a report
    /// and the `DELETE` one to cancel it.
    /// </remarks>
    /// <summary>
    /// Get the DocsCloud quota report status
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant/quota/report</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The state of the DocsCloud quota report job of the caller, or an empty result if there is no such job", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "The caller is not allowed to edit the portal settings")]
    [HttpGet("tenant/quota/report")]
    public async Task<DocumentBuilderTaskDto> GetTenantQuotaReport()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantId = tenantManager.GetCurrentTenant().Id;

        var task = await documentBuilderTaskManager.GetTask(tenantId, securityContext.CurrentAccount.ID, (int)ReportType.DocsCloudUserQuota);

        return DocumentBuilderTaskDto.Get(task);
    }

    /// <remarks>
    /// Cancels the DocsCloud user quota report that the current user started with
    /// `POST api/2.0/settings/docscloud/tenant/quota/report` and removes its job, so that a new report can be started
    /// right away. There is no precondition: the call is accepted even when this user has no report job at all, and
    /// it affects the caller's own job only, never one started by another administrator. The caller must be a portal
    /// administrator allowed to edit the portal settings. The cancellation is asynchronous and idempotent: 200 means
    /// the request has been queued for the report worker, not that the job has already stopped, so poll
    /// `GET api/2.0/settings/docscloud/tenant/quota/report` until it returns an empty result. Nothing is returned in
    /// the body. A report file that has already been saved in the "My documents" folder of the caller is left there
    /// and has to be deleted through the file operations if it is no longer wanted.
    /// </remarks>
    /// <summary>
    /// Terminate the DocsCloud quota report
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant/quota/report</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The termination request has been queued for the report worker; the response has no body")]
    [SwaggerResponse(403, "The caller is not allowed to edit the portal settings")]
    [HttpDelete("tenant/quota/report")]
    public async Task TerminateTenantQuotaReport()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantId = tenantManager.GetCurrentTenant().Id;

        var evt = new CustomerOperationsReportIntegrationEvent(securityContext.CurrentAccount.ID, tenantId, null, ReportType.DocsCloudUserQuota, terminate: true);

        await eventBus.PublishAsync(evt);
    }

    /// <remarks>
    /// Returns the DocsCloud usage of the current portal: the number of users who have been active in DocsCloud in
    /// the current period, and the moment that period is counted from. The portal must have an activated DocsCloud
    /// tenant, granted by `POST api/2.0/settings/docscloud/trial` or by a DocsCloud purchase: an empty result from
    /// `GET api/2.0/settings/docscloud/tenant` means there is none and this call fails with 400. The caller must be a
    /// portal administrator allowed to edit the portal settings, on an installation where the DocsCloud service is
    /// configured. The call is read-only, idempotent and cached for a minute, so pass `refresh=true` to read the
    /// current state from DocsCloud. In the result, `activeCount` counts the users seen since `since`, which is in
    /// UTC, and it is one total for the whole tenant, with no split by role and no limit to compare it against. For
    /// the editor and viewer breakdown with the license limits use `GET api/2.0/settings/docscloud/tenant/info`, and
    /// for the users counted one by one `GET api/2.0/settings/docscloud/tenant/quota`.
    /// </remarks>
    /// <summary>
    /// Get the DocsCloud tenant usage
    /// </summary>
    /// <path>api/2.0/settings/docscloud/tenant/usage</path>
    /// <param name="refresh">Pass `true` to skip the cached copy and request the usage statistics from DocsCloud again, replacing the cached one; with the default `false` the answer may be up to a minute old.</param>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "The number of active DocsCloud users of the portal and the date the count starts from", typeof(DocsCloudUsage))]
    [SwaggerResponse(400, "The portal has no activated DocsCloud tenant, so there is no usage information to return")]
    [SwaggerResponse(403, "The caller is not allowed to edit the portal settings")]
    [HttpGet("tenant/usage")]
    public async Task<DocsCloudUsage> GetTenantUsage(bool refresh = false)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await docsCloudClient.GetTenantUsageAsync(await GetPortalIdAsync(), refresh);
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
