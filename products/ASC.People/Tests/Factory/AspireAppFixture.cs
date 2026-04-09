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

using DocSpace.API.SDK.Api.Authentication;
using DocSpace.API.SDK.Api.Settings;

namespace ASC.People.Tests.Factory;

public class AspireAppFixture : IAsyncLifetime
{
    private DistributedApplication _app = null!;
    private ConnectionMultiplexer _redis = null!;
    private DbConnection _dbconnection = null!;
    private Respawner _respawner = null!;
    private Provider _provider;
    private readonly List<string> _tablesToBackup = ["files_folder", "files_folder_tree", "core_user", "core_usersecurity", "files_bunch_objects"];
    private readonly List<string> _tablesToIgnore = ["core_acl", "core_settings", "core_subscription", "core_subscriptionmethod", "core_usergroup", "login_events", "tenants_tenants", "tenants_quota", "webstudio_settings"];

    // People service
    public HttpClient PeopleHttpClient { get; private set; } = null!;
    public ProfilesApi ProfilesApi { get; private set; } = null!;
    public GroupApi GroupApi { get; private set; } = null!;
    public UserTypeApi UserTypeApi { get; private set; } = null!;

    // WebApi service
    public HttpClient WebApiHttpClient { get; private set; } = null!;
    public DocSpace.API.SDK.Api.People.QuotaApi SettingsQuotaApi { get; private set; } = null!;
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
            ["DOTNET_LAUNCH_PROFILE=integration-test"]);

        appHost.Configuration["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "";
        appHost.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "";

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        // Wait for services to be healthy
        var resourceNotifications = _app.ResourceNotifications;
        await resourceNotifications.WaitForResourceHealthyAsync("onlyoffice-people");
        await resourceNotifications.WaitForResourceHealthyAsync("onlyoffice-web-api");

        // Get connection strings from Aspire resources
        var dbConnectionString = await _app.GetConnectionStringAsync("docspace");
        var redisConnectionString = await _app.GetConnectionStringAsync("cache");

        // Create DB connection for Respawn
        _dbconnection = _provider == Provider.MySql
            ? new MySqlConnection(dbConnectionString)
            : new NpgsqlConnection(dbConnectionString);
        await _dbconnection.OpenAsync();

        // Connect to Redis for cache flush
        _redis = await ConnectionMultiplexer.ConnectAsync($"{redisConnectionString},allowAdmin=true");

        // Create HTTP clients with cookies disabled to avoid stale auth cookies
        PeopleHttpClient = CreateHttpClientNoCookies("onlyoffice-people");
        WebApiHttpClient = CreateHttpClientNoCookies("onlyoffice-web-api");

        // Initialize People API clients
        var peopleConfig = new Configuration { BasePath = PeopleHttpClient.BaseAddress!.ToString().TrimEnd('/') };
        ProfilesApi = new ProfilesApi(PeopleHttpClient, peopleConfig);
        GroupApi = new GroupApi(PeopleHttpClient, peopleConfig);
        UserTypeApi = new UserTypeApi(PeopleHttpClient, peopleConfig);

        // Initialize WebApi clients
        var webApiConfig = new Configuration { BasePath = WebApiHttpClient.BaseAddress!.ToString().TrimEnd('/') };
        SettingsQuotaApi = new DocSpace.API.SDK.Api.People.QuotaApi(WebApiHttpClient, webApiConfig);
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
        await FlushRedisAsync();
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

    private async Task FlushRedisAsync()
    {
        var server = _redis.GetServers()[0];
        await server.FlushAllDatabasesAsync();
    }

    private HttpClient CreateHttpClientNoCookies(string resourceName)
    {
        using var baseClient = _app.CreateHttpClient(resourceName);
        var handler = new HttpClientHandler { UseCookies = false };
        return new HttpClient(handler) { BaseAddress = baseClient.BaseAddress };
    }

    private static string MakeCopyTableName(string tableName)
    {
        return $"{tableName}_copy";
    }

    public async ValueTask DisposeAsync()
    {
        if (_redis is not null)
        {
            await _redis.DisposeAsync();
        }

        if (_dbconnection is not null)
        {
            await _dbconnection.DisposeAsync();
        }

        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
