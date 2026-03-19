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

using System.Collections.Specialized;

using ASC.Web.Core.Users;

namespace ASC.Core.Billing;

[Singleton]
public class AccountingClient
{
    public readonly bool Configured;

    private readonly AccountingConfiguration _configuration;
    private readonly ICache _cache;
    private readonly IHttpClientFactory _httpClientFactory;

    internal const string HttpClientName = "accountingHttpClient";
    internal const string ResiliencePipelineName = "accountingResiliencePipeline";
    internal const string BalanceResiliencePipelineName = "balanceResiliencePipeline";

    private readonly JsonSerializerOptions _deserializationOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly JsonSerializerOptions _serializationOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AccountingClient(IConfiguration configuration, ICache cache, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration.GetSection("core:accounting").Get<AccountingConfiguration>() ?? new AccountingConfiguration();
        _cache = cache;
        _httpClientFactory = httpClientFactory;

        _configuration.Url = (_configuration.Url ?? "").Trim().TrimEnd('/');

        _configuration.Currencies = _configuration.Currencies == null || _configuration.Currencies.Count == 0 ? ["USD"] : _configuration.Currencies;

        if (!string.IsNullOrEmpty(_configuration.Url))
        {
            Configured = true;
        }
    }


    public async Task<Balance> GetCustomerBalanceAsync(string portalId, bool addPolicy = false)
    {
        return await RequestAsync<Balance>(HttpMethod.Get, $"/customer/{portalId}/balance", addPolicy: addPolicy);
    }

    public async Task<Balance> GetCustomerServiceQuotaAsync(string portalId, string serviceName, bool addPolicy = false)
    {
        return await RequestAsync<Balance>(HttpMethod.Get, $"/customer/{portalId}/quota/{serviceName}", addPolicy: addPolicy);
    }

    public async Task<Session> OpenCustomerSessionAsync(string portalId, string serviceName, string externalRef, int quantity, int duration)
    {
        var data = new
        {
            CustomerName = portalId,
            ServiceName = serviceName,
            ExternalRef = externalRef,
            Quantity = quantity,
            Duration = duration
        };

        return await RequestAsync<Session>(HttpMethod.Post, "/session/open", data: data);
    }

    public async Task CloseCustomerSessionAsync(int sessionId)
    {
        var queryParams = new NameValueCollection
        {
            { "sessionId", sessionId.ToString() }
        };

        _ = await RequestAsync<string>(HttpMethod.Put, $"/session/close", queryParams);
    }

    public async Task<Session> ExtendCustomerSessionAsync(int sessionId, int duration)
    {
        var queryParams = new NameValueCollection
        {
            { "sessionId", sessionId.ToString() },
            { "duration", duration.ToString() }
        };

        return await RequestAsync<Session>(HttpMethod.Put, $"/session/extend", queryParams);
    }

    public async Task CompleteCustomerSessionAsync(string portalId, string serviceName, int sessionId, int quantity, string customerParticipantName, Dictionary<string, string> metadata = null)
    {
        var data = new
        {
            CustomerName = portalId,
            ServiceName = serviceName,
            SessionId = sessionId,
            Quantity = quantity,
            CustomerParticipantName = customerParticipantName,
            Metadata = metadata
        };

        _ = await RequestAsync<string>(HttpMethod.Post, "/operation/sessionComplete", data: data);
    }

    public async Task<ServicePayment> MakeServicePaymentAsync(string portalId, string serviceName, int quantity, string customerParticipantName, Dictionary<string, string> metadata = null)
    {
        var data = new
        {
            CustomerName = portalId,
            ServiceName = serviceName,
            Quantity = quantity,
            CustomerParticipantName = customerParticipantName,
            Metadata = metadata
        };

        return await RequestAsync<ServicePayment>(HttpMethod.Post, "/operation/servicePayment", data: data);
    }

