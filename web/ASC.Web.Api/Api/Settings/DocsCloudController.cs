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

namespace ASC.Web.Api.Controllers.Settings;

[DefaultRoute("docscloud")]
public class DocsCloudController(
    PermissionContext permissionContext,
    TenantManager tenantManager,
    CoreSettings coreSettings,
    DocsCloudClient docsCloudClient,
    ITariffService tariffService,
    IQuotaService quotaService,
    UserManager userManager,
    SecurityContext securityContext,
    MessageService messageService,
    WebItemManager webItemManager,
    IFusionCache fusionCache)
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

        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

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

        var result = await tariffService.GetDocsCloudTrialAsync(tenant.Id);

        if (result)
        {
            messageService.Send(MessageAction.CustomerSubscriptionUpdated, $"{docsCloudTrialQuota.Name}");
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
    public async Task<bool> SwitchToDevPack()
    {
        var (fromQuota, toQuota, quantity) = await PrepareSwitchAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var result = await tariffService.SwitchSubscriptionAsync(tenant.Id, fromQuota.GetPaymentId(), toQuota.GetPaymentId(), quantity, securityContext.CurrentAccount.ID.ToString());

        if (result)
        {
            messageService.Send(MessageAction.CustomerSubscriptionUpdated, $"{toQuota.Name} {quantity}");
        }

        return result;
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
    public async Task<PaymentCalculation> CalculateDevPack()
    {
        var (fromQuota, toQuota, quantity) = await PrepareSwitchAsync();

        var tenant = tenantManager.GetCurrentTenant();

        return await tariffService.CalculateSwitchSubscriptionAsync(tenant.Id, fromQuota.GetPaymentId(), toQuota.GetPaymentId(), quantity);
    }

    /// <remarks>
    /// Checks whether the DocsCloud server is reachable.
    /// </remarks>
    /// <summary>Check the DocsCloud server health</summary>
    /// <path>api/2.0/settings/docscloud/healthcheck</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "DocsCloud server is reachable")]
    [HttpGet("healthcheck")]
    public async Task CheckHealth()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await docsCloudClient.CheckHealthAsync();
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

        try
        {
            return await docsCloudClient.GetTenantAsync(await GetPortalIdAsync(), refresh);
        }
        catch (DocsCloudNotFoundException)
        {
            return null;
        }
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
            // for testing purposes
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

        var result = await docsCloudClient.UpdateTenantConfigAsync(await GetPortalIdAsync(), inDto);

        messageService.Send(MessageAction.DocsCloudConfigUpdated);

        return result;
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
    /// Downloads the DocsCloud user quota of the current portal as a CSV file.
    /// </remarks>
    /// <summary>Download the DocsCloud tenant quota</summary>
    /// <path>api/2.0/settings/docscloud/tenant/quota/download</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "DocsCloud user quota CSV file", typeof(FileResult))]
    [HttpGet("tenant/quota/download")]
    public async Task<FileResult> DownloadTenantQuota()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var stream = await docsCloudClient.DownloadTenantQuotaAsync(await GetPortalIdAsync());

        return File(stream, "text/csv", "quota.csv");
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

    // DocsCloud identifies a portal by its Customer.UID, which maps to the core key of the current tenant.
    private async Task<string> GetPortalIdAsync()
    {
        var tenant = tenantManager.GetCurrentTenant();

        return await coreSettings.GetKeyAsync(tenant.Id);
    }

    private async Task<(TenantQuota FromQuota, TenantQuota ToQuota, int Quantity)> PrepareSwitchAsync()
    {
        // Only the DocsCloud to DocsCloudDevPack transition is supported.
        const TenantWalletService from = TenantWalletService.DocsCloud;
        const TenantWalletService to = TenantWalletService.DocsCloudDevPack;

        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            throw new ItemNotFoundException("Customer could not be found");
        }

        await DemandPayerAsync(customerInfo);

        var tariff = await tariffService.GetTariffAsync(tenant.Id);

        if (tariff.State > TariffState.Paid)
        {
            throw new BillingException("Tariff is not paid");
        }

        var currentQuota = tariff.Quotas.FirstOrDefault(q => q.Id == (int)from);
        if (currentQuota == null)
        {
            throw new ArgumentException("DocsCloud subscription is not active");
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

        return (fromQuota, toQuota, currentQuota.Quantity);
    }

    private async Task DemandPayerAsync(CustomerInfo customerInfo)
    {
        var payer = await userManager.GetUserByEmailAsync(customerInfo?.Email);

        if (securityContext.CurrentAccount.ID != payer.Id)
        {
            throw new SecurityException("Access denied: insufficient permissions for this payment operation");
        }
    }
}
