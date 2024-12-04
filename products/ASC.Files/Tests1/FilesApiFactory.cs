// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Files.Tests1;

public class FilesApiFactory: WebApplicationFactory<FilesProgram>, IAsyncLifetime
{
    private readonly MySqlContainer _mySqlContainer = new MySqlBuilder()
        .WithImage("mysql:8.0")
        .Build();
    
    private DbConnection _dbconnection = default!;
    private Respawner _respawner = default!;
    readonly List<string> _tablesToBackup = ["files_folder"];
    
    public string ConnectionString => _mySqlContainer.GetConnectionString(); 
    public HttpClient HttpClient { get; private set;} = default!;
    public JsonSerializerOptions JsonRequestSerializerOptions { get; } = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };
    
    protected override IHost CreateHost(IHostBuilder builder)
    {             
        builder.ConfigureHostConfiguration(configBuilder =>
        {
            configBuilder.AddInMemoryCollection(new List<KeyValuePair<string, string?>>
            {
                new("log:dir",  Path.Combine("..", "..", "..", "..", "Logs", "Test")),
                new("$STORAGE_ROOT", Path.Combine("..", "..", "..", "..", "Data", "Test")),
                new("ConnectionStrings:default:connectionString", ConnectionString),
                new("testAssembly", $"ASC.Migrations.MySql.SaaS"),
                new("web:hub:internal", "")
            }); 
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
    
    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbconnection);
        
        await ExecuteScriptAsync("INSERT INTO {0} SELECT * FROM {1};");
    }

    public async Task InitializeAsync()
    {
        await _mySqlContainer.StartAsync();
        _dbconnection = new MySqlConnection(_mySqlContainer.GetConnectionString());

        HttpClient = CreateClient();
        HttpClient.BaseAddress = new Uri(HttpClient.BaseAddress, "api/2.0/files/");
        
        List<Table> tablesToIgnore = [ "core_user", "core_acl", "core_settings", "core_subscription", "core_subscriptionmethod", "core_usergroup", "core_usersecurity", "login_events", "tenants_tenants", "tenants_quota", "webstudio_settings" ];
        tablesToIgnore.AddRange(_tablesToBackup.Select(r=> new Table(MakeCopyTableName(r))));
        
        await _dbconnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_dbconnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.MySql,
            TablesToIgnore = tablesToIgnore.ToArray(),
            WithReseed = true
        });
    }

    public new Task DisposeAsync() => _mySqlContainer.StopAsync();


    public async Task BackupTables()
    {
        await ExecuteScriptAsync("CREATE TABLE IF NOT EXISTS {1} LIKE {0}; \nREPLACE INTO {1} SELECT * FROM {0};");
    }
    
    private async Task ExecuteScriptAsync(string scriptTemplate)
    {
        var backupScript = new StringBuilder();
                
        foreach (var table in _tablesToBackup)
        {
            backupScript.AppendFormat(scriptTemplate, table, MakeCopyTableName(table));
        }
            
        await _mySqlContainer.ExecScriptAsync(backupScript.ToString());
    }

    private string MakeCopyTableName(string tableName)
    {
        return $"{tableName}_copy";
    }
}