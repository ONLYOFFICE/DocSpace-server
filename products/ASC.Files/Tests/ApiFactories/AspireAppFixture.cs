// (c) Copyright Ascensio System SIA 2009-2026
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

using System.Net.Sockets;

using Aspire.Hosting.ApplicationModel;

using DocSpace.API.SDK.Api.AI;

using Microsoft.Extensions.DependencyInjection;

using GroupApi = DocSpace.API.SDK.Api.Group.GroupApi;
using SettingsApi = DocSpace.API.SDK.Api.Files.SettingsApi;
using QuotaApi = DocSpace.API.SDK.Api.Files.QuotaApi;

namespace ASC.Files.Tests.ApiFactories;

public class AspireAppFixture : IAsyncLifetime
{
    private DistributedApplication _app = null!;
    private DbConnection _dbConnection = null!;
    private Respawner _respawner = null!;
    private Provider _provider;
    private readonly List<string> _tablesToBackup = ["files_folder", "files_folder_tree", "core_user", "core_usersecurity", "files_bunch_objects"];
    private readonly List<string> _tablesToIgnore = ["core_acl", "core_settings", "core_subscription", "core_subscriptionmethod", "core_usergroup", "login_events", "tenants_tenants", "tenants_quota", "webstudio_settings"];
    public string? DbConnectionString { get; private set; }

    //AI service
    public HttpClient AIHttpClient { get; private set; } = null!;
    public ProvidersApi ProvidersApi { get; private set; } = null!;
    public AgentsApi AgentsApi { get; private set; } = null!;
    public string? OllamaModel { get; private set; }

    // Files service
    public HttpClient FilesHttpClient { get; private set; } = null!;
    public FoldersApi FoldersApi { get; private set; } = null!;
    public FilesApi FilesApi { get; private set; } = null!;
    public OperationsApi OperationsApi { get; private set; } = null!;
    public RoomsApi RoomsApi { get; private set; } = null!;
    public SettingsApi SettingsApi { get; private set; } = null!;
    public QuotaApi QuotaApi { get; private set; } = null!;
    public SharingApi SharingApi { get; private set; } = null!;
    public DocSpace.API.SDK.Api.Settings.QuotaApi SettingsQuotaApi { get; private set; } = null!;

    // People service
    public HttpClient PeopleHttpClient { get; private set; } = null!;
    public ProfilesApi ProfilesApi { get; private set; } = null!;
    public UserStatusApi UserStatusApi { get; private set; } = null!;
    public GroupApi GroupApi { get; private set; } = null!;
    public PhotosApi PhotosApi { get; private set; } = null!;

    // WebApi service
    public HttpClient WebApiHttpClient { get; private set; } = null!;
    public DocSpace.API.SDK.Api.Settings.QuotaApi WebApiSettingsQuotaApi { get; private set; } = null!;
    public AuthenticationApi AuthenticationApi { get; private set; } = null!;
    public AuthorizationApi AuthorizationApi { get; private set; } = null!;
    public CommonSettingsApi CommonSettingsApi { get; private set; } = null!;
    public UsersApi PortalUsersApi { get; private set; } = null!;


    public HttpClient? OllamaHttpClient { get; private set; }

    // Editors / Document Builder (served by the documentserver container at /docbuilder)
    public HttpClient EditorsHttpClient { get; private set; } = null!;
    private const string EditorsJwtSecret = "secret";

    public async ValueTask InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        _provider = config.GetValue<Provider>("dbProviderType");

        // Start Aspire AppHost with integration-test profile
        var args = new List<string>
        {
            "DOTNET_LAUNCH_PROFILE=integration-test",
            "SKIP_CLIENT=true"
        };

        OllamaModel = config.GetValue<string>("OLLAMA_MODEL");
        if (!string.IsNullOrEmpty(OllamaModel))
        {
            args.Add($"OLLAMA_MODEL={OllamaModel}");
        }

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.ASC_AppHost>(args.ToArray());

