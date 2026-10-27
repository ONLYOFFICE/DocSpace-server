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

namespace ASC.Core.Billing;

[Scope]
public class AccountingClient(IOptions<AccountingConfiguration> configuration, ICache cache, IFusionCache hybridCache, IAccountingApi accountingApi)
{
    private static readonly TimeSpan _servicePricesCacheDuration = TimeSpan.FromHours(1);

    // Prices are kept in Redis; the copy in process memory lives shorter, so a price key deleted in Redis
    // reaches every service within this time, without a restart.
    private static readonly TimeSpan _priceMemoryCacheDuration = TimeSpan.FromMinutes(5);
    private const string ApprovedServiceStatus = "Approved";
    private const string DefaultCurrencyCode = "USD";
    private const int ServicePageSize = 100;

    public bool Configured { get => !string.IsNullOrEmpty(configuration.Value.Url); }

    public async Task<Balance> GetCustomerBalanceAsync(string portalId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        return await accountingApi.GetCustomerBalanceAsync(portalId, cancellationToken);
    }

    public async Task<Session> OpenCustomerSessionAsync(string portalId, string serviceName, string externalRef,
        int quantity, int duration)
    {
        EnsureConfigured();

        return await accountingApi.OpenCustomerSessionAsync(new SessionOpenOperation(portalId, serviceName, externalRef, quantity, duration));
    }

    public async Task CloseCustomerSessionAsync(int sessionId)
    {
        EnsureConfigured();

        await accountingApi.CloseCustomerSessionAsync(sessionId);
    }

    public async Task<Session> ExtendCustomerSessionAsync(int sessionId, int duration)
    {
        EnsureConfigured();

        return await accountingApi.ExtendCustomerSessionAsync(sessionId, duration);
    }

    public async Task CompleteCustomerSessionAsync(string portalId, string serviceName, int sessionId, int quantity,
        string customerParticipantName, Dictionary<string, string> metadata = null)
    {
        EnsureConfigured();

        await accountingApi.CompleteCustomerSessionAsync(new SessionCompleteOperation(portalId, serviceName, sessionId, quantity,
            customerParticipantName, metadata));
    }

    /// <summary>
    /// Credits a bonus to the customer's wallet at once, without the approval a manual operation needs, and returns
    /// the completed operation. <paramref name="externalRef"/> is our reference of the bonus, and
    /// <paramref name="utcExpiresAt"/> the moment the bonus expires, or <c>null</c> for one that does not. A credit the
    /// accounting service refuses as conflicting throws <see cref="AccountingConflictException"/>. The request is
    /// never retried, so the caller decides what a failure means; the cached balance of the portal is not touched here.
    /// </summary>
    public async Task<OperationResult> CreditBonusAsync(string portalId, decimal sum, string currency, string externalRef,
        DateTime? utcExpiresAt = null, Dictionary<string, string> metadata = null)
    {
        EnsureConfigured();

        return await accountingApi.CreditBonusAsync(new BonusCreditOperation(portalId, externalRef, utcExpiresAt, sum, currency, metadata));
    }

    public async Task<Report> GetCustomerOperationsAsync(string portalId, OperationFilter filter)
    {
        EnsureConfigured();

        return await accountingApi.GetCustomerOperationsAsync(portalId, filter);
    }

    public async Task<List<CustomerMonthlyUsage>> GetCustomerMonthlyUsageAsync(string portalId, MonthlyUsageFilter filter)
    {
        EnsureConfigured();

        return await accountingApi.GetCustomerMonthlyUsageAsync(portalId, filter);
    }

    public async Task<UsageReport> GetCustomerServiceUsageAsync(string portalId, UsageFilter filter)
    {
        EnsureConfigured();

        return await accountingApi.GetCustomerServiceUsageAsync(portalId, filter);
    }

    /// <summary>
    /// Every operation matching the filter, one page per request; the page size is <see cref="PagedFilter.Limit"/>,
    /// <see cref="PagedFilter.Offset"/> must stay unset. Requests are sent while the result is enumerated.
    /// </summary>
    /// <exception cref="ArgumentException"><see cref="PagedFilter.Offset"/> is set.</exception>
    public PagedEnumerable<Report, Operation> GetCustomerOperationPages(string portalId, OperationFilter filter)
    {
        EnsureConfigured();
        EnsureNoOffset(filter);

        return accountingApi.GetCustomerOperationPages(portalId, filter);
    }

    /// <summary>
    /// Every service usage record matching the filter, paged as in <see cref="GetCustomerOperationPages"/>.
    /// </summary>
    /// <exception cref="ArgumentException"><see cref="PagedFilter.Offset"/> is set.</exception>
    public PagedEnumerable<UsageReport, CustomerServiceUsage> GetCustomerServiceUsagePages(string portalId, UsageFilter filter)
    {
        EnsureConfigured();
        EnsureNoOffset(filter);

        return accountingApi.GetCustomerServiceUsagePages(portalId, filter);
    }

    public async Task<DepositReport> GetCustomerDepositsAsync(string portalId, DepositFilter filter)
    {
        EnsureConfigured();

        return await accountingApi.GetCustomerDepositsAsync(portalId, filter);
    }

    /// <summary>
    /// Every balance top-up matching the filter, paged as in <see cref="GetCustomerOperationPages"/>.
    /// </summary>
    /// <exception cref="ArgumentException"><see cref="PagedFilter.Offset"/> is set.</exception>
    public PagedEnumerable<DepositReport, Deposit> GetCustomerDepositPages(string portalId, DepositFilter filter)
    {
        EnsureConfigured();
        EnsureNoOffset(filter);

        return accountingApi.GetCustomerDepositPages(portalId, filter);
    }

    // The page offset travels in a query parameter of its own, so a filter offset would put a second "offset"
    // into every request and leave it to the accounting service which one wins.
    private static void EnsureNoOffset(PagedFilter filter)
    {
        if (filter?.Offset != null)
        {
            throw new ArgumentException($"{nameof(PagedFilter.Offset)} must be unset for the paged methods.", nameof(filter));
        }
    }

    public async Task<List<Currency>> GetAllCurrenciesAsync()
    {
        var key = "accounting-currencies";
        var result = cache.Get<List<Currency>>(key);
        if (result == null)
        {
            EnsureConfigured();

            result = await accountingApi.GetAllCurrenciesAsync();
            cache.Insert(key, result, DateTime.Now.AddDays(1));
        }
        return result;
    }

    public List<string> GetSupportedCurrencies()
    {
        return configuration.Value.Currencies;
    }

