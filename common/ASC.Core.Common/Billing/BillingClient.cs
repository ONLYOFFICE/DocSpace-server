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

        var result = await billingApi.GetAccountLinkAsync(new GetAccountLinkRequestDto
        {
            PortalId = [portalId],
            BackRef = [backUrl]
        });

        var link = JsonSerializer.Deserialize<string>(result);
        return link;
    }

    public async Task<PaymentLast[]> GetCurrentPaymentsAsync(string portalId, bool refresh)
    {
        EnsureConfigured();

        var payments = await billingApi.GetActiveResourcesAsync(
            new BillingPortalRequestDto { PortalId = [portalId] }, refresh);

        if (!configuration.Value.Test)
        {
            payments = payments.Where(payment => payment.PaymentStatus != 4).ToArray();
        }

        return payments;
    }

    public async Task<IEnumerable<PaymentInfo>> GetPaymentsAsync(string portalId)
    {
        EnsureConfigured();

        return await billingApi.GetPaymentsAsync(new BillingPortalRequestDto { PortalId = [portalId] });
    }

    public async Task<string> GetPaymentUrlAsync(string portalId, IEnumerable<string> products,
        string affiliateId = null, string partnerId = null, string campaign = null, string currency = null,
        string language = null, string customerEmail = null, string quantity = null, string backUrl = null,
        string successUrl = null)
    {
        EnsureConfigured();

        var requestDto = new GetPaymentUrlRequestDto
        {
            PortalId = [portalId],
            ProductId = Multi(products?.Distinct()),
            PaymentSystemId = [StripePaymentSystemId.ToString()],
            AffiliateId = Optional(affiliateId),
            PartnerId = Optional(partnerId),
            Campaign = Optional(campaign),
            Currency = Optional(currency),
            Language = Optional(language),
            CustomerEmail = Optional(customerEmail),
            Quantity = Optional(quantity),
            // BackRef - redirect url after payment
            BackRef = Optional(successUrl),
            // ShopUrl - redirect url when canceling a purchase (back to the shop)
            ShopUrl = Optional(backUrl)
        };

        var result = await billingApi.GetSinglePaymentUrlAsync(requestDto);
        var paymentUrl = JsonSerializer.Deserialize<string>(result);

        return paymentUrl;
    }

    public async Task<CustomerInfo> GetCustomerInfoAsync(string portalId)
    {
        EnsureConfigured();

        return await billingApi.GetCustomerInfoAsync(new BillingPortalRequestDto { PortalId = [portalId] });
    }

    public async Task<bool> TopUpDepositAsync(string portalId, decimal amount, string currency,
        string customerParticipantName, string siteName, Dictionary<string, string> metadata = null)
    {
        EnsureConfigured();

        var requestDto = new DepositRequestDto
        {
            PortalId = [portalId],
            Amount = [amount.ToString(CultureInfo.InvariantCulture)],
            Currency = [currency],
            CustomerParticipantName = Optional(customerParticipantName),
            SiteName = Optional(siteName),
            Metadata = Metadata(metadata)
        };

        var result = await billingApi.DepositAsync(requestDto);
        return result == "\"ok\"";
    }

    public async Task<bool> ChangePaymentAsync(string portalId, IEnumerable<string> products, IEnumerable<int> quantity,
        ProductQuantityType productQuantityType, string currency, string customerParticipantName,
        Dictionary<string, string> metadata = null)
    {
        EnsureConfigured();

        var requestDto = new ChangeSubscriptionRequestDto
        {
            PortalId = [portalId],
            ProductId = Multi(products),
            ProductQty = Multi(quantity?.Select(q => q.ToString())),
            ProductQuantityType = [((int)productQuantityType).ToString()],
            Currency = [currency],
            CustomerParticipantName = Optional(customerParticipantName),
            Metadata = Metadata(metadata)
        };

        return await billingApi.ChangeSubscriptionAsync(requestDto);
    }

    public async Task<bool> SwitchSubscriptionAsync(string portalId, string fromProductId, string toProductId,
        int quantity, string customerParticipantName, Dictionary<string, string> metadata = null)
    {
        EnsureConfigured();

        var requestDto = new SwitchSubscriptionRequestDto
        {
            PortalId = [portalId],
            FromProductId = [fromProductId],
            ToProductId = [toProductId],
            ProductQty = [quantity.ToString()],
            CustomerParticipantName = Optional(customerParticipantName),
            Metadata = Metadata(metadata)
        };

        return await billingApi.SwitchSubscriptionAsync(requestDto);
    }

    public async Task<PaymentCalculation> CalculateSwitchSubscriptionAsync(string portalId, string fromProductId,
        string toProductId, int quantity)
    {
        EnsureConfigured();

        var requestDto = new CalculateSwitchSubscriptionRequestDto
        {
            PortalId = [portalId],
            FromProductId = [fromProductId],
            ToProductId = [toProductId],
            ProductQty = [quantity.ToString()]
        };

        return await billingApi.CalculateSwitchSubscriptionAsync(requestDto);
    }

    public async Task<PaymentCalculation> CalculatePaymentAsync(string portalId, IEnumerable<string> products,
        IEnumerable<int> quantity, ProductQuantityType productQuantityType, string currency)
    {
        EnsureConfigured();

        var requestDto = new CalculateSubscriptionRequestDto
        {
            PortalId = [portalId],
            ProductId = Multi(products),
            ProductQty = Multi(quantity?.Select(q => q.ToString())),
            ProductQuantityType = [((int)productQuantityType).ToString()],
            Currency = [currency]
        };

        return await billingApi.CalculateSubscriptionAsync(requestDto);
    }

    public async Task<Dictionary<string, Dictionary<string, decimal>>> GetProductPriceInfoAsync(string partnerId,
        bool wallet, List<string> productIds)
    {
        ArgumentNullException.ThrowIfNull(productIds);

        EnsureConfigured();

        var paymentSystemId = wallet ? AccountingPaymentSystemId : StripePaymentSystemId;

        var requestDto = new GetProductsPricesRequestDto
        {
            ProductId = Multi(productIds),
            PaymentSystemId = [paymentSystemId.ToString()],
            PartnerId = Optional(partnerId)
        };

        var prices = await billingApi.GetProductsPricesAsync(requestDto);

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

        return await billingApi.GetSubscriptionBalanceInfoAsync(
            new BillingProductRequestDto { PortalId = [portalId], ProductId = [productId] });
    }

    public async Task<SubscriptionToWalletResult> SubscriptionBalanceToWalletAsync(string portalId, string productId)
    {
        EnsureConfigured();

        return await billingApi.SubscriptionBalanceToWalletAsync(
            new BillingProductRequestDto { PortalId = [portalId], ProductId = [productId] });
    }

    public async Task<bool> GetDocsCloudTrialAsync(string portalId)
    {
        EnsureConfigured();

        var result =
            await billingApi.GetDocsCloudTrialAsync(new BillingPortalRequestDto { PortalId = [portalId] });

        return result == "\"ok\"";
    }

    private void EnsureConfigured()
    {
        if (!Configured)
        {
            throw new BillingNotConfiguredException("Billing service is not configured");
        }
    }

    // Wraps a single optional value into the multimap array shape, returning null (so the field is omitted)
    // when the value is absent - reproducing the original "add the key only when it has a value" behavior.
    private static List<string> Optional(string value)
    {
        return string.IsNullOrEmpty(value) ? null : [value];
    }

    // Materializes a multi-value field, returning null (omitted) for an empty or absent collection.
    private static List<string> Multi(IEnumerable<string> values)
    {
        var list = values?.ToList();
        return list is { Count: > 0 } ? list : null;
    }

    // Serializes operation metadata into the single JSON-object-string element the billing service expects,
    // or null when there is no metadata.
    private static List<string> Metadata(Dictionary<string, string> metadata)
    {
        return metadata == null ? null : [JsonSerializer.Serialize(metadata)];
    }
}

