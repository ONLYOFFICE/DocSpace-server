// (c) Copyright Ascensio System SIA 2009-2025
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

extern alias ASCFiles;

namespace ASC.Files.Tests.Factory;

public class FilesApiFactory: WebApplicationFactory<FilesProgram>, IAsyncLifetime
{
    private readonly MySqlContainer _mySqlContainer;
    private readonly PostgreSqlContainer _postgresSqlContainer;
    private readonly RedisContainer _redisContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;
    private readonly OpenSearchContainer _openSearchContainer;
    private readonly CustomProviderInfo _providerInfo;
    private DbConnection _dbconnection = null!;
    private Respawner _respawner = null!;
    private readonly List<string> _tablesToBackup = ["files_folder", "files_folder_tree", "core_user", "core_usersecurity", "files_bunch_objects" ];
    private readonly List<string> _tablesToIgnore = ["core_acl", "core_settings", "core_subscription", "core_subscriptionmethod", "core_usergroup", "login_events", "tenants_tenants", "tenants_quota", "webstudio_settings" ];
    
    public HttpClient HttpClient { get; private set;} = null!;
    public FoldersApi FoldersApi { get; private set;} = null!;
    public FilesApi FilesApi { get; private set;} = null!;
    public OperationsApi OperationsApi { get; private set;} = null!;
    public RoomsApi RoomsApi { get; private set;} = null!;
    public SettingsApi SettingsApi { get; private set;} = null!;
    public QuotaApi  QuotaApi { get; private set;} = null!;
    public SharingApi  SharingApi { get; private set;} = null!;
    
    public DocSpace.API.SDK.Api.Settings.QuotaApi  SettingsQuotaApi { get; private set;} = null!;
    
