// (c) Copyright Ascensio System SIA 2009-2026
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
    WalletStaticProvider walletStaticProvider)
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
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("url")]
    public async Task<Uri> GetPaymentUrl(PaymentUrlRequestDto inDto)
    {
        await DemandAdminAsync();

        if (!tariffService.IsConfigured())
        {
            return null;
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
        if (inDto.Quantity.Count != 1 || !monthQuotas.Any(q => q.Name == inDto.Quantity.First().Key))
        {
            return null;
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
            inDto.BackUrl);
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
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("update")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<bool> UpdatePayment(QuantityRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            return false;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return false;
        }

        await DemandPayerAsync(customerInfo);

        // TODO: Temporary restriction.
        // Possibility to buy only one product per transaction.
        // For the current paid tariff only quota change is available.
        if (inDto.Quantity.Count != 1)
        {
            return false;
        }

        var product = inDto.Quantity.First();
        var productName = product.Key;
        var productQty = product.Value;
        var quota = (await quotaService.GetTenantQuotasAsync())
            .FirstOrDefault(q => !string.IsNullOrEmpty(q.ProductId) && q.Name == productName);

        if (quota == null || quota.Wallet)
        {
            return false;
        }

        var currentQuota = await tenantManager.GetTenantQuotaAsync(tenant.Id);

        if (currentQuota.Price > 0 && currentQuota.Name != productName)
        {
            return false;
        }

        var tariff = await tariffService.GetTariffAsync(tenant.Id);

        if (tariff.Quotas.Any(q => q.Id == quota.TenantId && q.Quantity == productQty))
        {
            return false;
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
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("updatewallet")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<bool> UpdateWalletPayment(WalletQuantityRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            return false;
        }

        if (inDto.ProductQuantityType is ProductQuantityType.Renew or ProductQuantityType.Sub)
        {
            return false;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return false;
        }

        await DemandPayerAsync(customerInfo);

        // TODO: Temporary restriction.
        // Possibility to buy only one product per transaction.
        // Wallet tariffs are always available for purchase.
        if (inDto.Quantity.Count != 1)
        {
            return false;
        }

        var product = inDto.Quantity.First();
        var productName = product.Key;
        var productQty = product.Value;
        var quota = (await quotaService.GetTenantQuotasAsync())
            .FirstOrDefault(q => !string.IsNullOrEmpty(q.ProductId) && q.Name == productName);

        if (quota is not { Wallet: true })
        {
            return false;
        }

        var tariff = await tariffService.GetTariffAsync(tenant.Id);

        if (tariff.State > TariffState.Paid)
        {
            return false;
        }

        if (inDto.ProductQuantityType is ProductQuantityType.Set)
        {
            if (productQty.HasValue && productQty.Value != 0 && productQty.Value < 100) // min value 100Gb
            {
                return false;
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
            return false;
        }

        var hasActiveWalletQuota = tariff.Quotas.Any(q => q.Id == quota.TenantId && q.State == QuotaState.Active);
        if (!hasActiveWalletQuota && productQty < 100) // min value 100Gb
        {
            return false;
        }

        var balance = await tariffService.GetCustomerBalanceAsync(tenant.Id);
        if (balance == null)
        {
            return false;
        }

        // TODO: support other currencies
        var defaultCurrency = tariffService.GetSupportedAccountingCurrencies().First();
        var subAccount = balance.SubAccounts.FirstOrDefault(x => x.Currency == defaultCurrency);
        if (subAccount == null)
        {
            return false;
        }

        var quantity = new Dictionary<string, int> { { productName, productQty.Value } };

        var result = await tariffService.PaymentChangeAsync(tenant.Id, quantity, inDto.ProductQuantityType, defaultCurrency, false, securityContext.CurrentAccount.ID.ToString(), null);

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
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("calculatewallet")]
    public async Task<PaymentCalculation> CalculateWalletPayment(WalletQuantityRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            return null;
        }

        if (inDto.ProductQuantityType is not ProductQuantityType.Add)
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        await DemandPayerAsync(customerInfo);

        // TODO: Temporary restriction.
        // Possibility to buy only one product per transaction.
        // Wallet tariffs are always available for purchase.
        if (inDto.Quantity.Count != 1)
        {
            return null;
        }

        var product = inDto.Quantity.First();
        var productName = product.Key;
        var productQty = product.Value;
        var quota = (await quotaService.GetTenantQuotasAsync())
            .FirstOrDefault(q => !string.IsNullOrEmpty(q.ProductId) && q.Name == productName);

        if (quota is not { Wallet: true })
        {
            return null;
        }

        if (productQty is null or <= 0)
        {
            return null;
        }

        var balance = await tariffService.GetCustomerBalanceAsync(tenant.Id);
        if (balance == null)
        {
            return null;
        }

        // TODO: support other currencies
        var defaultCurrency = tariffService.GetSupportedAccountingCurrencies().First();
        var subAccount = balance.SubAccounts.FirstOrDefault(x => x.Currency == defaultCurrency);
        if (subAccount == null)
        {
            return null;
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
            return null;
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
    [HttpGet("prices")]
    public async Task<Dictionary<string, decimal>> GetPortalPrices()
    {
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
    [HttpGet("currencies")]
    public async IAsyncEnumerable<CurrenciesDto> GetPaymentCurrencies()
    {
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
    /// <collection>list</collection>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Wallet service", typeof(QuotaDto))]
    [HttpGet("walletservice")]
    public async Task<QuotaDto> GetWalletService(GetWalletServiceRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var quotaList = await quotaService.GetTenantQuotasAsync();
        var quota = quotaList.FirstOrDefault(q => q.Wallet && q.TenantId == (int)inDto.Service);
        if (quota == null)
        {
            throw new ItemNotFoundException();
        }

        return await tariffHelper.ToQuotaDtoAsync(quota, false);
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
    [SwaggerResponse(429, "Request limit is exceeded")]
    [HttpPost("request")]
    public async Task SendPaymentRequest(SalesRequestsDto inDto)
    {
        if (!inDto.Email.TestEmailRegex())
        {
            throw new Exception(Resource.ErrorNotCorrectEmail);
        }

        if (string.IsNullOrEmpty(inDto.Message))
        {
            throw new Exception(Resource.ErrorEmptyMessage);
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
        await DemandAdminAsync();

        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo != null)
        {
            await DemandPayerAsync(customerInfo);

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
        await DemandAdminAsync();

        if (!tariffService.IsConfigured())
        {
            return null;
        }

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
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPost("deposit")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<bool> TopUpDeposit(TopUpDepositRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            return false;
        }

        var supportedCurrencies = tariffService.GetSupportedAccountingCurrencies();
        if (!supportedCurrencies.Contains(inDto.Currency))
        {
            return false;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo is not { PaymentMethodStatus: PaymentMethodStatus.Set })
        {
            return false;
        }

        await DemandPayerAsync(customerInfo);

        var siteName = tenant.GetTenantDomain(coreSettings);

        var result = await tariffService.TopUpDepositAsync(tenant.Id, inDto.Amount, inDto.Currency, securityContext.CurrentAccount.ID.ToString(), siteName, null, true);

        if (result)
        {
            var description = $"{inDto.Amount} {inDto.Currency}";
            messageService.Send(MessageAction.CustomerWalletToppedUp, description);
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
        await DemandAdminAsync();

        if (!tariffService.IsConfigured())
        {
            return null;
        }

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
    /// Returns the service quota from the accounting service.
    /// </remarks>
    /// <summary>
    /// Get the service quota
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/servicequota</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The service quota", typeof(Balance))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Service could not be found")]
    [HttpGet("customer/servicequota")]
    public async Task<Balance> GetCustomerServiceQuota(CustomerServiceQuotaRequestDto inDto)
    {
        await DemandAdminAsync();

        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        var serviceName = await GetPaymentServiceName(inDto.ServiceName);

        var result = await tariffService.GetCustomerServiceQuotaAsync(tenant.Id, serviceName, inDto.Refresh);
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
        await DemandAdminAsync();

        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        var serviceName = string.IsNullOrEmpty(inDto.ServiceName) ? null : await GetPaymentServiceName(inDto.ServiceName);
        var utcStartDate = tenantUtil.DateTimeToUtc(inDto.StartDate ?? tenant.CreationDateTime);
        var utcEndDate = tenantUtil.DateTimeToUtc(inDto.EndDate ?? DateTime.UtcNow);

        var filter = new OperationFilter
        {
            ServiceName = serviceName,
            UtcStartDate = utcStartDate,
            UtcEndDate = utcEndDate,
            ParticipantName = inDto.ParticipantName,
            Credit = inDto.Credit,
            Debit = inDto.Debit,
            Offset = inDto.Offset,
            Limit = inDto.Limit,
            Types = inDto.Types,
            Status = inDto.Status,
            OrderBy = inDto.OrderBy,
            OrderType = inDto.OrderType
        };

        var report = await tariffService.GetCustomerOperationsAsync(tenant.Id, filter);
        if (report == null)
        {
            return null;
        }

        var participantDisplayNames = await report.GetParticipantDisplayNamesAsync(displayUserSettingsHelper);

        return new ReportDto(report, apiDateTimeHelper, participantDisplayNames, filter.ServiceName);
    }

    /// <remarks>
    /// Starts generating a customer operations report as an "xlsx" file and saves it in Documents.
    /// </remarks>
    /// <summary>
    /// Start the customer operations report generation
    /// </summary>
    /// <path>api/2.0/portal/payment/customer/operationsreport</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Ok", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Service could not be found")]
    [HttpPost("customer/operationsreport")]
    public async Task<DocumentBuilderTaskDto> CreateCustomerOperationsReport(CustomerOperationsReportRequestDto inDto)
    {
        await DemandAdminAsync();

        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenantId);
        if (customerInfo == null)
        {
            return null;
        }

        var serviceName = string.IsNullOrEmpty(inDto.ServiceName) ? null : await GetPaymentServiceName(inDto.ServiceName);

        inDto ??= new CustomerOperationsReportRequestDto();

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
            serviceName,
            inDto.StartDate,
            inDto.EndDate,
            inDto.ParticipantName,
            inDto.Credit,
            inDto.Debit,
            inDto.Types,
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
    [SwaggerResponse(200, "Ok", typeof(DocumentBuilderTaskDto))]
    [HttpGet("customer/operationsreport")]
    public async Task<DocumentBuilderTaskDto> GetCustomerOperationsReport()
    {
        await DemandAdminAsync();

        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenantId);
        if (customerInfo == null)
        {
            return null;
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
    [HttpDelete("customer/operationsreport")]
    public async Task TerminateCustomerOperationsReport()
    {
        await DemandAdminAsync();

        if (!tariffService.IsConfigured())
        {
            return;
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenantId);
        if (customerInfo == null)
        {
            return;
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
            return null;
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
    [HttpPost("topupsettings")]
    public async Task<TenantWalletSettings> SetTenantWalletSettings(TenantWalletSettingsWrapper inDto)
    {
        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
        }

        var balance = await tariffService.GetCustomerBalanceAsync(tenant.Id, false);
        if (balance == null)
        {
            return null;
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
            return null;
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
    [HttpPost("servicestate")]
    public async Task<TenantWalletServiceSettings> ChangeTenantWalletServiceState(ChangeWalletServiceStateRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
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

        return settings;
    }

    /// <summary>
    /// Purchases a wallet service with the specified quantity.
    /// </summary>
    /// <remarks>
    /// This method processes a payment for a wallet service using the configured payment method.
    /// Requires the tariff service to be configured and a valid payment method to be set for the customer.
    /// Rate limiting is applied according to the payments API policy.
    /// </remarks>
    /// <path>api/2.0/portal/payment/buywalletservice</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The service payment information", typeof(ServicePayment))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(404, "Service could not be found")]
    [HttpPost("buywalletservice")]
    [EnableRateLimiting(RateLimiterPolicy.PaymentsApi)]
    public async Task<ServicePayment> BuyWalletService(BuyWalletServiceRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo is not { PaymentMethodStatus: PaymentMethodStatus.Set })
        {
            return null;
        }

        await DemandPayerAsync(customerInfo);

        var serviceName = await GetPaymentServiceName(inDto.ServiceName);
        var customerParticipantName = securityContext.CurrentAccount.ID.ToString();
        var details = $"{serviceName} {inDto.Quantity}";
        //var metadata = new Dictionary<string, string> { { BillingClient.MetadataDetails, details } };

        var result = await tariffService.MakeServicePaymentAsync(tenant.Id, serviceName, inDto.Quantity, customerParticipantName, metadata: null);
        if (result != null)
        {
            messageService.Send(MessageAction.CustomerOperationPerformed, null, details);
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
    /// <path>api/2.0/portal/payment/aiprices</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Prices for AI models", typeof(AiPricesResponse))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("ai-prices")]
    public async Task<AiPricesDto> GetAiPrices()
    {
        if (!tariffService.IsConfigured())
        {
            return null;
        }

        await DemandAdminAsync();

        var aiPrices = await aiGateway.GetPricesAsync();

        var providers = aiPrices.Chat.Select(m => m.OwnedBy.ToLower()).Distinct();
        var icons = new Dictionary<string, string>();
        foreach (var provider in providers)
        {
            icons[provider] = await walletStaticProvider.GetImageAsync(provider);
        }

        var chat = aiPrices.Chat.Select(model => new AiEntryPricingDto<AiChatPriceDto>
        {
            Id = model.Id,
            Image = icons[model.OwnedBy.ToLower()],
            Alias = model.Alias,
            Provider = model.Provider,
            Price = new AiChatPriceDto
            {
                Prompt = model.Price.Prompt * 1_000_000,
                Completion = model.Price.Completion * 1_000_000
            }
        }).ToList();

        var embeddingImage = await walletStaticProvider.GetImageAsync("embedding");

        var embedding = aiPrices.Embedding.Select(e => new AiEntryPricingDto<AiEmbeddingPriceDto>
        {
            Id = e.Id,
            Alias = e.Alias,
            Provider = e.Provider,
            Image = embeddingImage,
            Price = new AiEmbeddingPriceDto { Prompt = e.Price.Prompt * 1_000_000 }
        }).ToList();

        return new AiPricesDto
        {
            Chat = chat,
            Embedding = embedding,
            WebSearch = [
                new AiEntryPricingDto<decimal>
                {
                    Id = "search",
                    Alias = "Web Search",
                    Image = await walletStaticProvider.GetImageAsync("search"),
                    Provider = aiPrices.WebSearch.Provider,
                    Price = aiPrices.WebSearch.Search
                },
                new AiEntryPricingDto<decimal>
                {
                    Id = "fetch",
                    Alias = "Web crawling",
                    Image = await walletStaticProvider.GetImageAsync("crawling"),
                    Provider = aiPrices.WebSearch.Provider,
                    Price = aiPrices.WebSearch.Contents
                }
            ],
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
        if (!tariffService.IsConfigured())
        {
            return null;
        }

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
    [HttpPut("ai-model/restrictions")]
    public async Task<RestrictedModelsResponse> SetRestrictedAiModels(SetRestrictedAiModelsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);
        if (customerInfo == null)
        {
            return null;
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

    private async Task<string> GetPaymentServiceName(string quotaName)
    {
        var quotaList = await tenantManager.GetTenantQuotasAsync(true, true);

        var selectedQuota = quotaList.FirstOrDefault(x =>
            x.Name.Equals(quotaName, StringComparison.InvariantCultureIgnoreCase));

        // only aitools available for purchasing!
        var serviceName = selectedQuota is not { TenantId: (int)TenantWalletService.AITools }
            ? throw new ItemNotFoundException("Service could not be found")
            : selectedQuota.GetPaymentId();

        return serviceName;
    }
}