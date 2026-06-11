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
public class DocsCloudClient(IOptions<DocsCloudConfiguration> configuration, IDocsCloudApi docsCloudApi)
{
    public bool Configured { get => !string.IsNullOrEmpty(configuration.Value.Url); }

    /// <summary>
    /// Pings the DocsCloud server health endpoint. Requires no authorization, so it intentionally skips the
    /// configured-check and can be used as a connectivity probe.
    /// </summary>
    public async Task CheckHealthAsync()
    {
        await docsCloudApi.HealthCheckAsync();
    }

    public async Task<DocsCloudTenant> GetTenantAsync(string portalId)
    {
        EnsureConfigured();

        return await docsCloudApi.GetTenantAsync(portalId);
    }

    public async Task<string> GetTenantInfoAsync(string portalId)
    {
        EnsureConfigured();

        return await docsCloudApi.GetTenantInfoAsync(portalId);
    }

    public async Task<DocsCloudConfigDto> GetTenantConfigAsync(string portalId)
    {
        EnsureConfigured();

        return await docsCloudApi.GetTenantConfigAsync(portalId);
    }

    public async Task<DocsCloudConfigDto> UpdateTenantConfigAsync(string portalId, DocsCloudConfigDto config)
    {
        EnsureConfigured();

        return await docsCloudApi.UpdateTenantConfigAsync(portalId, config);
    }

    public async Task<Stream> GetTenantQuotaAsync(string portalId)
    {
        EnsureConfigured();

        return await docsCloudApi.GetTenantQuotaAsync(portalId);
    }

    public async Task<DocsCloudUsage> GetTenantUsageAsync(string portalId)
    {
        EnsureConfigured();

        return await docsCloudApi.GetTenantUsageAsync(portalId);
    }

    private void EnsureConfigured()
    {
        if (!Configured)
        {
            throw new DocsCloudNotConfiguredException();
        }
    }
}

/// <summary>
/// Represents a DocsCloud tenant of a portal.
/// </summary>
public class DocsCloudTenant
{
    /// <summary>
    /// The external ID of the dedicated resource the tenant is hosted on.
    /// </summary>
    /// <example>12345</example>
    public int DedicatedResourceExId { get; init; }

    /// <summary>
    /// The tenant alias.
    /// </summary>
    /// <example>my-portal</example>
    public string Alias { get; init; }

    /// <summary>
    /// The tenant secret.
    /// </summary>
    public string Secret { get; init; }

    /// <summary>
    /// The tenant name.
    /// </summary>
    /// <example>My Portal</example>
    public string Name { get; init; }

    /// <summary>
    /// The date and time when the tenant was last modified.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime ModifiedDate { get; init; }

    /// <summary>
    /// The customer ID.
    /// </summary>
    /// <example>CustomerId</example>
    public string CustomerId { get; init; }

    /// <summary>
    /// The customer name.
    /// </summary>
    /// <example>CustomerName</example>
    public string CustomerName { get; init; }

    /// <summary>
    /// The date and time when the tenant subscription ends.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime EndDate { get; init; }

    /// <summary>
    /// The resource type.
    /// </summary>
    /// <example>1</example>
    public int ResourceType { get; init; }

    /// <summary>
    /// Whether the tenant is active (the end date is in the future).
    /// </summary>
    /// <example>false</example>
    public bool IsActive { get; init; }

    /// <summary>
    /// The tenant payment information.
    /// </summary>
    public DocsCloudPayment Payment { get; init; }
}

/// <summary>
/// Represents the payment information of a DocsCloud tenant.
/// </summary>
public class DocsCloudPayment
{
    /// <summary>
    /// The cart ID.
    /// </summary>
    /// <example>CartId</example>
    public string CartId { get; init; }

    /// <summary>
    /// The product ID.
    /// </summary>
    /// <example>12345</example>
    public int ProductId { get; init; }

    /// <summary>
    /// The payment status.
    /// </summary>
    /// <example>1</example>
    public int Status { get; init; }

    /// <summary>
    /// The interval unit.
    /// </summary>
    /// <example>1</example>
    public int IntervalUnit { get; init; }

    /// <summary>
    /// Whether the payment interval is yearly.
    /// </summary>
    /// <example>false</example>
    public bool IsYear { get; init; }

    /// <summary>
    /// Whether the payment is prepaid.
    /// </summary>
    /// <example>false</example>
    public bool IsPrepaid { get; init; }

