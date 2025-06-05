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

using ASC.Files.Core.Utils;

namespace ASC.Web.Api.Controllers;

///<summary>
/// Portal information access.
///</summary>
///<name>portal</name>
[Scope]
[DefaultRoute("payment")]
[ApiController]
[AllowNotPayment]
[ControllerName("portal")]
public class PaymentController(
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
    TempStream tempStream,
    CsvFileHelper csvFileHelper,
    CsvFileUploader csvFileUploader)
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
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("url")]
    public async Task<Uri> GetPaymentUrl(PaymentUrlRequestsDto inDto)
    {
        await DemandAdminAsync();

        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var hasCustomer = await HasCustomer(tenant);
        if (hasCustomer)
        {
            return null;
        }

        var monthQuotas = (await quotaService.GetTenantQuotasAsync())
            .Where(q => !string.IsNullOrEmpty(q.ProductId) && q.Visible && !q.Year)
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

    /// <summary>
    /// Updates the payment quantity with the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Update the payment quantity
    /// </short>
    /// <path>api/2.0/portal/payment/update</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("update")]
    public async Task<bool> UpdatePayment(QuantityRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            return false;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var hasCustomer = await HasCustomer(tenant);
        if (!hasCustomer)
        {
            return false;
        }

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

        var result = await tariffService.PaymentChangeAsync(tenant.Id, inDto.Quantity, ProductQuantityType.Set);

        if (result)
        {
            messageService.Send(MessageAction.CustomerSubscriptionUpdated, $"{productName} {productQty}");
        }

        return result;
    }

    /// <summary>
    /// Updates the wallet payment quantity with the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Update the wallet payment quantity
    /// </short>
    /// <path>api/2.0/portal/payment/updatewallet</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("updatewallet")]
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

        var hasCustomer = await HasCustomer(tenant);
        if (!hasCustomer)
        {
            return false;
        }

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

        if (quota == null || !quota.Wallet)
        {
            return false;
        }

        if (inDto.ProductQuantityType is ProductQuantityType.Set)
        {
            var tariff = await tariffService.GetTariffAsync(tenant.Id);

            // saving null value is equivalent to resetting to default
            var updated = await tariffService.UpdateNextQuantityAsync(tenant.Id, tariff, quota.TenantId, productQty);

            if (updated)
            {
                messageService.Send(MessageAction.CustomerSubscriptionUpdated, $"{productName} {productQty}");
            }

            return updated;
        }

        // inDto.ProductQuantityType === ProductQuantityType.Add

        if (!productQty.HasValue || productQty <= 0)
        {
            return false;
        }

        var balance = await tariffService.GetCustomerBalanceAsync(tenant.Id);
        if (balance == null)
        {
            return false;
        }

        // TODO: support other currencies
        var subAccount = balance.SubAccounts.FirstOrDefault(x => x.Currency == "USD");
        if (subAccount == null)
        {
            return false;
        }

        if (subAccount.Amount < productQty * quota.Price)
        {
            return false;
        }

        var quantity = new Dictionary<string, int> { { productName, productQty.Value } };

        var result = await tariffService.PaymentChangeAsync(tenant.Id, quantity, inDto.ProductQuantityType);

        if (result)
        {
            messageService.Send(MessageAction.CustomerSubscriptionUpdated, $"{productName} {productQty}");
        }

        return result;
    }

    /// <summary>
    /// Calculate amount of the wallet payment with the parameters specified in the request.
    /// </summary>
    /// <short>
    /// Calculate amount of the wallet payment
    /// </short>
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

        var hasCustomer = await HasCustomer(tenant);
        if (!hasCustomer)
        {
            return null;
        }

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

        if (quota == null || !quota.Wallet)
        {
            return null;
        }

        if (!productQty.HasValue || productQty <= 0)
        {
            return null;
        }

        var balance = await tariffService.GetCustomerBalanceAsync(tenant.Id);
        if (balance == null)
        {
            return null;
        }

        // TODO: support other currencies
        var subAccount = balance.SubAccounts.FirstOrDefault(x => x.Currency == "USD");
        if (subAccount == null)
        {
            return null;
        }

        var quantity = new Dictionary<string, int> { { productName, productQty.Value } };

        var result = await tariffService.PaymentCalculateAsync(tenant.Id, quantity, inDto.ProductQuantityType);

        return result;
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
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("account")]
    public async Task<string> GetPaymentAccount(PaymentUrlRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var hasCustomer = await HasCustomer(tenant);
        if (!hasCustomer)
        {
            return null;
        }

        await DemandPayerOrOwnerAsync(tenant);

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
    public async Task<object> GetPortalPrices()
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

        return await tariffHelper.GetQuotasAsync(inDto.Wallet).ToListAsync();
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
    public async Task<QuotaDto> GetQuotaPaymentInformation(PaymentInformationRequestDto inDto)
    {
        if (await userManager.IsGuestAsync(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }
        
        return await tariffHelper.GetCurrentQuotaAsync(inDto.Refresh);
    }

    /// <summary>
    /// Sends a request for the portal payment.
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


    /// <summary>
    /// Returns the URL to the checkout setup page.
    /// </summary>
    /// <short>
    /// Get the checkout setup page URL
    /// </short>
    /// <path>api/2.0/portal/payment/chechoutsetupurl</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The URL to the checkout setup page", typeof(Uri))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("chechoutsetupurl")]
    public async Task<Uri> GetCheckoutSetupUrl(CheckoutSetupUrlRequestsDto inDto)
    {
        await DemandAdminAsync();

        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var hasCustomer = await HasCustomer(tenant);
        if (hasCustomer)
        {
            return null;
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

    /// <summary>
    /// Returns the customer info.
    /// </summary>
    /// <short>
    /// Get the customer info
    /// </short>
    /// <path>api/2.0/portal/payment/customerinfo</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The customer info", typeof(CustomerInfo))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("customerinfo")]
    public async Task<CustomerInfo> GetCustomerInfo(PaymentInformationRequestDto inDto)
    {
        await DemandAdminAsync();

        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var result = await tariffService.GetCustomerInfoAsync(tenant.Id, inDto.Refresh);
        return result;
    }

    /// <summary>
    /// Returns result of putting money on deposit.
    /// </summary>
    /// <short>
    /// Put money on deposit
    /// </short>
    /// <path>api/2.0/portal/payment/deposit</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Success status", typeof(string))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPost("deposit")]
    public async Task<string> TopUpDeposit(TopUpDepositRequestDto inDto)
    {
        if (inDto.Amount <= 0)
        {
            return null;
        }

        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var hasCustomer = await HasCustomer(tenant);
        if (!hasCustomer)
        {
            return null;
        }

        await DemandPayerAsync(tenant);

        var result = await tariffService.TopUpDepositAsync(tenant.Id, inDto.Amount, inDto.Currency, true);

        messageService.Send(MessageAction.CustomerWalletToppedUp);

        return result;
    }

    /// <summary>
    /// Returns the customer balance from the accounting service.
    /// </summary>
    /// <short>
    /// Get the customer balance
    /// </short>
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

        var hasCustomer = await HasCustomer(tenant);
        if (!hasCustomer)
        {
            return null;
        }

        var result = await tariffService.GetCustomerBalanceAsync(tenant.Id, inDto.Refresh);
        return result;
    }

    /// <summary>
    /// Trying to open a customer session and block amount money on the balance.
    /// </summary>
    /// <short>
    /// Open customer session
    /// </short>
    /// <path>api/2.0/portal/payment/customer/opensession</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The customer session", typeof(Session))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPost("customer/opensession")]
    public async Task<Session> OpenCustomerSession(OpenCustomerSessionRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var hasCustomer = await HasCustomer(tenant);
        if (!hasCustomer)
        {
            return null;
        }

        await DemandPayerAsync(tenant);

        var result = await tariffService.OpenCustomerSessionAsync(tenant.Id, inDto.ServiceAccount, inDto.ExternalRef, inDto.Quantity);
        return result;
    }

    /// <summary>
    /// Perform customer operation and return true if the operation is succesfully provided.
    /// </summary>
    /// <short>
    /// Perform customer operation
    /// </short>
    /// <path>api/2.0/portal/payment/customer/performoperation</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "Boolean value: true if the operation is succesfully provided", typeof(bool))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPost("customer/performoperation")]
    public async Task<bool> PerformCustomerOperation(PerformCustomerOperationRequestDto inDto)
    {
        if (!tariffService.IsConfigured())
        {
            return false;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var hasCustomer = await HasCustomer(tenant);
        if (!hasCustomer)
        {
            return false;
        }

        await DemandPayerAsync(tenant);

        var result = await tariffService.PerformCustomerOperationAsync(tenant.Id, inDto.ServiceAccount, inDto.SessionId, inDto.Quantity);

        messageService.Send(MessageAction.CustomerOperationPerformed);

        return result;
    }

    /// <summary>
    /// Returns the report of customer operations from the accounting service.
    /// </summary>
    /// <short>
    /// Get the customer operations
    /// </short>
    /// <path>api/2.0/portal/payment/customer/operations</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The customer operations", typeof(Report))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("customer/operations")]
    public async Task<Report> GetCustomerOperations(CustomerOperationsRequestDto inDto)
    {
        await DemandAdminAsync();

        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var hasCustomer = await HasCustomer(tenant);
        if (!hasCustomer)
        {
            return null;
        }

        var utcStartDate = tenantUtil.DateTimeToUtc(inDto.StartDate);
        var utcEndDate = tenantUtil.DateTimeToUtc(inDto.EndDate);
        var result = await tariffService.GetCustomerOperationsAsync(tenant.Id, utcStartDate, utcEndDate, inDto.Credit, inDto.Withdrawal, inDto.Offset, inDto.Limit);
        return result;
    }

    /// <summary>
    /// Generates the customer operations report as csv file and save in Documents.
    /// </summary>
    /// <short>
    /// Generate the customer operations report
    /// </short>
    /// <path>api/2.0/portal/payment/customer/operationsreport</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "URL to the csv report file", typeof(string))]
    [HttpPost("customer/operationsreport")]
    public async Task<string> CreateCustomerOperationsReport(CustomerOperationsReportDto inDto)
    {
        await DemandAdminAsync();

        if (!tariffService.IsConfigured())
        {
            return null;
        }

        var tenant = tenantManager.GetCurrentTenant();

        var hasCustomer = await HasCustomer(tenant);
        if (!hasCustomer)
        {
            return null;
        }

        inDto = inDto ?? new CustomerOperationsReportDto();

        var utcStartDate = inDto.StartDate != null ? tenantUtil.DateTimeToUtc(inDto.StartDate.Value) : tenant.CreationDateTime;
        var utcEndDate = inDto.EndDate != null ? tenantUtil.DateTimeToUtc(inDto.EndDate.Value) : DateTime.UtcNow;

        var reportName = string.Format(Resource.AccountingCustomerOperationsReportName + ".csv",
            utcStartDate.ToString("MM.dd.yyyy", CultureInfo.InvariantCulture),
            utcEndDate.ToString("MM.dd.yyyy", CultureInfo.InvariantCulture));

        await using var stream = tempStream.Create();

        var partialRecords = GetCustomerOperationsReportDataAsync(tenant.Id, utcStartDate, utcEndDate, inDto.Credit, inDto.Withdrawal);

        await csvFileHelper.CreateLargeFileAsync(stream, partialRecords, new OperationMap());

        var result = await csvFileUploader.UploadFile(stream, reportName);

        messageService.Send(MessageAction.CustomerOperationsReportDownloaded);

        return result;
    }

    private async IAsyncEnumerable<List<Operation>> GetCustomerOperationsReportDataAsync(int tenantId, DateTime utcStartDate, DateTime utcEndDate, bool? credit, bool? withdrawal)
    {
        var offset = 0;
        var limit = 1000;

        while (true)
        {
            var report = await tariffService.GetCustomerOperationsAsync(tenantId, utcStartDate, utcEndDate, credit, withdrawal, offset, limit);

            yield return report.Collection;

            if (report.CurrentPage == report.TotalPage)
            {
                break;
            }

            offset += limit;
        }
    }

    internal class OperationMap : ClassMap<Operation>
    {
        public OperationMap()
        {
            Map(item => item.Date).TypeConverter<CsvFileHelper.CsvDateTimeConverter>();

            Map(item => item.Date).Name(Resource.AccountingCustomerOperationDate);
            Map(item => item.Service).Name(Resource.AccountingCustomerOperationService);
            Map(item => item.ServiceUnit).Name(Resource.AccountingCustomerOperationServiceUnit);
            Map(item => item.Quantity).Name(Resource.AccountingCustomerOperationQuantity);
            Map(item => item.Currency).Name(Resource.AccountingCustomerOperationCurrency);
            Map(item => item.Credit).Name(Resource.AccountingCustomerOperationCredit);
            Map(item => item.Withdrawal).Name(Resource.AccountingCustomerOperationWithdrawal);
        }
    }

    /// <summary>
    /// Returns the list of currencies from accounting service.
    /// </summary>
    /// <short>
    /// Get list of currencies
    /// </short>
    /// <path>api/2.0/portal/payment/accounting/currencies</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The list of currencies", typeof(List<Currency>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("accounting/currencies")]
    public async Task<List<Currency>> GetAllCurrencies()
    {
        if (!tariffService.IsConfigured())
        {
            return null;
        }

        await DemandAdminAsync();

        var result = await tariffService.GetAllCurrenciesAsync();
        return result;
    }

    /// <summary>
    /// Returns the wallet auto top up settings.
    /// </summary>
    /// <short>
    /// Get wallet auto top up settings
    /// </short>
    /// <path>api/2.0/portal/payment/topupsettings</path>
    [Tags("Portal / Payment")]
    [SwaggerResponse(200, "The wallet auto top up settings", typeof(TenantWalletSettings))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("topupsettings")]
    public async Task<TenantWalletSettings> GetTenantWalletSettings()
    {
        var tenant = tenantManager.GetCurrentTenant();

        await DemandAdminAsync();

        var result = await settingsManager.LoadAsync<TenantWalletSettings>();
        return result;
    }

    /// <summary>
    /// Set the wallet auto top up settings.
    /// </summary>
    /// <short>
    /// Set wallet auto top up settings
    /// </short>
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

        var hasCustomer = await HasCustomer(tenant);
        if (!hasCustomer)
        {
            return null;
        }

        var balance = await tariffService.GetCustomerBalanceAsync(tenant.Id, false);
        if (balance == null)
        {
            return null;
        }

        await DemandPayerAsync(tenant);

        var settings = inDto?.Settings ?? new TenantWalletSettings();

        var result = await settingsManager.SaveAsync(settings);

        messageService.Send(MessageAction.CustomerWalletTopUpSettingsUpdated);

        return settings;
    }

    private async Task<bool> HasCustomer(Tenant tenant)
    {
        var hasPayments = (await tariffService.GetPaymentsAsync(tenant.Id)).Any();
        if (hasPayments)
        {
            return true;
        }

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenant.Id);

        return customerInfo != null && customerInfo.PaymentMethodStatus != PaymentMethodStatus.None;
    }

    private async Task DemandAdminAsync()
    {
        if (!await userManager.IsDocSpaceAdminAsync(securityContext.CurrentAccount.ID))
        {
            throw new SecurityException();
        }
    }

    private async Task DemandPayerAsync(Tenant tenant)
    {
        var payerId = (await tariffService.GetTariffAsync(tenant.Id)).CustomerId;

        if (string.IsNullOrEmpty(payerId))
        {
            payerId = (await tariffService.GetCustomerInfoAsync(tenant.Id)).Email;
        }

        var payer = await userManager.GetUserByEmailAsync(payerId);

        if (securityContext.CurrentAccount.ID != payer.Id)
        {
            throw new SecurityException($"payerEmail {payerId}, payerId {payer.Id}, currentId {securityContext.CurrentAccount.ID}");
        }
    }

    private async Task DemandPayerOrOwnerAsync(Tenant tenant)
    {
        var payerId = (await tariffService.GetTariffAsync(tenant.Id)).CustomerId;

        if (string.IsNullOrEmpty(payerId))
        {
            payerId = (await tariffService.GetCustomerInfoAsync(tenant.Id)).Email;
        }

        var payer = await userManager.GetUserByEmailAsync(payerId);

        if (securityContext.CurrentAccount.ID != payer.Id &&
            securityContext.CurrentAccount.ID != tenant.OwnerId)
        {
            throw new SecurityException();
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
}
