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

namespace ASC.AI.Tests.ApiFactories;

public class AspireAppFixture : IAsyncLifetime
{
    private DistributedApplication _app = null!;
    private DbConnection _dbconnection = null!;
    private Respawner _respawner = null!;
    private Provider _provider;

    private readonly List<string> _tablesToBackup = ["files_folder", "files_folder_tree", "core_user", "core_usersecurity", "files_bunch_objects"];
    private readonly List<string> _tablesToIgnore = ["core_acl", "core_settings", "core_subscription", "core_subscriptionmethod", "core_usergroup", "login_events", "tenants_tenants", "tenants_quota", "webstudio_settings"];

    public HttpClient AiHttpClient { get; private set; } = null!;
    public HttpClient PeopleHttpClient { get; private set; } = null!;
    public HttpClient WebApiHttpClient { get; private set; } = null!;
    public HttpClient FilesHttpClient { get; private set; } = null!;

    public AiApiClient AiApi { get; private set; } = null!;
    public AiApiClient PeopleApi { get; private set; } = null!;
    public AiApiClient WebApi { get; private set; } = null!;
    public AiApiClient FilesApi { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        _provider = config.GetValue<Provider>("dbProviderType");

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.ASC_AppHost>(
            ["DOTNET_LAUNCH_PROFILE=integration-test", "SKIP_CLIENT=true"]);

        appHost.Configuration["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "";
        appHost.Configuration["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "";

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        const string onlyofficeAi = "onlyoffice-ai";
        const string onlyofficePeople = "onlyoffice-people";
        const string onlyofficeWebApi = "onlyoffice-web-api";
        const string onlyofficeFiles = "onlyoffice-files";

        var resourceNotifications = _app.ResourceNotifications;
        var waitForAi = resourceNotifications.WaitForResourceHealthyAsync(onlyofficeAi);
        var waitForPeople = resourceNotifications.WaitForResourceHealthyAsync(onlyofficePeople);
        var waitForApi = resourceNotifications.WaitForResourceHealthyAsync(onlyofficeWebApi);
        var waitForFiles = resourceNotifications.WaitForResourceHealthyAsync(onlyofficeFiles);

        await Task.WhenAll(waitForAi, waitForPeople, waitForApi, waitForFiles);

        var dbConnectionString = await _app.GetConnectionStringAsync("docspace");

        _dbconnection = _provider == Provider.MySql
            ? new MySqlConnection(dbConnectionString)
            : new NpgsqlConnection(dbConnectionString);
        await _dbconnection.OpenAsync();

        AiHttpClient = CreateHttpClientNoCookies(onlyofficeAi);
        PeopleHttpClient = CreateHttpClientNoCookies(onlyofficePeople);
        WebApiHttpClient = CreateHttpClientNoCookies(onlyofficeWebApi);
        FilesHttpClient = CreateHttpClientNoCookies(onlyofficeFiles);

        AiApi = new AiApiClient(AiHttpClient);
        PeopleApi = new AiApiClient(PeopleHttpClient);
        WebApi = new AiApiClient(WebApiHttpClient);
        FilesApi = new AiApiClient(FilesHttpClient);

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

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _dbconnection.DisposeAsync();
        await _app.DisposeAsync();
    }
}
