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

namespace ASC.Core.Billing;

[Singleton]
public class BillingClient
{
    public readonly bool Configured;
    private readonly PaymentConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private const int StripePaymentSystemId = 9;

    internal const string HttpClientOption = "billing";
    public const string GetCurrentPaymentsUri = "GetActiveResources";

    public BillingClient(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration.GetSection("core:payment").Get<PaymentConfiguration>() ?? new PaymentConfiguration();
        _httpClientFactory = httpClientFactory;

        _configuration.Url = (_configuration.Url ?? "").Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(_configuration.Url))
        {
            _configuration.Url += "/billing/";

            Configured = true;
        }
    }

    public async Task<string> GetAccountLinkAsync(string portalId, string backUrl)
    {
        var result = await RequestAsync("GetAccountLink", portalId, [Tuple.Create("BackRef", backUrl)]);
        var link = JsonSerializer.Deserialize<string>(result);
        return link;
    }

    public async Task<PaymentLast[]> GetCurrentPaymentsAsync(string portalId, bool refresh)
    {
        var result = await RequestAsync(GetCurrentPaymentsUri, portalId, addPolicy: refresh);
        var payments = JsonSerializer.Deserialize<PaymentLast[]>(result);

        if (!_configuration.Test)
        {
            payments = payments.Where(payment => payment.PaymentStatus != 4).ToArray();
        }

        return payments;
    }

    public async Task<IEnumerable<PaymentInfo>> GetPaymentsAsync(string portalId)
    {
        var result = await RequestAsync("GetPayments", portalId);
        var payments = JsonSerializer.Deserialize<List<PaymentInfo>>(result);

        return payments;
    }

    public async Task<string> GetPaymentUrlAsync(string portalId, IEnumerable<string> products, string affiliateId = null, string partnerId = null, string campaign = null, string currency = null, string language = null, string customerEmail = null, string quantity = null, string backUrl = null)
    {
        var additionalParameters = new List<Tuple<string, string>> { Tuple.Create("PaymentSystemId", StripePaymentSystemId.ToString()) };
        if (!string.IsNullOrEmpty(affiliateId))
        {
            additionalParameters.Add(Tuple.Create("AffiliateId", affiliateId));
        }
        if (!string.IsNullOrEmpty(partnerId))
        {
            additionalParameters.Add(Tuple.Create("PartnerId", partnerId));
        }
        if (!string.IsNullOrEmpty(campaign))
        {
            additionalParameters.Add(Tuple.Create("campaign", campaign));
        }
        if (!string.IsNullOrEmpty(currency))
        {
            additionalParameters.Add(Tuple.Create("Currency", currency));
        }
        if (!string.IsNullOrEmpty(language))
        {
            additionalParameters.Add(Tuple.Create("Language", language));
        }
        if (!string.IsNullOrEmpty(customerEmail))
        {
            additionalParameters.Add(Tuple.Create("CustomerEmail", customerEmail));
        }
        if (!string.IsNullOrEmpty(quantity))
        {
            additionalParameters.Add(Tuple.Create("Quantity", quantity));
        }
        if (!string.IsNullOrEmpty(backUrl))
        {
            additionalParameters.Add(Tuple.Create("BackRef", backUrl));
            additionalParameters.Add(Tuple.Create("ShopUrl", backUrl));
        }

        var parameters = products
            .Distinct()
            .Select(p => Tuple.Create("ProductId", p))
            .Concat(additionalParameters)
            .ToArray();

        var result = await RequestAsync("GetSinglePaymentUrl", portalId, parameters);
        var paymentUrl = JsonSerializer.Deserialize<string>(result);

        return paymentUrl;
    }

    public async Task<bool> ChangePaymentAsync(string portalId, IEnumerable<string> products, IEnumerable<int> quantity)
    {
        var parameters = products.Select(p => Tuple.Create("ProductId", p))
            .Concat(quantity.Select(q => Tuple.Create("ProductQty", q.ToString())))
            .ToArray();

        var result = await RequestAsync("ChangeSubscription", portalId, parameters);
        var changed = JsonSerializer.Deserialize<bool>(result);

        return changed;
    }

    public async Task<IDictionary<string, Dictionary<string, decimal>>> GetProductPriceInfoAsync(string partnerId, params string[] productIds)
    {
        ArgumentNullException.ThrowIfNull(productIds);

        var parameters = productIds.Select(pid => Tuple.Create("ProductId", pid)).ToList();
        parameters.Add(Tuple.Create("PaymentSystemId", StripePaymentSystemId.ToString()));

        if (!string.IsNullOrEmpty(partnerId))
        {
            parameters.Add(Tuple.Create("PartnerId", partnerId));
        }
        
        var result = await RequestAsync("GetProductsPrices", null, parameters.ToArray());
        var prices = JsonSerializer.Deserialize<Dictionary<int, Dictionary<string, Dictionary<string, decimal>>>>(result);

        if (prices.TryGetValue(StripePaymentSystemId, out var pricesPaymentSystem))
        {
            return productIds.Select(productId =>
            {
                if (pricesPaymentSystem.TryGetValue(productId, out var pricesByProduct))
                {
                    return new { ProductId = productId, Prices = pricesByProduct };
                }
                return new { ProductId = productId, Prices = new Dictionary<string, decimal>() };
            })
                .ToDictionary(e => e.ProductId, e => e.Prices);
        }

        return new Dictionary<string, Dictionary<string, decimal>>();
    }


    private string CreateAuthToken(string pkey, string machinekey)
    {
        using var hasher = new HMACSHA1(Encoding.UTF8.GetBytes(machinekey));
        var now = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var hash = WebEncoders.Base64UrlEncode(hasher.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", now, pkey))));

        return $"ASC {pkey}:{now}:{hash}";
    }

    private async Task<string> RequestAsync(string method, string portalId, Tuple<string, string>[] parameters = null, bool addPolicy = false)
    {
        var url = _configuration.Url + method;

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(url),
            Method = HttpMethod.Post
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
                if (!data.ContainsKey(parameter.Item1))
                {
                    data.Add(parameter.Item1, [parameter.Item2]);
                }
                else
                {
                    data[parameter.Item1].Add(parameter.Item2);
                }
            }
        }

        var body = JsonSerializer.Serialize(data);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        string result;
        using (var response = await httpClient.SendAsync(request))
        await using (var stream = await response.Content.ReadAsStreamAsync())
        {
            if (stream == null)
            {
                throw new BillingNotConfiguredException("Billing response is null");
            }
            using (var readStream = new StreamReader(stream))
            {
                result = await readStream.ReadToEndAsync();
            }
        }

        if (string.IsNullOrEmpty(result))
        {
            throw new BillingNotConfiguredException("Billing response is null");
        }
        if (!result.StartsWith("{\"Message\":\"error", true, null))
        {
            return result;
        }

        var info = new { Method = method, PortalId = portalId, Params = parameters != null ? string.Join(", ", parameters.Select(p => p.Item1 + ": " + p.Item2)) : "" };
        if (result.Contains("{\"Message\":\"error: cannot find "))
        {
            throw new BillingNotFoundException(result, info);
        }

        throw new BillingException(result, info);
    }
}

public static class BillingHttplClientExtension
{
    public static void AddBillingHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient(BillingClient.HttpClientOption)
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddPolicyHandler((_, request) =>
            {
                if (!request.RequestUri.AbsolutePath.EndsWith(BillingClient.GetCurrentPaymentsUri))
                {
                    return null;
                }

                return Policy.HandleResult<HttpResponseMessage>
                    (msg =>
                    {
                        var result = msg.Content.ReadAsStringAsync().Result;
                        return result.Contains("{\"Message\":\"error: cannot find ");
                    })
                    .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
            });
    }
}

public class BillingException : Exception
{
    public BillingException(string message, object debugInfo = null) : base(message + (debugInfo != null ? " Debug info: " + debugInfo : string.Empty))
    {
    }

    public BillingException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class BillingNotFoundException(string message, object debugInfo = null) : BillingException(message, debugInfo);

public class BillingNotConfiguredException : BillingException
{
    public BillingNotConfiguredException(string message, object debugInfo = null) : base(message, debugInfo)
    {
    }

    public BillingNotConfiguredException(string message, Exception inner) : base(message, inner)
    {
    }
}
