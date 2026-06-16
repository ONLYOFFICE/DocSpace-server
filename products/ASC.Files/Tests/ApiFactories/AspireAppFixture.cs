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

using Aspire.Hosting.ApplicationModel;

using Microsoft.Extensions.DependencyInjection;

using GroupApi = DocSpace.API.SDK.Api.Group.GroupApi;
using SettingsApi = DocSpace.API.SDK.Api.Files.SettingsApi;
using QuotaApi = DocSpace.API.SDK.Api.Files.QuotaApi;

namespace ASC.Files.Tests.ApiFactories;

public class AspireAppFixture : IAsyncLifetime
{
    private DistributedApplication _app = null!;
    private DbConnection _dbconnection = null!;
    private Respawner _respawner = null!;
    private Provider _provider;
    private readonly List<string> _tablesToBackup = ["files_folder", "files_folder_tree", "core_user", "core_usersecurity", "files_bunch_objects"];
    private readonly List<string> _tablesToIgnore = ["core_acl", "core_settings", "core_subscription", "core_subscriptionmethod", "core_usergroup", "login_events", "tenants_tenants", "tenants_quota", "webstudio_settings"];

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

    // Editors service
    public HttpClient EditorsHttpClient { get; private set; } = null!;
    private const string EditorsJwtSecret = "secret";

    // WebApi service
    public HttpClient WebApiHttpClient { get; private set; } = null!;
    public DocSpace.API.SDK.Api.Settings.QuotaApi WebApiSettingsQuotaApi { get; private set; } = null!;
    public AuthenticationApi AuthenticationApi { get; private set; } = null!;
    public CommonSettingsApi CommonSettingsApi { get; private set; } = null!;
    public UsersApi PortalUsersApi { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        _provider = config.GetValue<Provider>("dbProviderType");

        // Start Aspire AppHost with integration-test profile
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.ASC_AppHost>(
            ["DOTNET_LAUNCH_PROFILE=integration-test", "SKIP_CLIENT=true"]);

        appHost.Configuration["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "";
        appHost.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "";

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        // Wait for services to be healthy
        var resourceNotifications = _app.ResourceNotifications;
        const string onlyofficeFiles = "onlyoffice-files";
        const string onlyofficePeople = "onlyoffice-people";
        const string onlyofficeWebApi = "onlyoffice-web-api";
        const string onlyofficeEditors = "onlyoffice-editors";

        var waitForFiles = resourceNotifications.WaitForResourceHealthyAsync(onlyofficeFiles);
        var waitForPeople = resourceNotifications.WaitForResourceHealthyAsync(onlyofficePeople);
        var waitForApi = resourceNotifications.WaitForResourceHealthyAsync(onlyofficeWebApi);
        var waitForEditors = resourceNotifications.WaitForResourceHealthyAsync(onlyofficeEditors);

        await Task.WhenAll(waitForFiles, waitForPeople, waitForApi, waitForEditors);

        // Get connection strings from Aspire resources
        var dbConnectionString = await _app.GetConnectionStringAsync("docspace");

        // Create DB connection for Respawn
        _dbconnection = _provider == Provider.MySql
            ? new MySqlConnection(dbConnectionString)
            : new NpgsqlConnection(dbConnectionString);
        await _dbconnection.OpenAsync();

        // Create HTTP clients with cookies disabled to avoid stale auth cookies
        FilesHttpClient = CreateHttpClientNoCookies(onlyofficeFiles);
        PeopleHttpClient = CreateHttpClientNoCookies(onlyofficePeople);
        WebApiHttpClient = CreateHttpClientNoCookies(onlyofficeWebApi);
        EditorsHttpClient = CreateHttpClientNoCookies(onlyofficeEditors);

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
        CommonSettingsApi = new CommonSettingsApi(WebApiHttpClient, webApiConfig);
        PortalUsersApi = new UsersApi(WebApiHttpClient, webApiConfig);

        // Create Respawner
        var tablesToIgnore = _tablesToIgnore.Select(t => new Table(t)).ToList();
        tablesToIgnore.AddRange(_tablesToBackup.Select(r => new Table(MakeCopyTableName(r))));

        _respawner = await Respawner.CreateAsync(_dbconnection, new RespawnerOptions
        {
            DbAdapter = _provider == Provider.MySql ? DbAdapter.MySql : DbAdapter.Postgres,
            TablesToIgnore = tablesToIgnore.ToArray(),
        });
    }

    internal async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbconnection);

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

