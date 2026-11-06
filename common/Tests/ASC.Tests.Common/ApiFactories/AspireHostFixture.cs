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

namespace ASC.Tests.Common.ApiFactories;

/// <summary>
/// Owns the shared Aspire application for the whole test assembly. It does NOT hold any
/// tenant-scoped HTTP clients: every test creates its own portal and its own client bundle through
/// <see cref="CreatePortalAsync"/>, so test classes can run in parallel without sharing tenant state.
/// </summary>
/// <remarks>
/// A suite derives from this once, names the services it needs in <see cref="Resources"/>, and builds
/// its own bundle in <see cref="CreateClients"/>. The derived type has to be concrete and non-generic,
/// because it is what <c>[assembly: AssemblyFixture(...)]</c> names.
/// </remarks>
public abstract class AspireHostFixture<TClients> : IAsyncLifetime where TClients : PortalClientsBase
{
    // Tenant.DefaultTenant — the tenant portal-independent settings are stored under.
    private const int DefaultTenantId = -1;

    /// <summary>
    /// Generous, because a cold run builds every project in the graph and pulls the container images on
    /// top of starting them. Bounded, because a container that dies on startup never reports any state
    /// at all and would otherwise hang the whole assembly instead of failing it.
    /// </summary>
    private static readonly TimeSpan _startUpBudget = TimeSpan.FromMinutes(15);

    private static readonly JsonSerializerOptions _apiSystemJsonOptions = new(JsonSerializerDefaults.Web);

    private DistributedApplication _app = null!;
    private HttpClient _apiSystemClient = null!;

    private readonly Dictionary<string, Uri> _baseAddresses = [];

    // Local and in whole seconds, like the dates in the log it is compared with.
    private readonly DateTime _startedAt = new(DateTime.Now.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond);

    // A single connection pool shared by every per-test HttpClient. The clients stay per-test (so
    // their Origin/Authorization default headers never collide across parallel tests), but they all
    // reuse this one handler, which avoids creating a short-lived connection pool per test and the
    // socket churn / TIME_WAIT exhaustion that comes with it.
    //
    // The links a portal returns carry its own host, <alias>.docspace.localhost (see SetBaseDomainAsync).
    // Windows does not resolve *.localhost, so the handler connects every such host to the loopback itself.
    // "localhost" goes there too: Windows tries ::1 first, and the ports Docker publishes (identity, OpenSearch)
    // listen on 127.0.0.1 only, so every new connection waited ~2 s for the refusal before falling back.
    private readonly SocketsHttpHandler _sharedHandler = new() { UseCookies = false, ConnectCallback = ConnectAsync };

    /// <summary>The base domain of the test portals: each one is <c>&lt;alias&gt;.docspace.localhost</c>.</summary>
    public const string BaseDomain = "docspace.localhost";

    private static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        EndPoint endPoint = host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            ? new IPEndPoint(IPAddress.Loopback, context.DnsEndPoint.Port)
            : context.DnsEndPoint;

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

