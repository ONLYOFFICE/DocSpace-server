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
public class AccountingClient(IOptions<AccountingConfiguration> configuration, ICache cache, IAccountingApi accountingApi)
{
    public bool Configured { get => !string.IsNullOrEmpty(configuration.Value.Url); }
    public bool SubAccountsEnabled { get => configuration.Value.SubAccounts; }

    public async Task<Balance> GetCustomerBalanceAsync(string portalId)
    {
        EnsureConfigured();

        return await accountingApi.GetCustomerBalanceAsync(portalId);
    }

    public async Task<Balance> GetCustomerAiBalanceAsync(string portalId)
    {
        EnsureConfigured();

        return await accountingApi.GetCustomerAiBalanceAsync(portalId);
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

    public async Task<ServicePayment> MakeAiCreditAsync(string portalId, decimal amount, string currency,
        string customerParticipantName, Dictionary<string, string> metadata = null)
    {
        EnsureConfigured();

        return await accountingApi.MakeAiCreditAsync(new AiCreditOperation(portalId, amount, currency, customerParticipantName, metadata));
    }

    public async Task<Report> GetCustomerAiOperationsAsync(string portalId, OperationFilter filter)
    {
        EnsureConfigured();

        return await accountingApi.GetCustomerAiOperationsAsync(portalId, filter);
    }

    public async Task<Report> GetCustomerOperationsAsync(string portalId, OperationFilter filter)
    {
        EnsureConfigured();

        return await accountingApi.GetCustomerOperationsAsync(portalId, filter);
    }

    public async Task<List<CustomerMonthlyUsage>> GetCustomerMonthlyUsageAsync(string portalId, DateTime? utcStartDate, DateTime? utcEndDate)
    {
        EnsureConfigured();

        return await accountingApi.GetCustomerMonthlyUsageAsync(portalId, utcStartDate, utcEndDate);
    }

    public async Task<UsageReport> GetCustomerServiceUsageAsync(string portalId, UsageFilter filter)
    {
        EnsureConfigured();

        return await accountingApi.GetCustomerServiceUsageAsync(portalId, filter);
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

    public async Task<ServiceInfo> GetServiceInfoAsync(string serviceName)
    {
        EnsureConfigured();

        return await accountingApi.GetServiceInfoAsync(serviceName);
    }

    public async Task<Dictionary<string, Dictionary<string, decimal>>> GetProductPriceInfoAsync(string partnerId, List<string> serviceNames)
    {
        var key = $"accounting-prices-{partnerId}-{string.Join(",", serviceNames)}";
        var result = cache.Get<Dictionary<string, Dictionary<string, decimal>>>(key);

        if (result != null)
        {
            return result;
        }

        var currencies = await GetAllCurrenciesAsync();

        var serviceInfos = await Task.WhenAll(serviceNames.Select(GetServiceInfoAsync));

        result = [];
        for (var i = 0; i < serviceNames.Count; i++)
        {
            var serviceInfo = serviceInfos[i];
            if (serviceInfo == null)
            {
                continue;
            }

            var currency = currencies.FirstOrDefault(c => c.Id == serviceInfo.CurrencyId);
            var currencyCode = currency?.Code ?? "USD";

            result.Add(serviceNames[i], new Dictionary<string, decimal>
            {
                { currencyCode, serviceInfo.PriceValue }
            });
        }

        cache.Insert(key, result, DateTime.Now.AddDays(1));

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

/// <summary>
/// Represents an object for filtering the list of customer operations.
/// </summary>
public class OperationFilter
{
    /// <summary>
    /// The service name.
    /// </summary>
    public string ServiceName { get; init; }
    /// <summary>
    /// The start date of the period to filter operations from (inclusive).
    /// </summary>
    [AliasAs("startDate")]
    [Query(Format = "o")]
    public DateTime? UtcStartDate { get; init; }
    /// <summary>
    /// The end date of the period to filter operations until (inclusive).
    /// </summary>
    [AliasAs("endDate")]
    [Query(Format = "o")]
    public DateTime? UtcEndDate { get; init; }
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
    /// The number of items to skip before starting to return results. Used for pagination.
    /// </summary>
    /// <remarks>Mutable (set) because it is reassigned per page while paginating, e.g. in CustomerOperationsReportTask.</remarks>
    public int? Offset { get; set; }
    /// <summary>
    /// The maximum number of items to return in the response.
    /// </summary>
    /// <remarks>Mutable (set) because it is reassigned per page while paginating, e.g. in CustomerOperationsReportTask.</remarks>
    public int? Limit { get; set; }
    /// <summary>
    /// The operation type to filter by.
    /// </summary>
    [AliasAs("types")]
    public OperationType? Type { get; init; }
    /// <summary>
    /// The operation status to filter by.
    /// </summary>
    public OperationStatus? Status { get; init; }
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
/// The filter for customer service usage statistics.
/// </summary>
public class UsageFilter
{
    /// <summary>
    /// The service name.
    /// </summary>
    public string ServiceName { get; init; }
    /// <summary>
    /// Unique name of customer participant to filter by.
    /// </summary>
    public string ParticipantName { get; init; }
    /// <summary>
    /// The operation status to filter by.
    /// </summary>
    public OperationStatus? Status { get; init; }
    /// <summary>
    /// The start date of the period to filter usage from (inclusive).
    /// </summary>
    public DateTime? UtcStartDate { get; init; }
    /// <summary>
    /// The end date of the period to filter usage until (inclusive).
    /// </summary>
    public DateTime? UtcEndDate { get; init; }
    /// <summary>
    /// Metadata key-value pairs to filter by.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; }
    /// <summary>
    /// The number of items to skip before starting to return results. Used for pagination.
    /// </summary>
    public int? Offset { get; set; }
    /// <summary>
    /// The maximum number of items to return in the response.
    /// </summary>
    public int? Limit { get; set; }
    /// <summary>
    /// The field to order by.
    /// </summary>
    public string OrderBy { get; init; }
    /// <summary>
    /// Order direction: ASC or DESC.
    /// </summary>
    public OperationOrderType? OrderType { get; init; }
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
/// Represents information about the transaction applied to an account.
/// </summary>
public class TransactionInfo
{
    /// <summary>
    /// The date and time when the credit transaction occurred.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime Date { get; init; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol of the transaction.
    /// </summary>
    /// <example>"USD"</example>
    public string Currency { get; init; }

    /// <summary>
    /// Amount of the transaction.
    /// </summary>
    /// <example>1500.75</example>
    public decimal Amount { get; init; }
}

/// <summary>
/// Represents a sub-account with a specific currency and balance.
/// </summary>
public class SubAccount
{
    /// <summary>
    /// The three-character ISO 4217 currency symbol of the sub-account.
    /// </summary>
    /// <example>"USD"</example>
    public string Currency { get; init; }

    /// <summary>
    /// The balance of the sub-account in the specified currency.
    /// </summary>
    /// <example>1500.75</example>
    public decimal Amount { get; init; }
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
    /// The name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The account number.
    /// </summary>
    public int AccountNumber { get; init; }
}

/// <summary>
/// Represents a report containing a collection of operations.
/// </summary>
public class Report
{
    /// <summary>
    /// Collection of operations.
    /// </summary>
    public List<Operation> Collection { get; init; }

    /// <summary>
    /// Offset of the report data.
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    /// Limit of the report data.
    /// </summary>
    public int Limit { get; init; }

    /// <summary>
    /// Total quantity of operations in the report.
    /// </summary>
    public int TotalQuantity { get; init; }

    /// <summary>
    /// Total number of pages in the report.
    /// </summary>
    public int TotalPage { get; init; }

    /// <summary>
    /// Current page number of the report.
    /// </summary>
    public int CurrentPage { get; init; }

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
public class CustomerMonthlyUsage
{
    /// <summary>
    /// Calendar year (e.g. 2025).
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Calendar month (1-12).
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Currency code of the amounts (e.g. USD, EUR).
    /// </summary>
    public string Currency { get; set; }

    /// <summary>
    /// Total amount charged across all services in this month.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Number of individual purchase operations in this month.
    /// </summary>
    public int OperationCount { get; set; }
}

/// <summary>
/// Aggregated customer usage statistics for a service over a period.
/// </summary>
public class CustomerServiceUsage
{
    /// <summary>
    /// Name of the service.
    /// </summary>
    public string Service { get; set; }

    /// <summary>
    /// Unit of measurement for the service (e.g. requests, GB, hours).
    /// </summary>
    public string ServiceUnit { get; set; }

    /// <summary>
    /// Currency code of the amounts (e.g. USD, EUR).
    /// </summary>
    public string Currency { get; set; }

    /// <summary>
    /// Total number of units consumed.
    /// </summary>
    public int TotalQuantity { get; set; }

    /// <summary>
    /// Total amount charged for the service.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Number of individual purchase operations.
    /// </summary>
    public int OperationCount { get; set; }
}

/// <summary>
/// Represents a paged report of customer service usage statistics.
/// </summary>
public class UsageReport
{
    /// <summary>
    /// Collection of service usage statistics.
    /// </summary>
    public List<CustomerServiceUsage> Collection { get; set; }

    /// <summary>
    /// Offset of the report data.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// Limit of the report data.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Total quantity of records in the report.
    /// </summary>
    public long TotalQuantity { get; set; }

    /// <summary>
    /// Total number of pages in the report.
    /// </summary>
    public int TotalPage { get; set; }

    /// <summary>
    /// Current page number of the report.
    /// </summary>
    public int CurrentPage { get; set; }
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
    /// AI Agent id.
    /// </summary>
    /// <example>123</example>
    public string AgentId { get; set; }

    /// <summary>
    /// AI Agent name.
    /// </summary>
    /// <example>My AI Agent</example>
    public string AgentTitle { get; set; }

    /// <summary>
    /// Metadata of the operation.
    /// </summary>
    /// <example>{}</example>
    public Dictionary<string, string> Metadata { get; set; }
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
/// Represents service payment information.
/// </summary>
public class ServicePayment
{
    /// <summary>
    /// The payment operation ID.
    /// </summary>
    /// <example>12345</example>
    public int OperationId { get; init; }
    /// <summary>
    /// The balance of the sub-account in the specified currency.
    /// </summary>
    /// <example>1500.75</example>
    public decimal Amount { get; init; }
    /// <summary>
    /// The three-character ISO 4217 currency symbol.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; init; }
    /// <summary>
    /// Total quantity of operations.
    /// </summary>
    /// <example>10</example>
    public int Quantity { get; init; }
    /// <summary>
    /// The subscription ID
    /// </summary>
    /// <example>12345</example>
    public int? SubscriptionId { get; init; }
    /// <summary>
    /// The subscription start date.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? StartDate { get; init; }
    /// <summary>
    /// The subscription end date.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? EndDate { get; init; }
}

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

public record AiCreditOperation(
    string CustomerName,
    decimal Sum,
    string Currency,
    string CustomerParticipantName,
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

        services
            .AddRefitClient<IAccountingApi>(new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }),
                UrlParameterFormatter = new AccountingUrlParameterFormatter(),
                UrlParameterKeyFormatter = new CamelCaseUrlParameterKeyFormatter(),
                ExceptionFactory = CreateExceptionAsync
            })
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

    // Maps non-success responses to the domain exceptions the callers expect (payment required / customer not found),
    // and wraps any other failure into AccountingException with the status code and response body.
    private static async Task<Exception> CreateExceptionAsync(HttpResponseMessage response)
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
        if (IsCustomerNotFound(response.StatusCode, content))
        {
            return new AccountingCustomerNotFoundException();
        }

        return new AccountingException($"Accounting request failed with status code {response.StatusCode} {content}");
    }

    private static bool IsCustomerNotFound(HttpStatusCode status, string content)
    {
        return status == HttpStatusCode.BadRequest &&
               content.Contains("not found", StringComparison.OrdinalIgnoreCase);
    }

    // The accounting service expects lowercase boolean query values ("true"/"false"); Refit's default formatter
    // renders them as "True"/"False". Everything else falls through to the default behaviour.
    private sealed class AccountingUrlParameterFormatter : DefaultUrlParameterFormatter
    {
        public override string Format(object parameterValue, ICustomAttributeProvider attributeProvider, Type type)
        {
            if (parameterValue is bool boolValue)
            {
                return boolValue ? "true" : "false";
            }

            return base.Format(parameterValue, attributeProvider, type);
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