// Strongly-typed request bodies for IBillingApi. Every value is a List<string> to preserve the billing
// multimap wire format (each field is an array of string values); null fields are omitted from the
// serialized body by the JsonIgnoreCondition.WhenWritingNull setting in AddBillingHttpClient.

/// <summary>
/// Base billing request scoped to a single portal.
/// </summary>
public class BillingPortalRequestDto
{
    public List<string> PortalId { get; set; }
}

/// <summary>A portal-scoped request that targets a single product.</summary>
public class BillingProductRequestDto : BillingPortalRequestDto
{
    public List<string> ProductId { get; set; }
}

/// <summary>Request for the customer account management link.</summary>
public class GetAccountLinkRequestDto : BillingPortalRequestDto
{
    public List<string> BackRef { get; set; }
}

/// <summary>Request for a single Stripe payment (checkout) URL.</summary>
public class GetPaymentUrlRequestDto : BillingPortalRequestDto
{
    public List<string> ProductId { get; set; }
    public List<string> PaymentSystemId { get; set; }
    public List<string> AffiliateId { get; set; }
    public List<string> PartnerId { get; set; }

    /// <summary>The marketing campaign identifier. The billing service expects the lowercase <c>campaign</c> key.</summary>
    [JsonPropertyName("campaign")]
    public List<string> Campaign { get; set; }

