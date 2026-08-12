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
    IConfiguration configuration,
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
    PaymentHelper paymentHelper)
    : ControllerBase
{
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
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        ArgumentNullException.ThrowIfNull(inDto);

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

        // Only monthly tariff available for purchase.
        if (monthQuotas.All(q => q.Name != inDto.Quantity.First().Key))
        {
            throw new ArgumentException("Only monthly product can be purchased per transaction");
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
        var tenant = tenantManager.GetCurrentTenant();

        await paymentHelper.DemandCustomerPayerAsync(tenant.Id);

        var product = inDto.Quantity.First();
        var productName = product.Key;
        var productQty = product.Value;
        var quota = await paymentHelper.GetQuotaByProductNameAsync(productName, wallet: false);

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

        return await paymentHelper.PaymentChangeAsync(tenant.Id, inDto.Quantity, ProductQuantityType.Set, currency, true, securityContext.CurrentAccount.ID.ToString());
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
    [SwaggerResponse(402, "Payment required")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer could not be found")]
    [HttpPut("updatewallet")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<bool> UpdateWalletPayment(WalletQuantityRequestDto inDto)
    {
        paymentHelper.DemandConfigured();

        if (inDto.ProductQuantityType is ProductQuantityType.Renew or ProductQuantityType.Sub)
        {
            throw new ArgumentException("Invalid product quantity type");
        }

        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync(refresh: true);

        var product = inDto.Quantity.First();
        var productName = product.Key;
        var productQty = product.Value;
        var quota = await paymentHelper.GetQuotaByProductNameAsync(productName, wallet: true);

        var tariff = await tariffService.GetTariffAsync(tenantId);

        if (tariff.State > TariffState.Paid && quota.Additional)
        {
            throw new BillingException("Tariff is not paid");
        }

        var minValue = quota.TenantId switch
        {
            (int)TenantWalletService.Storage => configuration.GetValue<int?>("core:accounting:minStorageQuantity") ?? 100,
            (int)TenantWalletService.DocsCloudDevPack => configuration.GetValue<int?>("core:docscloud:minDevPackQuantity") ?? 10,
            _ => 1
        };

        // requesting DocsCloudDevPack while DocsCloud is active is never valid here, in either flow -
        // that upgrade goes through DocsCloudController.SwitchToDevPack instead. Only the reverse
        // direction (DocsCloud while DevPack is active) is allowed here, and only to schedule a
        // reversion via the Set branch below
        if (quota.TenantId == (int)TenantWalletService.DocsCloudDevPack &&
            tariff.Quotas.Any(q => q.Id == (int)TenantWalletService.DocsCloud))
        {
            throw new ArgumentException("Quota is already set");
        }

        if (inDto.ProductQuantityType is ProductQuantityType.Set)
        {
            if (productQty.HasValue && productQty.Value != 0 && productQty.Value < minValue)
            {
                throw new ArgumentException("Invalid quantity");
            }

            // requesting the DocsCloud product while DocsCloudDevPack is active schedules a reversion to
            // DocsCloud at the next period, rather than an immediate switch
            var targetQuota = quota.TenantId;
            int? nextQuota = null;
            if (targetQuota == (int)TenantWalletService.DocsCloud &&
                tariff.Quotas.Any(q => q.Id == (int)TenantWalletService.DocsCloudDevPack))
            {
                targetQuota = (int)TenantWalletService.DocsCloudDevPack;
                nextQuota = (int)TenantWalletService.DocsCloud;

                // a scheduled switch is a real purchase of a new product, so unlike a plain quantity
                // change there's no "reset to default" for 0/null - it would just be silently dropped
                // at renewal by RenewSubscriptionAsync's NextQuantity <= 0 guard
                if (productQty is null or <= 0)
                {
                    throw new ArgumentException("Invalid quantity");
                }
            }

            // saving null value is equivalent to resetting to default
            return await paymentHelper.UpdateNextQuantityAsync(tenantId, tariff, targetQuota, productQty, productName, nextQuota);
        }

        // inDto.ProductQuantityType === ProductQuantityType.Add

        if (quota.TenantId == (int)TenantWalletService.DocsCloud &&
            tariff.Quotas.Any(q => q.Id == (int)TenantWalletService.DocsCloudDevPack))
        {
            throw new ArgumentException("Quota is already set");
        }

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

        // TODO: support other currencies
        var defaultCurrency = tariffService.GetSupportedAccountingCurrencies().First();

        await paymentHelper.GetSubAccountRequiredAsync(tenantId, defaultCurrency, refresh: true);

        var quantity = new Dictionary<string, int> { { productName, productQty.Value } };

        return await paymentHelper.PaymentChangeAsync(tenantId, quantity, inDto.ProductQuantityType, defaultCurrency, false, securityContext.CurrentAccount.ID.ToString(), true);
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
        paymentHelper.DemandConfigured();

        if (inDto.ProductQuantityType is not ProductQuantityType.Add)
        {
            throw new ArgumentException("Invalid product quantity type");
        }

        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var product = inDto.Quantity.First();
        var productName = product.Key;
        var productQty = product.Value;

        await paymentHelper.GetQuotaByProductNameAsync(productName, wallet: true);

        if (productQty is null or <= 0)
        {
            throw new ArgumentException("Invalid quantity");
        }

        // TODO: support other currencies
        var defaultCurrency = tariffService.GetSupportedAccountingCurrencies().First();

        await paymentHelper.GetSubAccountRequiredAsync(tenantId, defaultCurrency);

        var quantity = new Dictionary<string, int> { { productName, productQty.Value } };

        var result = await tariffService.PaymentCalculateAsync(tenantId, quantity, inDto.ProductQuantityType, defaultCurrency);

        return result;
    }

    /// <remarks>
    /// Returns the information about the current subscription and its unused (prorated) balance.
    /// </remarks>
    /// <summary>
    /// Get the subscription balance information
    /// </summary>
    /// <path>api/2.0/portal/payment/subscription/balance</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The subscription balance information", typeof(SubscriptionBalanceInfo))]
    [SwaggerResponse(400, "Invalid request parameters")]
    [SwaggerResponse(402, "Tariff is not paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer or subscription could not be found")]
    [HttpGet("subscription/balance")]
    public async Task<SubscriptionBalanceInfo> GetSubscriptionBalanceInfo()
    {
        var tenant = tenantManager.GetCurrentTenant();

        await paymentHelper.DemandCustomerPayerAsync(tenant.Id);

        var productId = await paymentHelper.GetCurrentSubscriptionProductIdAsync(tenant.Id);

        return await tariffService.GetSubscriptionBalanceInfoAsync(tenant.Id, productId);
    }

    /// <remarks>
    /// Cancels the current subscription, moves its unused balance to the wallet, and purchases the requested number of
    /// admins from the wallet. If the wallet balance is not enough, it is topped up for the missing amount first
    /// (with several attempts, as the balance may be consumed concurrently).
    /// </remarks>
    /// <summary>
    /// Move the subscription balance to the wallet and purchase admins
    /// </summary>
    /// <path>api/2.0/portal/payment/subscription/movetowallet</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(400, "Invalid request parameters")]
    [SwaggerResponse(402, "Tariff is not paid")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer or subscription could not be found")]
    [HttpPost("subscription/movetowallet")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<bool> MoveSubscriptionToWallet(QuantityRequestDto inDto)
    {
        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await paymentHelper.DemandCustomerPayerAsync(tenant.Id);

        if (customerInfo.PaymentMethodStatus != PaymentMethodStatus.Set)
        {
            throw new InvalidOperationException("Customer payment method is not set");
        }

        var product = inDto.Quantity.First();
        var productName = product.Key;
        var productQty = product.Value;

        var quota = await paymentHelper.GetQuotaByProductNameAsync(productName, wallet: true);

        if (quota.TenantId != (int)TenantWalletService.Admin)
        {
            throw new ArgumentException("Invalid product");
        }

        if (productQty <= 0)
        {
            throw new ArgumentException("Invalid quantity");
        }

        // The requested number of admins must not be less than the current number of portal admins.
        var currentAdminCount = (await userManager.GetUsersByGroupAsync(ASC.Core.Users.Constants.GroupRoomAdmin.ID)).Length;
        if (productQty < currentAdminCount)
        {
            throw new ArgumentException("Invalid quantity");
        }

        // Resolve the current Stripe subscription product before it is cancelled.
        var productId = await paymentHelper.GetCurrentSubscriptionProductIdAsync(tenant.Id);

        // TODO: support other currencies
        var defaultCurrency = tariffService.GetSupportedAccountingCurrencies().First();
        var participant = securityContext.CurrentAccount.ID.ToString();

        // Calculate the cost of the requested admins from the known quota price (price * quantity).
        var walletQuotas = await tariffHelper.GetQuotasAsync(wallet: true).ToListAsync();
        var quotaDto = walletQuotas.FirstOrDefault(q => q.Id == quota.TenantId);
        if (quotaDto?.Price?.Value is not { } unitPrice)
        {
            throw new ItemNotFoundException("Quota price could not be found");
        }

        var requiredAmount = unitPrice * productQty;

        // Move the unused subscription balance to the wallet.
        await paymentHelper.SubscriptionBalanceToWalletAsync(tenant.Id, productId);

        // Make sure the wallet balance covers the cost, topping it up for the missing amount if necessary.
        var siteName = tenant.GetTenantDomain(coreSettings);

        if (!await tariffService.EnsureWalletBalanceAsync(tenant.Id, requiredAmount, defaultCurrency, participant, siteName, false))
        {
            throw new BillingException("Insufficient balance");
        }

        // Purchase the requested admins from the wallet.
        return await paymentHelper.PaymentChangeAsync(tenant.Id, inDto.Quantity, ProductQuantityType.Add, defaultCurrency, false, participant);
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
        paymentHelper.DemandConfigured();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        await paymentHelper.DemandPayerOrOwnerAsync(tenant, customerInfo);

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

        if (inDto.Additional.HasValue && !inDto.Additional.Value)
        {
            var currentQuota = await tariffHelper.GetCurrentQuotaAsync(false, false);
            if (currentQuota.NonProfit)
            {
                return [currentQuota];
            }
        }

        return await tariffHelper.GetQuotasAsync(false, inDto.Additional, inDto.Wallet).ToListAsync();
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
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task SendPaymentRequest(SalesRequestsDto inDto)
    {
        await paymentHelper.DemandAdminAsync();

        if (!inDto.Email.TestEmailRegex())
        {
            throw new ArgumentException(Resource.ErrorNotCorrectEmail);
        }

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
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo != null)
        {
            if (!string.IsNullOrEmpty(customerInfo.Email))
            {
                await paymentHelper.DemandPayerAsync(customerInfo);
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
            // do not throw an exception, just return null
            return null;
        }

        await paymentHelper.DemandAdminAsync();

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
        paymentHelper.DemandConfigured();

        var supportedCurrencies = tariffService.GetSupportedAccountingCurrencies();
        if (!supportedCurrencies.Contains(inDto.Currency))
        {
            throw new ArgumentException("Unsupported currency");
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await paymentHelper.DemandCustomerPayerAsync(tenant.Id);

        if (customerInfo.PaymentMethodStatus != PaymentMethodStatus.Set)
        {
            throw new InvalidOperationException("Customer payment method is not set");
        }

        var siteName = tenant.GetTenantDomain(coreSettings);

        return await paymentHelper.TopUpDepositAsync(tenant.Id, inDto.Amount, inDto.Currency, securityContext.CurrentAccount.ID.ToString(), siteName);
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
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        return await tariffService.GetCustomerBalanceAsync(tenant.Id, inDto.Refresh);
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
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        inDto.ServiceName = await paymentHelper.GetCorrectServiceNamesAsync(inDto.ServiceName);

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

        return new ReportDto(report, apiDateTimeHelper, participantDisplayNames);
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
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

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
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        inDto.ServiceName = await paymentHelper.GetCorrectServiceNamesAsync(inDto.ServiceName);

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
        if (report == null)
        {
            return null;
        }

        var tenantQuotas = (await quotaService.GetTenantQuotasAsync()).ToList();
        var walletQuotas = tenantQuotas.Where(x => x.Wallet)
            .ToDictionary(x => x.ServiceName, x => x);

        var customUom = new Dictionary<string, string>();
        var aiQuota = tenantQuotas.SingleOrDefault(q => q.TenantId == (int)TenantWalletService.AITools);
        if (aiQuota != null)
        {
            // For ai-tools, usage is displayed in Tokens instead of AI Credits.
            customUom.Add(aiQuota.ServiceName, "chat");
        }

        return new CustomerServiceUsageReportDto(report, walletQuotas, customUom);
    }

    /// <remarks>
    /// Returns all the active wallet services (quotas) of the current portal: the active additional quotas
    /// from the tariff, plus the services enabled manually via the wallet service settings.
    /// </remarks>
    /// <summary>
    /// Get the active wallet services
    /// </summary>
    /// <path>api/2.0/portal/payment/activeservices</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The list of active wallet services", typeof(IEnumerable<ActiveServiceDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("activeservices")]
    public async Task<List<ActiveServiceDto>> GetActiveServices()
    {
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        var tenant = tenantManager.GetCurrentTenant();

        return await paymentHelper.GetActiveServicesAsync(tenant.Id);
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
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        inDto ??= new CustomerOperationsReportRequestDto();

        inDto.ServiceName = await paymentHelper.GetCorrectServiceNamesAsync(inDto.ServiceName);

        var userId = securityContext.CurrentAccount.ID;

        var task = serviceProvider.GetRequiredService<CustomerOperationsReportTask>();

        var baseUri = commonLinkUtility.ServerRootPath;

        task.Init(baseUri, tenantId, userId, null, DocumentBuilderTaskManager.GetTaskId(tenantId, userId, (int)ReportType.Operations));

        var taskProgress = await documentBuilderTaskManager.StartTask(task, false);

        var headers = MessageSettings.GetHttpHeaders(Request)?
            .ToDictionary(x => x.Key, x => x.Value.ToString()) ?? [];

        var evt = new CustomerOperationsReportIntegrationEvent(
            userId,
            tenantId,
            baseUri,
            ReportType.Operations,
            inDto.ServiceName,
            inDto.StartDate,
            inDto.EndDate,
            inDto.ParticipantName,
            inDto.Credit,
            inDto.Debit,
            inDto.Type,
            inDto.Status,
            orderBy: inDto.OrderBy,
            orderType: inDto.OrderType,
            headers: headers);

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
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var task = await documentBuilderTaskManager.GetTask(tenantId, securityContext.CurrentAccount.ID, (int)ReportType.Operations);

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
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var evt = new CustomerOperationsReportIntegrationEvent(securityContext.CurrentAccount.ID, tenantId, null, ReportType.Operations, terminate: true);

        await eventBus.PublishAsync(evt);
    }

    /// <remarks>
    /// Starts generating a customer service usage report as an "xlsx" file and saves it in Documents.
    /// </remarks>
    /// <summary>
    /// Start the customer service usage report generation
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/usage/report</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Operation execution status", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer or service could not be found")]
    [HttpPost("customer/usage/report")]
    public async Task<DocumentBuilderTaskDto> CreateCustomerServiceUsageReport(CustomerServiceUsageReportRequestDto inDto)
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        inDto ??= new CustomerServiceUsageReportRequestDto();

        inDto.ServiceName = await paymentHelper.GetCorrectServiceNamesAsync(inDto.ServiceName);

        var userId = securityContext.CurrentAccount.ID;

        var task = serviceProvider.GetRequiredService<CustomerOperationsReportTask>();

        var baseUri = commonLinkUtility.ServerRootPath;

        task.Init(baseUri, tenantId, userId, null, DocumentBuilderTaskManager.GetTaskId(tenantId, userId, (int)ReportType.ServiceUsage));

        var taskProgress = await documentBuilderTaskManager.StartTask(task, false);

        var headers = MessageSettings.GetHttpHeaders(Request)?
            .ToDictionary(x => x.Key, x => x.Value.ToString()) ?? [];

        var evt = new CustomerOperationsReportIntegrationEvent(
            userId,
            tenantId,
            baseUri,
            ReportType.ServiceUsage,
            inDto.ServiceName,
            inDto.StartDate,
            inDto.EndDate,
            inDto.ParticipantName,
            status: inDto.Status,
            metadata: inDto.Metadata,
            orderBy: inDto.OrderBy,
            orderType: inDto.OrderType,
            headers: headers);

        await eventBus.PublishAsync(evt);

        return DocumentBuilderTaskDto.Get(taskProgress);
    }

    /// <remarks>
    /// Returns the status of generating a customer service usage report.
    /// </remarks>
    /// <summary>Get the status of the customer service usage report generation</summary>
    /// <path>api/2.0/portal/payment/customer/usage/report</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Operation execution status", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer could not be found")]
    [HttpGet("customer/usage/report")]
    public async Task<DocumentBuilderTaskDto> GetCustomerServiceUsageReport()
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var task = await documentBuilderTaskManager.GetTask(tenantId, securityContext.CurrentAccount.ID, (int)ReportType.ServiceUsage);

        return DocumentBuilderTaskDto.Get(task);
    }

    /// <remarks>
    /// Terminates generating a customer service usage report.
    /// </remarks>
    /// <summary>Terminate the customer service usage report generation</summary>
    /// <path>api/2.0/portal/payment/customer/usage/report</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer could not be found")]
    [HttpDelete("customer/usage/report")]
    public async Task TerminateCustomerServiceUsageReport()
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var evt = new CustomerOperationsReportIntegrationEvent(securityContext.CurrentAccount.ID, tenantId, null, ReportType.ServiceUsage, terminate: true);

        await eventBus.PublishAsync(evt);
    }

    /// <remarks>
    /// Starts generating a customer monthly usage report as an "xlsx" file and saves it in Documents.
    /// </remarks>
    /// <summary>
    /// Start the customer monthly usage report generation
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/usage/monthly/report</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Operation execution status", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer could not be found")]
    [HttpPost("customer/usage/monthly/report")]
    public async Task<DocumentBuilderTaskDto> CreateCustomerMonthlyUsageReport(CustomerMonthlyUsageReportRequestDto inDto)
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        inDto ??= new CustomerMonthlyUsageReportRequestDto();

        var userId = securityContext.CurrentAccount.ID;

        var task = serviceProvider.GetRequiredService<CustomerOperationsReportTask>();

        var baseUri = commonLinkUtility.ServerRootPath;

        task.Init(baseUri, tenantId, userId, null, DocumentBuilderTaskManager.GetTaskId(tenantId, userId, (int)ReportType.MonthlyUsage));

        var taskProgress = await documentBuilderTaskManager.StartTask(task, false);

        var headers = MessageSettings.GetHttpHeaders(Request)?
            .ToDictionary(x => x.Key, x => x.Value.ToString()) ?? [];

        var evt = new CustomerOperationsReportIntegrationEvent(
            userId,
            tenantId,
            baseUri,
            ReportType.MonthlyUsage,
            startDate: inDto.StartDate,
            endDate: inDto.EndDate,
            headers: headers);

        await eventBus.PublishAsync(evt);

        return DocumentBuilderTaskDto.Get(taskProgress);
    }

    /// <remarks>
    /// Returns the status of generating a customer monthly usage report.
    /// </remarks>
    /// <summary>Get the status of the customer monthly usage report generation</summary>
    /// <path>api/2.0/portal/payment/customer/usage/monthly/report</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Operation execution status", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer could not be found")]
    [HttpGet("customer/usage/monthly/report")]
    public async Task<DocumentBuilderTaskDto> GetCustomerMonthlyUsageReport()
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var task = await documentBuilderTaskManager.GetTask(tenantId, securityContext.CurrentAccount.ID, (int)ReportType.MonthlyUsage);

        return DocumentBuilderTaskDto.Get(task);
    }

    /// <remarks>
    /// Terminates generating a customer monthly usage report.
    /// </remarks>
    /// <summary>Terminate the customer monthly usage report generation</summary>
    /// <path>api/2.0/portal/payment/customer/usage/monthly/report</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer could not be found")]
    [HttpDelete("customer/usage/monthly/report")]
    public async Task TerminateCustomerMonthlyUsageReport()
    {
        var tenantId = await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        var evt = new CustomerOperationsReportIntegrationEvent(securityContext.CurrentAccount.ID, tenantId, null, ReportType.MonthlyUsage, terminate: true);

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
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

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
        await paymentHelper.DemandAdminAsync();

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
        var tenant = tenantManager.GetCurrentTenant();

        await paymentHelper.DemandCustomerPayerAsync(tenant.Id);

        var balance = await tariffService.GetCustomerBalanceAsync(tenant.Id);
        if (balance == null)
        {
            throw new ItemNotFoundException("Balance could not be found");
        }

        var settings = inDto?.Settings ?? new TenantWalletSettings();

        // LowBalanceThreshold/LowBalanceNotified are internal-only: never trust them from client input,
        // always recompute from what was previously persisted so a stale GET->POST round-trip can't
        // resurrect an old value (e.g. permanently suppressing the low-balance notification)
        var existing = await settingsManager.LoadAsync<TenantWalletSettings>();
        settings.LowBalanceThreshold = existing.LowBalanceThreshold;
        settings.LowBalanceNotified = existing.LowBalanceNotified;

        if (settings.Enabled)
        {
            settings.LowBalanceNotified = false;
        }
        else
        {
            // keep the settings row persisted (not equal to GetDefault()) even when auto top-up is
            // turned off, so the low-balance poller can still discover this tenant
            settings.LowBalanceThreshold = paymentHelper.GetDefaultLowBalanceThreshold();
        }

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
        paymentHelper.DemandConfigured();

        await paymentHelper.DemandAdminAsync();

        return await settingsManager.LoadAsync<TenantWalletServiceSettings>();
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
        paymentHelper.DemandConfigured();

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        return await paymentHelper.ChangeWalletServiceStateAsync(inDto.Service, inDto.Enabled);
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
        paymentHelper.DemandAiGatewayConfiguration();

        await paymentHelper.DemandAdminAsync();

        return await paymentHelper.GetAiPricesAsync();
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
        if (!tariffService.IsConfigured() || !await aiGateway.IsAiEnabledAsync())
        {
            return new RestrictedModelsResponse { Models = [] };
        }

        await paymentHelper.DemandAdminAsync();

        return await aiGateway.GetRestrictedModelsAsync();
    }

    /// <summary>
    /// Set restricted AI models
    /// </summary>
    /// <remarks>
    /// Overwrites the entire set of restricted AI model IDs for the current tenant.
    /// The request body must contain the complete desired set — to add a restriction, include the new model alongside existing ones;
    /// to remove one, omit it. An empty set lifts all restrictions. Only portal administrators can perform this action.
    /// </remarks>
    /// <path>api/2.0/portal/payment/ai-model/restrictions</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The updated list of restricted AI model IDs", typeof(RestrictedModelsResponse))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Customer could not be found")]
    [HttpPut("ai-model/restrictions")]
    public async Task<RestrictedModelsResponse> SetRestrictedAiModels(SetRestrictedAiModelsRequestDto inDto)
    {
        paymentHelper.DemandAiGatewayConfiguration();

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await paymentHelper.EnsureCustomerAndAdminRightsAsync();

        return await paymentHelper.SetRestrictedAiModelsAsync(inDto.Models);
    }
}