    /// <summary>
    /// Returns the prices of the service. The accounting service addresses prices by service ID, so the name is
    /// resolved first; the result is not tenant-specific and is cached for all portals.
    /// </summary>
    public async Task<List<ServicePriceInfo>> GetServicePricesAsync(string serviceName, bool active = false)
    {
        EnsureConfigured();

        return await hybridCache.GetOrSetAsync<List<ServicePriceInfo>>(GetServicePricesCacheKey(serviceName, active), async (_, _) =>
        {
            var serviceInfo = await accountingApi.GetServiceInfoAsync(serviceName);
            if (serviceInfo == null)
            {
                return [];
            }

            return await accountingApi.GetServicePricesAsync(serviceInfo.Id, active);
        }, opt => opt.SetDuration(_servicePricesCacheDuration).SetMemoryCacheDuration(_priceMemoryCacheDuration));
    }

    public async Task<ServiceLimit> GetServiceLimitAsync(int id)
    {
        EnsureConfigured();

        return await accountingApi.GetServiceLimitAsync(id);
    }

    /// <summary>
    /// The limit of the customer as a whole on the service group, with its spending in the current period when
    /// <paramref name="includeUsage"/> is set. Throws <see cref="AccountingNotFoundException"/> when there is none.
    /// </summary>
    public async Task<ServiceLimit> GetTotalServiceLimitAsync(string portalId, string groupName, bool includeUsage)
    {
        EnsureConfigured();

        return await accountingApi.GetTotalServiceLimitAsync(portalId, groupName, includeUsage);
    }

    /// <summary>
    /// The limit for every participant of the customer on the service group, without spending: that is counted per
    /// participant. Throws <see cref="AccountingNotFoundException"/> when there is none.
    /// </summary>
    public async Task<ServiceLimit> GetGlobalServiceLimitAsync(string portalId, string groupName)
    {
        EnsureConfigured();

        return await accountingApi.GetGlobalServiceLimitAsync(portalId, groupName);
    }

    public async Task<ServiceLimitReport> GetParticipantServiceLimitsAsync(string portalId, string groupName, ServiceLimitFilter filter)
    {
        EnsureConfigured();

        return await accountingApi.GetParticipantServiceLimitsAsync(portalId, groupName, filter);
    }

    /// <summary>
    /// The limit in force for every participant of the customer on the service group - the personal one, or else the
    /// one for every participant - one page per request; the page size is <see cref="ServiceLimitFilter.Limit"/>,
    /// <see cref="ServiceLimitFilter.Offset"/> must stay unset. Requests are sent while the result is enumerated.
    /// </summary>
    /// <exception cref="ArgumentException"><see cref="ServiceLimitFilter.Offset"/> is set.</exception>
    public PagedEnumerable<ServiceLimitReport, ServiceLimit> GetParticipantServiceLimitPages(string portalId, string groupName, ServiceLimitFilter filter)
    {
        EnsureConfigured();

        // The page offset travels in a query parameter of its own, as in EnsureNoOffset.
        if (filter?.Offset != null)
        {
            throw new ArgumentException($"{nameof(ServiceLimitFilter.Offset)} must be unset for the paged methods.", nameof(filter));
        }

        return accountingApi.GetParticipantServiceLimitPages(portalId, groupName, filter);
    }

    public async Task<ServiceLimit> CreateServiceLimitAsync(string portalId, string groupName, ServiceLimitScope scope, string customerParticipantName,
        decimal? amountValue, int? quantityValue, ServiceLimitPeriod period)
    {
        EnsureConfigured();

        return await accountingApi.CreateServiceLimitAsync(new ServiceLimitCreateOperation(portalId, groupName, scope, customerParticipantName,
            amountValue, quantityValue, period));
    }

    public async Task<ServiceLimit> UpdateServiceLimitAsync(int id, decimal? amountValue, int? quantityValue, bool? enabled)
    {
        EnsureConfigured();

        return await accountingApi.UpdateServiceLimitAsync(new ServiceLimitUpdateOperation(id, amountValue, quantityValue, enabled));
    }

    /// <summary>
    /// Sets the same personal limit for each of the participants, replacing the personal limits they already have and
    /// switching them on, and returns the number of limits set.
    /// </summary>
    public async Task<int> SetParticipantServiceLimitsAsync(string portalId, string groupName, List<string> customerParticipantNames,
        decimal? amountValue, int? quantityValue, ServiceLimitPeriod period)
    {
        EnsureConfigured();

        return await accountingApi.SetParticipantServiceLimitsAsync(new ServiceLimitParticipantSetOperation(portalId, groupName,
            customerParticipantNames, amountValue, quantityValue, period));
    }

    /// <summary>
    /// Switches off every personal limit of the participants of the customer on the service group, leaving the limit of
    /// the customer as a whole and the one for every participant as they are, and returns the number of limits switched
    /// off.
    /// </summary>
    public async Task<int> DisableParticipantServiceLimitsAsync(string portalId, string groupName)
    {
        EnsureConfigured();

        return await accountingApi.DisableParticipantServiceLimitsAsync(portalId, groupName);
    }

    public async Task DeleteServiceLimitAsync(int id)
    {
        EnsureConfigured();

        await accountingApi.DeleteServiceLimitAsync(id);
    }

    private static string GetServicePricesCacheKey(string serviceName, bool active)
    {
        return $"accounting-service-prices-{serviceName}-{active}";
    }

    /// <summary>
    /// Returns the unit price of each named service in the currency of the service, as service name to currency code
    /// to price; a service that is not among the approved ones is left out. All the names go in one request (one per
    /// page of the answer). Not cached here: the caller caches the price of every service on its own.
    /// </summary>
    public async Task<Dictionary<string, Dictionary<string, decimal>>> GetProductPriceInfoAsync(List<string> serviceNames)
    {
        EnsureConfigured();

        var result = new Dictionary<string, Dictionary<string, decimal>>();

        if (serviceNames.Count == 0)
        {
            return result;
        }

        var requested = serviceNames.ToHashSet();

        await foreach (var page in accountingApi.GetServicePages(ApprovedServiceStatus, requested, ServicePageSize).AsPages())
        {
            // An accounting service that does not filter by names yet answers with every approved service.
            foreach (var service in (page.Collection ?? []).Where(s => requested.Contains(s.Name)))
            {
                result[service.Name] = new Dictionary<string, decimal>
                {
                    { service.Currency?.Code ?? DefaultCurrencyCode, service.PriceValue }
                };
            }
        }

        return result;
    }

    private void EnsureConfigured()
    {
        if (!Configured)
        {
            throw new AccountingNotConfiguredException();
        }
    }
}

/// <summary>
/// The payment method status.
/// </summary>
public enum PaymentMethodStatus
{
    [Description("None")]
    None,
    [Description("Set")]
    Set,
    [Description("Expired")]
    Expired
}

/// <summary>
/// The operation filtering order type
/// </summary>
public enum OperationOrderType
{
    [Description("Descending")]
    Descending,
    [Description("Ascending")]
    Ascending
}