    public FilesApiFactory()
    {        
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
        
        var containers = config.GetSection("containers").Get<List<Container>>() ?? [];
        var postgresSqlContainer = containers.FirstOrDefault(r => r.Name == "postgres") ?? new Container
        {
            Name = "postgres",
            Image = "postgres",
            Tag = "17.2"
        };
        
        _postgresSqlContainer = new PostgreSqlBuilder()
            .WithImage($"{postgresSqlContainer.Image}:{postgresSqlContainer.Tag}")
            .Build();
        
        var redisContainer = containers.FirstOrDefault(r => r.Name == "redis") ?? new Container
        {
            Name = "redis",
            Image = "redis",
            Tag = "7.0"
        };
        _redisContainer = new RedisBuilder()
            .WithImage($"{redisContainer.Image}:{redisContainer.Tag}")
            .Build();

        var rabbitMqContainer = containers.FirstOrDefault(r => r.Name == "rabbitmq") ?? new Container
        {
            Name = "rabbitmq", 
            Image = "rabbitmq", 
            Tag = "3.13"
        };
        
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage($"{rabbitMqContainer.Image}:{rabbitMqContainer.Tag}")
            .Build();

        var openSearchContainer = containers.FirstOrDefault(r => r.Name == "opensearch") ?? new Container
        {
            Name = "opensearch", 
            Image = "opensearchproject/opensearch", 
            Tag = "2.18.0"
        };
        
        _openSearchContainer = new OpenSearchBuilder()
            .WithImage($"{openSearchContainer.Image}:{openSearchContainer.Tag}")
            .WithSecurityEnabled(false)
            .Build();

        var mysqlContainer = containers.FirstOrDefault(r => r.Name == "mysql") ?? new Container
        {
            Name = "mysql", 
            Image = "mysql", 
            Tag = "8.4.3"
        };
        
        _mySqlContainer = new MySqlBuilder()
            .WithImage($"{mysqlContainer.Image}:{mysqlContainer.Tag}")
            .Build();
        
        _providerInfo = GetProviderInfo(config.GetValue<Provider>("dbProviderType"));
    }
    
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configBuilder =>
        {
            Initializer.InitSettings(_providerInfo, _redisContainer.GetConnectionString(), _rabbitMqContainer.GetConnectionString(), $"{_openSearchContainer.Hostname}:{_openSearchContainer.GetMappedPublicPort(9200)}");
            configBuilder.AddInMemoryCollection(Initializer.GlobalSettings);
        });
        
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.AddBaseDbContext<MigrationContext>();
            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<MigrationContext>().Database.Migrate();
            
            BackupTables().Wait();
        });
    }
    
    internal async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbconnection);
        
        var script = _providerInfo.Provider switch
        {
            Provider.MySql => "INSERT INTO {0} SELECT * FROM {1};",
            Provider.PostgreSql => "INSERT INTO {0} SELECT * FROM {1};SELECT setval('{0}_id_seq', (SELECT MAX(id) FROM {0})+1);",
            _ => ""
        };
        
        await ExecuteScriptAsync(script);
    }

    public async ValueTask InitializeAsync()
    {
        await StartAllContainersAsync(_providerInfo.Provider == Provider.MySql ? _mySqlContainer : _postgresSqlContainer, _redisContainer, _rabbitMqContainer, _openSearchContainer);
        
        _dbconnection =  _providerInfo.Provider == Provider.MySql ?  new MySqlConnection(_mySqlContainer.GetConnectionString()) : new NpgsqlConnection(_postgresSqlContainer.GetConnectionString());

        HttpClient = CreateClient();
        var configuration = new Configuration { BasePath = HttpClient.BaseAddress!.ToString().TrimEnd('/') };
        FoldersApi = new FoldersApi(HttpClient, configuration);
        FilesApi = new FilesApi(HttpClient, configuration);
        OperationsApi = new OperationsApi(HttpClient, configuration);
        RoomsApi = new RoomsApi(HttpClient, configuration);
        SettingsApi = new SettingsApi(HttpClient, configuration);
        QuotaApi = new QuotaApi(HttpClient, configuration);
        SharingApi = new SharingApi(HttpClient, configuration);
        SettingsQuotaApi = new DocSpace.API.SDK.Api.Settings.QuotaApi(HttpClient, configuration);
        
        var tablesToIgnore = _tablesToIgnore.Select(t => new Table(t)).ToList();
        tablesToIgnore.AddRange(_tablesToBackup.Select(r=> new Table(MakeCopyTableName(r))));
        
        await _dbconnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_dbconnection, new RespawnerOptions
        {
            DbAdapter = _providerInfo.Provider == Provider.MySql ? DbAdapter.MySql : DbAdapter.Postgres,
            TablesToIgnore = tablesToIgnore.ToArray(),
            WithReseed = true
        });
    }

    internal async Task BackupTables()
    {
        var script = _providerInfo.Provider switch
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

        if (_providerInfo.Provider == Provider.MySql)
        {
            await _mySqlContainer.ExecScriptAsync(backupScript.ToString());
        }
        else
        {
            await _postgresSqlContainer.ExecScriptAsync(backupScript.ToString());
        }
    }

    private static string MakeCopyTableName(string tableName)
    {
        return $"{tableName}_copy";
    }
    
    private static async Task StartAllContainersAsync(params IContainer[] containers)
    {
        var tasks = containers.Select(r => r.StartAsync()).ToArray();

        await Task.WhenAll(tasks);
    }

    private CustomProviderInfo GetProviderInfo(Provider dbProviderType)
    {
        return dbProviderType switch
        {
            Provider.MySql => new CustomProviderInfo { Provider = Provider.MySql, ConnectionString = _mySqlContainer.GetConnectionString, ProviderFullName = "MySql.Data.MySqlClient" },
            Provider.PostgreSql => new CustomProviderInfo { Provider = Provider.PostgreSql, ConnectionString = _postgresSqlContainer.GetConnectionString, ProviderFullName = "Npgsql" },
            _ => throw new ArgumentException("Unknown provider type", nameof(dbProviderType))
        };
    }
}

public class CustomProviderInfo
{
    public required Func<string> ConnectionString { get; init; }
    public Provider Provider { get; init; }
    public required string ProviderFullName { get; init; }
}

public class Container
{
    public required string Name { get; init; }
    public required string Image { get; init; }
    public required string Tag { get; init; }
}