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
public class BillingClient(IOptions<PaymentConfiguration> configuration, IBillingApi billingApi)
{
    private const int StripePaymentSystemId = 9;
    private const int AccountingPaymentSystemId = 11;

    public const string GetCurrentPaymentsUri = "GetActiveResources";
    public const string MetadataDetails = "details";
    public const string MetadataType = "type";
    public const string MetadataModel = "model";
    public const string MetadataAgentTitle = "agent_title";
    public const string MetadataAgentId = "agent_id";

    public bool Configured { get => !string.IsNullOrEmpty(configuration.Value.Url); }

    public async Task<string> GetAccountLinkAsync(string portalId, string backUrl)
    {
        EnsureConfigured();

        var result = await billingApi.GetAccountLinkAsync(CreateRequestData(portalId, [("BackRef", backUrl)]));
        var link = JsonSerializer.Deserialize<string>(result);
        return link;
    }

    public async Task<PaymentLast[]> GetCurrentPaymentsAsync(string portalId, bool refresh)
    {
        EnsureConfigured();

        var payments = await billingApi.GetActiveResourcesAsync(CreateRequestData(portalId), refresh);

        if (!configuration.Value.Test)
        {
            payments = payments.Where(payment => payment.PaymentStatus != 4).ToArray();
        }

        return payments;
    }

    public async Task<IEnumerable<PaymentInfo>> GetPaymentsAsync(string portalId)
    {
        EnsureConfigured();

        return await billingApi.GetPaymentsAsync(CreateRequestData(portalId));
    }

    public async Task<string> GetPaymentUrlAsync(string portalId, IEnumerable<string> products, string affiliateId = null, string partnerId = null, string campaign = null, string currency = null, string language = null, string customerEmail = null, string quantity = null, string backUrl = null, string successUrl = null)
    {
        EnsureConfigured();

        var parameters = products
            .Distinct()
            .Select(p => ("ProductId", p))
            .ToList();

        parameters.Add(("PaymentSystemId", StripePaymentSystemId.ToString()));

        if (!string.IsNullOrEmpty(affiliateId))
        {
            parameters.Add(("AffiliateId", affiliateId));
        }
        if (!string.IsNullOrEmpty(partnerId))
        {
            parameters.Add(("PartnerId", partnerId));
        }
        if (!string.IsNullOrEmpty(campaign))
        {
            parameters.Add(("campaign", campaign));
        }
        if (!string.IsNullOrEmpty(currency))
        {
            parameters.Add(("Currency", currency));
        }
        if (!string.IsNullOrEmpty(language))
        {
            parameters.Add(("Language", language));
        }
        if (!string.IsNullOrEmpty(customerEmail))
        {
            parameters.Add(("CustomerEmail", customerEmail));
        }
        if (!string.IsNullOrEmpty(quantity))
        {
            parameters.Add(("Quantity", quantity));
        }
        if (!string.IsNullOrEmpty(successUrl))
        {
            // BackRef - redirect url after payment
            parameters.Add(("BackRef", successUrl));
        }
        if (!string.IsNullOrEmpty(backUrl))
        {
            // ShopUrl - redirect url when canceling a purchase (back to the shop)
            parameters.Add(("ShopUrl", backUrl));
        }

        var result = await billingApi.GetSinglePaymentUrlAsync(CreateRequestData(portalId, parameters));
        var paymentUrl = JsonSerializer.Deserialize<string>(result);

        return paymentUrl;
    }

    public async Task<CustomerInfo> GetCustomerInfoAsync(string portalId)
    {
        EnsureConfigured();

        return await billingApi.GetCustomerInfoAsync(CreateRequestData(portalId));
    }

    public async Task<bool> TopUpDepositAsync(string portalId, decimal amount, string currency, string customerParticipantName, string siteName, Dictionary<string, string> metadata = null)
    {
        EnsureConfigured();

        var parameters = new List<(string, string)>
        {
            ("Amount", amount.ToString(CultureInfo.InvariantCulture)),
            ("Currency", currency)
        };

        if (!string.IsNullOrEmpty(customerParticipantName))
        {
            parameters.Add(("CustomerParticipantName", customerParticipantName));
        }

        if (!string.IsNullOrEmpty(siteName))
        {
            parameters.Add(("SiteName", siteName));
        }

        if (metadata != null)
        {
            parameters.Add(("Metadata", JsonSerializer.Serialize(metadata)));
        }

        var result = await billingApi.DepositAsync(CreateRequestData(portalId, parameters));
        return result == "\"ok\"";
    }