        await using var cmd = _dbconnection.CreateCommand();
        cmd.CommandText = backupScript.ToString();
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task ClearCacheAsync()
    {
        var commandService = _app.Services.GetRequiredService<ResourceCommandService>();
        await commandService.ExecuteCommandAsync("cache", "clear-cache", CancellationToken.None);
    }

    private HttpClient CreateHttpClientNoCookies(string resourceName)
    {
        Uri? baseAddress;
        using (var baseClient = _app.CreateHttpClient(resourceName))
        {
            baseAddress = baseClient.BaseAddress;
        }
        var handler = new HttpClientHandler { UseCookies = false };
        return new HttpClient(handler) { BaseAddress = baseAddress };
    }

    private static string MakeCopyTableName(string tableName)
    {
        return $"{tableName}_copy";
    }

    public async Task SimulateDocServiceSubmitFormAsync(
        string callbackUrl,
        string docKey,
        Guid fillerUserId,
        byte[] filledPdfBytes,
        CancellationToken cancellationToken = default)
    {
        // Without formsdataurl the server throws inside SubmitFillingRoomFormAsync's try-catch, silently skipping link deletion.
        var emptyFormsData = Encoding.UTF8.GetBytes("{}");
        await ServeBytesOverHttpAsync(
            emptyFormsData, "formsdata.json", "application/json", "host.docker.internal",
            async formsDataUri =>
            {
                await ServeBytesOverHttpAsync(
                    filledPdfBytes, "filled_form.pdf", "application/pdf", "host.docker.internal",
                    async downloadUri =>
                    {
                        var submitKey = Convert.ToBase64String(
                            Encoding.UTF8.GetBytes($"submit_{Guid.NewGuid()}_{docKey}")).TrimEnd('=');
                        var payload = $$"""{"status":6,"forceSaveType":3,"key":"{{submitKey}}","users":["{{fillerUserId}}"],"url":"{{downloadUri}}","filetype":"pdf","formsdataurl":"{{formsDataUri}}"}""";

                        // DocSpace validates the JWT token field when DocServiceSignatureSecret is configured
                        var token = CreateJwtHs256(payload, EditorsJwtSecret);
                        var trackerJson = $$"""{"status":6,"forceSaveType":3,"key":"{{submitKey}}","users":["{{fillerUserId}}"],"url":"{{downloadUri}}","filetype":"pdf","formsdataurl":"{{formsDataUri}}","token":"{{token}}"}""";

                        var callbackPath = new Uri(callbackUrl).PathAndQuery;
                        using var content = new StringContent(trackerJson, Encoding.UTF8, "application/json");
                        using var response = await FilesHttpClient.PostAsync(callbackPath, content, cancellationToken);
                        var body = await response.Content.ReadAsStringAsync(cancellationToken);
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new InvalidOperationException($"DS callback failed: {response.StatusCode} - {body}");
                        }
                        return body;
                    },
                    cancellationToken);
                return formsDataUri;
            },
            cancellationToken);
    }

    public async Task SimulateDocServiceSessionCloseAsync(
        string callbackUrl,
        string docKey,
        Guid fillerUserId,
        CancellationToken cancellationToken = default)
    {
        var payload = $$"""{"status":4,"key":"{{docKey}}","users":["{{fillerUserId}}"]}""";
        var token = CreateJwtHs256(payload, EditorsJwtSecret);
        var json = $$"""{"status":4,"key":"{{docKey}}","users":["{{fillerUserId}}"],"token":"{{token}}"}""";
        var callbackPath = new Uri(callbackUrl).PathAndQuery;
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await FilesHttpClient.PostAsync(callbackPath, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"DS session close callback failed: {response.StatusCode} - {body}");
        }
    }

    public Task<byte[]> RunDocBuilderAsync(string scriptResourceName, string outputFileName, CancellationToken cancellationToken = default) =>
        RunDocBuilderAsync(scriptResourceName, outputFileName, argumentJson: null, cancellationToken);

    public async Task<byte[]> RunDocBuilderAsync(string scriptResourceName, string outputFileName, string? argumentJson, CancellationToken cancellationToken = default)
    {
        var assembly = typeof(AspireAppFixture).Assembly;

        await using var scriptStream = assembly.GetManifestResourceStream($"ASC.Files.Tests.Data.{scriptResourceName}")
            ?? throw new FileNotFoundException($"Embedded docbuilder script '{scriptResourceName}' not found.", scriptResourceName);

        using var scriptBuffer = new MemoryStream();
        await scriptStream.CopyToAsync(scriptBuffer, cancellationToken);
        var scriptBytes = scriptBuffer.ToArray();

        return await ServeBytesOverHttpAsync(
            scriptBytes,
            scriptResourceName,
            "text/plain; charset=utf-8",
            "host.docker.internal",
            scriptUrl => CallDocBuilderAsync(scriptUrl, outputFileName, argumentJson, cancellationToken),
            cancellationToken);
    }

    private async Task<byte[]> CallDocBuilderAsync(string scriptUrl, string outputFileName, string? argumentJson, CancellationToken cancellationToken)
    {
        var argumentFragment = string.IsNullOrEmpty(argumentJson) ? string.Empty : $",\"argument\":{argumentJson}";
        var payloadJson = $"{{\"async\":false,\"url\":\"{scriptUrl}\"{argumentFragment}}}";
        var tokenJson = $"{{\"payload\":{payloadJson}}}";
        var jwt = CreateJwtHs256(tokenJson, EditorsJwtSecret);
        var signedBody = $"{{\"async\":false,\"url\":\"{scriptUrl}\"{argumentFragment},\"token\":\"{jwt}\"}}";

        const int maxAttempts = 5;
        HttpResponseMessage? response = null;
        string? lastErrorBody = null;
        HttpStatusCode lastStatus = 0;

        try
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                response?.Dispose();

                using var requestContent = new StringContent(signedBody, Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(HttpMethod.Post, "docbuilder") { Content = requestContent };
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
                throw new InvalidOperationException($"docbuilder request failed after retries. Status: {lastStatus}. Body: {lastErrorBody}");
            }

            await using var bodyStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(bodyStream, cancellationToken: cancellationToken);

            if (!doc.RootElement.TryGetProperty("urls", out var urls)
                || !urls.TryGetProperty(outputFileName, out var outputUrlElement)
                || outputUrlElement.GetString() is not { } outputUrl)
            {
                throw new InvalidOperationException($"docbuilder response did not contain url for '{outputFileName}'.");
            }

            var outputPath = new Uri(outputUrl).PathAndQuery;
            return await EditorsHttpClient.GetByteArrayAsync(outputPath, cancellationToken);
        }
        finally
        {
            response?.Dispose();
        }
    }

    public async Task<TResult> ServeBytesOverHttpAsync<TResult>(
        byte[] bytes,
        string fileName,
        string contentType,
        string hostnameForUrl,
        Func<string, Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        using var listener = new TcpListener(IPAddress.Any, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        using var serveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var serveTask = ServeBytesLoopAsync(listener, bytes, contentType, serveCts.Token);

        try
        {
            var url = $"http://{hostnameForUrl}:{port}/{fileName}";
            return await action(url);
        }
        finally
        {
            await serveCts.CancelAsync();
            listener.Stop();
            try { await serveTask; } catch { /* listener stopped */ }
        }
    }

    private static async Task ServeBytesLoopAsync(TcpListener listener, byte[] bytes, string contentType, CancellationToken cancellationToken)
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

            _ = HandleBytesRequestAsync(client, bytes, contentType, cancellationToken);
        }
    }

    private static async Task HandleBytesRequestAsync(TcpClient client, byte[] bytes, string contentType, CancellationToken cancellationToken)
    {
        try
        {
            using (client)
            await using (var stream = client.GetStream())
            {
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

                var method = ExtractHttpMethod(requestBuffer, totalRead);
                var headers = $"HTTP/1.1 200 OK\r\nContent-Type: {contentType}\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n";
                var headerBytes = Encoding.ASCII.GetBytes(headers);
                await stream.WriteAsync(headerBytes, cancellationToken);

                if (!string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    await stream.WriteAsync(bytes, cancellationToken);
                }

                await stream.FlushAsync(cancellationToken);
            }
        }
        catch { }
    }

    private static string ExtractHttpMethod(byte[] buffer, int length)
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

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _dbconnection.DisposeAsync();
        await _app.DisposeAsync();
    }
}
