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
    /// Starts the DocsCloud trial.
    /// </remarks>
    /// <summary>
    /// Start the DocsCloud trial
    /// </summary>
    /// <path>api/2.0/settings/docscloud/trial</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(400, "Quota is already set")]
    [SwaggerResponse(402, "Tariff is not paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Quota could not be found")]
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
    /// Switches the current DocsCloud subscription to DocsCloudDevPack: charges the price difference
    /// from the wallet and transfers the subscription (with its license) to the target product.
    /// The quantity is taken from the currently purchased DocsCloud quota.
    /// Only the portal payer can perform this action.
    /// </remarks>
    /// <summary>
    /// Switch the DocsCloud subscription to DocsCloudDevPack
    /// </summary>
    /// <path>api/2.0/settings/docscloud/switchtodevpack</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(400, "Invalid request parameters")]
    [SwaggerResponse(402, "Tariff is not paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer or service could not be found")]
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
    /// Calculates the top-up cost of switching the current DocsCloud subscription to DocsCloudDevPack,
    /// without making any changes. The quantity is taken from the currently purchased DocsCloud quota.
    /// Only the portal payer can perform this action.
    /// </remarks>
    /// <summary>
    /// Calculate the DocsCloud subscription switch cost
    /// </summary>
    /// <path>api/2.0/settings/docscloud/calculatedevpack</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "Payment calculation", typeof(PaymentCalculation))]
    [SwaggerResponse(400, "Invalid request parameters")]
    [SwaggerResponse(402, "Tariff is not paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer or service could not be found")]
    [HttpPost("calculatedevpack")]
    public async Task<PaymentCalculation> CalculateDevPack(DocsCloudDevPackRequestDto inDto)
    {
        var (fromQuota, toQuota) = await PrepareSwitchAsync(inDto.Quantity);

        var tenant = tenantManager.GetCurrentTenant();

        return await tariffService.CalculateSwitchSubscriptionAsync(tenant.Id, fromQuota.GetPaymentId(), toQuota.GetPaymentId(), inDto.Quantity);
    }

    /// <remarks>
    /// Returns the DocsCloud tenant of the current portal.
    /// </remarks>
    /// <summary>Get the DocsCloud tenant</summary>
    /// <path>api/2.0/settings/docscloud/tenant</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "DocsCloud tenant", typeof(DocsCloudTenant))]
    [HttpGet("tenant")]
    public async Task<DocsCloudTenant> GetTenant(bool refresh = false)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await docsCloudClient.GetTenantAsync(await GetPortalIdAsync(), refresh);
    }

    /// <remarks>
    /// Returns the DocsCloud license and server information with usage statistics of the current portal.
    /// </remarks>
    /// <summary>Get the DocsCloud tenant information</summary>
    /// <path>api/2.0/settings/docscloud/tenant/info</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "DocsCloud tenant information", typeof(DocsCloudTenantInfo))]
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
    /// Returns the DocsCloud tenant configuration of the current portal.
    /// </remarks>
    /// <summary>Get the DocsCloud tenant configuration</summary>
    /// <path>api/2.0/settings/docscloud/tenant/config</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "DocsCloud tenant configuration", typeof(DocsCloudConfig))]
    [HttpGet("tenant/config")]
    public async Task<DocsCloudConfig> GetTenantConfig(bool refresh = false)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await docsCloudClient.GetTenantConfigAsync(await GetPortalIdAsync(), refresh);
    }

    /// <remarks>
    /// Updates the DocsCloud tenant configuration of the current portal with the parameters specified in the request.
    /// </remarks>
    /// <summary>Update the DocsCloud tenant configuration</summary>
    /// <path>api/2.0/settings/docscloud/tenant/config</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "Updated DocsCloud tenant configuration", typeof(DocsCloudConfig))]
    [HttpPut("tenant/config")]
    public async Task<DocsCloudConfig> UpdateTenantConfig(DocsCloudConfig inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await paymentHelper.UpdateTenantConfigAsync(await GetPortalIdAsync(), inDto);
    }

    /// <remarks>
    /// Returns the DocsCloud user quota (active users) of the current portal.
    /// </remarks>
    /// <summary>Get the DocsCloud tenant quota</summary>
    /// <path>api/2.0/settings/docscloud/tenant/quota</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "DocsCloud user quota", typeof(DocsCloudQuota))]
    [HttpGet("tenant/quota")]
    public async Task<DocsCloudQuota> GetTenantQuota(bool refresh = false)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await docsCloudClient.GetTenantQuotaAsync(await GetPortalIdAsync(), refresh);
    }

    /// <remarks>
    /// Starts generating the DocsCloud user quota report as an "xlsx" file and saves it in "My Documents".
    /// </remarks>
    /// <summary>Start the DocsCloud tenant quota report generation</summary>
    /// <path>api/2.0/settings/docscloud/tenant/quota/report</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "Operation execution status", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
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
    /// Returns the status of generating the DocsCloud user quota report.
    /// </remarks>
    /// <summary>Get the status of the DocsCloud tenant quota report generation</summary>
    /// <path>api/2.0/settings/docscloud/tenant/quota/report</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "Operation execution status", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("tenant/quota/report")]
    public async Task<DocumentBuilderTaskDto> GetTenantQuotaReport()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantId = tenantManager.GetCurrentTenant().Id;

        var task = await documentBuilderTaskManager.GetTask(tenantId, securityContext.CurrentAccount.ID, (int)ReportType.DocsCloudUserQuota);

        return DocumentBuilderTaskDto.Get(task);
    }

    /// <remarks>
    /// Terminates generating the DocsCloud user quota report.
    /// </remarks>
    /// <summary>Terminate the DocsCloud tenant quota report generation</summary>
    /// <path>api/2.0/settings/docscloud/tenant/quota/report</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpDelete("tenant/quota/report")]
    public async Task TerminateTenantQuotaReport()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenantId = tenantManager.GetCurrentTenant().Id;

        var evt = new CustomerOperationsReportIntegrationEvent(securityContext.CurrentAccount.ID, tenantId, null, ReportType.DocsCloudUserQuota, terminate: true);

        await eventBus.PublishAsync(evt);
    }

    /// <remarks>
    /// Returns the DocsCloud usage statistics of the current portal.
    /// </remarks>
    /// <summary>Get the DocsCloud tenant usage</summary>
    /// <path>api/2.0/settings/docscloud/tenant/usage</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "DocsCloud tenant usage statistics", typeof(DocsCloudUsage))]
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