    public async Task<bool> ChangePaymentAsync(string portalId, IEnumerable<string> products, IEnumerable<int> quantity, ProductQuantityType productQuantityType, string currency, string customerParticipantName, Dictionary<string, string> metadata = null)
    {
        EnsureConfigured();

        var parameters = products.Select(p => ("ProductId", p))
            .Concat(quantity.Select(q => ("ProductQty", q.ToString())))
            .ToList();

        parameters.Add(("ProductQuantityType", ((int)productQuantityType).ToString()));
        parameters.Add(("Currency", currency));

        if (!string.IsNullOrEmpty(customerParticipantName))
        {
            parameters.Add(("CustomerParticipantName", customerParticipantName));
        }

        if (metadata != null)
        {
            parameters.Add(("Metadata", JsonSerializer.Serialize(metadata)));
        }

        return await billingApi.ChangeSubscriptionAsync(CreateRequestData(portalId, parameters));
    }

    public async Task<bool> SwitchSubscriptionAsync(string portalId, string fromProductId, string toProductId, int quantity, string customerParticipantName, Dictionary<string, string> metadata = null)
    {
        EnsureConfigured();

        var parameters = new List<(string, string)>
        {
            ("FromProductId", fromProductId),
            ("ToProductId", toProductId),
            ("ProductQty", quantity.ToString())
        };

        if (!string.IsNullOrEmpty(customerParticipantName))
        {
            parameters.Add(("CustomerParticipantName", customerParticipantName));
        }

        if (metadata != null)
        {
            parameters.Add(("Metadata", JsonSerializer.Serialize(metadata)));
        }

        return await billingApi.SwitchSubscriptionAsync(CreateRequestData(portalId, parameters));
    }

    public async Task<PaymentCalculation> CalculateSwitchSubscriptionAsync(string portalId, string fromProductId, string toProductId, int quantity)
    {
        EnsureConfigured();

        var parameters = new List<(string, string)>
        {
            ("FromProductId", fromProductId),
            ("ToProductId", toProductId),
            ("ProductQty", quantity.ToString())
        };

        return await billingApi.CalculateSwitchSubscriptionAsync(CreateRequestData(portalId, parameters));
    }

    public async Task<PaymentCalculation> CalculatePaymentAsync(string portalId, IEnumerable<string> products, IEnumerable<int> quantity, ProductQuantityType productQuantityType, string currency)
    {
        EnsureConfigured();

        var parameters = products.Select(p => ("ProductId", p))
            .Concat(quantity.Select(q => ("ProductQty", q.ToString())))
            .ToList();

        parameters.Add(("ProductQuantityType", ((int)productQuantityType).ToString()));
        parameters.Add(("Currency", currency));

        return await billingApi.CalculateSubscriptionAsync(CreateRequestData(portalId, parameters));
    }

    public async Task<Dictionary<string, Dictionary<string, decimal>>> GetProductPriceInfoAsync(string partnerId, bool wallet, List<string> productIds)
    {
        ArgumentNullException.ThrowIfNull(productIds);

        EnsureConfigured();

        var parameters = productIds.Select(pid => ("ProductId", pid)).ToList();
        var paymentSystemId = wallet ? AccountingPaymentSystemId : StripePaymentSystemId;
        parameters.Add(("PaymentSystemId", paymentSystemId.ToString()));

        if (!string.IsNullOrEmpty(partnerId))
        {
            parameters.Add(("PartnerId", partnerId));
        }

        var prices = await billingApi.GetProductsPricesAsync(CreateRequestData(null, parameters));

        if (prices.TryGetValue(paymentSystemId, out var pricesPaymentSystem))
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

        return [];
    }

    public async Task<SubscriptionBalanceInfo> GetSubscriptionBalanceInfoAsync(string portalId, string productId)
    {
        EnsureConfigured();

        return await billingApi.GetSubscriptionBalanceInfoAsync(CreateRequestData(portalId, [("ProductId", productId)]));
    }