    public async Task<Report> GetCustomerOperationsAsync(string portalId, OperationFilter filter)
    {
        var queryParams = new NameValueCollection();

        if (filter.UtcStartDate != null)
        {
            queryParams.Add("startDate", filter.UtcStartDate.Value.ToString("o"));
        }

        if (filter.UtcEndDate != null)
        {
            queryParams.Add("endDate", filter.UtcEndDate.Value.ToString("o"));
        }

        if (!string.IsNullOrEmpty(filter.ParticipantName))
        {
            queryParams.Add("participantName", filter.ParticipantName.Trim());
        }

        if (filter.Credit.HasValue)
        {
            queryParams.Add("credit", filter.Credit.Value.ToString().ToLowerInvariant());
        }

        if (filter.Debit.HasValue)
        {
            queryParams.Add("debit", filter.Debit.Value.ToString().ToLowerInvariant());
        }

        if (filter.Offset.HasValue)
        {
            queryParams.Add("offset", filter.Offset.Value.ToString());
        }

        if (filter.Limit.HasValue)
        {
            queryParams.Add("limit", filter.Limit.Value.ToString());
        }

        if (filter.Types.HasValue && filter.Types is not OperationType.Any)
        {
            foreach (var operationType in Enum.GetValues<OperationType>())
            {
                if (operationType is OperationType.Any || !filter.Types.Value.HasFlag(operationType))
                {
                    continue;
                }
                queryParams.Add("types", operationType.ToString());
            }
        }

        if (filter.Status.HasValue && filter.Status is not OperationStatus.Any)
        {
            foreach (var operationStatus in Enum.GetValues<OperationStatus>())
            {
                if (operationStatus is OperationStatus.Any || !filter.Status.Value.HasFlag(operationStatus))
                {
                    continue;
                }
                queryParams.Add("status", operationStatus.ToString());
            }
        }

        if (!string.IsNullOrEmpty(filter.OrderBy))
        {
            queryParams.Add("orderBy", filter.OrderBy.Trim());
        }

        if (filter.OrderType.HasValue && filter.OrderType is not OperationOrderType.Descending)
        {
            queryParams.Add("orderType", filter.OrderType.Value.ToString());
        }

        var path = string.IsNullOrEmpty(filter.ServiceName)
            ? $"/customer/{portalId}/operations"
            : $"/customer/{portalId}/quota-detail/{filter.ServiceName}";

        return await RequestAsync<Report>(HttpMethod.Get, path, queryParams);
    }

    public async Task<List<Currency>> GetAllCurrenciesAsync()
    {
        var key = "accounting-currencies";
        var result = _cache.Get<List<Currency>>(key);
        if (result == null)
        {
            result = await RequestAsync<List<Currency>>(HttpMethod.Get, "/currency/all");
            _cache.Insert(key, result, DateTime.Now.AddDays(1));
        }
        return result;
    }

    public List<string> GetSupportedCurrencies()
    {
        return _configuration.Currencies;
    }

    public async Task<ServiceInfo> GetServiceInfoAsync(string serviceName)
    {
        return await RequestAsync<ServiceInfo>(HttpMethod.Get, $"/service/{serviceName}/name");
    }

    public async Task<Dictionary<string, Dictionary<string, decimal>>> GetProductPriceInfoAsync(string partnerId, List<string> serviceNames)
    {
        var key = $"accounting-prices-{partnerId}-{string.Join(",", serviceNames)}";
        var result = _cache.Get<Dictionary<string, Dictionary<string, decimal>>>(key);

        if (result != null)
        {
            return result;
        }

        var currencies = await GetAllCurrenciesAsync();

        result = [];
        foreach (var serviceName in serviceNames)
        {
            var serviceInfo = await GetServiceInfoAsync(serviceName);
            if (serviceInfo == null)
            {
                continue;
            }

            var currency = currencies.FirstOrDefault(c => c.Id == serviceInfo.CurrencyId);
            var currencyCode = currency?.Code ?? "USD";

            result.Add(serviceName, new Dictionary<string, decimal>
            {
                { currencyCode, serviceInfo.PriceValue }
            });
        }

        _cache.Insert(key, result, DateTime.Now.AddDays(1));

        return result;
    }

    private async Task<T> RequestAsync<T>(HttpMethod httpMethod, string path, NameValueCollection queryParams = null, object data = null, bool addPolicy = false)
    {
        if (!Configured)
        {
            throw new AccountingNotConfiguredException();
        }

        var uriBuilder = new UriBuilder(_configuration.Url + path);

        if (queryParams != null)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);

            foreach (string key in queryParams)
            {
                query[key] = queryParams[key];
            }

