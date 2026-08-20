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

namespace ASC.Notify.Tests.Infrastructure;

/// <summary>
/// Everything a letter test needs from the outside world, started once for the whole assembly:
///
/// <list type="bullet">
/// <item>a migrated database with a freshly registered portal — a notify action's <c>Init</c> resolves
/// links, and shortens them, against a real tenant;</item>
/// <item>the DocSpace service graph in this process (<see cref="LetterHost"/>), which is what makes the
/// action resolvable at all;</item>
/// <item>the MailPit the rendered letters are delivered to.</item>
/// </list>
///
/// The Aspire graph is trimmed to that before the host is built: the <c>integration-test</c> profile
/// also brings up six services, OpenResty and the document server, and a letter needs none of them.
/// </summary>
public sealed class LetterStackFixture : IAsyncLifetime
{
    private const string MailPitResource = "mailpit";
    private const string ApiSystemResource = "onlyoffice-apisystem";
    private const string MigrateResource = "migrate";
    private const string DatabaseResource = "docspace";

    /// <summary>
    /// What stays in the graph. Redis (<c>cache</c>), RabbitMQ (<c>messaging</c>) and OpenSearch are
    /// kept even though <see cref="LetterHost"/> switches the first two off for itself: every project in
    /// the graph waits for them (<c>ProjectConfigurator.AddWaitFor</c>), so dropping them would strand
    /// ApiSystem on a dependency that no longer exists.
    /// </summary>
    private static readonly string[] _keptResources =
    [
        "mysql", DatabaseResource, "mysql-root-password", MigrateResource,
        MailPitResource, ApiSystemResource, "messaging", "cache", "opensearch"
    ];

    /// <summary>
    /// Generous, because a cold run builds and starts the migration runner and ApiSystem on top of
    /// pulling container images. Bounded, because a container that dies on startup never reports any
    /// state at all and would otherwise hang the whole assembly instead of failing it.
    /// </summary>
    private static readonly TimeSpan _startUpBudget = TimeSpan.FromMinutes(15);

    private static readonly JsonSerializerOptions _apiSystemJson = new(JsonSerializerDefaults.Web);

    // Nullable, and disposed as such: InitializeAsync builds these one after another over minutes of
    // containers and migrations, and xUnit disposes the fixture even when it threw halfway. Declaring
    // them null-forgiving would turn any startup failure into an NRE out of DisposeAsync, which is the
    // exception the run then reports instead of the one that actually broke the stack.
    private DistributedApplication? _app;
    private HttpClient? _apiSystem;
    private HttpClient? _mailPitApi;
    private MailPitInbox? _inbox;
    private LetterHost? _host;
    private LetterPortal? _portal;

    /// <summary>The inbox every letter test delivers to and reads its score back from.</summary>
    internal MailPitInbox Inbox => _inbox ?? throw NotStarted();

    /// <summary>The service graph a letter test resolves its notify action from.</summary>
    internal LetterHost Host => _host ?? throw NotStarted();

    /// <summary>The portal the letters are rendered for.</summary>
    internal LetterPortal Portal => _portal ?? throw NotStarted();

    private static InvalidOperationException NotStarted([CallerMemberName] string member = "")
    {
        return new InvalidOperationException(
            $"The letter stack has no {member}: InitializeAsync did not get that far. The failure that "
            + "stopped it is the one to read, not this.");
    }

    public async ValueTask InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.ASC_AppHost>(
            ["DOTNET_LAUNCH_PROFILE=integration-test", "SKIP_CLIENT=true", "APP_HOSTING_STANDALONE=true"]);