    public List<string> Currency { get; set; }
    public List<string> Language { get; set; }
    public List<string> CustomerEmail { get; set; }
    public List<string> Quantity { get; set; }

    /// <summary>The redirect URL after a successful payment.</summary>
    public List<string> BackRef { get; set; }

    /// <summary>The redirect URL when the purchase is cancelled (back to the shop).</summary>
    public List<string> ShopUrl { get; set; }
}

/// <summary>Request to top up the customer wallet (deposit).</summary>
public class DepositRequestDto : BillingPortalRequestDto
{
    public List<string> Amount { get; set; }
    public List<string> Currency { get; set; }
    public List<string> CustomerParticipantName { get; set; }
    public List<string> SiteName { get; set; }

    /// <summary>Operation metadata serialized as a single JSON object string.</summary>
    public List<string> Metadata { get; set; }
}

/// <summary>Request to change (up/downgrade) the current subscription quantities.</summary>
public class ChangeSubscriptionRequestDto : BillingPortalRequestDto
{
    public List<string> ProductId { get; set; }
    public List<string> ProductQty { get; set; }
    public List<string> ProductQuantityType { get; set; }
    public List<string> Currency { get; set; }
    public List<string> CustomerParticipantName { get; set; }

    /// <summary>Operation metadata serialized as a single JSON object string.</summary>
    public List<string> Metadata { get; set; }
}

/// <summary>Request to estimate the cost of a subscription change.</summary>
public class CalculateSubscriptionRequestDto : BillingPortalRequestDto
{
    public List<string> ProductId { get; set; }
    public List<string> ProductQty { get; set; }
    public List<string> ProductQuantityType { get; set; }
    public List<string> Currency { get; set; }
}

/// <summary>Request to switch the subscription from one product to another.</summary>
public class SwitchSubscriptionRequestDto : BillingPortalRequestDto
{
    public List<string> FromProductId { get; set; }
    public List<string> ToProductId { get; set; }
    public List<string> ProductQty { get; set; }
    public List<string> CustomerParticipantName { get; set; }

    /// <summary>Operation metadata serialized as a single JSON object string.</summary>
    public List<string> Metadata { get; set; }
}

/// <summary>Request to estimate the cost of a subscription switch.</summary>
public class CalculateSwitchSubscriptionRequestDto : BillingPortalRequestDto
{
    public List<string> FromProductId { get; set; }
    public List<string> ToProductId { get; set; }
    public List<string> ProductQty { get; set; }
}

/// <summary>
/// Request for product prices. This endpoint is not portal-scoped, so it intentionally does not
/// derive from <see cref="BillingPortalRequestDto"/> and carries no <c>PortalId</c>.
/// </summary>
public class GetProductsPricesRequestDto
{
    public List<string> ProductId { get; set; }
    public List<string> PaymentSystemId { get; set; }
    public List<string> PartnerId { get; set; }
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
                    PropertyNameCaseInsensitive = true,
                    // Optional request fields are modelled as nullable List<string> properties; omit them from the
                    // body when null so the wire format matches the original "add the key only when it has a value".
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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
