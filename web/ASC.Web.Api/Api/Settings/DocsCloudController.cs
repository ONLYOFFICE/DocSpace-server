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
    SecurityContext securityContext,
    TenantManager tenantManager,
    CoreSettings coreSettings,
    DocsCloudClient docsCloudClient,
    ITariffService tariffService,
    IQuotaService quotaService,
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
    public async Task<bool> UpdateWalletPayment()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        var tenant = tenantManager.GetCurrentTenant();

        var quota = (await quotaService.GetTenantQuotasAsync())
            .FirstOrDefault(q => q.Name == "docscloudtrial");

        if (quota == null)
        {
            throw new ItemNotFoundException("Quota could not be found");
        }

        var tariff = await tariffService.GetTariffAsync(tenant.Id);

        if (tariff.State > TariffState.Paid)
        {
            throw new BillingException("Tariff is not paid");
        }

        if (tariff.Quotas.Any(q => q.Id == quota.TenantId))
        {
            throw new ArgumentException("Quota is already set");
        }

        var quantity = new Dictionary<string, int> { { quota.Name, 1 } };
        var defaultCurrency = tariffService.GetSupportedAccountingCurrencies().First();
        var participant = securityContext.CurrentAccount.ID.ToString();

        var result = await tariffService.PaymentChangeAsync(tenant.Id, quantity, ProductQuantityType.Add, defaultCurrency, false, participant);

        if (result)
        {
            messageService.Send(MessageAction.CustomerSubscriptionUpdated, $"{quota.Name}");
        }

        return result;
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
    public async Task<DocsCloudTenant> GetTenant()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await docsCloudClient.GetTenantAsync(await GetPortalIdAsync());
    }

    /// <remarks>
    /// Returns the DocsCloud license and server information with usage statistics of the current portal.
    /// </remarks>
    /// <summary>Get the DocsCloud tenant information</summary>
    /// <path>api/2.0/settings/docscloud/tenant/info</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "DocsCloud tenant information", typeof(DocsCloudTenantInfo))]
    [HttpGet("tenant/info")]
    public async Task<DocsCloudTenantInfo> GetTenantInfo()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await docsCloudClient.GetTenantInfoAsync(await GetPortalIdAsync());
    }

    /// <remarks>
    /// Returns the DocsCloud tenant configuration of the current portal.
    /// </remarks>
    /// <summary>Get the DocsCloud tenant configuration</summary>
    /// <path>api/2.0/settings/docscloud/tenant/config</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "DocsCloud tenant configuration", typeof(DocsCloudConfigDto))]
    [HttpGet("tenant/config")]
    public async Task<DocsCloudConfigDto> GetTenantConfig()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await docsCloudClient.GetTenantConfigAsync(await GetPortalIdAsync());
    }

    /// <remarks>
    /// Updates the DocsCloud tenant configuration of the current portal with the parameters specified in the request.
    /// </remarks>
    /// <summary>Update the DocsCloud tenant configuration</summary>
    /// <path>api/2.0/settings/docscloud/tenant/config</path>
    [Tags("Settings / DocsCloud")]
    [SwaggerResponse(200, "Updated DocsCloud tenant configuration", typeof(DocsCloudConfigDto))]
    [HttpPut("tenant/config")]
    public async Task<DocsCloudConfigDto> UpdateTenantConfig(DocsCloudConfigDto inDto)
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
    public async Task<DocsCloudQuota> GetTenantQuota()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await docsCloudClient.GetTenantQuotaAsync(await GetPortalIdAsync());
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
    public async Task<DocsCloudUsage> GetTenantUsage()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await docsCloudClient.GetTenantUsageAsync(await GetPortalIdAsync());
    }

    // DocsCloud identifies a portal by its Customer.UID, which maps to the core key of the current tenant.
    private async Task<string> GetPortalIdAsync()
    {
        var tenant = tenantManager.GetCurrentTenant();

        return await coreSettings.GetKeyAsync(tenant.Id);
    }
}
