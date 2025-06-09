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

using System.Collections.Specialized;

using Polly.Extensions.Http;

namespace ASC.Core.Billing;

[Singleton]
public class AccountingClient
{
    public readonly bool Configured;

    private readonly AccountingConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    internal const string HttpClientName = "accountingHttpClient";

    private readonly JsonSerializerOptions _deserializationOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly JsonSerializerOptions _serializationOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AccountingClient(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration.GetSection("core:accounting").Get<AccountingConfiguration>() ?? new AccountingConfiguration();
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
        return await RequestAsync<Balance>(HttpMethod.Get, $"/customer/balance/{portalId}", addPolicy: addPolicy);
    }

    public async Task<Session> OpenCustomerSessionAsync(string portalId, int serviceAccount, string externalRef, int quantity)
    {
        var data = new
        {
            CustomerName = portalId,
            ServiceAccount = serviceAccount,
            ExternalRef = externalRef,
            Quantity = quantity
        };

        return await RequestAsync<Session>(HttpMethod.Post, "/session/open", data: data);
    }

    public async Task PerformCustomerOperationAsync(string portalId, int serviceAccount, int sessionId, int quantity)
    {
        var data = new
        {
            CustomerName = portalId,
            ServiceAccount = serviceAccount,
            SessionId = sessionId,
            Quantity = quantity
        };

        _ = await RequestAsync<string>(HttpMethod.Post, "/operation/provided", data: data);
    }

    public async Task<Report> GetCustomerOperationsAsync(string portalId, DateTime utcStartDate, DateTime utcEndDate, bool? credit, bool? withdrawal, int? offset, int? limit)
    {
        var queryParams = new NameValueCollection
        {
            { "startDate", utcStartDate.ToString("o") },
            { "endDate", utcEndDate.ToString("o") }
        };

        if (credit.HasValue)
        {
            queryParams.Add("credit", credit.Value.ToString().ToLowerInvariant());
        }

        if (withdrawal.HasValue)
        {
            queryParams.Add("withdrawal", withdrawal.Value.ToString().ToLowerInvariant());
        }

        if (offset.HasValue)
        {
            queryParams.Add("offset", offset.Value.ToString());
        }

        if (limit.HasValue)
        {
            queryParams.Add("limit", limit.Value.ToString());
        }

        return await RequestAsync<Report>(HttpMethod.Get, $"/customer/operations/{portalId}", queryParams);
    }

    public async Task<List<Currency>> GetAllCurrenciesAsync()
    {
        return await RequestAsync<List<Currency>>(HttpMethod.Get, "/currency/all", null);
    }

    public List<string> GetSupportedCurrencies()
    {
        return _configuration.Currencies;
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

        var request = new HttpRequestMessage
        {
            RequestUri = uriBuilder.Uri,
            Method = httpMethod
        };

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


public enum PaymentMethodStatus
{
    None,
    Set,
    Expired
}

public record CustomerInfo(string PortalId, PaymentMethodStatus PaymentMethodStatus, string Email);

public record Balance(int AccountNumber, List<SubAccount> SubAccounts);

public record SubAccount(string Currency, decimal Amount);

public record Session(int SessionId, decimal ReservedAmount, string Currency);

public record Report(List<Operation> Collection, int Offset, int Limit, int TotalQuantity, int TotalPage, int CurrentPage);

public record Operation(DateTime Date, string Service, string Description, string ServiceUnit, int Quantity, string Currency, decimal Credit, decimal Withdrawal);

public record Currency(int Id, string Code);


public static class AccountingHttplClientExtension
{
    public static void AddAccountingHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient(AccountingClient.HttpClientName)
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler((_, _) => HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(x => !x.IsSuccessStatusCode)
                .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

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