    /// <summary>
    /// The quantity.
    /// </summary>
    /// <example>10</example>
    public int Quantity { get; init; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol of the payment.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; init; }
}

/// <summary>
/// Represents the configuration of a DocsCloud tenant.
/// </summary>
public class DocsCloudConfigDto
{
    /// <summary>
    /// The tenant name.
    /// </summary>
    /// <example>My Portal</example>
    public string TenantName { get; init; }

    /// <summary>
    /// The security configuration.
    /// </summary>
    public DocsCloudSecurity Security { get; init; }

    /// <summary>
    /// The server configuration.
    /// </summary>
    public DocsCloudServerConfig Server { get; init; }
}

/// <summary>
/// Represents the security configuration of a DocsCloud tenant.
/// </summary>
public class DocsCloudSecurity
{
    /// <summary>
    /// The security secret.
    /// </summary>
    /// <example>abc123</example>
    public string Secret { get; init; }

    /// <summary>
    /// The security header name.
    /// </summary>
    /// <example>Authorization</example>
    public string Header { get; init; }
}

/// <summary>
/// Represents the server configuration of a DocsCloud tenant.
/// </summary>
public class DocsCloudServerConfig
{
    /// <summary>
    /// Whether anonymous access is supported.
    /// </summary>
    /// <example>false</example>
    public bool IsAnonymousSupport { get; init; }
}

/// <summary>
/// Represents the usage statistics of a DocsCloud tenant.
/// </summary>
public class DocsCloudUsage
{
    /// <summary>
    /// The date and time the usage statistics are counted from.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime Since { get; init; }

    /// <summary>
    /// The number of active users.
    /// </summary>
    /// <example>10</example>
    public int ActiveCount { get; init; }
}

public static class DocsCloudHttpClientExtension
{
    private const string ResiliencePipelineName = "docsCloudResiliencePipeline";

    public static void AddDocsCloudHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        var docsCloudSettingsSection = configuration.GetSection("core:docscloud");
        var docsCloudSettings = docsCloudSettingsSection.Get<DocsCloudConfiguration>();
        services.Configure<DocsCloudConfiguration>(docsCloudSettingsSection);

        services.AddTransient<DocsCloudAuthHandler>();

        services
            .AddRefitClient<IDocsCloudApi>(new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }),
                ExceptionFactory = CreateExceptionAsync
            })
            .ConfigureHttpClient((sp, client) =>
            {
                var url = docsCloudSettings?.Url;

                if (!string.IsNullOrEmpty(url))
                {
                    client.BaseAddress = new Uri(url);
                }

                client.Timeout = TimeSpan.FromMilliseconds(60000);
            })
            .AddHttpMessageHandler<DocsCloudAuthHandler>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddResilienceHandler(ResiliencePipelineName, builder =>
            {
                builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = 2,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = args =>
                    {
                        // Retry only idempotent GET requests on a non-success response. POST is never retried to avoid
                        // duplicating mutations (e.g. tenant config updates). Definitive client errors (not found,
                        // forbidden, bad request) won't change on retry, so they are not retried either.
                        var response = args.Outcome.Result;
                        if (response is null || response.IsSuccessStatusCode || response.RequestMessage?.Method != HttpMethod.Get)
                        {
                            return ValueTask.FromResult(false);
                        }

                        var retry = response.StatusCode is not (HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest);

                        return ValueTask.FromResult(retry);
                    }
                });
            });
    }

    // Maps non-success responses to the domain exceptions the callers expect (resource not found / authorization
    // failed), and wraps any other failure into DocsCloudException with the status code and response body.
    private static async Task<Exception> CreateExceptionAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new DocsCloudNotFoundException();
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return new DocsCloudForbiddenException();
        }

        var content = await response.Content.ReadAsStringAsync();

        return new DocsCloudException($"DocsCloud request failed with status code {response.StatusCode} {content}");
    }
}

public class DocsCloudException : Exception
{
    public DocsCloudException(string message) : base(message)
    {
    }

    public DocsCloudException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class DocsCloudNotConfiguredException(string message = "DocsCloud service is not configured") : DocsCloudException(message);

public class DocsCloudNotFoundException(string message = "DocsCloud resource not found") : DocsCloudException(message);

public class DocsCloudForbiddenException(string message = "DocsCloud authorization failed") : DocsCloudException(message);
