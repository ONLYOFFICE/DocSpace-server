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

namespace ASC.Core.Billing;

[Singleton]
public class AccountingClient
{
    public readonly bool Configured;

    private readonly AccountingConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    internal const string HttpClientOption = "accounting";

    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public AccountingClient(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration.GetSection("core:accounting").Get<AccountingConfiguration>() ?? new AccountingConfiguration();
        _httpClientFactory = httpClientFactory;

        _configuration.Url = (_configuration.Url ?? "").Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(_configuration.Url))
        {
            Configured = true;
        }
    }


    public async Task<decimal> GetBalance(string portalId, bool addPolicy = false)
    {
        return await RequestAsync<decimal>(HttpMethod.Post, "/balance", portalId, addPolicy: addPolicy);
    }

    public async Task<bool> BlockMoney(string portalId, decimal amount)
    {
        return await RequestAsync<bool>(HttpMethod.Post, "/money/block", portalId, [Tuple.Create("Amount", amount.ToString())]);
    }

    public async Task<decimal> TakeOffMoney(string portalId, decimal amount)
    {
        return await RequestAsync<decimal>(HttpMethod.Post, "/money/takeoff", portalId, [Tuple.Create("Amount", amount.ToString())]);
    }

    public async Task<List<PurchaseInfo>> GetReport(string portalId, DateTime utcFrom, DateTime utcTo)
    {
        return await RequestAsync<List<PurchaseInfo>>(HttpMethod.Post, "/report", portalId, [Tuple.Create("From", utcFrom.ToString("o")), Tuple.Create("To", utcTo.ToString("o"))]);
    }

    public async Task<List<CurrencyInfo>> GetAllCurrenciesAsync()
    {
        return await RequestAsync<List<CurrencyInfo>>(HttpMethod.Get, "/currency/all", null);
    }


    private async Task<T> RequestAsync<T>(HttpMethod httpMethod, string url, string portalId, Tuple<string, string>[] parameters = null, bool addPolicy = false)
    {
        if (!Configured)
        {
            throw new AccountingNotConfiguredException();
        }

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(_configuration.Url + url),
            Method = httpMethod
        };

        if (!string.IsNullOrEmpty(_configuration.Key))
        {
            request.Headers.Add("Authorization", CreateAuthToken(_configuration.Key, _configuration.Secret));
        }

        var httpClient = _httpClientFactory.CreateClient(addPolicy ? HttpClientOption : "");
        httpClient.Timeout = TimeSpan.FromMilliseconds(60000);

        var data = new Dictionary<string, List<string>>();

        if (!string.IsNullOrEmpty(portalId))
        {
            data.Add("PortalId", [portalId]);
        }

        if (parameters != null)
        {
            foreach (var parameter in parameters)
            {
                if (data.TryGetValue(parameter.Item1, out var value))
                {
                    value.Add(parameter.Item2);
                }
                else
                {
                    data.Add(parameter.Item1, [parameter.Item2]);
                }
            }
        }

        if (data.Count > 0)
        {
            var body = JsonSerializer.Serialize(data);

            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        string responseString = null;

        try
        {
            using var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Accounting request failed with status code {response.StatusCode}");
            }

            responseString = await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            throw new AccountingException(ex.Message, ex);
        }

        if (string.IsNullOrEmpty(responseString))
        {
            throw new AccountingException("Accounting responseString is null");
        }

        if (!responseString.StartsWith("{\"Message\":\"error", true, null))
        {
            var result = JsonSerializer.Deserialize<T>(responseString, _jsonSerializerOptions);

            return result;
        }

        throw new AccountingException(responseString);
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
/// UOM = Unit of measurement
/// </summary>
public record PurchaseInfo(DateTime Date,string Service, string UOM, decimal Quantity, decimal Price);

public record CurrencyInfo(int Id, string Code);

public static class AccountingHttplClientExtension
{
    public static void AddAccountingHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient(AccountingClient.HttpClientOption)
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler((_, request) =>
            {
                return Policy.HandleResult<HttpResponseMessage>
                    (msg =>
                    {
                        var result = msg.Content.ReadAsStringAsync().Result;
                        return result.StartsWith("{\"Message\":\"error: cannot find", true, null);
                    })
                    .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
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