/// <summary>
/// The operation type
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OperationType
{
    [Description("Unknown")]
    Unknown,
    [Description("ServicePayment")]
    ServicePayment,
    [Description("PackagePayment")]
    PackagePayment,
    [Description("AiServicePayment")]
    AiServicePayment,
    [Description("Deposit")]
    Deposit,
    [Description("ReceiveProviderInvoice")]
    ReceiveProviderInvoice,
    [Description("ProcessProviderInvoice")]
    ProcessProviderInvoice,
    [Description("WriteOffServiceProfit")]
    WriteOffServiceProfit,
    [Description("Profit")]
    Profit,
    [Description("PartnerAccrual")]
    PartnerAccrual,
    [Description("ProviderPayment")]
    ProviderPayment,
    [Description("PartnerPayment")]
    PartnerPayment,
    [Description("Refund")]
    Refund,
    [Description("BankDeposit")]
    BankDeposit,
    [Description("BankWithdrawal")]
    BankWithdrawal,
    [Description("GoodwillCredit")]
    GoodwillCredit,
    [Description("WriteOffProfit")]
    WriteOffProfit,
    [Description("WriteOffDifferenceCurrency")]
    WriteOffDifferenceCurrency,
    [Description("AiDebit")]
    AiDebit,
    [Description("AiCredit")]
    AiCredit
}

/// <summary>
/// The operation status
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OperationStatus
{
    [Description("Pending")]
    Pending,
    [Description("Completed")]
    Completed,
    [Description("Rejected")]
    Rejected,
    [Description("Canceled")]
    Canceled
}

public class DateRangeFilter
{
    /// <summary>
    /// The start date of the period to filter from (inclusive).
    /// </summary>
    [AliasAs("startDate")]
    [Query(Format = "o")]
    public DateTime? UtcStartDate { get; init; }

    /// <summary>
    /// The end date of the period to filter until (inclusive).
    /// </summary>
    [AliasAs("endDate")]
    [Query(Format = "o")]
    public DateTime? UtcEndDate { get; init; }
}

public class PagedFilter : DateRangeFilter
{
    /// <summary>
    /// The number of items to skip before starting to return results. Used for pagination.
    /// </summary>
    /// <remarks>
    /// For a single page only. Leave it unset for the paged methods (<see cref="AccountingClient.GetCustomerOperationPages"/>,
    /// <see cref="AccountingClient.GetCustomerServiceUsagePages"/>): they send the offset in a parameter of their own.
    /// </remarks>
    public int? Offset { get; set; }

    /// <summary>
    /// The maximum number of items to return in the response.
    /// </summary>
    /// <remarks>For the paged methods this is the page size of every request.</remarks>
    public int? Limit { get; set; }

    /// <summary>
    /// The field to order by.
    /// </summary>
    public string OrderBy
    {
        get;
        init => field = value?.Trim();
    }

    /// <summary>
    /// Order direction: ASC or DESC.
    /// </summary>
    /// <remarks>
    /// Descending is the server-side default, so it is normalized to <c>null</c> here:
    /// an explicit Descending and an unspecified value produce the same request (no orderType param).
    /// </remarks>
    public OperationOrderType? OrderType
    {
        get;
        init => field = value is OperationOrderType.Descending ? null : value;
    }
}

/// <summary>
/// Represents an object for filtering the list of customer operations.
/// </summary>
public class OperationFilter : PagedFilter
{
    /// <summary>
    /// The service name list.
    /// </summary>
    [Query(CollectionFormat.Multi)]
    public List<string> ServiceName { get; init; }

    /// <summary>
    /// Unique name of customer participant to filter by.
    /// </summary>
    public string ParticipantName
    {
        get;
        init => field = value?.Trim();
    }
    /// <summary>
    /// Whether to include credit operations.
    /// </summary>
    public bool? Credit { get; init; }
    /// <summary>
    /// Whether to include debit operations.
    /// </summary>
    public bool? Debit { get; init; }
    /// <summary>
    /// The operation type to filter by.
    /// </summary>
    [AliasAs("types")]
    public OperationType? Type { get; init; }
    /// <summary>
    /// The operation status to filter by.
    /// </summary>
    public OperationStatus? Status { get; init; }
}

/// <summary>
/// Represents an object for filtering the list of monthly usage statistics.
/// </summary>
public class MonthlyUsageFilter : DateRangeFilter
{
    public string TimeZome { get; init; }
}

/// <summary>
/// The filter for customer service usage statistics.
/// </summary>
public class UsageFilter : PagedFilter
{
    /// <summary>
    /// The service name list.
    /// </summary>
    [Query(CollectionFormat.Multi)]
    public List<string> ServiceName { get; init; }
    /// <summary>
    /// Unique name of customer participant to filter by.
    /// </summary>
    public string ParticipantName
    {
        get;
        init => field = value?.Trim();
    }
    /// <summary>
    /// The operation status to filter by.
    /// </summary>
    public OperationStatus? Status { get; init; }
    /// <summary>
    /// Metadata key-value pairs to filter by.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; }
}

/// <summary>
/// The filter for customer balance top-ups.
/// </summary>
public class DepositFilter : PagedFilter
{
    /// <summary>
    /// The operation status to filter by.
    /// </summary>
    public OperationStatus? Status { get; init; }
}

/// <summary>
/// The customer information.
/// </summary>
public class CustomerInfo
{
    /// <summary>
    /// The portal ID.
    /// </summary>
    public string PortalId { get; init; }

    /// <summary>
    /// The customer's payment method.
    /// </summary>
    public PaymentMethodStatus PaymentMethodStatus { get; init; }

    /// <summary>
    /// The payment method type, e.g. "card".
    /// </summary>
    public string PaymentMethodType { get; init; }

    /// <summary>
    /// Indicates whether the payment method is delayed, i.e. a deposit is accepted immediately but the money
    /// reaches the wallet only once the transfer settles. Such a wallet cannot be topped up automatically.
    /// </summary>
    public bool IsDelayedPaymentMethod { get; init; }

    /// <summary>
    /// The email address of the customer.
    /// </summary>
    public string Email { get; init; }

    public bool IsDefault()
    {
        return PortalId == null && PaymentMethodStatus == PaymentMethodStatus.None && Email == null;
    }
}

