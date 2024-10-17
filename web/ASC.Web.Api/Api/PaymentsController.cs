// (c) Copyright Ascensio System SIA 2009-2024
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
///<visible>false</visible>
[Scope]
[DefaultRoute("payment")]
[ApiController]
[AllowNotPayment]
[ControllerName("portal")]
public class PaymentController(UserManager userManager,
        TenantManager tenantManager,
        ITariffService tariffService,
        SecurityContext securityContext,
        RegionHelper regionHelper,
        QuotaHelper tariffHelper,
        IMemoryCache memoryCache,
        IHttpContextAccessor httpContextAccessor,
        MessageService messageService,
        StudioNotifyService studioNotifyService)
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
    /// <category>Payment</category>
    /// <param type="ASC.Web.Api.Models.PaymentUrlRequestsDto, ASC.Web.Api" name="inDto">Payment URL request parameters</param>
    /// <returns type="System.Uri, System">The URL to the payment page</returns>
    /// <path>api/2.0/portal/payment/url</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("url")]
    public async Task<Uri> GetPaymentUrlAsync(PaymentUrlRequestsDto inDto)
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();
        
        if ((await tariffService.GetPaymentsAsync(tenant.Id)).Any() ||
            !await userManager.IsDocSpaceAdminAsync(securityContext.CurrentAccount.ID))
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
    /// <category>Payment</category>
    /// <param type="ASC.Web.Api.Models.PaymentUrlRequestsDto, ASC.Web.Api" name="inDto">Payment URL request parameters</param>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <path>api/2.0/portal/payment/update</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("update")]
    public async Task<bool> PaymentUpdateAsync(PaymentUrlRequestsDto inDto)
    {
        var tenant = await tenantManager.GetCurrentTenantAsync();
        var payerId = (await tariffService.GetTariffAsync(tenant.Id)).CustomerId;
        var payer = await userManager.GetUserByEmailAsync(payerId);

        if (!(await tariffService.GetPaymentsAsync(tenant.Id)).Any() ||
            securityContext.CurrentAccount.ID != payer.Id)
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
    /// <category>Payment</category>
    /// <param type="System.String, System" name="backUrl">Back URL</param>
    /// <returns type="System.Object, System">The URL to the payment account</returns>
    /// <path>api/2.0/portal/payment/account</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("account")]
    public async Task<object> GetPaymentAccountAsync(string backUrl)
    {
        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = await tenantManager.GetCurrentTenantAsync();
        var payerId = (await tariffService.GetTariffAsync(tenant.Id)).CustomerId;
        var payer = await userManager.GetUserByEmailAsync(payerId);

        if (securityContext.CurrentAccount.ID != payer.Id &&
            securityContext.CurrentAccount.ID != tenant.OwnerId)
        {
            return null;
        }

        var result = "payment.ashx";
        return !string.IsNullOrEmpty(backUrl) ? $"{result}?backUrl={backUrl}" : result;
    }

    /// <summary>
    /// Returns the available portal prices.
    /// </summary>
    /// <short>
    /// Get prices
    /// </short>
    /// <category>Payment</category>
    /// <returns type="System.Object, System">List of available portal prices</returns>
    /// <path>api/2.0/portal/payment/prices</path>
    /// <httpMethod>GET</httpMethod>
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
    /// <category>Payment</category>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.CurrenciesDto, ASC.Web.Api">List of available portal currencies</returns>
    /// <path>api/2.0/portal/payment/currencies</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
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
    /// <category>Quota</category>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.QuotaDto, ASC.Web.Api">List of available portal quotas</returns>
    /// <path>api/2.0/portal/payment/quotas</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("quotas")]
    public async Task<IEnumerable<QuotaDto>> GetQuotasAsync()
    {
        return await tariffHelper.GetQuotasAsync().ToListAsync();
    }

    /// <summary>
    /// Returns the payment information about the current portal quota.
    /// </summary>
    /// <short>
    /// Get quota payment information
    /// </short>
    /// <category>Payment</category>
    /// <returns type="ASC.Web.Api.ApiModels.ResponseDto.QuotaDto, ASC.Web.Api">Payment information about the current portal quota</returns>
    /// <path>api/2.0/portal/payment/quota</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("quota")]
    public async Task<QuotaDto> GetQuotaAsync(bool refresh)
    {
        if (await userManager.IsGuestAsync(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }
        
        return await tariffHelper.GetCurrentQuotaAsync(refresh);
    }

    /// <summary>
    /// Sends a request for portal payment.
    /// </summary>
    /// <short>
    /// Send a payment request
    /// </short>
    /// <category>Payment</category>
    /// <param type="ASC.Web.Api.ApiModels.RequestsDto.SalesRequestsDto, ASC.Web.Api" name="inDto">Portal payment request parameters</param>
    /// <returns></returns>
    /// <path>api/2.0/portal/payment/request</path>
    /// <httpMethod>POST</httpMethod>
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

        CheckCache("salesrequest");

        await studioNotifyService.SendMsgToSalesAsync(inDto.Email, inDto.UserName, inDto.Message);
        await messageService.SendAsync(MessageAction.ContactSalesMailSent);
    }

    private void CheckCache(string baseKey)
    {
        var key = httpContextAccessor.HttpContext.Connection.RemoteIpAddress + baseKey;

        if (memoryCache.TryGetValue<int>(key, out var count) && count > _maxCount)
        {
            throw new Exception(Resource.ErrorRequestLimitExceeded);
        }

        memoryCache.Set(key, count + 1, TimeSpan.FromMinutes(_expirationMinutes));
    }
}
