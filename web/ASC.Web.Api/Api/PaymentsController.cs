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

using ASC.Core.Common.AI;
using ASC.Files.Core.ApiModels.ResponseDto;
using ASC.Files.Core.IntegrationEvents.Events;
using ASC.Files.Core.Services.DocumentBuilderService;

using Microsoft.AspNetCore.RateLimiting;

namespace ASC.Web.Api.Controllers;

///<remarks>
/// Portal information access.
///</remarks>
///<name>portal</name>
[Scope]
[DefaultRoute("payment")]
[ApiController]
[AllowNotPayment]
[ControllerName("portal")]
public class PaymentController(
    CoreSettings coreSettings,
    UserManager userManager,
    TenantManager tenantManager,
    SettingsManager settingsManager,
    ITariffService tariffService,
    IQuotaService quotaService,
    SecurityContext securityContext,
    RegionHelper regionHelper,
    QuotaHelper tariffHelper,
    IFusionCache fusionCache,
    MessageService messageService,
    StudioNotifyService studioNotifyService,
    PermissionContext permissionContext,
    TenantUtil tenantUtil,
    AiGateway aiGateway,
    ApiDateTimeHelper apiDateTimeHelper,
    EmployeeDtoHelper employeeWrapperHelper,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    IEventBus eventBus,
    CommonLinkUtility commonLinkUtility,
    DocumentBuilderTaskManager<CustomerOperationsReportTask, int, CustomerOperationsReportTaskData> documentBuilderTaskManager,
    IServiceProvider serviceProvider,
    WalletStaticProvider walletStaticProvider,
    QuotaSocketManager quotaSocketManager)
    : ControllerBase
{
    private readonly int _maxCount = 10;
    private readonly int _expirationMinutes = 2;

    /// <remarks>
    /// Returns the URL to the payment page.
    /// </remarks>
    /// <summary>
    /// Get the payment page URL
    /// </summary>
    /// <path>api/2.0/portal/payment/url</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The URL to the payment page", typeof(Uri))]
    [SwaggerResponse(400, "Invalid request parameters")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("url")]
    public async Task<Uri> GetPaymentUrl(PaymentUrlRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        await DemandAdminAsync();

        ArgumentNullException.ThrowIfNull(inDto?.Quantity);

        if (inDto.Quantity.Any(item => item.Value <= 0))
        {
            throw new ArgumentException("Invalid quantity");
        }

        var tenant = tenantManager.GetCurrentTenant();
        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo != null)
        {
            var tariff = await tariffService.GetTariffAsync(tenant.Id);
            if (tariff.State == TariffState.Paid)
            {
                return null;
            }
        }

        var monthQuotas = (await quotaService.GetTenantQuotasAsync())
            .Where(q => !string.IsNullOrEmpty(q.ProductId) && q.Visible && !q.Wallet && !q.Year)
            .ToList();

        // TODO: Temporary restriction.
        // Possibility to buy only one product per transaction.
        // Only monthly tariff available for purchase.
        if (inDto.Quantity.Count != 1 || monthQuotas.All(q => q.Name != inDto.Quantity.First().Key))
        {
            throw new ArgumentException();
        }

        var currency = await regionHelper.GetCurrencyFromRequestAsync();

        return await tariffService.GetShoppingUriAsync(
            tenant.Id,
            tenant.AffiliateId,
            tenant.PartnerId,
            currency,
            CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            (await userManager.GetUsersAsync(securityContext.CurrentAccount.ID)).Email,
            inDto.Quantity,
            inDto.BackUrl,
            inDto.SuccessUrl);
    }

    /// <remarks>
    /// Updates the payment quantity with the parameters specified in the request.
    /// </remarks>
    /// <summary>
    /// Update the payment quantity
    /// </summary>
    /// <path>api/2.0/portal/payment/update</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(400, "Invalid request parameters")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer could not be found")]
    [HttpPut("update")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<bool> UpdatePayment(QuantityRequestDto inDto)
    {
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

        // TODO: Temporary restriction.
        // Possibility to buy only one product per transaction.
        // For the current paid tariff only quota change is available.
        if (inDto.Quantity.Count != 1)
        {
            throw new ArgumentException();
        }

        var product = inDto.Quantity.First();
        var productName = product.Key;
        var productQty = product.Value;
        var quota = (await quotaService.GetTenantQuotasAsync())
            .FirstOrDefault(q => !string.IsNullOrEmpty(q.ProductId) && q.Name == productName);

        if (quota == null || quota.Wallet)
        {
            throw new ArgumentException("Invalid product");
        }

        var currentQuota = await tenantManager.GetTenantQuotaAsync(tenant.Id);

        if (currentQuota.Price > 0 && currentQuota.Name != productName)
        {
            throw new ArgumentException("Invalid product");
        }

        var tariff = await tariffService.GetTariffAsync(tenant.Id);

        if (tariff.Quotas.Any(q => q.Id == quota.TenantId && q.Quantity == productQty))
        {
            throw new ArgumentException("Invalid quantity");
        }

        var currency = await regionHelper.GetCurrencyFromRequestAsync();

        var result = await tariffService.PaymentChangeAsync(tenant.Id, inDto.Quantity, ProductQuantityType.Set, currency, true, securityContext.CurrentAccount.ID.ToString());

        if (result)
        {
            messageService.Send(MessageAction.CustomerSubscriptionUpdated, $"{productName} {productQty}");
        }

        return result;
    }

    /// <remarks>
    /// Updates the wallet payment quantity with the parameters specified in the request.
    /// </remarks>
    /// <summary>
    /// Update the wallet payment quantity
    /// </summary>
    /// <path>api/2.0/portal/payment/updatewallet</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(400, "Invalid request parameters")]
    [SwaggerResponse(402, "Tariff is not paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer could not be found")]
    [HttpPut("updatewallet")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<bool> UpdateWalletPayment(WalletQuantityRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        if (inDto.ProductQuantityType is ProductQuantityType.Renew or ProductQuantityType.Sub)
        {
            throw new ArgumentException("Invalid product quantity type");
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            throw new ItemNotFoundException("Customer could not be found");
        }

        await DemandPayerAsync(customerInfo);

        // TODO: Temporary restriction.
        // Possibility to buy only one product per transaction.
        // Wallet tariffs are always available for purchase.
        if (inDto.Quantity.Count != 1)
        {
            throw new ArgumentException();
        }

        var product = inDto.Quantity.First();
        var productName = product.Key;
        var productQty = product.Value;
        var quota = (await quotaService.GetTenantQuotasAsync())
            .FirstOrDefault(q => !string.IsNullOrEmpty(q.ProductId) && q.Name == productName);

        if (quota is not { Wallet: true })
        {
            throw new ArgumentException("Invalid product");
        }

        var tariff = await tariffService.GetTariffAsync(tenant.Id);

        if (tariff.State > TariffState.Paid)
        {
            throw new BillingException("Tariff is not paid");
        }

        if (quota.TenantId == (int)TenantWalletService.DocsCloud &&
            tariff.Quotas.Any(q => q.Id == (int)TenantWalletService.DocsCloudDevPack))
        {
            throw new ArgumentException("Quota is already set");
        }

        if (quota.TenantId == (int)TenantWalletService.DocsCloudDevPack &&
            tariff.Quotas.Any(q => q.Id == (int)TenantWalletService.DocsCloud))
        {
            throw new ArgumentException("Quota is already set");
        }

        var minValue = quota.TenantId switch
        {
            (int)TenantWalletService.Storage => 100,
            (int)TenantWalletService.DocsCloudDevPack => 10,
            _ => 1
        };

        if (inDto.ProductQuantityType is ProductQuantityType.Set)
        {
            if (productQty.HasValue && productQty.Value != 0 && productQty.Value < minValue)
            {
                throw new ArgumentException("Invalid quantity");
            }

            // saving null value is equivalent to resetting to default
            var updated = await tariffService.UpdateNextQuantityAsync(tenant.Id, tariff, quota.TenantId, productQty);

            if (updated)
            {
                messageService.Send(MessageAction.CustomerSubscriptionUpdated, $"{productName} {productQty}");
            }

            return updated;
        }

        // inDto.ProductQuantityType === ProductQuantityType.Add

        if (productQty is null or <= 0)
        {
            throw new ArgumentException("Invalid quantity");
        }

        if (quota.TenantId == (int)TenantWalletService.Admin)
        {
            minValue = (await userManager.GetUsersByGroupAsync(ASC.Core.Users.Constants.GroupRoomAdmin.ID)).Length;
        }

        var hasActiveWalletQuota = tariff.Quotas.Any(q => q.Id == quota.TenantId && q.State == QuotaState.Active);
        if (!hasActiveWalletQuota && productQty < minValue)
        {
            throw new ArgumentException("Invalid quantity");
        }

        var balance = await tariffService.GetCustomerBalanceAsync(tenant.Id);
        if (balance == null)
        {
            throw new ItemNotFoundException("Balance could not be found");
        }

        // TODO: support other currencies
        var defaultCurrency = tariffService.GetSupportedAccountingCurrencies().First();
        var subAccount = balance.SubAccounts.FirstOrDefault(x => x.Currency == defaultCurrency);
        if (subAccount == null)
        {
            throw new ItemNotFoundException("Subaccount could not be found");
        }

        var quantity = new Dictionary<string, int> { { productName, productQty.Value } };

        var result = await tariffService.PaymentChangeAsync(tenant.Id, quantity, inDto.ProductQuantityType, defaultCurrency, false, securityContext.CurrentAccount.ID.ToString());

        if (result)
        {
            messageService.Send(MessageAction.CustomerSubscriptionUpdated, $"{productName} {productQty}");
        }

        return result;
    }

    /// <remarks>
    /// Calculates an amount of the wallet payment with the parameters specified in the request.
    /// </remarks>
    /// <summary>
    /// Calculate the wallet payment amount
    /// </summary>
    /// <path>api/2.0/portal/payment/calculatewallet</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Payment calculation", typeof(PaymentCalculation))]
    [SwaggerResponse(400, "Invalid request parameters")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer could not be found")]
    [HttpPut("calculatewallet")]
    public async Task<PaymentCalculation> CalculateWalletPayment(WalletQuantityRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        if (inDto.ProductQuantityType is not ProductQuantityType.Add)
        {
            throw new ArgumentException("Invalid product quantity type");
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            throw new ItemNotFoundException("Customer could not be found");
        }

        await DemandPayerAsync(customerInfo);

        // TODO: Temporary restriction.
        // Possibility to buy only one product per transaction.
        // Wallet tariffs are always available for purchase.
        if (inDto.Quantity.Count != 1)
        {
            throw new ArgumentException();
        }

        var product = inDto.Quantity.First();
        var productName = product.Key;
        var productQty = product.Value;
        var quota = (await quotaService.GetTenantQuotasAsync())
            .FirstOrDefault(q => !string.IsNullOrEmpty(q.ProductId) && q.Name == productName);

        if (quota is not { Wallet: true })
        {
            throw new ArgumentException("Invalid product");
        }

        if (productQty is null or <= 0)
        {
            throw new ArgumentException("Invalid quantity");
        }

        var balance = await tariffService.GetCustomerBalanceAsync(tenant.Id);
        if (balance == null)
        {
            throw new ItemNotFoundException("Balance could not be found");
        }

        // TODO: support other currencies
        var defaultCurrency = tariffService.GetSupportedAccountingCurrencies().First();
        var subAccount = balance.SubAccounts.FirstOrDefault(x => x.Currency == defaultCurrency);
        if (subAccount == null)
        {
            throw new ItemNotFoundException("Subaccount could not be found");
        }

        var quantity = new Dictionary<string, int> { { productName, productQty.Value } };

        var result = await tariffService.PaymentCalculateAsync(tenant.Id, quantity, inDto.ProductQuantityType, defaultCurrency);

        return result;
    }

    /// <remarks>
    /// Returns the URL to the payment account.
    /// </remarks>
    /// <summary>
    /// Get the payment account
    /// </summary>
    /// <path>api/2.0/portal/payment/account</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The URL to the payment account", typeof(string))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("account")]
    public async Task<string> GetPaymentAccount(PaymentAccountRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        await DemandPayerOrOwnerAsync(tenant, customerInfo);

        var result = "payment.ashx";
        return !string.IsNullOrEmpty(inDto.BackUrl) ? $"{result}?backUrl={inDto.BackUrl}" : result;
    }

    /// <remarks>
    /// Returns the available portal prices.
    /// </remarks>
    /// <summary>
    /// Get prices
    /// </summary>
    /// <path>api/2.0/portal/payment/prices</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "List of available portal prices", typeof(Dictionary<string, decimal>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("prices")]
    public async Task<Dictionary<string, decimal>> GetPortalPrices()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var currency = await regionHelper.GetCurrencyFromRequestAsync();
        var result = (await tenantManager.GetProductPriceInfoAsync())
            .ToDictionary(pr => pr.Key, pr => pr.Value.GetValueOrDefault(currency, 0));
        return result;
    }


    /// <remarks>
    /// Returns the available portal currencies.
    /// </remarks>
    /// <summary>
    /// Get currencies
    /// </summary>
    /// <path>api/2.0/portal/payment/currencies</path>
    /// <collection>list</collection>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "List of available portal currencies", typeof(IAsyncEnumerable<CurrenciesDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("currencies")]
    public async IAsyncEnumerable<CurrenciesDto> GetPaymentCurrencies()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var defaultRegion = regionHelper.GetDefaultRegionInfo();
        var currentRegion = await regionHelper.GetCurrentRegionInfoAsync();

        yield return new CurrenciesDto(defaultRegion);

        if (!currentRegion.Name.Equals(defaultRegion.Name))
        {
            yield return new CurrenciesDto(currentRegion);
        }
    }

    /// <remarks>
    /// Returns the available portal quotas.
    /// </remarks>
    /// <summary>
    /// Get quotas
    /// </summary>
    /// <path>api/2.0/portal/payment/quotas</path>
    /// <collection>list</collection>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "List of available portal quotas", typeof(IEnumerable<QuotaDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("quotas")]
    public async Task<IEnumerable<QuotaDto>> GetPaymentQuotas(QuotasRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!inDto.Wallet)
        {
            var currentQuota = await tariffHelper.GetCurrentQuotaAsync(false, false);
            if (currentQuota.NonProfit)
            {
                return [currentQuota];
            }
        }

        return await tariffHelper.GetQuotasAsync(false, inDto.Wallet).ToListAsync();
    }

    /// <remarks>
    /// Returns the available wallet services.
    /// </remarks>
    /// <summary>
    /// Get wallet services
    /// </summary>
    /// <path>api/2.0/portal/payment/walletservices</path>
    /// <collection>list</collection>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "List of available wallet services", typeof(IEnumerable<WalletServiceDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("walletservices")]
    public async Task<IEnumerable<WalletServiceDto>> GetWalletServices()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await tariffHelper.GetWalletServicesAsync();
    }

    /// <remarks>
    /// Returns the specified wallet service.
    /// </remarks>
    /// <summary>
    /// Get wallet service
    /// </summary>
    /// <path>api/2.0/portal/payment/walletservice</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Wallet service", typeof(WalletServiceDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Service could not be found")]
    [HttpGet("walletservice")]
    public async Task<WalletServiceDto> GetWalletService(GetWalletServiceRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var quotaList = await quotaService.GetTenantQuotasAsync();
        var quota = quotaList.FirstOrDefault(q => q.Wallet && q.TenantId == (int)inDto.Service);
        if (quota == null)
        {
            throw new ItemNotFoundException("Service could not be found");
        }

        var quotaDto = await tariffHelper.ToQuotaDtoAsync(quota, false);
        var walletServiceDto = quotaDto.MapToWalletServiceDto();
        walletServiceDto.ServiceName = quota.ServiceName;
        return walletServiceDto;
    }

    /// <remarks>
    /// Returns the payment information about the current portal quota.
    /// </remarks>
    /// <summary>
    /// Get quota payment information
    /// </summary>
    /// <path>api/2.0/portal/payment/quota</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Payment information about the current portal quota", typeof(QuotaDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("quota")]
    public async Task<QuotaDto> GetQuotaPaymentInformation(PaymentInformationRequestDto inDto)
    {
        if (await userManager.IsGuestAsync(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }

        return await tariffHelper.GetCurrentQuotaAsync(inDto.Refresh);
    }

    /// <remarks>
    /// Sends a request for the portal payment.
    /// </remarks>
    /// <summary>
    /// Send a payment request
    /// </summary>
    /// <path>api/2.0/portal/payment/request</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(400, "Incorrect email or message text is empty")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(429, "Request limit is exceeded")]
    [HttpPost("request")]
    public async Task SendPaymentRequest(SalesRequestsDto inDto)
    {
        await DemandAdminAsync();

        if (!inDto.Email.TestEmailRegex())
        {
            throw new ArgumentException(Resource.ErrorNotCorrectEmail);
        }

        if (string.IsNullOrEmpty(inDto.UserName))
        {
            throw new ArgumentException(Resource.ErrorIncorrectUserName);
        }

        if (string.IsNullOrEmpty(inDto.Message))
        {
            throw new ArgumentException(Resource.ErrorEmptyMessage);
        }

        await CheckCache("salesrequest");

        await studioNotifyService.SendMsgToSalesAsync(inDto.Email, inDto.UserName, inDto.Message);
        messageService.Send(MessageAction.ContactSalesMailSent);
    }


    /// <remarks>
    /// Returns the URL to the checkout setup page.
    /// </remarks>
    /// <summary>
    /// Get the checkout setup page URL
    /// </summary>
    /// <path>api/2.0/portal/payment/checkoutsetupurl</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The URL to the checkout setup page", typeof(Uri))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("checkoutsetupurl")]
    public async Task<Uri> GetCheckoutSetupUrl(CheckoutSetupUrlRequestsDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        await DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo != null)
        {
            var currentQuota = await tariffHelper.GetCurrentQuotaAsync(false, false);
            if (!currentQuota.NonProfit || !string.IsNullOrEmpty(customerInfo.Email))
            {
                await DemandPayerAsync(customerInfo);
            }

            if (customerInfo.PaymentMethodStatus == PaymentMethodStatus.Set)
            {
                return null;
            }
        }

        var user = await userManager.GetUsersAsync(securityContext.CurrentAccount.ID);
        var currency = await regionHelper.GetCurrencyFromRequestAsync();

        return await tariffService.GetShoppingUriAsync(
            tenant.Id,
            tenant.AffiliateId,
            tenant.PartnerId,
            currency,
            CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            user.Email,
            [],
            inDto.BackUrl,
            inDto.SuccessUrl,
            true);
    }

    /// <remarks>
    /// Returns the customer information.
    /// </remarks>
    /// <summary>
    /// Get the customer information
    /// </summary>
    /// <path>api/2.0/portal/payment/customerinfo</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The customer info", typeof(CustomerInfoDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("customerinfo")]
    public async Task<CustomerInfoDto> GetCustomerInfo(PaymentInformationRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        await DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id, inDto.Refresh);

        if (customerInfo == null)
        {
            return null;
        }

        var payerUserInfo = await userManager.GetUserByEmailAsync(customerInfo.Email);

        var payerDto = payerUserInfo.Id == ASC.Core.Users.Constants.LostUser.Id
                ? null
                : await employeeWrapperHelper.GetAsync(payerUserInfo);

        var result = new CustomerInfoDto(customerInfo, payerDto);

        return result;
    }

    /// <remarks>
    /// Returns the result of putting money on deposit.
    /// </remarks>
    /// <summary>
    /// Put money on deposit
    /// </summary>
    /// <path>api/2.0/portal/payment/deposit</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(400, "Invalid request parameters")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer could not be found")]
    [HttpPost("deposit")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<bool> TopUpDeposit(TopUpDepositRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        var supportedCurrencies = tariffService.GetSupportedAccountingCurrencies();
        if (!supportedCurrencies.Contains(inDto.Currency))
        {
            throw new ArgumentException("Unsupported currency");
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            throw new ItemNotFoundException("Customer could not be found");
        }

        if (customerInfo.PaymentMethodStatus != PaymentMethodStatus.Set)
        {
            throw new InvalidOperationException("Customer payment method is not set");
        }

        await DemandPayerAsync(customerInfo);

        var siteName = tenant.GetTenantDomain(coreSettings);

        var result = await tariffService.TopUpDepositAsync(tenant.Id, inDto.Amount, inDto.Currency, securityContext.CurrentAccount.ID.ToString(), siteName, null, true);

        if (result)
        {
            var description = $"{inDto.Amount} {inDto.Currency}";
            messageService.Send(MessageAction.CustomerWalletToppedUp, description);

            await quotaSocketManager.TopUpWallet(false);
        }

        return result;
    }

    /// <remarks>
    /// Returns the customer balance from the accounting service.
    /// </remarks>
    /// <summary>
    /// Get the customer balance
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/balance</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The customer balance", typeof(Balance))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("customer/balance")]
    public async Task<Balance> GetCustomerBalance(PaymentInformationRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        await DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        var result = await tariffService.GetCustomerBalanceAsync(tenant.Id, inDto.Refresh);
        return result;
    }

    /// <remarks>
    /// Returns the AI quota balance of a customer from the accounting service.
    /// </remarks>
    /// <summary>
    /// Get the customer AI balance
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/aibalance</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The customer AI balance", typeof(Balance))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("customer/aibalance")]
    public async Task<Balance> GetCustomerAiBalance(PaymentInformationRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        await DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        var result = await tariffService.GetCustomerAiBalanceAsync(tenant.Id, inDto.Refresh);
        return result;
    }

    /// <remarks>
    /// Returns the report of customer operations from the accounting service.
    /// </remarks>
    /// <summary>
    /// Get the customer operations
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/operations</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The customer operations", typeof(ReportDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Service could not be found")]
    [HttpGet("customer/operations")]
    public async Task<ReportDto> GetCustomerOperations([FromQuery]CustomerOperationsRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        await DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(inDto.ServiceName))
        {
            await CheckWalletServiceName(inDto.ServiceName);
        }

        var utcStartDate = tenantUtil.DateTimeToUtc(inDto.StartDate ?? tenant.CreationDateTime);
        var utcEndDate = tenantUtil.DateTimeToUtc(inDto.EndDate ?? DateTime.UtcNow);

        var filter = new OperationFilter
        {
            ServiceName = inDto.ServiceName,
            UtcStartDate = utcStartDate,
            UtcEndDate = utcEndDate,
            ParticipantName = inDto.ParticipantName,
            Credit = inDto.Credit,
            Debit = inDto.Debit,
            Offset = inDto.Offset,
            Limit = inDto.Limit,
            Type = inDto.Type,
            Status = inDto.Status,
            OrderBy = inDto.OrderBy,
            OrderType = inDto.OrderType
        };

        var report = await tariffService.GetCustomerOperationsAsync(tenant.Id, filter);
        if (report == null)
        {
            return null;
        }

        var participantDisplayNames = await report.GetParticipantDisplayNamesAsync(displayUserSettingsHelper, true);

        return new ReportDto(report, apiDateTimeHelper, participantDisplayNames, filter.ServiceName);
    }

    /// <remarks>
    /// Returns the customer spending aggregated per calendar month from the accounting service.
    /// </remarks>
    /// <summary>
    /// Get the customer monthly usage
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/usage/monthly</path>
    /// <collection>list</collection>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The customer monthly usage", typeof(IEnumerable<CustomerMonthlyUsageDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("customer/usage/monthly")]
    public async Task<List<CustomerMonthlyUsageDto>> GetCustomerMonthlyUsage([FromQuery] CustomerMonthlyUsageRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        await DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        var filter = new MonthlyUsageFilter
        {
            UtcStartDate = tenantUtil.DateTimeToUtc(inDto.StartDate ?? tenant.CreationDateTime),
            UtcEndDate = tenantUtil.DateTimeToUtc(inDto.EndDate ?? DateTime.UtcNow)
        };

        var usage = await tariffService.GetCustomerMonthlyUsageAsync(tenant.Id, filter);

        return usage?.Select(u => new CustomerMonthlyUsageDto(u)).ToList();
    }

    /// <remarks>
    /// Returns the customer usage statistics aggregated per service from the accounting service.
    /// </remarks>
    /// <summary>
    /// Get the customer service usage
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/usage</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The customer service usage", typeof(CustomerServiceUsageReportDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Service could not be found")]
    [HttpGet("customer/usage")]
    public async Task<CustomerServiceUsageReportDto> GetCustomerServiceUsage([FromQuery] CustomerServiceUsageRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        await DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(inDto.ServiceName))
        {
            await CheckWalletServiceName(inDto.ServiceName);
        }

        var utcStartDate = tenantUtil.DateTimeToUtc(inDto.StartDate ?? tenant.CreationDateTime);
        var utcEndDate = tenantUtil.DateTimeToUtc(inDto.EndDate ?? DateTime.UtcNow);

        var filter = new UsageFilter
        {
            ServiceName = inDto.ServiceName,
            ParticipantName = inDto.ParticipantName,
            Status = inDto.Status,
            UtcStartDate = utcStartDate,
            UtcEndDate = utcEndDate,
            Metadata = inDto.Metadata,
            Offset = inDto.Offset,
            Limit = inDto.Limit,
            OrderBy = inDto.OrderBy,
            OrderType = inDto.OrderType
        };

        var report = await tariffService.GetCustomerServiceUsageAsync(tenant.Id, filter);

        return report == null ? null : new CustomerServiceUsageReportDto(report);
    }

    /// <remarks>
    /// Starts generating a customer operations report as an "xlsx" file and saves it in Documents.
    /// </remarks>
    /// <summary>
    /// Start the customer operations report generation
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/operationsreport</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Operation execution status", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer or service could not be found")]
    [HttpPost("customer/operationsreport")]
    public async Task<DocumentBuilderTaskDto> CreateCustomerOperationsReport(CustomerOperationsReportRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        await DemandAdminAsync();

        var tenantId = tenantManager.GetCurrentTenantId();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenantId);
        if (customerInfo == null)
        {
            throw new ItemNotFoundException("Customer could not be found");
        }

        inDto ??= new CustomerOperationsReportRequestDto();

        if (!string.IsNullOrEmpty(inDto.ServiceName))
        {
            await CheckWalletServiceName(inDto.ServiceName);
        }

        var userId = securityContext.CurrentAccount.ID;

        var task = serviceProvider.GetService<CustomerOperationsReportTask>();

        var baseUri = commonLinkUtility.ServerRootPath;

        task.Init(baseUri, tenantId, userId, null);

        var taskProgress = await documentBuilderTaskManager.StartTask(task, false);

        var headers = MessageSettings.GetHttpHeaders(Request)?
            .ToDictionary(x => x.Key, x => x.Value.ToString()) ?? [];

        var evt = new CustomerOperationsReportIntegrationEvent(
            userId,
            tenantId,
            baseUri,
            inDto.ServiceName,
            inDto.StartDate,
            inDto.EndDate,
            inDto.ParticipantName,
            inDto.Credit,
            inDto.Debit,
            inDto.Type,
            inDto.Status,
            inDto.OrderBy,
            inDto.OrderType,
            headers);

        await eventBus.PublishAsync(evt);

        return DocumentBuilderTaskDto.Get(taskProgress);
    }

    /// <remarks>
    /// Returns the status of generating a customer operations report.
    /// </remarks>
    /// <summary>Get the status of the customer operations report generation</summary>
    /// <path>api/2.0/portal/payment/customer/operationsreport</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Operation execution status", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer could not be found")]
    [HttpGet("customer/operationsreport")]
    public async Task<DocumentBuilderTaskDto> GetCustomerOperationsReport()
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        await DemandAdminAsync();

        var tenantId = tenantManager.GetCurrentTenantId();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenantId);
        if (customerInfo == null)
        {
            throw new ItemNotFoundException("Customer could not be found");
        }

        var task = await documentBuilderTaskManager.GetTask(tenantId, securityContext.CurrentAccount.ID);

        return DocumentBuilderTaskDto.Get(task);
    }

    /// <remarks>
    /// Terminates generating a customer operations report.
    /// </remarks>
    /// <summary>Terminate the customer operations report generation</summary>
    /// <path>api/2.0/portal/payment/customer/operationsreport</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer could not be found")]
    [HttpDelete("customer/operationsreport")]
    public async Task TerminateCustomerOperationsReport()
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        await DemandAdminAsync();

        var tenantId = tenantManager.GetCurrentTenantId();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenantId);
        if (customerInfo == null)
        {
            throw new ItemNotFoundException("Customer could not be found");
        }

        var evt = new CustomerOperationsReportIntegrationEvent(securityContext.CurrentAccount.ID, tenantId, null, null, terminate: true);

        await eventBus.PublishAsync(evt);
    }

    /// <summary>
    /// Get currencies from the accounting service
    /// </summary>
    /// <remarks>
    /// Returns the list of available currencies from the accounting service.
    /// </remarks>
    /// <path>api/2.0/portal/payment/accounting/currencies</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The list of currencies", typeof(List<Currency>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("accounting/currencies")]
    public async Task<List<Currency>> GetAccountingCurrencies()
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        await DemandAdminAsync();

        var supportedCurrencies = tariffService.GetSupportedAccountingCurrencies();

        var allCurrencies = await tariffService.GetAllAccountingCurrenciesAsync();

        return allCurrencies.Where(x => supportedCurrencies.Contains(x.Code)).ToList();
    }

    /// <summary>
    /// Gets the tenant wallet auto top up settings
    /// </summary>
    /// <remarks>
    /// Returns the wallet auto top up settings for the current tenant.
    /// </remarks>
    /// <path>api/2.0/portal/payment/topupsettings</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The wallet auto top up settings", typeof(TenantWalletSettings))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("topupsettings")]
    public async Task<TenantWalletSettings> GetTenantWalletSettings()
    {
        await DemandAdminAsync();

        var result = await settingsManager.LoadAsync<TenantWalletSettings>();
        return result;
    }

    /// <summary>
    /// Set the wallet auto top up settings
    /// </summary>
    /// <remarks>
    /// Updates the wallet auto top up settings for the current tenant.
    /// Requires the tariff service to be configured and the user to be authorized as a payer.
    /// Returns null if the tariff service is not configured or customer information/balance cannot be retrieved.
    /// </remarks>
    /// <path>api/2.0/portal/payment/topupsettings</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The wallet auto top up settings", typeof(TenantWalletSettings))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer could not be found")]
    [HttpPost("topupsettings")]
    public async Task<TenantWalletSettings> SetTenantWalletSettings(TenantWalletSettingsWrapper inDto)
    {
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

        var balance = await tariffService.GetCustomerBalanceAsync(tenant.Id);
        if (balance == null)
        {
            throw new ItemNotFoundException("Balance could not be found");
        }

        await DemandPayerAsync(customerInfo);

        var settings = inDto?.Settings ?? new TenantWalletSettings();

        var result = await settingsManager.SaveAsync(settings);

        messageService.Send(MessageAction.CustomerWalletTopUpSettingsUpdated);

        return settings;
    }


    /// <summary>
    /// Gets the wallet service settings for the tenant.
    /// </summary>
    /// <remarks>
    /// Retrieves configuration settings related to the wallet service associated with the current tenant.
    /// </remarks>
    /// <path>api/2.0/portal/payment/servicessettings</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The wallet service settings for the tenant", typeof(TenantWalletServiceSettings))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("servicessettings")]
    public async Task<TenantWalletServiceSettings> GetTenantWalletServiceSettings()
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        await DemandAdminAsync();

        var settings = await settingsManager.LoadAsync<TenantWalletServiceSettings>();

        return settings;
    }

    /// <summary>
    /// Change tenant wallet service state
    /// </summary>
    /// <remarks>
    /// Changes the state of a wallet service for the current tenant.
    /// Requires permission to edit portal settings and a configured tariff service.
    /// Adds or removes the specified service from the enabled services list based on the enabled flag.
    /// </remarks>
    /// <path>api/2.0/portal/payment/servicestate</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The updated tenant wallet service settings", typeof(TenantWalletServiceSettings))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer could not be found")]
    [HttpPost("servicestate")]
    public async Task<TenantWalletServiceSettings> ChangeTenantWalletServiceState(ChangeWalletServiceStateRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            throw new InvalidOperationException("Tariff service is not configured");
        }

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            throw new ItemNotFoundException("Customer could not be found");
        }

        await DemandPayerAsync(customerInfo);

        var settings = await settingsManager.LoadAsync<TenantWalletServiceSettings>();

        settings.EnabledServices ??= [];

        if (inDto.Enabled && !settings.EnabledServices.Contains(inDto.Service))
        {
            settings.EnabledServices.Add(inDto.Service);
        }

        if (!inDto.Enabled && settings.EnabledServices.Contains(inDto.Service))
        {
            settings.EnabledServices.Remove(inDto.Service);
        }

        if (settings.EnabledServices.Count == 0)
        {
            settings.EnabledServices = null;
        }

        var result = await settingsManager.SaveAsync(settings);

        messageService.Send(MessageAction.CustomerWalletServicesSettingsUpdated);

        if (inDto.Service == TenantWalletService.AITools)
        {
            await quotaSocketManager.ChangeAiConfigAsync();
        }

        return settings;
    }

    /// <summary>
    /// Credit AI balance
    /// </summary>
    /// <remarks>
    /// Credits AI quota to the customer AI sub-account from their main balance.
    /// Requires the customer to have a configured payment method.
    /// </remarks>
    /// <path>api/2.0/portal/payment/creditaibalance</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The AI credit operation result", typeof(ServicePayment))]
    [SwaggerResponse(400, "Unsupported currency or insufficient balance")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer or AiTools quota could not be found")]
    [HttpPost("creditaibalance")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<ServicePayment> CreditAiBalance(CreditAiBalanceRequestDto inDto)
    {
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

        var balance = await tariffService.GetCustomerBalanceAsync(tenant.Id);
        if (balance == null)
        {
            throw new ItemNotFoundException("Balance could not be found");
        }

        var supportedCurrencies = tariffService.GetSupportedAccountingCurrencies();

        if (string.IsNullOrEmpty(inDto.Currency))
        {
            inDto.Currency = supportedCurrencies.FirstOrDefault();
        }

        if (!supportedCurrencies.Contains(inDto.Currency))
        {
            throw new ArgumentException("Unsupported currency");
        }

        var subAccount = balance.SubAccounts.FirstOrDefault(x => x.Currency == inDto.Currency);
        if (subAccount == null)
        {
            throw new ItemNotFoundException("Subaccount could not be found");
        }

        if (subAccount.Amount < inDto.Amount)
        {
            throw new ArgumentException("Insufficient balance");
        }

        // The method must throw an exception if the AiTools quota is hidden or not found in the database!
        var quotaList = await tenantManager.GetTenantQuotasAsync(false, true);
        var aiToolsQuota = quotaList.FirstOrDefault(x => x.TenantId == (int)TenantWalletService.AITools);
        if (aiToolsQuota == null)
        {
            throw new ItemNotFoundException("AiTools quota not found");
        }

        var customerParticipantName = securityContext.CurrentAccount.ID.ToString();
        var result = await tariffService.MakeAiCreditAsync(tenant.Id, inDto.Amount, inDto.Currency, customerParticipantName, metadata: null);
        if (result != null)
        {
            var details = $"{aiToolsQuota.ServiceName} {inDto.Amount} {inDto.Currency}";
            messageService.Send(MessageAction.CustomerOperationPerformed, null, details);
            await ChangeTenantWalletServiceState(new ChangeWalletServiceStateRequestDto
            {
                Service = TenantWalletService.AITools,
                Enabled = true
            });
        }

        return result;
    }

    /// <summary>
    /// Get AI model prices
    /// </summary>
    /// <remarks>
    /// Retrieves the pricing information for AI models including chat, embedding, and web search services.
    /// The prices are returned in the configured currency and normalized per million tokens.
    /// Requires administrator permissions to access.
    /// </remarks>
    /// <path>api/2.0/portal/payment/ai-prices</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Prices for AI models", typeof(AiPricesResponse))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("ai-prices")]
    public async Task<AiPricesDto> GetAiPrices()
    {
        DemandAiGatewayConfiguration();

        await DemandAdminAsync();

        var aiPrices = await aiGateway.GetPricesAsync();
        var icons = new Dictionary<string, string>();

        var providers = aiPrices.Chat.Select(m => m.OwnedBy.ToLower()).Distinct();
        var searchTypes = aiPrices.Search.Select(s => s.Id).Distinct();

        foreach (var provider in providers)
        {
            icons[provider] = await walletStaticProvider.GetImageAsync(provider);
        }

        foreach (var searchType in searchTypes)
        {
            icons[searchType] = await walletStaticProvider.GetImageAsync(searchType);
        }

        var chat = aiPrices.Chat.Select(m => new AiEntryPricingDto<AiChatPriceDto>
        {
            Id = m.Id,
            Image = icons[m.OwnedBy.ToLower()],
            Alias = m.Alias,
            Provider = m.Provider,
            Price = new AiChatPriceDto { Prompt = m.Price.Prompt, Completion = m.Price.Completion },
            Link = m.Link
        }).ToList();

        var embeddingImage = await walletStaticProvider.GetImageAsync("embedding");

        var embedding = aiPrices.Embedding.Select(e => new AiEntryPricingDto<AiEmbeddingPriceDto>
        {
            Id = e.Id,
            Alias = e.Alias,
            Provider = e.Provider,
            Image = embeddingImage,
            Price = new AiEmbeddingPriceDto { Prompt = e.Price.Prompt },
            Link = e.Link
        }).ToList();

        var search = aiPrices.Search.Select(s => new AiEntryPricingDto<decimal>
        {
            Id = s.Id,
            Alias = Resource.ResourceManager.GetString($"AccountingCustomerOperationServiceDesc_{s.Id}"),
            Image = icons[s.Id],
            Provider = s.Provider,
            Price = s.Price,
            Link = s.Link
        }).ToList();

        return new AiPricesDto
        {
            Chat = chat,
            Embedding = embedding,
            WebSearch = search,
            Currency = aiPrices.Currency
        };
    }

    /// <summary>
    /// Get restricted AI models
    /// </summary>
    /// <remarks>
    /// Returns the list of AI chat model IDs that are restricted (disabled) for the current tenant.
    /// Restricted models cannot be used for AI chat conversations by any user within the portal.
    /// Only DocSpace administrators can access this endpoint.
    /// </remarks>
    /// <path>api/2.0/portal/payment/ai-model/restrictions</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The list of restricted AI model IDs", typeof(RestrictedModelsResponse))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("ai-model/restrictions")]
    public async Task<RestrictedModelsResponse> GetRestrictedAiModels()
    {
        DemandAiGatewayConfiguration();

        await DemandAdminAsync();

        return await aiGateway.GetRestrictedModelsAsync();
    }

    /// <summary>
    /// Set restricted AI models
    /// </summary>
    /// <remarks>
    /// Overwrites the entire set of restricted AI model IDs for the current tenant.
    /// The request body must contain the complete desired set — to add a restriction, include the new model alongside existing ones;
    /// to remove one, omit it. An empty set lifts all restrictions. Only the portal payer can perform this action.
    /// </remarks>
    /// <path>api/2.0/portal/payment/ai-model/restrictions</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The updated list of restricted AI model IDs", typeof(RestrictedModelsResponse))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer could not be found")]
    [HttpPut("ai-model/restrictions")]
    public async Task<RestrictedModelsResponse> SetRestrictedAiModels(SetRestrictedAiModelsRequestDto inDto)
    {
        DemandAiGatewayConfiguration();

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            throw new ItemNotFoundException("Customer could not be found");
        }

        await DemandPayerAsync(customerInfo);

        var result = await aiGateway.SetRestrictedModelsAsync(inDto.Models);

        messageService.Send(MessageAction.CustomerWalletServicesSettingsUpdated);

        return result;
    }

    private async Task DemandAdminAsync()
    {
        if (!await userManager.IsDocSpaceAdminAsync(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }
    }

    private async Task DemandPayerAsync(CustomerInfo customerInfo)
    {
        var payer = await userManager.GetUserByEmailAsync(customerInfo?.Email);

        if (securityContext.CurrentAccount.ID != payer.Id)
        {
            throw new SecurityException($"payerEmail {customerInfo?.Email}, payerId {payer.Id}, currentId {securityContext.CurrentAccount.ID}");
        }
    }

    private async Task DemandPayerOrOwnerAsync(Tenant tenant, CustomerInfo customerInfo)
    {
        if (securityContext.CurrentAccount.ID != tenant.OwnerId)
        {
            var payer = await userManager.GetUserByEmailAsync(customerInfo?.Email);

            if (securityContext.CurrentAccount.ID != payer.Id)
            {
                throw new SecurityException($"payerEmail {customerInfo?.Email}, payerId {payer.Id}, ownerId {tenant.OwnerId}, currentId {securityContext.CurrentAccount.ID}");
            }
        }
    }

    private void DemandAiGatewayConfiguration()
    {
        if (!tariffService.IsConfigured() || !aiGateway.Configured)
        {
            throw new InvalidOperationException("Tariff service or AI gateway is not configured");
        }
    }

    private async Task CheckCache(string baseKey)
    {
        var key = HttpContext.Connection.RemoteIpAddress + baseKey;
        var countFromCache = await fusionCache.TryGetAsync<int>(key);
        var count = countFromCache.HasValue ? countFromCache.Value : 0;
        if (count > _maxCount)
        {
            throw new Exception(Resource.ErrorRequestLimitExceeded);
        }

        await fusionCache.SetAsync(key, count + 1, TimeSpan.FromMinutes(_expirationMinutes));
    }

    /// <summary>
    /// Validates the service name and returns the corresponding tenant wallet service
    /// </summary>
    /// <remarks>
    /// Checks if the provided service name matches any tenant quota service name and verifies that the corresponding tenant ID is a valid TenantWalletService enum value.
    /// </remarks>
    /// <param name="serviceName">The service name to validate</param>
    /// <return>The corresponding TenantWalletService enum value</return>
    /// <exception cref="ItemNotFoundException">Thrown when the quota with the corresponding service name is hidden or not found in the database.</exception>
    private async Task<TenantWalletService> CheckWalletServiceName(string serviceName)
    {
        var quotaList = await tenantManager.GetTenantQuotasAsync(false, true);

        var selectedQuota = quotaList.FirstOrDefault(x =>
            x.ServiceName.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));

        if (selectedQuota != null && Enum.IsDefined(typeof(TenantWalletService), selectedQuota.TenantId))
        {
            return (TenantWalletService)selectedQuota.TenantId;
        }

        throw new ItemNotFoundException("Service could not be found");
    }
}