/// <summary>
/// Represents a balance with an account number and a list of sub-accounts.
/// </summary>
public class Balance
{
    /// <summary>
    /// The account number.
    /// </summary>
    /// <example>12345</example>
    public int AccountNumber { get; init; }
    /// <summary>
    /// The sub-account number.
    /// </summary>
    /// <example>12345</example>
    public int SubAccountNumber { get; init; }
    /// <summary>
    /// The account name.
    /// </summary>
    /// <example>account name</example>
    public string AccountName { get; init; }
    /// <summary>
    /// The account currency.
    /// </summary>
    /// <example>"USD"</example>
    public string AccountCurrency { get; init; }
    /// <summary>
    /// A list of sub-accounts.
    /// </summary>
    /// <example>[{"currency": "USD", "amount": 1500.75}]</example>
    public List<SubAccount> SubAccounts { get; init; }
    /// <summary>
    /// The most recent credit transaction applied to the account.
    /// </summary>
    /// <example>{"Date": "2024-01-15T10:30:00Z", "currency": "USD", "amount": 1500.75}</example>
    /// {
    ///   "date": "2024-01-01T00:00:00Z",
    ///   "currency": "USD",
    ///   "amount": 1500.75
    /// }
    public TransactionInfo LastCredit { get; init; }

    public bool IsDefault()
    {
        return AccountNumber == 0 && SubAccounts == null;
    }
}

/// <summary>
/// The currency an amount is expressed in.
/// </summary>
public class CurrencyCode
{
    /// <summary>
    /// The three-character ISO 4217 currency symbol.
    /// </summary>
    /// <example>"USD"</example>
    public string Currency { get; init; }
}

/// <summary>
/// An amount of money together with its currency.
/// </summary>
public class CurrencyAmount : CurrencyCode
{
    /// <summary>
    /// The amount in the specified currency.
    /// </summary>
    /// <example>1500.75</example>
    public decimal Amount { get; init; }
}

public class CurrencyTotalAmount : CurrencyCode
{
    /// <summary>
    /// The total amount in the specified currency.
    /// </summary>
    /// <example>1500.75</example>
    public decimal TotalAmount { get; init; }
}

/// <summary>
/// Represents information about the transaction applied to an account.
/// </summary>
public class TransactionInfo : CurrencyAmount
{
    /// <summary>
    /// The date and time when the credit transaction occurred.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime Date { get; init; }
}

/// <summary>
/// Represents a sub-account with a specific currency and balance.
/// </summary>
public class SubAccount : CurrencyAmount
{
}

/// <summary>
/// Represents a session with reserved amount and currency.
/// </summary>
public class Session
{
    /// <summary>
    /// Unique identifier of the session.
    /// </summary>
    public int SessionId { get; init; }

    /// <summary>
    /// Amount reserved for the session.
    /// </summary>
    public decimal ReservedAmount { get; init; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol of the reserved amount.
    /// </summary>
    public string Currency { get; init; }

    /// <summary>
    /// The expiration date of the session.
    /// </summary>
    public DateTime Expire { get; init; }
}

/// <summary>
/// The result of an operation the accounting service completed at once.
/// </summary>
public class OperationResult : CurrencyAmount
{
    /// <summary>
    /// The ID of the operation in the accounting service.
    /// </summary>
    public long OperationId { get; init; }

    /// <summary>
    /// The quantity of the service the operation covers.
    /// </summary>
    public int Quantity { get; init; }
}

/// <summary>
/// Represents service information.
/// </summary>
public class ServiceInfo
{
    /// <summary>
    /// The service ID.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// The price value.
    /// </summary>
    public decimal PriceValue { get; init; }

    /// <summary>
    /// The currency ID.
    /// </summary>
    public int CurrencyId { get; init; }

    /// <summary>
    /// The currency of the price value.
    /// </summary>
    public Currency Currency { get; init; }

    /// <summary>
    /// The name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The account number.
    /// </summary>
    public int AccountNumber { get; init; }

    /// <summary>
    /// The service period.
    /// </summary>
    public PriceTimeUnit ServicePeriod { get; init; }
}

/// <summary>
/// A unit of time of the accounting service: the time unit a price is bound to, and the calendar period a service
/// limit is reset with. Numbered as the accounting service numbers it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PriceTimeUnit
{
    /// <summary>
    /// No time unit: a price charged per use rather than per period, or a limit with no calendar period.
    /// </summary>
    [Description("None")]
    None = 1,

    /// <summary>One hour.</summary>
    [Description("Hour")]
    Hour = 2,

    /// <summary>One day.</summary>
    [Description("Day")]
    Day = 3,

    /// <summary>One week.</summary>
    [Description("Week")]
    Week = 4,

    /// <summary>One month.</summary>
    [Description("Month")]
    Month = 5,

    /// <summary>One year.</summary>
    [Description("Year")]
    Year = 6,

    /// <summary>Three years.</summary>
    [Description("ThreeYears")]
    ThreeYears = 7,

    /// <summary>One minute.</summary>
    [Description("Minute")]
    Minute = 8
}

/// <summary>
/// The price status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PriceStatus
{
    [Description("Draft")]
    Draft,
    [Description("Approved")]
    Approved,
    [Description("Rejected")]
    Rejected
}

/// <summary>
/// Represents a price of the service.
/// </summary>
public class ServicePriceInfo
{
    /// <summary>
    /// The price unique identifier.
    /// </summary>
    /// <example>12345</example>
    public int Id { get; init; }

    /// <summary>
    /// The account number.
    /// </summary>
    /// <example>1010</example>
    public int AccountNumber { get; init; }

    /// <summary>
    /// The service ID.
    /// </summary>
    /// <example>12345</example>
    public int ServiceId { get; init; }

    /// <summary>
    /// The time unit the price is bound to.
    /// </summary>
    /// <example>None</example>
    public PriceTimeUnit TimeUnit { get; init; }

    /// <summary>
    /// The cost price.
    /// </summary>
    /// <example>1500.75</example>
    public decimal CostPrice { get; init; }

    /// <summary>
    /// The extra charge added to the cost price.
    /// </summary>
    /// <example>1500.75</example>
    public decimal ExtraCharge { get; init; }

    /// <summary>
    /// The resulting service price.
    /// </summary>
    /// <example>1500.75</example>
    public decimal ServicePrice { get; init; }

    /// <summary>
    /// The quota the price is set for.
    /// </summary>
    /// <example>100</example>
    public double? Quota { get; init; }

    /// <summary>
    /// The period the price is effective in.
    /// </summary>
    public TimeBound TimeBound { get; init; }

    /// <summary>
    /// The price status.
    /// </summary>
    /// <example>Draft</example>
    public PriceStatus Status { get; init; }

    /// <summary>
    /// The date and time when the price was created.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime Created { get; init; }

    /// <summary>
    /// The discount category ID.
    /// </summary>
    /// <example>12345</example>
    public int? DiscountCategoryId { get; init; }

