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

namespace ASC.Files.Tests.ApiFactories;

/// <summary>
/// Owns the shared Aspire application for the whole test assembly. It does NOT hold any
/// tenant-scoped HTTP clients: every test creates its own portal and its own
/// <see cref="PortalClients"/> through <see cref="CreatePortalClients"/>, so test classes can run
/// in parallel without sharing tenant state.
/// </summary>
public class AspireAppFixture : IAsyncLifetime
{
    private const string OnlyofficeFiles = "onlyoffice-files";
    private const string OnlyofficePeople = "onlyoffice-people";
    private const string OnlyofficeWebApi = "onlyoffice-web-api";
    private const string OnlyofficeApiSystem = "onlyoffice-apisystem";

    private static readonly JsonSerializerOptions _apiSystemJsonOptions = new(JsonSerializerDefaults.Web);

    private DistributedApplication _app = null!;
    private HttpClient _apiSystemClient = null!;

    // A single connection pool shared by every per-test HttpClient. The clients stay per-test (so
    // their Origin/Authorization default headers never collide across parallel tests), but they all
    // reuse this one handler, which avoids creating ~1000+ short-lived connection pools over a run
    // and the socket churn / TIME_WAIT exhaustion that comes with it.
    private readonly SocketsHttpHandler _sharedHandler = new() { UseCookies = false };

    private Uri FilesBaseAddress { get; set; } = null!;
    private Uri PeopleBaseAddress { get; set; } = null!;
    private Uri WebApiBaseAddress { get; set; } = null!;

    public async ValueTask InitializeAsync()
    {
        // Start Aspire AppHost with integration-test profile.
        // APP_HOSTING_STANDALONE=true makes the platform resolve the current tenant from the
        // Origin header first (see TenantManager.SetCurrentTenantAsync), which is how every test
        // is scoped to its own freshly registered portal.
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.ASC_AppHost>(
            ["DOTNET_LAUNCH_PROFILE=integration-test", "SKIP_CLIENT=true", "APP_HOSTING_STANDALONE=true"]);

        appHost.Configuration["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "";
        appHost.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "";

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        // Wait for services to be healthy
        var resourceNotifications = _app.ResourceNotifications;

        await Task.WhenAll(
            resourceNotifications.WaitForResourceHealthyAsync(OnlyofficeFiles),
            resourceNotifications.WaitForResourceHealthyAsync(OnlyofficePeople),
            resourceNotifications.WaitForResourceHealthyAsync(OnlyofficeWebApi),
            resourceNotifications.WaitForResourceHealthyAsync(OnlyofficeApiSystem));

        FilesBaseAddress = ResolveBaseAddress(OnlyofficeFiles);
        PeopleBaseAddress = ResolveBaseAddress(OnlyofficePeople);
        WebApiBaseAddress = ResolveBaseAddress(OnlyofficeWebApi);

        // A single ApiSystem client is enough: portal registration is tenant-agnostic and only
        // issues stateless POSTs, so it is safe to share across parallel tests.
        _apiSystemClient = CreateRawClient(ResolveBaseAddress(OnlyofficeApiSystem), origin: null);

        await InitializePasswordSaltAsync();
    }

    /// <summary>
    /// Registers a brand-new portal and returns a fresh, self-contained <see cref="PortalClients"/>
    /// bundle bound to it (clients scoped via the <c>Origin</c> header, plus the portal's own owner).
    /// Each test owns its own bundle, which is what makes the tests safe to run in parallel.
    /// </summary>
    public async Task<PortalClients> CreatePortalAsync(CancellationToken cancellationToken = default)
    {
        // Lowercase, starts with a letter, 13 chars (> 6) — a valid portal alias and Origin host.
        var portalName = "t" + Guid.NewGuid().ToString("N")[..12];

        var registration = await RegisterPortalAsync(new RegisterPortalModel
        {
            PortalName = portalName,
            FirstName = "Portal",
            LastName = "Owner",
            Email = Initializer.OwnerEmail,
            Password = Initializer.OwnerPassword,
            Language = "en-US",
            TimeZoneName = "UTC",
            SkipWelcome = true
        }, cancellationToken);

        // The owner Id is unique to this portal and comes straight from the registration response.
        var owner = new User(Initializer.OwnerEmail, Initializer.OwnerPassword)
        {
            Id = registration.Tenant.OwnerId,
            PasswordHash = Initializer.GetClientPassword(Initializer.OwnerPassword)
        };

        return new PortalClients(FilesBaseAddress, PeopleBaseAddress, WebApiBaseAddress, portalName, owner, CreateRawClient);
    }

    /// <summary>
    /// Registers a new portal through the ASC.ApiSystem <c>portal/register</c> endpoint.
    /// </summary>
    public async Task<PortalRegistrationResult> RegisterPortalAsync(RegisterPortalModel model, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(model, _apiSystemJsonOptions);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _apiSystemClient.PostAsync("portal/register", content, cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Portal registration failed ({(int)response.StatusCode}): {body}");
        }

        return JsonSerializer.Deserialize<PortalRegistrationResult>(body, _apiSystemJsonOptions)!;
    }

    private async Task InitializePasswordSaltAsync()
    {
        // The password-hash salt is derived from the machine key and is identical for every portal,
        // so it is fetched once from the default tenant (no Origin header) and shared by all tests.
        using var defaultClient = CreateRawClient(WebApiBaseAddress, origin: null);
        var commonSettingsApi = new CommonSettingsApi(defaultClient, new Configuration { BasePath = WebApiBaseAddress.ToString().TrimEnd('/') });

        var settings = (await commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        Initializer.InitializePasswordHasher(settings.PasswordHash);
    }

    private Uri ResolveBaseAddress(string resourceName)
    {
        using var baseClient = _app.CreateHttpClient(resourceName);
        return baseClient.BaseAddress!;
    }

    private HttpClient CreateRawClient(Uri baseAddress, string? origin)
    {
        // disposeHandler: false — the shared connection pool outlives individual clients and is
        // disposed once with the fixture. Disposing a per-test client must NOT tear down the pool.
        var client = new HttpClient(_sharedHandler, disposeHandler: false) { BaseAddress = baseAddress };

        if (!string.IsNullOrEmpty(origin))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Origin", origin);
        }

        return client;
    }

    public async ValueTask DisposeAsync()
    {
        _apiSystemClient.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        _sharedHandler.Dispose();
    }
}