        appHost.Configuration["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "";
        appHost.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "";

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        // Wait for services to be healthy
        var resourceNotifications = _app.ResourceNotifications;
        const string onlyofficeAI = "onlyoffice-ai";
        const string onlyofficeFiles = "onlyoffice-files";
        const string onlyofficePeople = "onlyoffice-people";
        const string onlyofficeWebApi = "onlyoffice-web-api";
        const string ollama = "ollama";
        const string onlyofficeEditors = "onlyoffice-editors";

        var waitForAI = resourceNotifications.WaitForResourceHealthyAsync(onlyofficeAI);
        var waitForFiles = resourceNotifications.WaitForResourceHealthyAsync(onlyofficeFiles);
        var waitForPeople = resourceNotifications.WaitForResourceHealthyAsync(onlyofficePeople);
        var waitForApi = resourceNotifications.WaitForResourceHealthyAsync(onlyofficeWebApi);
        var waitForEditors = resourceNotifications.WaitForResourceHealthyAsync(onlyofficeEditors);

        await Task.WhenAll(waitForAI, waitForFiles, waitForPeople, waitForApi, waitForEditors);

        // Get connection strings from Aspire resources
        var dbConnectionString = await _app.GetConnectionStringAsync("docspace");
        DbConnectionString = dbConnectionString;

        // Create DB connection for Respawn
        _dbConnection = _provider == Provider.MySql
            ? new MySqlConnection(dbConnectionString)
            : new NpgsqlConnection(dbConnectionString);
        await _dbConnection.OpenAsync();

        // Create HTTP clients with cookies disabled to avoid stale auth cookies
        AIHttpClient = CreateHttpClientNoCookies(onlyofficeAI);
        FilesHttpClient = CreateHttpClientNoCookies(onlyofficeFiles);
        PeopleHttpClient = CreateHttpClientNoCookies(onlyofficePeople);
        WebApiHttpClient = CreateHttpClientNoCookies(onlyofficeWebApi);

        if (!string.IsNullOrEmpty(OllamaModel))
        {
            OllamaHttpClient = CreateHttpClientNoCookies(ollama, "/v1/");
        }

        EditorsHttpClient = CreateHttpClientNoCookies(onlyofficeEditors);

        // Initialize AI API clients
        var aiConfig = new Configuration { BasePath = AIHttpClient.BaseAddress!.ToString().TrimEnd('/') };
        ProvidersApi = new ProvidersApi(AIHttpClient, aiConfig);
        AgentsApi = new AgentsApi(AIHttpClient, aiConfig);

        // Initialize Files API clients
        var filesConfig = new Configuration { BasePath = FilesHttpClient.BaseAddress!.ToString().TrimEnd('/') };
        FoldersApi = new FoldersApi(FilesHttpClient, filesConfig);
        FilesApi = new FilesApi(FilesHttpClient, filesConfig);
        OperationsApi = new OperationsApi(FilesHttpClient, filesConfig);
        RoomsApi = new RoomsApi(FilesHttpClient, filesConfig);
        SettingsApi = new SettingsApi(FilesHttpClient, filesConfig);
        QuotaApi = new QuotaApi(FilesHttpClient, filesConfig);
        SharingApi = new SharingApi(FilesHttpClient, filesConfig);
        SettingsQuotaApi = new DocSpace.API.SDK.Api.Settings.QuotaApi(FilesHttpClient, filesConfig);

        // Initialize People API clients
        var peopleConfig = new Configuration { BasePath = PeopleHttpClient.BaseAddress!.ToString().TrimEnd('/') };
        ProfilesApi = new ProfilesApi(PeopleHttpClient, peopleConfig);
        GroupApi = new GroupApi(PeopleHttpClient, peopleConfig);
        UserStatusApi = new UserStatusApi(PeopleHttpClient, peopleConfig);
        PhotosApi = new PhotosApi(PeopleHttpClient, peopleConfig);

        // Initialize WebApi clients
        var webApiConfig = new Configuration { BasePath = WebApiHttpClient.BaseAddress!.ToString().TrimEnd('/') };
        WebApiSettingsQuotaApi = new DocSpace.API.SDK.Api.Settings.QuotaApi(WebApiHttpClient, webApiConfig);
        AuthenticationApi = new AuthenticationApi(WebApiHttpClient, webApiConfig);
        AuthorizationApi = new AuthorizationApi(WebApiHttpClient, webApiConfig);
        CommonSettingsApi = new CommonSettingsApi(WebApiHttpClient, webApiConfig);
        PortalUsersApi = new UsersApi(WebApiHttpClient, webApiConfig);

        // Create Respawner
        var tablesToIgnore = _tablesToIgnore.Select(t => new Table(t)).ToList();
        tablesToIgnore.AddRange(_tablesToBackup.Select(r => new Table(MakeCopyTableName(r))));

        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = _provider == Provider.MySql ? DbAdapter.MySql : DbAdapter.Postgres,
            TablesToIgnore = tablesToIgnore.ToArray(),
        });
    }

    internal async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);

        var script = _provider switch
        {
            Provider.MySql => "INSERT INTO {0} SELECT * FROM {1};",
            Provider.PostgreSql => "INSERT INTO {0} SELECT * FROM {1};SELECT setval('{0}_id_seq', (SELECT MAX(id) FROM {0})+1);",
            _ => ""
        };

        await ExecuteScriptAsync(script);
        await ClearCacheAsync();
    }

    internal async Task BackupTables()
    {
        var script = _provider switch
        {
            Provider.MySql => "CREATE TABLE IF NOT EXISTS {1} LIKE {0}; \nREPLACE INTO {1} SELECT * FROM {0};",
            Provider.PostgreSql => "CREATE TABLE IF NOT EXISTS {1} (LIKE {0} INCLUDING ALL);\n DELETE FROM {1}; \nINSERT INTO {1} SELECT * FROM {0};",
            _ => ""
        };

        if (!string.IsNullOrEmpty(script))
        {
            await ExecuteScriptAsync(script);
        }
    }

    private async Task ExecuteScriptAsync(string scriptTemplate)
    {
        var backupScript = new StringBuilder();

        foreach (var table in _tablesToBackup)
        {
            backupScript.AppendFormat(scriptTemplate, table, MakeCopyTableName(table));
        }

        await using var cmd = _dbConnection.CreateCommand();
        cmd.CommandText = backupScript.ToString();
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task ClearCacheAsync()
    {
        var commandService = _app.Services.GetRequiredService<ResourceCommandService>();
        await commandService.ExecuteCommandAsync("cache", "clear-cache", CancellationToken.None);
    }

    public async Task<byte[]> RunDocBuilderAsync(string scriptResourceName, string outputFileName, CancellationToken cancellationToken = default)
    {
        var assembly = typeof(AspireAppFixture).Assembly;

        await using var scriptStream = assembly.GetManifestResourceStream($"ASC.Files.Tests.Data.{scriptResourceName}")
            ?? throw new FileNotFoundException(
                $"Embedded docbuilder script '{scriptResourceName}' not found.", scriptResourceName);

        using var scriptBuffer = new MemoryStream();
        await scriptStream.CopyToAsync(scriptBuffer, cancellationToken);
        var scriptBytes = scriptBuffer.ToArray();

        // Serve the script over an ephemeral HTTP listener that the editors container reaches via host.docker.internal.
        // documentserver may issue multiple requests (HEAD/GET/retry) so the listener must accept connections in a loop.
        using var listener = new TcpListener(IPAddress.Any, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        using var serveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var serveTask = ServeScriptLoopAsync(listener, scriptBytes, serveCts.Token);
        HttpResponseMessage? response = null;

        try
        {
            var scriptUrl = $"http://host.docker.internal:{port}/{scriptResourceName}";

            // documentserver has JWT_ENABLED=true, so wrap the original payload into a signed token.
            var payloadJson = $"{{\"async\":false,\"url\":\"{scriptUrl}\"}}";
            var tokenJson = $"{{\"payload\":{payloadJson}}}";
            var jwt = CreateJwtHs256(tokenJson, EditorsJwtSecret);
            var signedBody = $"{{\"async\":false,\"url\":\"{scriptUrl}\",\"token\":\"{jwt}\"}}";

            // 502/504 from documentserver's nginx is usually a transient docservice startup race — retry a few times.
            const int maxAttempts = 5;
            string? lastErrorBody = null;
            HttpStatusCode lastStatus = 0;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                response?.Dispose();

                using var requestContent = new StringContent(signedBody, Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(HttpMethod.Post, "docbuilder")
                {
                    Content = requestContent
                };
                request.Headers.TryAddWithoutValidation("AuthorizationJwt", $"Bearer {jwt}");

                response = await EditorsHttpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    break;
                }

                lastStatus = response.StatusCode;
                lastErrorBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if ((int)response.StatusCode is not (502 or 503 or 504))
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }

            if (response is null || !response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"docbuilder request failed after retries. Status: {lastStatus}. Body: {lastErrorBody}");
            }

            await using var bodyStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(bodyStream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("urls", out var urls)
                || !urls.TryGetProperty(outputFileName, out var outputUrlElement)
                || outputUrlElement.GetString() is not { } outputUrl)
            {
                throw new InvalidOperationException(
                    $"docbuilder response did not contain url for '{outputFileName}'. Body: {doc.RootElement.GetRawText()}");
            }

            // The result URL points to the editors host as seen from inside the container; only the path is portable.
            var outputPath = new Uri(outputUrl).PathAndQuery;
            return await EditorsHttpClient.GetByteArrayAsync(outputPath, cancellationToken);
        }
        finally
        {
            response?.Dispose();
            await serveCts.CancelAsync();
            listener.Stop();
            try { await serveTask; } catch { /* listener stopped */ }
        }
    }

    private static async Task ServeScriptLoopAsync(TcpListener listener, byte[] scriptBytes, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException) { return; }
            catch (ObjectDisposedException) { return; }
            catch (SocketException) { return; }

            _ = HandleScriptRequestAsync(client, scriptBytes, cancellationToken);
        }
    }

    private static async Task HandleScriptRequestAsync(TcpClient client, byte[] scriptBytes, CancellationToken cancellationToken)
    {
        try
        {
            using (client)
            await using (var stream = client.GetStream())
            {
                // Drain the request line + headers
                var requestBuffer = new byte[4096];
                var totalRead = 0;
                while (totalRead < requestBuffer.Length)
                {
                    var read = await stream.ReadAsync(requestBuffer.AsMemory(totalRead), cancellationToken);
                    if (read == 0)
                    {
                        break;
                    }
                    totalRead += read;
                    var headerEnd = Encoding.ASCII.GetString(requestBuffer, 0, totalRead).IndexOf("\r\n\r\n", StringComparison.Ordinal);
                    if (headerEnd >= 0)
                    {
                        break;
                    }
                }

                var method = ExtractMethod(requestBuffer, totalRead);
                var headers = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: {scriptBytes.Length}\r\nConnection: close\r\n\r\n";
                var headerBytes = Encoding.ASCII.GetBytes(headers);
                await stream.WriteAsync(headerBytes, cancellationToken);

                if (!string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    await stream.WriteAsync(scriptBytes, cancellationToken);
                }

                await stream.FlushAsync(cancellationToken);
            }
        }
        catch
        {
            // best-effort
        }
    }

    private static string ExtractMethod(byte[] buffer, int length)
    {
        var spaceIndex = Array.IndexOf(buffer, (byte)' ', 0, length);
        return spaceIndex > 0 ? Encoding.ASCII.GetString(buffer, 0, spaceIndex) : string.Empty;
    }

    private static string CreateJwtHs256(string payloadJson, string secret)
    {
        static string Base64Url(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var header = Base64Url(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
        var payload = Base64Url(Encoding.UTF8.GetBytes(payloadJson));
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{header}.{payload}")));
        return $"{header}.{payload}.{signature}";
    }

    private HttpClient CreateHttpClientNoCookies(string resourceName, string? path = null)
    {
        Uri? baseAddress;
        using (var baseClient = _app.CreateHttpClient(resourceName))
        {
            baseAddress = baseClient.BaseAddress;
        }

        if (path != null && baseAddress != null)
        {
            baseAddress = new Uri(baseAddress, path);
        }

        var handler = new HttpClientHandler { UseCookies = false };
        return new HttpClient(handler) { BaseAddress = baseAddress };
    }

    private static string MakeCopyTableName(string tableName)
    {
        return $"{tableName}_copy";
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _dbConnection.DisposeAsync();
        await _app.DisposeAsync();
    }
}