    /// <summary>
    /// The discount category.
    /// </summary>
    public DiscountCategory DiscountCategory { get; init; }
}

/// <summary>
/// Represents the period the price is effective in.
/// </summary>
public class TimeBound
{
    /// <summary>
    /// The date and time when the period starts.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// The date and time when the period ends.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? EndDate { get; init; }
}

/// <summary>
/// Represents a discount category applied to the price.
/// </summary>
public class DiscountCategory
{
    /// <summary>
    /// The discount category unique identifier.
    /// </summary>
    /// <example>12345</example>
    public int Id { get; init; }

    /// <summary>
    /// The discount value.
    /// </summary>
    /// <example>10.5</example>
    public decimal ValueDiscount { get; init; }

    /// <summary>
    /// The discount category description.
    /// </summary>
    /// <example>Annual subscription discount</example>
    public string Description { get; init; }

    /// <summary>
    /// The date and time when the discount category was created.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime Created { get; init; }
}

public class BaseReport<T>
{
    /// <summary>
    /// Collection of report items.
    /// </summary>
    public List<T> Collection { get; init; }

    /// <summary>
    /// Offset of the report data.
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    /// Limit of the report data.
    /// </summary>
    public int Limit { get; init; }

    /// <summary>
    /// Total number of pages in the report.
    /// </summary>
    public int TotalPage { get; init; }

    /// <summary>
    /// Current page number of the report.
    /// </summary>
    public int CurrentPage { get; init; }

    /// <summary>
    /// Total quantity of records in the report.
    /// </summary>
    public int TotalQuantity { get; init; }
}

/// <summary>
/// Represents a report containing a collection of operations.
/// </summary>
public class Report : BaseReport<Operation>
{
    public async Task<Dictionary<string, string>> GetParticipantDisplayNamesAsync(DisplayUserSettingsHelper displayUserSettingsHelper, bool withHtmlEncode)
    {
        var participantDisplayNames = new Dictionary<string, string>();

        foreach (var operation in Collection)
        {
            if (string.IsNullOrEmpty(operation.ParticipantName) || participantDisplayNames.ContainsKey(operation.ParticipantName))
            {
                continue;
            }

            if (Guid.TryParse(operation.ParticipantName, out var userId))
            {
                var participantDisplayName = await displayUserSettingsHelper.GetFullUserNameAsync(userId, withHtmlEncode, false);
                participantDisplayNames.Add(operation.ParticipantName, participantDisplayName);
            }
        }

        return participantDisplayNames;
    }
}

/// <summary>
/// Aggregated customer spending for a single calendar month.
/// </summary>
public class CustomerMonthlyUsage : CurrencyTotalAmount
{
    /// <summary>
    /// Calendar year (e.g. 2025).
    /// </summary>
    public int Year { get; init; }

    /// <summary>
    /// Calendar month (1-12).
    /// </summary>
    public int Month { get; init; }

    /// <summary>
    /// Number of individual purchase operations in this month.
    /// </summary>
    public int OperationCount { get; init; }
}

/// <summary>
/// Aggregated customer usage statistics for a service over a period.
/// </summary>
public class CustomerServiceUsage : CurrencyTotalAmount
{
    /// <summary>
    /// Name of the service.
    /// </summary>
    public string Service { get; init; }

    /// <summary>
    /// Unit of measurement for the service (e.g. requests, GB, hours).
    /// </summary>
    public string ServiceUnit { get; init; }

    /// <summary>
    /// Total number of units consumed.
    /// </summary>
    public int TotalQuantity { get; init; }

    /// <summary>
    /// Number of individual purchase operations.
    /// </summary>
    public int OperationCount { get; init; }
}

/// <summary>
/// Represents a paged report of customer service usage statistics.
/// </summary>
public class UsageReport : BaseReport<CustomerServiceUsage>
{
}

/// <summary>
/// Represents a page of the accounting service catalog.
/// </summary>
public class ServiceReport : BaseReport<ServiceInfo>
{
}

/// <summary>
/// Represents a paged report of customer balance top-ups.
/// </summary>
public class DepositReport : BaseReport<Deposit>
{
}

/// <summary>
/// Represents a customer balance top-up with the reference of the payment behind it.
/// </summary>
public class Deposit
{
    /// <summary>
    /// The operation ID.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// The date of the top-up.
    /// </summary>
    public DateTime Date { get; init; }

    /// <summary>
    /// The top-up amount.
    /// </summary>
    public decimal Sum { get; init; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol of the amount.
    /// </summary>
    public string Currency { get; init; }

    /// <summary>
    /// The operation status.
    /// </summary>
    public OperationStatus Status { get; init; }

    /// <summary>
    /// The name of the bank (payment system) that received the payment.
    /// </summary>
    public string BankName { get; init; }

    /// <summary>
    /// The external payment reference passed with the top-up.
    /// </summary>
    /// <remarks>A Stripe payment intent (<c>pi_…</c>) or invoice (<c>in_…</c>) has an invoice behind it.</remarks>
    public string PaymentRef { get; init; }

    /// <summary>
    /// The additional metadata of the top-up.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; }

    /// <summary>
    /// Whether the billing service can give an invoice link for the payment behind the top-up.
    /// </summary>
    [JsonIgnore]
    public bool HasInvoice
    {
        get => PaymentRef != null &&
               (PaymentRef.StartsWith("pi_", StringComparison.Ordinal) || PaymentRef.StartsWith("in_", StringComparison.Ordinal));
    }
}

/// <summary>
/// Represents an operation.
/// </summary>
public class Operation
{
    /// <summary>
    /// Date of the operation.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime Date { get; set; }

    /// <summary>
    /// Service related to the operation.
    /// </summary>
    /// <example>backup</example>
    public string Service { get; set; }

    /// <summary>
    /// Brief description of the operation.
    /// </summary>
    /// <example>Backup</example>
    public string Description { get; set; }

    /// <summary>
    /// Brief details of the operation.
    /// </summary>
    /// <example>Automatic backup</example>
    public string Details { get; set; }

    /// <summary>
    /// Unit of the service.
    /// </summary>
    /// <example>GB</example>
    public string ServiceUnit { get; set; }

    /// <summary>
    /// Quantity of the service used.
    /// </summary>
    /// <example>1</example>
    public int Quantity { get; set; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol of the operation.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; set; }

    /// <summary>
    /// Credit amount of the operation.
    /// </summary>
    /// <example>1500.75</example>
    public decimal Credit { get; set; }

    /// <summary>
    /// Debit amount of the operation.
    /// </summary>
    /// <example>1500.75</example>
    public decimal Debit { get; set; }

    /// <summary>
    /// Original name of the participant.
    /// </summary>
    /// <example>My Own Corporation</example>
    public string ParticipantName { get; set; }

    /// <summary>
    /// Type of the operation
    /// </summary>
    /// <example>Unknown</example>
    public OperationType Type { get; set; }

    /// <summary>
    /// Display name of the participant.
    /// </summary>
    /// <example>My Own Corporation</example>
    public string ParticipantDisplayName { get; set; }