        try
        {
            await socket.ConnectAsync(endPoint, cancellationToken);

            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    /// <summary>
    /// The services this suite needs on top of ApiSystem and WebApi, which are always started
    /// (portal registration and authentication). Names come from <see cref="ResourceNames"/>.
    /// </summary>
    protected abstract IEnumerable<string> Resources { get; }

    /// <summary>
    /// Services only some test classes of the suite need. They start with the stack and get their clients at once,
    /// but the run does not wait for them: a test that uses them calls <see cref="WaitForLazyResourcesAsync"/>, so the
    /// other classes start as soon as <see cref="Resources"/> are healthy.
    /// </summary>
    protected virtual IEnumerable<string> LazyResources => [];

    private readonly Lock _routerLock = new();
    private Task? _routerUp;

    private readonly Lock _lazyResourcesLock = new();
    private Task? _lazyResourcesHealthy;

    /// <summary>
    /// The AppHost launch profile this suite's stack is built from. The profile is where the resource
    /// graph is decided (the switch in <c>ASC.AppHost/Program.cs</c>), so a suite that needs a smaller
    /// graph than <c>integration-test</c> gets a profile of its own there rather than pruning the graph
    /// here — one mechanism for "what starts", not two.
    /// </summary>
    protected virtual string LaunchProfile => "integration-test";

    /// <summary>
    /// Whether the stack runs as a standalone installation (the default) or as SaaS. Standalone resolves the current
    /// tenant from the <c>Origin</c> header first, which scopes each test to its portal, but gives every portal the
    /// installation's one payment key. A SaaS stack gives each portal a key of its own; it resolves the tenant from
    /// the request host, so the clients send the portal as <c>Host</c> too, and it names the portals itself.
    /// </summary>
    protected virtual bool Standalone => true;

    /// <summary>Builds the suite's own client bundle for a freshly registered portal.</summary>
    protected abstract TClients CreateClients(PortalContext context);

    /// <summary>
    /// The stack is about to start. For whatever the services reach from the moment they come up and so has to
    /// answer before they do, such as a stub of an external service.
    /// </summary>
    protected virtual ValueTask OnStartingAsync()
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// The app is up and portals can be registered, but no test has run yet. For whatever a suite has
    /// to stand up once against the running stack.
    /// </summary>
    protected virtual ValueTask OnStartedAsync()
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Runs before the app is torn down, so a suite can dispose what still holds handles into it.
    /// </summary>
    protected virtual ValueTask OnDisposingAsync()
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Runs once the services of the app have stopped. For what they call until then - a stub of an external service
    /// started in <see cref="OnStartingAsync"/> - so a service shutting down never meets a closed port, and its log
    /// has no connection errors a test run did not cause.
    /// </summary>
    protected virtual ValueTask OnStoppedAsync()
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Suite-specific warmup, run once against a throwaway portal whose owner is already signed in
    /// on <see cref="PortalClientsBase.WebApiHttpClient"/>. Touch whatever every test hits first.
    /// </summary>
    protected virtual ValueTask WarmUpAsync(TClients clients)
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask InitializeAsync()
    {
        await OnStartingAsync();

        // Start the Aspire AppHost with the suite's launch profile.
        // APP_HOSTING_STANDALONE=true makes the platform resolve the current tenant from the
        // Origin header first (see TenantManager.SetCurrentTenantAsync), which is how every test
        // is scoped to its own freshly registered portal; see Standalone for the SaaS stack.
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.ASC_AppHost>(
            [$"DOTNET_LAUNCH_PROFILE={LaunchProfile}", "SKIP_CLIENT=true", $"APP_HOSTING_STANDALONE={(Standalone ? "true" : "false")}"]);

        appHost.Configuration["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "";
        appHost.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "";

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        // Wait for services to be healthy
        var resourceNotifications = _app.ResourceNotifications;
        var resources = new[] { ResourceNames.ApiSystem, ResourceNames.WebApi }.Union(Resources).ToList();

        // Bounded: a resource that dies without ever reporting a state would otherwise leave the whole
        // assembly waiting forever instead of failing it.
        using var startUp = new CancellationTokenSource(_startUpBudget);

        try
        {
            await Task.WhenAll(resources.Select(r => resourceNotifications.WaitForResourceHealthyAsync(r, startUp.Token)));
        }
        catch (OperationCanceledException) when (startUp.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"The Aspire stack did not come up within {_startUpBudget.TotalMinutes} minutes. It was "
                + $"waited on for: {string.Join(", ", resources)}. The log of whichever of them never "
                + "went healthy is the one to read, not this.");
        }

        foreach (var resource in resources.Union(LazyResources))
        {
            _baseAddresses[resource] = ResolveBaseAddress(resource);
        }

        // A lazy Nextcloud counts too: its address is known once the stack runs, and the tests that use it wait for
        // it themselves before they read these settings.
        if (resources.Union(LazyResources).Contains(ResourceNames.Nextcloud))
        {
            PublishNextcloudSettings();
        }

        // A single ApiSystem client is enough: portal registration is tenant-agnostic and only
        // issues stateless POSTs, so it is safe to share across parallel tests.
        _apiSystemClient = CreateRawClient(_baseAddresses[ResourceNames.ApiSystem], origin: null);

        // Before the first request of the run — see the method for why the order matters.
        await DisableStartDocsAsync();

        await InitializePasswordSaltAsync();

        // A SaaS installation takes its base domain from the configuration; ApiSystem saves it for a server one only.
        if (Standalone)
        {
            await SetBaseDomainAsync();
        }

        await OnStartedAsync();

        // Warm up the full portal flow ONCE, serially, before any test runs. Without this, the moment
        // the host goes healthy every parallel test class fires portal/register simultaneously against
        // a completely cold system (JIT of the large registration flow, EF model init, first DB pool
        // open, cache population). That thundering herd of cold registrations contends heavily and can
        // stall. Paying those one-time costs once here keeps the parallel flood hitting warm code paths.
        await WarmUpAsync();
    }

    /// <summary>
    /// Exercises the same end-to-end path a test does (register a throwaway portal, authenticate its
    /// owner, plus whatever the suite adds) so all the cold, one-time code paths are warm before the
    /// parallel test run begins. Failures are non-fatal: warmup is an optimization, not a gate.
    /// </summary>
    private async Task WarmUpAsync()
    {
        try
        {
            await Timing.Measure("warmup.total", async () =>
            {
                using var clients = await CreatePortalAsync();

                await clients.WebApiHttpClient.Authenticate(clients.Owner);

                await WarmUpAsync(clients);
            });
        }
        catch (Exception e)
        {
            Timing.Write($"warmup.failed({e.GetType().Name}: {e.Message})", 0);
        }
    }

    /// <summary>
    /// Turns the sample documents ("start docs") off for every portal of the run.
    /// </summary>
    /// <remarks>
    /// On the first access to a My or Common folder, <c>Global.GetFolderIdAndProcessFirstVisitAsync</c>
    /// starts an unawaited task that copies ~2 MB of sample documents out of DocStore into the portal.
    /// The request does not wait for it, but every test registers its own portal, so that copying
    /// (storage writes, file rows, new-item marks, socket notifications) runs concurrently with the
    /// suite and races with anything that reads My right after registration. Nothing here depends on
    /// those documents.
    ///
    /// Written straight to the database rather than through <c>POST settings/rebranding/additional</c>:
    /// that endpoint demands the white-label permission, which a test portal — with no paid quota —
    /// does not have, so it answers 402. The row belongs to the DEFAULT tenant, which is where
    /// <c>SettingsManager.LoadForDefaultTenantAsync</c> reads it from, hence one row for the whole run.
    ///
    /// It has to happen before the first HTTP request: settings are cached for five minutes, and the
    /// cache is populated by the first reader (the password settings read right after this).
    ///
    /// The payload is what <c>AdditionalWhiteLabelSettings.GetDefault()</c> produces for the shipped
    /// <c>externalresources.json</c> — every domain there is non-empty, so every flag is true — with
    /// <c>StartDocsEnabled</c> flipped, so nothing but the sample documents changes.
    ///
    /// A failure is reported and left non-fatal: the suite still passes, just slower.
    /// </remarks>
    /// <summary>
    /// Gives the installation a base domain the way a server installation sets it: through ApiSystem
    /// <c>settings/save</c>, stored for tenant -1. The services keep <c>core:base-domain=localhost</c>, which is
    /// what switches the standalone mode on, but with only that every portal's domain is "localhost"
    /// (<c>Tenant.GetTenantDomain</c>): the links the server hands the document server (a docbuilder script, a
    /// file to convert, a callback) then led to the default portal and were refused there. With the base domain
    /// each portal is <c>&lt;alias&gt;.docspace.localhost</c> and those links reach the portal that signed them.
    /// On a server installation the call needs a DocSpace admin, so it is made as the owner of a portal of its own.
    /// </summary>
    private async Task SetBaseDomainAsync()
    {
        await Timing.Measure("basedomain.set", async () =>
        {
            using var clients = await CreatePortalAsync(TestContext.Current.CancellationToken);
            await clients.WebApiHttpClient.Authenticate(clients.Owner);

            using var apiSystem = CreateRawClient(_baseAddresses[ResourceNames.ApiSystem], $"http://{clients.PortalName}");
            apiSystem.DefaultRequestHeaders.Authorization = clients.WebApiHttpClient.DefaultRequestHeaders.Authorization;

            using var response = await apiSystem.PostAsJsonAsync(
                "settings/save",
                new { tenantId = -1, key = "BaseDomain", value = BaseDomain },
                TestContext.Current.CancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

                throw new HttpRequestException($"Setting the base domain failed ({(int)response.StatusCode}): {body}");
            }
        });
    }

    private async Task DisableStartDocsAsync()
    {
        // AdditionalWhiteLabelSettings.ID, as EF stores it in the varchar(64) key column.
        const string settingsId = "0108422f-c05d-488e-b271-30c4032494da";
        const string settingsData =
            """
            {"StartDocsEnabled":false,"HelpCenterEnabled":true,"FeedbackAndSupportEnabled":true,"UserForumEnabled":true,"VideoGuidesEnabled":true,"LicenseAgreementsEnabled":true}
            """;

        var cancellationToken = TestContext.Current.CancellationToken;

        try
        {
            await Timing.Measure("startdocs.disable", async () =>
            {
                var connectionString = await _app.GetConnectionStringAsync(ResourceNames.Database, cancellationToken)
                    ?? throw new InvalidOperationException($"No connection string for the '{ResourceNames.Database}' resource.");

                await using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);

                await using var command = connection.CreateCommand();

                command.CommandText =
                    """
                    INSERT INTO webstudio_settings (TenantID, ID, UserID, Data, last_modified)
                    VALUES (@tenantId, @id, @userId, @data, UTC_TIMESTAMP())
                    ON DUPLICATE KEY UPDATE Data = @data, last_modified = UTC_TIMESTAMP();
                    """;

                command.Parameters.AddWithValue("@tenantId", DefaultTenantId);
                command.Parameters.AddWithValue("@id", settingsId);
                command.Parameters.AddWithValue("@userId", Guid.Empty.ToString());
                command.Parameters.AddWithValue("@data", settingsData);

                await command.ExecuteNonQueryAsync(cancellationToken);
            });
        }
        catch (Exception e)
        {
            Timing.Write($"startdocs.disable.failed({e.GetType().Name}: {e.Message})", 0);
        }
    }

