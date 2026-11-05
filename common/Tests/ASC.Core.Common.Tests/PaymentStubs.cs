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

namespace ASC.Core.Common.Tests;

/// <summary>
/// The billing, accounting and docscloud clients as production registers them, pointed at <see cref="HttpStub"/>s.
/// </summary>
/// <remarks>
/// The clients read their base URL once, when they are registered, so the stub's address goes into the configuration
/// before <c>Add*HttpClient</c> runs. A service left without a stub gets no URL and reports itself not configured.
/// </remarks>
internal static class PaymentStubs
{
    public const string Key = "test-key";
    public const string Secret = "test-secret";

    /// <summary>The sub-path the accounting service is published under, as in the real configuration.</summary>
    public const string AccountingPath = "/api";

    public static (BillingClient client, HttpStub stub) CreateBillingClient(Func<StubRequest, StubResponse> respond, string path = "", bool test = false)
    {
        return CreateBillingClient(new HttpStub(respond), path, test);
    }

    public static (BillingClient client, HttpStub stub) CreateBillingClient(HttpStub stub, string path = "", bool test = false)
    {
        return (BuildProvider(billing: stub, billingPath: path, billingTest: test).GetRequiredService<BillingClient>(), stub);
    }

    public static (AccountingClient client, HttpStub stub) CreateAccountingClient(Func<StubRequest, StubResponse> respond, string path = AccountingPath)
    {
        return CreateAccountingClient(new HttpStub(respond), path);
    }

    public static (AccountingClient client, HttpStub stub) CreateAccountingClient(Func<StubRequest, Task<StubResponse>> respond, string path = AccountingPath)
    {
        return CreateAccountingClient(new HttpStub(respond), path);
    }

    public static (DocsCloudClient client, HttpStub stub) CreateDocsCloudClient(Func<StubRequest, StubResponse> respond)
    {
        return CreateDocsCloudClient(new HttpStub(respond));
    }

    public static (DocsCloudClient client, HttpStub stub) CreateDocsCloudClient(HttpStub stub)
    {
        return (BuildProvider(docsCloud: stub).GetRequiredService<DocsCloudClient>(), stub);
    }

    /// <summary>
    /// A provider with all three clients registered; each one points at its stub, or is left unconfigured without one.
    /// </summary>
    public static ServiceProvider BuildProvider(
        HttpStub? billing = null,
        HttpStub? accounting = null,
        HttpStub? docsCloud = null,
        string billingPath = "",
        bool billingTest = false,
        string accountingPath = AccountingPath)
    {
        var settings = new Dictionary<string, string?>
        {
            ["core:payment:key"] = Key,
            ["core:payment:secret"] = Secret,
            ["core:payment:test"] = billingTest.ToString(),
            ["core:accounting:key"] = Key,
            ["core:accounting:secret"] = Secret,
            ["core:docscloud:key"] = Key,
            ["core:docscloud:secret"] = Secret
        };

        if (billing is not null)
        {
            settings["core:payment:url"] = billing.Url + billingPath;
        }

        if (accounting is not null)
        {
            settings["core:accounting:url"] = accounting.Url + accountingPath;
        }

        if (docsCloud is not null)
        {
            settings["core:docscloud:url"] = docsCloud.Url + "/api/";
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(typeof(ICache), typeof(AscCache));
        services.AddScoped<BillingClient>();
        services.AddScoped<AccountingClient>();
        services.AddScoped<DocsCloudClient>();
        services.AddFusionCache();

        services.AddBillingHttpClient(configuration);
        services.AddAccountingHttpClient(configuration);
        services.AddDocsCloudHttpClient(configuration);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// A <see cref="TariffService"/> over the given payment clients. Every dependency a test leaves out is null, so a
    /// path that reaches one it was not meant to fails at once instead of quietly using a default.
    /// </summary>
    public static TariffService CreateTariffService(
        AccountingClient? accountingClient = null,
        BillingClient? billingClient = null,
        IFusionCache? hybridCache = null,
        CoreSettings? coreSettings = null,
        IConfiguration? configuration = null,
        ICache? cache = null,
        IServiceProvider? serviceProvider = null)
    {
        return new TariffService(
            quotaService: null!,
            tenantService: null!,
            coreBaseSettings: null!,
            coreSettings: coreSettings!,
            configuration: configuration!,
            coreDbContextManager: null!,
            cache: cache!,
            hybridCache: hybridCache!,
            distributedLockProvider: null!,
            logger: NullLogger<TariffService>.Instance,
            billingClient: billingClient!,
            accountingClient: accountingClient!,
            docsCloudClient: null!,
            serviceProvider: serviceProvider!,
            resiliencePipelineProvider: null!,
            tenantExtraConfig: null!);
    }

    /// <summary>
    /// SaaS core settings over a tenant service that knows no tenants, so a portal gets the portal ID
    /// "<paramref name="paymentRegion"/><c>{tenant ID}</c>".
    /// </summary>
    public static CoreSettings CreateCoreSettings(string paymentRegion)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["core:payment:region"] = paymentRegion })
            .Build();

        return new CoreSettings(DefaultResultProxy<ITenantService>.Create(), new CoreBaseSettings(configuration), configuration, null!);
    }

    /// <summary>A JSON answer; named apart from <c>Json</c>, which the enclosing <c>ASC.Core.Common.Json</c> namespace takes.</summary>
    public static StubResponse Reply(HttpStatusCode status, string json)
    {
        return StubResponse.Json(status, json);
    }

    public static StubResponse TransportFault()
    {
        return StubResponse.TransportFault();
    }

    /// <summary>The authorization billing and accounting expect: <c>ASC {key}:{timestamp}:{HMAC-SHA1 of "{timestamp}\n{key}"}</c>.</summary>
    public static string ExpectedToken(string key, string secret, string timestamp)
    {
        using var hasher = new HMACSHA1(Encoding.UTF8.GetBytes(secret));
        var hash = WebEncoders.Base64UrlEncode(hasher.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", timestamp, key))));

        return $"ASC {key}:{timestamp}:{hash}";
    }

    public static Dictionary<string, string> ParseQuery(Uri uri)
    {
        var result = new Dictionary<string, string>();
        var query = uri.Query.TrimStart('?');

        if (string.IsNullOrEmpty(query))
        {
            return result;
        }

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            var name = Uri.UnescapeDataString(idx < 0 ? pair : pair[..idx]);
            var value = idx < 0 ? "" : Uri.UnescapeDataString(pair[(idx + 1)..]);
            result[name] = value;
        }

        return result;
    }

    public static List<string> QueryValues(Uri uri, string name)
    {
        return uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(pair => Uri.UnescapeDataString(pair[0]) == name)
            .Select(pair => pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : "")
            .ToList();
    }

    public static (AccountingClient client, HttpStub stub) CreateAccountingClient(HttpStub stub, string path = AccountingPath)
    {
        return (BuildProvider(accounting: stub, accountingPath: path).GetRequiredService<AccountingClient>(), stub);
    }
}