    /// <summary>
    /// Id of the entity the AI operation was performed on.
    /// </summary>
    /// <example>123</example>
    public string SourceId { get; set; }

    /// <summary>
    /// Type of the entity the AI operation was performed on: Agent, File, Folder, Room or Form.
    /// </summary>
    /// <example>Agent</example>
    public string SourceType { get; set; }

    /// <summary>
    /// Name of the entity the AI operation was performed on.
    /// </summary>
    /// <example>My AI Agent</example>
    public string SourceTitle { get; set; }

    /// <summary>
    /// Tokens consumed by the AI operation, broken down by kind.
    /// </summary>
    public OperationTokenUsage TokenUsage { get; set; }

    /// <summary>
    /// Metadata of the operation.
    /// </summary>
    /// <example>{}</example>
    public Dictionary<string, string> Metadata { get; set; }
}

/// <summary>
/// Tokens an AI operation consumed, as recorded in the operation metadata. A kind the provider did not report is `0`.
/// </summary>
public class OperationTokenUsage
{
    /// <summary>
    /// All tokens of the request: prompt plus completion.
    /// </summary>
    /// <example>20747</example>
    public long TotalTokens { get; set; }

    /// <summary>
    /// Tokens sent to the model, cached ones included.
    /// </summary>
    /// <example>19332</example>
    public long PromptTokens { get; set; }

    /// <summary>
    /// Tokens the model generated, reasoning ones included.
    /// </summary>
    /// <example>1415</example>
    public long CompletionTokens { get; set; }

    /// <summary>
    /// Part of the prompt tokens read from the provider cache.
    /// </summary>
    /// <example>19226</example>
    public long CachedTokens { get; set; }

    /// <summary>
    /// Part of the prompt tokens written to the provider cache.
    /// </summary>
    /// <example>104</example>
    public long CacheWriteTokens { get; set; }

    /// <summary>
    /// Part of the completion tokens the model spent on reasoning.
    /// </summary>
    /// <example>68</example>
    public long ReasoningTokens { get; set; }

    /// <summary>
    /// Tokens spent on images.
    /// </summary>
    /// <example>0</example>
    public long ImageTokens { get; set; }
}

/// <summary>
/// Represents a currency.
/// </summary>
public class Currency
{
    /// <summary>
    /// The currency unique identifier.
    /// </summary>
    /// <example>12345</example>
    public int Id { get; init; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol.
    /// </summary>
    /// <example>USD</example>
    public string Code { get; init; }
}

/// <summary>
/// The field the service limits in force for the portal users are sorted by.
/// </summary>
public enum ServiceLimitOrderBy
{
    /// <summary>
    /// The participant the limit is set for.
    /// </summary>
    [Description("Participant")]
    Participant,

    /// <summary>
    /// The date and time the limit was created.
    /// </summary>
    [Description("Created")]
    Created
}

/// <summary>
/// The paging and ordering of the service limits in force for the portal users.
/// </summary>
public class ServiceLimitFilter
{
    /// <summary>
    /// The number of items to skip before starting to return results. Used for pagination.
    /// </summary>
    public int? Offset { get; init; }

    /// <summary>
    /// The maximum number of items to return in the response.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// The field to order by.
    /// </summary>
    public ServiceLimitOrderBy? OrderBy { get; init; }

    /// <summary>
    /// Order direction: ASC or DESC.
    /// </summary>
    /// <remarks>
    /// Descending is the server-side default, so it is normalized to <c>null</c> here:
    /// an explicit Descending and an unspecified value produce the same request (no orderType param).
    /// </remarks>
    public OperationOrderType? OrderType
    {
        get;
        init => field = value is OperationOrderType.Descending ? null : value;
    }

    /// <summary>
    /// Whether every limit comes with its spending in the current period. The accounting service attaches it when
    /// this is omitted.
    /// </summary>
    public bool? IncludeUsage { get; init; }
}

/// <summary>
/// The service groups the accounting service keeps spending limits for. A limit is always set on a group, never on a
/// single service of it.
/// </summary>
public static class ServiceLimitGroups
{
    /// <summary>
    /// The AI services of the portal as one group: AI tools and AI search.
    /// </summary>
    public const string AiServices = "ai-services";
}

/// <summary>
/// Whom a service limit applies to. Numbered as the accounting service numbers it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServiceLimitScope
{
    /// <summary>
    /// The portal as a whole: the spending of every user counts towards one ceiling. Checked in addition to the
    /// limits of the users.
    /// </summary>
    [Description("Total")]
    Total = 1,

    /// <summary>
    /// Every user separately, the same ceiling for each: applies to a user who has no personal limit in force.
    /// </summary>
    [Description("Global")]
    Global = 2,

    /// <summary>
    /// One user: a personal limit that applies to this user instead of the one for every user.
    /// </summary>
    [Description("Custom")]
    Custom = 3
}

/// <summary>
/// The window a service limit counts the spending over, in UTC. The windows are calendar-aligned rather than rolling,
/// except for a lifetime limit, which never resets. Numbered as the accounting service numbers it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServiceLimitPeriod
{
    /// <summary>
    /// The current day, from 00:00 to 23:59:59 UTC.
    /// </summary>
    [Description("Day")]
    Day = 1,

    /// <summary>
    /// The current ISO week, from Monday.
    /// </summary>
    [Description("Week")]
    Week = 2,

    /// <summary>
    /// The current calendar month.
    /// </summary>
    [Description("Month")]
    Month = 3,

    /// <summary>
    /// From the moment the limit was created, never reset; spending before that moment does not count.
    /// </summary>
    [Description("Lifetime")]
    Lifetime = 4
}

/// <summary>
/// A spending limit on a service group, set for the portal as a whole, for every user, or for one user.
/// </summary>
public class ServiceLimit
{
    /// <summary>
    /// The service limit ID.
    /// </summary>
    /// <example>42</example>
    public int Id { get; init; }

    /// <summary>
    /// The limited service group.
    /// </summary>
    /// <example>ai-services</example>
    public string ServiceGroupName { get; init; }

    /// <summary>
    /// The number of the billing account of the portal the limit belongs to.
    /// </summary>
    /// <example>12345</example>
    public int CustomerAccountNumber { get; init; }

    /// <summary>
    /// Whom the limit applies to.
    /// </summary>
    /// <example>Custom</example>
    public ServiceLimitScope Scope { get; init; }

    /// <summary>
    /// The participant the limit applies to: the user of a personal limit, or, in the list of participants, the user a
    /// limit for every user applies to. Null for a limit of the portal as a whole, and for a limit for every user read
    /// on its own.
    /// </summary>
    /// <example>3f2504e0-4f89-11d3-9a0c-0305e82c3301</example>
    public string Participant { get; init; }