    /// <summary>
    /// Registers a brand-new portal and returns a fresh, self-contained client bundle bound to it
    /// (clients scoped via the <c>Origin</c> header, plus the portal's own owner).
    /// Each test owns its own bundle, which is what makes the tests safe to run in parallel.
    /// </summary>
    public async Task<TClients> CreatePortalAsync(CancellationToken cancellationToken = default)
    {
        // Lowercase, starts with a letter, 13 chars (> 6) — a valid portal alias and Origin host.
        var portalName = "t" + Guid.NewGuid().ToString("N")[..12];

        var registration = await Timing.Measure("portal.register", () => RegisterPortalAsync(new RegisterPortalModel
        {
            PortalName = portalName,
            FirstName = "Portal",
            LastName = "Owner",
            Email = Initializer.OwnerEmail,
            Password = Initializer.OwnerPassword,
            Language = "en-US",
            TimeZoneName = "UTC",
            SkipWelcome = true
        }, cancellationToken));

        // A SaaS ApiSystem ignores the requested name and picks one of its own.
        if (!Standalone)
        {
            portalName = registration.Tenant.PortalName;
        }

        // The owner Id is unique to this portal and comes straight from the registration response.
        var owner = new User(Initializer.OwnerEmail, Initializer.OwnerPassword)
        {
            Id = registration.Tenant.OwnerId,
            PasswordHash = Initializer.GetClientPassword(Initializer.OwnerPassword)
        };

        return CreateClients(new PortalContext(
            portalName, registration.Tenant.TenantId, owner, _baseAddresses, CreateRawClient));
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
        var baseAddress = _baseAddresses[ResourceNames.WebApi];
        using var defaultClient = CreateRawClient(baseAddress, origin: null);

        var settingsApi = new CommonSettingsApi(
            defaultClient,
            new Configuration { BasePath = baseAddress.ToString().TrimEnd('/') });

        var settings = (await settingsApi.GetPortalSettingsAsync(
            withpassword: true,
            TestContext.Current.CancellationToken)).Response;

        Initializer.InitializePasswordHasher(settings.PasswordHash);
    }