        appHost.Configuration["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "";
        appHost.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "";

        Trim(appHost);

        _app = await appHost.BuildAsync();

        await _app.StartAsync();

        using var startUp = new CancellationTokenSource(_startUpBudget);

        // The migration runner is a one-shot project every service waits to exit, and so must this
        // fixture: the schema it creates is what the portal is registered into.
        await _app.ResourceNotifications.WaitForResourceAsync(
            MigrateResource, KnownResourceStates.Finished, startUp.Token);

        await Task.WhenAll(
            _app.ResourceNotifications.WaitForResourceHealthyAsync(MailPitResource, startUp.Token),
            _app.ResourceNotifications.WaitForResourceHealthyAsync(ApiSystemResource, startUp.Token));

        // Two named endpoints: "smtp" for delivery, "http" for the web API. Aspire publishes both on
        // random host ports, which is why nothing here is hard-coded.
        var smtp = _app.GetEndpoint(MailPitResource, "smtp");

        _mailPitApi = _app.CreateHttpClient(MailPitResource, "http");

        _inbox = new MailPitInbox(smtp.Host, smtp.Port, _mailPitApi);

        _apiSystem = _app.CreateHttpClient(ApiSystemResource);

        _portal = await RegisterPortalAsync(_apiSystem, startUp.Token);

        var connectionString = await _app.GetConnectionStringAsync(DatabaseResource, startUp.Token)
            ?? throw new InvalidOperationException(
                $"Aspire published no connection string for '{DatabaseResource}'.");

        _host = await LetterHost.BuildAsync(connectionString, _portal.Url);
    }

    /// <summary>
    /// Drops everything outside <see cref="_keptResources"/>. Parameter resources stay whatever they are
    /// named: Aspire adds them for passwords and endpoints, and a kept resource may point at one.
    /// </summary>
    private static void Trim(IDistributedApplicationTestingBuilder appHost)
    {
        var doomed = appHost.Resources
            .Where(resource => !_keptResources.Contains(resource.Name) && resource is not ParameterResource)
            .ToArray();

        foreach (var resource in doomed)
        {
            appHost.Resources.Remove(resource);
        }
    }

    /// <summary>
    /// Registers a portal through ApiSystem, the way the Files and People suites do. The letters need a
    /// real owner: an action reads the recipient's email and name, and the tenant the migrations seed
    /// has an owner with no email at all.
    /// </summary>
    private static async Task<LetterPortal> RegisterPortalAsync(HttpClient apiSystem, CancellationToken cancellationToken)
    {
        // Lowercase, starts with a letter, 13 chars — a valid portal alias, which is also the host the
        // portal answers on while `core:base-domain` is empty.
        var alias = "t" + Guid.NewGuid().ToString("N")[..12];

        // This exact address is what AllowPortalRegistration puts into `web:autotest:secret-email`,
        // which is what lets registration skip the rate limit and the recaptcha.
        var payload = JsonSerializer.Serialize(new
        {
            PortalName = alias,
            FirstName = "Portal",
            LastName = "Owner",
            Email = "test@example.com",
            Password = "11111111",
            Language = LetterCultures.DefaultCultureName,
            TimeZoneName = "UTC",
            // Otherwise registration queues a welcome letter of its own.
            SkipWelcome = true
        }, _apiSystemJson);

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await apiSystem.PostAsync("portal/register", content, cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Portal registration failed ({(int)response.StatusCode}): {body}");
        }

        var registration = JsonSerializer.Deserialize<PortalRegistrationResult>(body, _apiSystemJson)
            ?? throw new InvalidOperationException($"Unreadable registration response: {body}");

        // Not the alias: `core:base-domain` is `localhost` here — both in buildtools/config and from the
        // AppHost, which sets it for every standalone project — and Tenant.GetTenantDomain
        // short-circuits on that to `localhost` whatever the alias is. So a registered portal answers on
        // the address the stack publishes, exactly like the letters have always assumed.
        // LetterScope checks that against CommonLinkUtility rather than trusting this comment.
        return new LetterPortal(
            registration.Tenant.TenantId,
            registration.Tenant.OwnerId,
            alias,
            LetterEnvironment.PortalUrl);
    }

    public async ValueTask DisposeAsync()
    {
        if (_host != null)
        {
            await _host.DisposeAsync();
        }

        _inbox?.Dispose();
        _mailPitApi?.Dispose();
        _apiSystem?.Dispose();

        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}

/// <summary>The registered portal the letters are rendered for.</summary>
internal sealed record LetterPortal(int TenantId, Guid OwnerId, string Alias, string Url);

/// <summary>The part of the ApiSystem <c>portal/register</c> response the letters need.</summary>
internal sealed record PortalRegistrationResult
{
    public PortalTenant Tenant { get; init; } = null!;
}

/// <summary>The tenant information embedded in a portal registration response.</summary>
internal sealed record PortalTenant
{
    public int TenantId { get; init; }

    public Guid OwnerId { get; init; }
}