    /// <summary>
    /// The most money the group may cost per period, in `currency`; null when the limit has no money threshold.
    /// </summary>
    /// <example>50</example>
    public decimal? AmountValue { get; init; }

    /// <summary>
    /// The currency of `amountValue`; null when the limit has no money threshold.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; init; }

    /// <summary>
    /// The largest quantity the group may consume per period, in `serviceUnit`; null when the limit has no quantity
    /// threshold.
    /// </summary>
    /// <example>1000</example>
    public int? QuantityValue { get; init; }

    /// <summary>
    /// The unit of `quantityValue`; null when the limit has no quantity threshold.
    /// </summary>
    /// <example>request</example>
    public string ServiceUnit { get; init; }

    /// <summary>
    /// The window the thresholds apply to.
    /// </summary>
    /// <example>Day</example>
    public ServiceLimitPeriod Period { get; init; }

    /// <summary>
    /// The moment a lifetime limit counts from; null for the other periods.
    /// </summary>
    /// <example>2026-10-01T09:12:00Z</example>
    public DateTime? UsageFrom { get; init; }

    /// <summary>
    /// Whether the limit is in force.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; init; }

    /// <summary>
    /// The date and time when the limit was created.
    /// </summary>
    /// <example>2026-09-01T10:30:00Z</example>
    public DateTime Created { get; init; }

    /// <summary>
    /// The date and time when the limit was last changed.
    /// </summary>
    /// <example>2026-09-15T08:00:00Z</example>
    public DateTime Modified { get; init; }

    /// <summary>
    /// The spending in the current period; filled only by the reads that are asked for it.
    /// </summary>
    public ServiceLimitUsage Usage { get; init; }
}

/// <summary>
/// What has been spent against a service limit in its current period, and what is left.
/// </summary>
public class ServiceLimitUsage
{
    /// <summary>
    /// The start of the current period, in UTC.
    /// </summary>
    /// <example>2026-10-01T00:00:00Z</example>
    public DateTime PeriodStart { get; init; }

    /// <summary>
    /// The end of the current period, in UTC; null for a lifetime limit, which never resets.
    /// </summary>
    /// <example>2026-10-01T23:59:59Z</example>
    public DateTime? PeriodEnd { get; init; }

    /// <summary>
    /// The money charged in the current period.
    /// </summary>
    /// <example>12.5</example>
    public decimal AmountConsumed { get; init; }

    /// <summary>
    /// The money still available: the threshold less what was charged and what is reserved, never below zero; null
    /// when the limit has no money threshold.
    /// </summary>
    /// <example>37.5</example>
    public decimal? AmountAvailable { get; init; }

    /// <summary>
    /// The units consumed in the current period.
    /// </summary>
    /// <example>250</example>
    public int QuantityConsumed { get; init; }

    /// <summary>
    /// The units still available, never below zero; null when the limit has no quantity threshold.
    /// </summary>
    /// <example>750</example>
    public int? QuantityAvailable { get; init; }
}

/// <summary>
/// Represents a paged list of the service limits in force for the portal users.
/// </summary>
public class ServiceLimitReport : BaseReport<ServiceLimit>
{
}

public record ServiceLimitCreateOperation(
    string CustomerName,
    string ServiceGroupName,
    ServiceLimitScope Scope,
    string CustomerParticipantName,
    decimal? AmountValue,
    int? QuantityValue,
    ServiceLimitPeriod Period);

public record ServiceLimitUpdateOperation(
    int Id,
    decimal? AmountValue,
    int? QuantityValue,
    bool? Enabled);

public record ServiceLimitParticipantSetOperation(
    string CustomerName,
    string ServiceGroupName,
    List<string> CustomerParticipantNames,
    decimal? AmountValue,
    int? QuantityValue,
    ServiceLimitPeriod Period);

public record SessionOpenOperation(
    string CustomerName,
    string ServiceName,
    string ExternalRef,
    int Quantity,
    int Duration);

public record SessionCompleteOperation(
    string CustomerName,
    string ServiceName,
    int SessionId,
    int Quantity,
    string CustomerParticipantName,
    Dictionary<string, string> Metadata);

public record BonusCreditOperation(
    string CustomerName,
    string ExternalRef,
    DateTime? ExpiresAt,
    decimal Sum,
    string Currency,
    Dictionary<string, string> Metadata);

public static class AccountingHttpClientExtension
{
    private const string ResiliencePipelineName = "accountingResiliencePipeline";
    internal const string BalanceResiliencePipelineName = "balanceResiliencePipeline";

    public static void AddAccountingHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        var accountingSettingsSection = configuration.GetSection("core:accounting");
        var accountingSettings = accountingSettingsSection.Get<AccountingConfiguration>();
        services.Configure<AccountingConfiguration>(accountingSettingsSection);

        services.AddTransient<AccountingAuthHandler>();

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }),
            UrlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter(),
            ExceptionFactory = CreateExceptionAsync,
            TransportExceptionFactory = CreateTransportException,
            UrlParameterFormatterMap = {
                // The accounting service expects lowercase boolean query values ("true"/"false"); Refit's default
                // formatter renders them as "True"/"False". Everything else keeps the default formatting.
                [typeof(bool)] = new LowerCaseBooleanFormatter()
            }
        };

