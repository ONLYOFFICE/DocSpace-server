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

namespace ASC.Web.Api.Controllers;

///<summary>
/// Portal information access.
///</summary>
///<name>portal</name>
[ApiExplorerSettings(IgnoreApi = true)]
[Scope]
[DefaultRoute("payment")]
[ApiController]
[AllowNotPayment]
[ControllerName("portal")]
public class PaymentController(
    UserManager userManager,
    TenantManager tenantManager,
    ITariffService tariffService,
    IQuotaService quotaService,
    SecurityContext securityContext,
    RegionHelper regionHelper,
    QuotaHelper tariffHelper,
    IFusionCache fusionCache,
    IHttpContextAccessor httpContextAccessor,
    MessageService messageService,
    StudioNotifyService studioNotifyService,
    PermissionContext permissionContext)
    : ControllerBase
{
    private readonly int _maxCount = 10;
    private readonly int _expirationMinutes = 2;

    /// <summary>
    /// Returns the URL to the payment page.
    /// </summary>
    /// <short>
    /// Get the payment page URL
    /// </short>
    /// <path>api/2.0/portal/payment/url</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The URL to the payment page", typeof(Uri))]
    [HttpPut("url")]
    public async Task<Uri> GetPaymentUrlAsync(PaymentUrlRequestsDto inDto)
    {
        var tenant = tenantManager.GetCurrentTenant();
        
        if ((await tariffService.GetPaymentsAsync(tenant.Id)).Any() ||
            !await userManager.IsDocSpaceAdminAsync(securityContext.CurrentAccount.ID))
        {
            return null;
        }

        var monthQuotas = (await quotaService.GetTenantQuotasAsync())
            .Where(q => !string.IsNullOrEmpty(q.ProductId) && q.Visible && !q.Year)
            .ToList();

        // TODO: Temporary restriction. Only monthly tariff available for purchase
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

    /// <summary>
    /// Updates the quantity of payment.
    /// </summary>
    /// <short>
    /// Update the payment quantity
    /// </short>
    /// <path>api/2.0/portal/payment/update</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPut("update")]
    public async Task<bool> PaymentUpdateAsync(QuantityRequestDto inDto)
    {
        var tenant = tenantManager.GetCurrentTenant();
        var payerId = (await tariffService.GetTariffAsync(tenant.Id)).CustomerId;
        var payer = await userManager.GetUserByEmailAsync(payerId);

        if (!(await tariffService.GetPaymentsAsync(tenant.Id)).Any() ||
            securityContext.CurrentAccount.ID != payer.Id)
        {
            return false;
        }

        var quota = await tenantManager.GetTenantQuotaAsync(tenant.Id);

        // TODO: Temporary restriction. Only changing the quota for the current tariff is available
        if (inDto.Quantity.Count != 1 || quota.Name != inDto.Quantity.First().Key)
        {
            return false;
        }

        return await tariffService.PaymentChangeAsync(tenant.Id, inDto.Quantity);
    }

    /// <summary>
    /// Returns the URL to the payment account.
    /// </summary>
    /// <short>
    /// Get the payment account
    /// </short>
    /// <path>api/2.0/portal/payment/account</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The URL to the payment account", typeof(string))]
    [HttpGet("account")]
    public async Task<string> GetPaymentAccountAsync(PaymentUrlRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();
        var hasPayments = (await tariffService.GetPaymentsAsync(tenant.Id)).Any();

        if (!hasPayments)
        {
            return null;
        }

        var payerId = (await tariffService.GetTariffAsync(tenant.Id)).CustomerId;
        var payer = await userManager.GetUserByEmailAsync(payerId);

        if (securityContext.CurrentAccount.ID != payer.Id &&
            securityContext.CurrentAccount.ID != tenant.OwnerId)
        {
            return null;
        }

        var result = "payment.ashx";
        return !string.IsNullOrEmpty(inDto.BackUrl) ? $"{result}?backUrl={inDto.BackUrl}" : result;
    }

    /// <summary>
    /// Returns the available portal prices.
    /// </summary>
    /// <short>
    /// Get prices
    /// </short>
    /// <path>api/2.0/portal/payment/prices</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "List of available portal prices", typeof(object))]
    [HttpGet("prices")]
    public async Task<object> GetPricesAsync()
    {
        var currency = await regionHelper.GetCurrencyFromRequestAsync();
        var result = (await tenantManager.GetProductPriceInfoAsync())
            .ToDictionary(pr => pr.Key, pr => pr.Value.GetValueOrDefault(currency, 0));
        return result;
    }


    /// <summary>
    /// Returns the available portal currencies.
    /// </summary>
    /// <short>
    /// Get currencies
    /// </short>
    /// <path>api/2.0/portal/payment/currencies</path>
    /// <collection>list</collection>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "List of available portal currencies", typeof(IAsyncEnumerable<CurrenciesDto>))]
    [HttpGet("currencies")]
    public async IAsyncEnumerable<CurrenciesDto> GetCurrenciesAsync()
    {
        var defaultRegion = regionHelper.GetDefaultRegionInfo();
        var currentRegion = await regionHelper.GetCurrentRegionInfoAsync();

        yield return new CurrenciesDto(defaultRegion);

        if (!currentRegion.Name.Equals(defaultRegion.Name))
        {
            yield return new CurrenciesDto(currentRegion);
        }
    }

    /// <summary>
    /// Returns the available portal quotas.
    /// </summary>
    /// <short>
    /// Get quotas
    /// </short>
    /// <path>api/2.0/portal/payment/quotas</path>
    /// <collection>list</collection>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "List of available portal quotas", typeof(IEnumerable<QuotaDto>))]
    [HttpGet("quotas")]
    public async Task<IEnumerable<QuotaDto>> GetQuotasAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var currentQuota = await tariffHelper.GetCurrentQuotaAsync(false, false);
        if (currentQuota.NonProfit)
        {
            return [currentQuota];
        }

        return await tariffHelper.GetQuotasAsync().ToListAsync();
    }

    /// <summary>
    /// Returns the payment information about the current portal quota.
    /// </summary>
    /// <short>
    /// Get quota payment information
    /// </short>
    /// <path>api/2.0/portal/payment/quota</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Payment information about the current portal quota", typeof(QuotaDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("quota")]
    public async Task<QuotaDto> GetQuotaAsync(PaymentInformationRequestDto inDto)
    {
        if (await userManager.IsGuestAsync(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }
        
        return await tariffHelper.GetCurrentQuotaAsync(inDto.Refresh);
    }

    /// <summary>
    /// Sends a request for portal payment.
    /// </summary>
    /// <short>
    /// Send a payment request
    /// </short>
    /// <path>api/2.0/portal/payment/request</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(400, "Incorrect email or message text is empty")]
    [SwaggerResponse(429, "Request limit is exceeded")]
    [HttpPost("request")]
    public async Task SendSalesRequestAsync(SalesRequestsDto inDto)
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

    /// <summary>
    /// Returns the portal balance from the accounting service.
    /// </summary>
    /// <short>
    /// Get the portal balance
    /// </short>
    /// <path>api/2.0/portal/payment/accounting/balance</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The portal balance", typeof(decimal))]
    [HttpGet("accounting/balance")]
    public async Task<decimal> GetBalanceAsync()
    {
        var tenant = await CheckAccountingAndReturnTenantAsync();
        var result = await tariffService.GetBalanceAsync(tenant.Id);
        return result;
    }

    /// <summary>
    /// Trying to block amount money on the portal balance.
    /// </summary>
    /// <short>
    /// Block amount money on the portal balance
    /// </short>
    /// <path>api/2.0/portal/payment/accounting/money/block</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The money is blocked successfully or not", typeof(bool))]
    [HttpPost("accounting/money/block")]
    public async Task<bool> BlockMoneyAsync(AccountingBlockMoneyRequestDto inDto)
    {
        var tenant = await CheckAccountingAndReturnTenantAsync();
        var result = await tariffService.BlockMoneyAsync(tenant.Id, inDto.Amount);
        return result;
    }

    /// <summary>
    /// Take off amount money on the portal balance and return new balance.
    /// </summary>
    /// <short>
    /// Take off amount money on the portal balance
    /// </short>
    /// <path>api/2.0/portal/payment/accounting/money/takeoff</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The new portal balance", typeof(decimal))]
    [HttpPost("accounting/money/takeoff")]
    public async Task<decimal> TakeOffMoneyAsync(AccountingTakeOffMoneyRequestDto inDto)
    {
        var tenant = await CheckAccountingAndReturnTenantAsync();
        var result = await tariffService.TakeOffMoneyAsync(tenant.Id, inDto.Amount);
        return result;
    }

    /// <summary>
    /// Returns the portal accounting report from the accounting service.
    /// </summary>
    /// <short>
    /// Get the accounting report
    /// </short>
    /// <path>api/2.0/portal/payment/accounting/report</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The accounting report", typeof(List<PurchaseInfo>))]
    [HttpGet("accounting/report")]
    public async Task<List<PurchaseInfo>> GetReportAsync(AccountingReportRequestDto inDto)
    {
        var tenant = await CheckAccountingAndReturnTenantAsync();
        var result = await tariffService.GetReportAsync(tenant.Id, inDto.From, inDto.To);
        return result;
    }

    /// <summary>
    /// Returns the list of currencies from accounting service.
    /// </summary>
    /// <short>
    /// Get list of currencies
    /// </short>
    /// <path>api/2.0/portal/payment/accounting/currency/all</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The list of currencies", typeof(List<CurrencyInfo>))]
    [HttpGet("accounting/currency/all")]
    public async Task<List<CurrencyInfo>> GetAllCurrenciesAsync()
    {
        var result = await tariffService.GetAllCurrenciesAsync();
        return result;
    }
    

    private async Task<Tenant> CheckAccountingAndReturnTenantAsync()
    {
        if (!tariffService.IsAccountingClientConfigured())
        {
            throw new AccountingNotConfiguredException();
        }

        var tenant = tenantManager.GetCurrentTenant();
        var hasPayments = (await tariffService.GetPaymentsAsync(tenant.Id)).Any();

        if (!hasPayments)
        {
            throw new BillingNotFoundException("No one payment found");
        }

        var payerId = (await tariffService.GetTariffAsync(tenant.Id)).CustomerId;
        var payer = await userManager.GetUserByEmailAsync(payerId);

        if (securityContext.CurrentAccount.ID != payer.Id &&
            securityContext.CurrentAccount.ID != tenant.OwnerId)
        {
            throw new SecurityException();
        }

        return tenant;
    }


    private async Task CheckCache(string baseKey)
    {
        var key = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress + baseKey;
        var countFromCache = await fusionCache.TryGetAsync<int>(key);
        var count = countFromCache.HasValue ? countFromCache.Value : 0;
        if (count > _maxCount)
        {
            throw new Exception(Resource.ErrorRequestLimitExceeded);
        }

        await fusionCache.SetAsync(key, count + 1, TimeSpan.FromMinutes(_expirationMinutes));
    }
}