            uriBuilder.Query = query.ToString();
        }

        using var request = new HttpRequestMessage(httpMethod, uriBuilder.Uri);

        if (!string.IsNullOrEmpty(_configuration.Key))
        {
            request.Headers.Add("Authorization", CreateAuthToken(_configuration.Key, _configuration.Secret));
        }

        var httpClient = _httpClientFactory.CreateClient(addPolicy ? HttpClientName : "");
        httpClient.Timeout = TimeSpan.FromMilliseconds(60000);

        if (data != null)
        {
            var body = JsonSerializer.Serialize(data, _serializationOptions);

            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        try
        {
            using var response = await httpClient.SendAsync(request);

            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.PaymentRequired)
                {
                    throw new AccountingPaymentRequiredException();
                }

                if (response.StatusCode == HttpStatusCode.BadRequest && Regex.IsMatch(responseString, @"Customer account '.*?' not found"))
                {
                    throw new AccountingCustomerNotFoundException();
                }

                throw new Exception($"Accounting request failed with status code {response.StatusCode} {responseString}");
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)responseString;
            }

            if (string.IsNullOrEmpty(responseString))
            {
                throw new Exception("Accounting responseString is null or empty");
            }

            var result = JsonSerializer.Deserialize<T>(responseString, _deserializationOptions);

            return result;
        }
        catch (AccountingPaymentRequiredException)
        {
            throw;
        }
        catch (AccountingCustomerNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AccountingException(ex.Message, ex);
        }
    }

    private static string CreateAuthToken(string pkey, string machinekey)
    {
        using var hasher = new HMACSHA1(Encoding.UTF8.GetBytes(machinekey));
        var now = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var hash = WebEncoders.Base64UrlEncode(hasher.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", now, pkey))));

        return $"ASC {pkey}:{now}:{hash}";
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
[Flags]
public enum OperationType
{
    [Description("Any")]
    Any = 0,
    [Description("Unknown")]
    Unknown = 1 << 0,
    [Description("ServicePayment")]
    ServicePayment = 1 << 1,
    [Description("PackagePayment")]
    PackagePayment = 1 << 2,
    [Description("ServiceUsage")]
    ServiceUsage = 1 << 3,
    [Description("Deposit")]
    Deposit = 1 << 4,
    [Description("ReceiveProviderInvoice")]
    ReceiveProviderInvoice = 1 << 5,
    [Description("ProcessProviderInvoice")]
    ProcessProviderInvoice = 1 << 6,
    [Description("WriteOffServiceProfit")]
    WriteOffServiceProfit = 1 << 7,
    [Description("Profit")]
    Profit = 1 << 8,
    [Description("PartnerAccrual")]
    PartnerAccrual = 1 << 9,
    [Description("ProviderPayment")]
    ProviderPayment = 1 << 10,
    [Description("PartnerPayment")]
    PartnerPayment = 1 << 11,
    [Description("Refund")]
    Refund = 1 << 12,
    [Description("BankDeposit")]
    BankDeposit = 1 << 13,
    [Description("BankWithdrawal")]
    BankWithdrawal = 1 << 14,
    [Description("GoodwillCredit")]
    GoodwillCredit = 1 << 15,
    [Description("WriteOffProfit")]
    WriteOffProfit = 1 << 16,
    [Description("WriteOffDifferenceCurrency")]
    WriteOffDifferenceCurrency = 1 << 17
}

/// <summary>
/// The operation status
/// </summary>
[Flags]
public enum OperationStatus
{
    [Description("Any")]
    Any = 0,
    [Description("Pending")]
    Pending = 1 << 0,
    [Description("Completed")]
    Completed = 1 << 1,
    [Description("Rejected")]
    Rejected = 1 << 2,
    [Description("Canceled")]
    Canceled = 1 << 3
}

/// <summary>
/// Represents an object for filtering the list of customer operations.
/// </summary>
public class OperationFilter
{
    /// <summary>
    /// The service name.
    /// </summary>
    public string ServiceName { get; set; }
    /// <summary>
    /// The start date of the period to filter operations from (inclusive).
    /// </summary>
    public DateTime? UtcStartDate { get; init; }
    /// <summary>
    /// The end date of the period to filter operations until (inclusive).
    /// </summary>
    public DateTime? UtcEndDate { get; init; }
    /// <summary>
    /// Unique name of customer participant to filter by.
    /// </summary>
    public string ParticipantName { get; init; }
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
    public int? Offset { get; set; }
    /// <summary>
    /// The maximum number of items to return in the response.
    /// </summary>
    public int? Limit { get; set; }
    /// <summary>
    /// List of operation types to filter by.
    /// </summary>
    public OperationType? Types { get; init; }
    /// <summary>
    /// List of operation status to filter by.
    /// </summary>
    public OperationStatus? Status { get; init; }
    /// <summary>
    /// The field to order by.
    /// </summary>
    public string OrderBy { get; init; }
    /// <summary>
    /// Order direction: ASC or DESC.
    /// </summary>
    public OperationOrderType? OrderType  { get; init; }
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
    /// <example>aitools</example>
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
/// Represents a service information.
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
    public List<Operation> Collection { get; set; }

    /// <summary>
    /// Offset of the report data.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// Limit of the report data.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Total quantity of operations in the report.
    /// </summary>
    public int TotalQuantity { get; set; }

    /// <summary>
    /// Total number of pages in the report.
    /// </summary>
    public int TotalPage { get; set; }

    /// <summary>
    /// Current page number of the report.
    /// </summary>
    public int CurrentPage { get; set; }

    public async Task<Dictionary<string, string>> GetParticipantDisplayNamesAsync(DisplayUserSettingsHelper displayUserSettingsHelper)
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
                var participantDisplayName = await displayUserSettingsHelper.GetFullUserNameAsync(userId, true, false);
                participantDisplayNames.Add(operation.ParticipantName, participantDisplayName);
            }
        }

        return participantDisplayNames;
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
    /// <example>ACME Corp</example>
    public string ParticipantName { get; set; }

    /// <summary>
    /// Display name of the participant.
    /// </summary>
    /// <example>ACME Corp</example>
    public string ParticipantDisplayName { get; set; }

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

public static class AccountingHttpClientExtension
{
    public static void AddAccountingHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient(AccountingClient.HttpClientName)
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddResilienceHandler(AccountingClient.ResiliencePipelineName, builder =>
            {
                builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
                        .HandleResult(response => !response.IsSuccessStatusCode)
                });
            });

        services.AddResiliencePipeline<string, bool>(AccountingClient.BalanceResiliencePipelineName, pipelineBuilder =>
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