        services
            .AddRefitGeneratedClient<IAccountingApi>(refitSettings)
            .ConfigureHttpClient((sp, client) =>
            {
                var url = accountingSettings?.Url;

                if (!string.IsNullOrEmpty(url))
                {
                    client.BaseAddress = new Uri(url);
                }

                client.Timeout = TimeSpan.FromMilliseconds(60000);
            })
            .AddHttpMessageHandler<AccountingAuthHandler>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddResilienceHandler(ResiliencePipelineName, builder =>
            {
                builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = async args =>
                    {
                        // Retry only idempotent GET requests on a non-success response. POST/PUT are never retried to
                        // avoid duplicating accounting operations (payments, sessions); transient errors that require
                        // stronger retries (e.g. balance refresh after a deposit) are handled by the balance pipeline.
                        var response = args.Outcome.Result;
                        if (response is null || response.IsSuccessStatusCode || response.RequestMessage?.Method != HttpMethod.Get)
                        {
                            return false;
                        }

                        // A missing service limit is a definitive result ("no limit is set"), so its 404 is not retried.
                        // Other 404s are retried: a customer created a moment ago may not be visible yet.
                        if (response.StatusCode == HttpStatusCode.NotFound && IsServiceLimitRequest(response.RequestMessage))
                        {
                            return false;
                        }

                        // "Customer not found" is a definitive result, not a transient error - retrying won't change it.
                        if (response.StatusCode == HttpStatusCode.BadRequest)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            return !IsCustomerNotFound(response.StatusCode, content);
                        }

                        return true;
                    }
                });
            });

        services.AddResiliencePipeline<string, bool>(BalanceResiliencePipelineName, pipelineBuilder =>
        {
            pipelineBuilder.AddRetry(new RetryStrategyOptions<bool>
            {
                MaxRetryAttempts = 15,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = new PredicateBuilder<bool>().HandleResult(result => !result)
            });
        });
    }

    // ExceptionFactory only sees HTTP responses. A transport failure (DNS, connect, TLS, timeout) would otherwise
    // surface as Refit.ApiRequestException and escape the AccountingException hierarchy the callers catch.
    private static Exception CreateTransportException(HttpRequestMessage request, Exception exception, CancellationToken cancellationToken)
    {
        // A caller-requested cancellation is not a service failure - let it propagate unchanged.
        if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
        {
            return exception;
        }

        return new AccountingException($"Accounting request to {request.RequestUri} failed: {exception.Message}", exception);
    }
    // Maps non-success responses to the domain exceptions the callers expect (payment required / customer not found /
    // resource not found), and wraps any other failure into AccountingException with the status code and response body.
    private static async ValueTask<Exception> CreateExceptionAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.PaymentRequired)
        {
            return new AccountingPaymentRequiredException();
        }

        var content = await response.Content.ReadAsStringAsync();
        var detail = GetProblemDetail(content);
        var serviceLimitRequest = IsServiceLimitRequest(response.RequestMessage);

        // On the service limit paths a 400 "not found" may name something other than the customer - a service group the
        // accounting service does not have - and reporting that as a missing customer would hide the real reason.
        if (IsCustomerNotFound(response.StatusCode, content) &&
            (!serviceLimitRequest || detail.StartsWith("Customer", StringComparison.OrdinalIgnoreCase)))
        {
            return new AccountingCustomerNotFoundException();
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new AccountingNotFoundException($"Accounting resource not found {content}");
        }

        // A limit that does not exist is answered with 404 on a read but with 400 on an update or a deletion; both mean
        // the same, and only this one 400 must not be confused with a threshold the accounting service rejects.
        if (response.StatusCode == HttpStatusCode.BadRequest && serviceLimitRequest && IsServiceLimitNotFound(detail))
        {
            return new AccountingNotFoundException($"Accounting resource not found {content}");
        }

        // A charge or a reservation refused by a spending limit; a 403 without this title means our API client lacks
        // the access level, which stays a plain failure.
        if (response.StatusCode == HttpStatusCode.Forbidden && content.Contains(ServiceLimitExceededTitle, StringComparison.OrdinalIgnoreCase))
        {
            return new AccountingServiceLimitExceededException(detail);
        }

        // A second limit of the same scope or for the same user, or a bonus credit that conflicts with one already made;
        // elsewhere a 409 stays a plain failure, as before.
        if (response.StatusCode == HttpStatusCode.Conflict && (serviceLimitRequest || IsBonusCreditRequest(response.RequestMessage)))
        {
            return new AccountingConflictException(detail);
        }

        // The service limit operations pass values the portal user chose, so their 400 explains a mistake of that user
        // (a quantity threshold for services with different units, a missing threshold) or a gap in the setup of the
        // accounting service (a missing service group). Elsewhere a 400 is our bug.
        if (response.StatusCode == HttpStatusCode.BadRequest && serviceLimitRequest)
        {
            return new AccountingBadRequestException(detail);
        }

        return new AccountingException($"Accounting request failed with status code {response.StatusCode} {content}");
    }

    private const string ServiceLimitExceededTitle = "Service limit exceeded";

    // The accounting service answers errors as ProblemDetails, the reason in "detail"; validation errors come in the
    // ASP.NET format with an "errors" object instead.
    private static string GetProblemDetail(string content)
    {
        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
            {
                return detail.GetString();
            }

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                return string.Join(" ", errors.EnumerateObject()
                    .SelectMany(error => error.Value.ValueKind == JsonValueKind.Array ? error.Value.EnumerateArray() : Enumerable.Empty<JsonElement>())
                    .Where(message => message.ValueKind == JsonValueKind.String)
                    .Select(message => message.GetString()));
            }
        }
        catch (JsonException)
        {
            // Not a JSON body: the text itself is the reason.
        }

        return content;
    }

    // Both /serviceLimit itself (create, update) and every path below it.
    private static bool IsServiceLimitRequest(HttpRequestMessage request)
    {
        var path = request?.RequestUri?.AbsolutePath;

        return path != null &&
               (path.EndsWith("/serviceLimit", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("/serviceLimit/", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsBonusCreditRequest(HttpRequestMessage request)
    {
        return request?.RequestUri?.AbsolutePath.EndsWith("/operation/bonus-credit", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsServiceLimitNotFound(string detail)
    {
        return detail.StartsWith("Service limit", StringComparison.OrdinalIgnoreCase) &&
               detail.Contains("not found", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCustomerNotFound(HttpStatusCode status, string content)
    {
        return status == HttpStatusCode.BadRequest &&
               content.Contains("not found", StringComparison.OrdinalIgnoreCase);
    }

    // Registered in UrlParameterFormatterMap for bool only, so nullable and non-nullable booleans in both
    // scalar parameters and flattened query objects render as "true"/"false".
    private sealed class LowerCaseBooleanFormatter : IUrlParameterFormatter
    {
        public string Format(object parameterValue, ICustomAttributeProvider attributeProvider, Type type)
        {
            return parameterValue is bool boolValue ? (boolValue ? "true" : "false") : parameterValue?.ToString();
        }
    }
}

public class AccountingException : Exception
{
    public AccountingException(string message) : base(message)
    {
    }

    public AccountingException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class AccountingNotConfiguredException(string message = "Accounting service is not configured") : AccountingException(message);

public class AccountingPaymentRequiredException(string message = "Payment required") : AccountingException(message);

public class AccountingCustomerNotFoundException(string message = "Customer not found") : AccountingException(message);

/// <summary>
/// The accounting service answered 404: the requested resource (e.g. a service limit) does not exist.
/// </summary>
public class AccountingNotFoundException(string message = "Resource not found") : AccountingException(message);

/// <summary>
/// The accounting service refused a charge or a reservation because a spending limit has no room left for it. The
/// wallet may well have the money; a wallet without it is <see cref="AccountingPaymentRequiredException"/>.
/// </summary>
public class AccountingServiceLimitExceededException(string message) : AccountingException(message);

/// <summary>
/// The accounting service answered 409: what was to be created exists already, e.g. a second limit of the same scope.
/// </summary>
public class AccountingConflictException(string message) : AccountingException(message);

/// <summary>
/// The accounting service rejected a service limit request as invalid; the message says why.
/// </summary>
public class AccountingBadRequestException(string message) : AccountingException(message);