    /// <summary>
    /// The connection string Aspire published for a resource — for a suite that talks to a container
    /// directly instead of through a service.
    /// </summary>
    protected async Task<string> GetConnectionStringAsync(string resourceName, CancellationToken cancellationToken = default)
    {
        return await _app.GetConnectionStringAsync(resourceName, cancellationToken)
            ?? throw new InvalidOperationException($"Aspire published no connection string for '{resourceName}'.");
    }

    /// <summary>
    /// An open connection to the stack's database, for a test that has to plant a row no API call
    /// writes any more — data left behind by an older release that the current code must still read.
    /// </summary>
    public async Task<MySqlConnection> OpenDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(await GetConnectionStringAsync(ResourceNames.Database, cancellationToken));
        await connection.OpenAsync(cancellationToken);

        return connection;
    }

    /// <summary>
    /// Whether the suite's launch profile starts the resource at all - the environment signal for a feature whose
    /// container only some profiles include (<see cref="ResourceNames.DocSpaceMcp"/>).
    /// </summary>
    public bool HasResource(string resourceName) =>
        _app.Services.GetRequiredService<DistributedApplicationModel>().Resources.Any(resource => resource.Name == resourceName);

    /// <summary>
    /// A value of the AppHost configuration. The services read the same <c>buildtools/config</c>
    /// files, so a setting such as <c>core:machinekey</c> read here is the one they work with.
    /// </summary>
    public string GetAppHostSetting(string key)
    {
        return _app.Services.GetRequiredService<IConfiguration>()[key]
            ?? throw new InvalidOperationException($"The AppHost configuration has no '{key}'.");
    }

    /// <summary>
    /// One of a resource's named endpoints, as the host can reach it. Needed for anything that is not
    /// the default HTTP endpoint — MailPit, for instance, publishes both <c>smtp</c> and <c>http</c>,
    /// and Aspire gives each of them a random host port.
    /// </summary>
    protected Uri GetEndpoint(string resourceName, string endpointName)
    {
        return _app.GetEndpoint(resourceName, endpointName);
    }

    /// <summary>An <see cref="HttpClient"/> for a resource's named HTTP endpoint.</summary>
    protected HttpClient CreateHttpClient(string resourceName, string endpointName)
    {
        return _app.CreateHttpClient(resourceName, endpointName);
    }

    /// <summary>
    /// Hands the started Nextcloud to the tests the way the TypeScript suite is configured: through
    /// <c>NEXTCLOUD_URL</c> (the WebDAV root of the account), <c>NEXTCLOUD_LOGIN</c> and
    /// <c>NEXTCLOUD_PASSWORD</c>. Values already present in the environment win, so a run can still be
    /// pointed at a real Nextcloud from outside.
    /// </summary>
    private void PublishNextcloudSettings()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NEXTCLOUD_URL")))
        {
            return;
        }

        var origin = _baseAddresses[ResourceNames.Nextcloud].ToString().TrimEnd('/');
        var user = ASC.AppHost.Configuration.Constants.NextcloudUser;

        Environment.SetEnvironmentVariable("NEXTCLOUD_URL", $"{origin}/remote.php/dav/files/{user}/");
        Environment.SetEnvironmentVariable("NEXTCLOUD_LOGIN", user);
        Environment.SetEnvironmentVariable("NEXTCLOUD_PASSWORD", ASC.AppHost.Configuration.Constants.NextcloudPassword);
    }

    /// <summary>
    /// Waits until every service of <see cref="LazyResources"/> is healthy. The first caller starts the wait and the
    /// others share it, so it costs the run once.
    /// </summary>
    public Task WaitForLazyResourcesAsync()
    {
        lock (_lazyResourcesLock)
        {
            return _lazyResourcesHealthy ??= WaitForHealthyAsync(LazyResources.ToList());
        }
    }

    private readonly Lock _documentServerLock = new();
    private Task? _documentServerReady;

    /// <summary>
    /// Waits until the document server has finished starting. Its container has no health check of its own, so the
    /// services start as soon as it runs, and its start has two halves: a first docservice and converter answer within
    /// a second, the fonts and caches are regenerated (~70 s, 2026-11-03), and then supervisor restarts both. A
    /// conversion in flight at that restart hangs and fails with the router's HTML error page, while the test that
    /// asked for it waits out its whole timeout for a file that never comes. The health check already answers in the
    /// first half, so the wait is on the line the restart logs; the health check after it only confirms the router
    /// reaches the new instance. The first caller starts the wait and the others share it, so it costs the run once.
    /// </summary>
    public Task WaitForDocumentServerAsync()
    {
        lock (_documentServerLock)
        {
            return _documentServerReady ??= WaitForDocumentServerStartAsync();
        }
    }

    /// <summary>What supervisor logs once the converter is back after the fonts were regenerated.</summary>
    private const string DocumentServerRestartedLine = "ds:converter: started";

    /// <summary>
    /// How long the restart line is waited for. An image that stops logging it must not stall the suite for the whole
    /// start-up budget: past this the health check alone decides.
    /// </summary>
    private static readonly TimeSpan _documentServerRestartBudget = TimeSpan.FromMinutes(4);

    private async Task WaitForDocumentServerStartAsync()
    {
        await WaitForDocumentServerRestartAsync();
        await PollDocumentServerAsync();
    }

    private async Task WaitForDocumentServerRestartAsync()
    {
        using var restart = new CancellationTokenSource(_documentServerRestartBudget);
        var editors = _app.Services.GetRequiredService<DistributedApplicationModel>().Resources
            .Single(resource => resource.Name == ResourceNames.Editors);

        try
        {
            // The watch replays the lines logged before it started, so a server that is long up is seen at once.
            await foreach (var batch in _app.Services.GetRequiredService<ResourceLoggerService>().WatchAsync(editors).WithCancellation(restart.Token))
            {
                if (batch.Any(line => line.Content.Contains(DocumentServerRestartedLine, StringComparison.Ordinal)))
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (restart.IsCancellationRequested)
        {
            // Not logged within the budget: fall through to the health check.
        }
    }

    /// <summary>
    /// Waits, once per run, until the router passes requests on to the services. The run itself does not need the
    /// router and starts without it, but the socket service checks a cookie through it (<c>API_HOST</c>), and until it
    /// is up every connection is refused. Answered by People, because a socket connection asks it first.
    /// </summary>
    public Task WaitForRouterAsync()
    {
        lock (_routerLock)
        {
            return _routerUp ??= PollRouterAsync();
        }
    }

    private async Task PollRouterAsync()
    {
        using var startUp = new CancellationTokenSource(_startUpBudget);
        using var client = new HttpClient(_sharedHandler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(10) };
        var probe = new Uri($"http://localhost:{AppHostConstants.AppHostPort}/api/2.0/people/@self");

        try
        {
            while (true)
            {
                try
                {
                    using var response = await client.GetAsync(probe, startUp.Token);

                    // Anonymous, so 401 is the answer of a service that was reached.
                    if ((int)response.StatusCode < 500)
                    {
                        return;
                    }
                }
                catch (Exception exception) when (!startUp.IsCancellationRequested && exception is HttpRequestException or TaskCanceledException)
                {
                    // Not up yet: the connection is refused or dropped until the container listens.
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500), startUp.Token);
            }
        }
        catch (OperationCanceledException) when (startUp.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"The router did not answer {probe} within {_startUpBudget.TotalMinutes} minutes. The log of the "
                + "openresty container is the one to read.");
        }
    }

    private async Task PollDocumentServerAsync()
    {
        using var startUp = new CancellationTokenSource(_startUpBudget);
        using var client = new HttpClient(_sharedHandler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(10) };
        // The router port the services reach the document server through.
        var healthCheck = new Uri($"http://localhost:{AppHostConstants.AppHostPort}/ds-vpath/healthcheck");

        try
        {
            while (true)
            {
                try
                {
                    using var response = await client.GetAsync(healthCheck, startUp.Token);

                    if (response.IsSuccessStatusCode && (await response.Content.ReadAsStringAsync(startUp.Token)).Trim() == "true")
                    {
                        return;
                    }
                }
                catch (Exception exception) when (!startUp.IsCancellationRequested && exception is HttpRequestException or TaskCanceledException)
                {
                    // Not up yet: the router answers 502 or the connection is refused until the container listens.
                }

                await Task.Delay(TimeSpan.FromSeconds(2), startUp.Token);
            }
        }
        catch (OperationCanceledException) when (startUp.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"The document server did not answer {healthCheck} within {_startUpBudget.TotalMinutes} minutes. "
                + "The editors container's log is the one to read.");
        }
    }

    private async Task WaitForHealthyAsync(List<string> resources)
    {
        using var startUp = new CancellationTokenSource(_startUpBudget);

        try
        {
            await Task.WhenAll(resources.Select(r => _app.ResourceNotifications.WaitForResourceHealthyAsync(r, startUp.Token)));
        }
        catch (OperationCanceledException) when (startUp.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"{string.Join(", ", resources)} did not come up within {_startUpBudget.TotalMinutes} minutes. The log "
                + "of whichever of them never went healthy is the one to read, not this.");
        }
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

            // A SaaS stack resolves the tenant from the request host; "localhost" is a portal of its own there.
            if (!Standalone)
            {
                client.DefaultRequestHeaders.Host = new Uri(origin).Host;
            }
        }

        return client;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            // First: what a suite stood up in OnStartedAsync can still hold handles into the running app.
            await OnDisposingAsync();
        }
        catch (Exception ex)
        {
            // Teardown must not mask the original test failure, but a broken suite-specific
            // disposal should still be visible instead of silently vanishing.
            await Console.Error.WriteLineAsync($"OnDisposingAsync failed for {GetType().Name}: {ex}");
        }

        _apiSystemClient.Dispose();

        // Teardown is a large, easily-overlooked share of a run's wall clock — it is Aspire/DCP
        // stopping every project process, and one service that ignores its shutdown token holds up
        // all of them. Measured separately so a regression there is visible instead of invisible.
        await Timing.Measure("teardown.stop", async () => await _app.StopAsync());
        await Timing.Measure("teardown.dispose", async () => await _app.DisposeAsync());

        try
        {
            await OnStoppedAsync();
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"OnStoppedAsync failed for {GetType().Name}: {ex}");
        }

        _sharedHandler.Dispose();

        // Only after the services are gone: while they run they hold handles inside the folder.
        // For the AppHost itself Aspire generates ProjectPath as the project *directory*.
        var cleanupFailure = await Timing.Measure("teardown.storage",
            () => TestArtifacts.DeleteStorageAsync(Projects.ASC_AppHost.ProjectPath));

        if (cleanupFailure is not null)
        {
            Timing.Write($"cleanup.failed({cleanupFailure.GetType().Name}: {cleanupFailure.Message})", 0);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The notifications the socket service refused so far in this run, as "route status" with how many times. Nothing
    /// else would ever say so: the operation succeeds and the client stays stale. A suite asks at the very end of its
    /// run, from a test in a collection with <c>DisableParallelization</c>, which xUnit runs after the parallel ones.
    /// It is a test and not a failure of this fixture's disposal, because that is reported once for every test run.
    /// </summary>
    public Task<Dictionary<string, int>> FindSocketHubRefusalsAsync()
    {
        var logs = AppPaths.GetTestLogsDirectory(AppPaths.GetBasePath(Projects.ASC_AppHost.ProjectPath));

        return SocketHubRefusals.FindAsync(logs, _startedAt);
    }
}