    public async Task<SubscriptionToWalletResult> SubscriptionBalanceToWalletAsync(string portalId, string productId)
    {
        EnsureConfigured();

        return await billingApi.SubscriptionBalanceToWalletAsync(CreateRequestData(portalId, [("ProductId", productId)]));
    }

    public async Task<bool> GetDocsCloudTrialAsync(string portalId)
    {
        EnsureConfigured();

        var result = await billingApi.GetDocsCloudTrialAsync(CreateRequestData(portalId));

        return result == "\"ok\"";
    }

    private void EnsureConfigured()
    {
        if (!Configured)
        {
            throw new BillingNotConfiguredException("Billing service is not configured");
        }
    }

    private static Dictionary<string, List<string>> CreateRequestData(string portalId, IEnumerable<(string Key, string Value)> parameters = null)
    {
        var data = new Dictionary<string, List<string>>();

        if (!string.IsNullOrEmpty(portalId))
        {
            data.Add("PortalId", [portalId]);
        }

        if (parameters != null)
        {
            foreach (var (key, value) in parameters)
            {
                if (data.TryGetValue(key, out var values))
                {
                    values.Add(value);
                }
                else
                {
                    data.Add(key, [value]);
                }
            }
        }

        return data;
    }
}

public static class BillingHttpClientExtension
{
    private const string ResiliencePipelineName = "billingResiliencePipeline";
    internal const string RetryOptionKey = "billingRetryEnabled";

    private const string ErrorMarker = "{\"Message\":\"error";
    private const string NotFoundMarker = "{\"Message\":\"error: cannot find ";

    public static void AddBillingHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        var paymentSettingsSection = configuration.GetSection("core:payment");
        var paymentSettings = paymentSettingsSection.Get<PaymentConfiguration>();
        services.Configure<PaymentConfiguration>(paymentSettingsSection);

        services.AddTransient<BillingAuthHandler>();

        services
            .AddRefitClient<IBillingApi>(new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }),
                ExceptionFactory = CreateExceptionAsync
            })
            .ConfigureHttpClient((_, client) =>
            {
                var url = paymentSettings?.Url;

                if (!string.IsNullOrEmpty(url))
                {
                    client.BaseAddress = new Uri(url);
                }

                client.Timeout = TimeSpan.FromMilliseconds(60000);
            })
            .AddHttpMessageHandler<BillingAuthHandler>()
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
                        // Only requests that explicitly opt in via [Property(RetryOptionKey)] are retried
                        // (GetActiveResources with refresh) - and only on the "cannot find" response a freshly
                        // created portal produces until the billing service learns about it.
                        var response = args.Outcome.Result;
                        if (response?.RequestMessage is null ||
                            !response.RequestMessage.Options.TryGetValue(new HttpRequestOptionsKey<bool>(RetryOptionKey), out var retryEnabled) ||
                            !retryEnabled)
                        {
                            return false;
                        }

                        var content = await response.Content.ReadAsStringAsync();
                        return content.Contains(NotFoundMarker);
                    }
                });
            });
    }

    // The billing service reports errors as 200 OK with a '{"Message":"error...' body, so the content is inspected
    // for every response, not only for non-success status codes.
    private static async Task<Exception> CreateExceptionAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrEmpty(content))
        {
            return new BillingNotConfiguredException("Billing response is null");
        }

        if (content.StartsWith(ErrorMarker, StringComparison.OrdinalIgnoreCase))
        {
            return content.Contains(NotFoundMarker)
                ? new BillingNotFoundException(content)
                : new BillingException(content);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new BillingException($"Billing request failed with status code {response.StatusCode} {content}");
        }

        return null;
    }
}

public class BillingException : Exception
{
    public BillingException(string message) : base(message)
    {
    }

    public BillingException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class BillingNotFoundException(string message) : BillingException(message);

public class BillingLicenseTypeException(string message) : BillingException(message);

public class BillingNotConfiguredException : BillingException
{
    public BillingNotConfiguredException(string message) : base(message)
    {
    }

    public BillingNotConfiguredException(string message, Exception inner) : base(message, inner)
    {
    }
}
