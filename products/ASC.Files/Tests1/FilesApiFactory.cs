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
using System.Data.Common;

using MySql.Data.MySqlClient;

using Respawn;

using FilesProgram = ASCFiles::Program;

namespace ASC.Files.Tests1;

public class FilesApiFactory: WebApplicationFactory<FilesProgram>, IAsyncLifetime
{
    private readonly MySqlContainer _mySqlContainer = new MySqlBuilder()
        .WithImage("mysql:8.0")
        .Build();
    
    private DbConnection _dbconnection = default!;
    private Respawner _respawner = default!;
    
    public HttpClient HttpClient { get; private set;} = default!;
    public JsonSerializerOptions JsonRequestSerializerOptions { get; } = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };
    public JsonSerializerOptions JsonResponseSerializerOptions { get; } = new JsonSerializerOptions { AllowTrailingCommas = true, PropertyNameCaseInsensitive = true };
    
    protected override IHost CreateHost(IHostBuilder builder)
    { 
        builder.ConfigureHostConfiguration(configBuilder =>
        {
            configBuilder.AddInMemoryCollection(new List<KeyValuePair<string, string?>>
            {
                new("log:dir",  Path.Combine("..", "..", "..", "Logs", "Test")),
                new("ConnectionStrings:default:connectionString", _mySqlContainer.GetConnectionString()),
                new("testAssembly", $"ASC.Migrations.MySql.SaaS"),
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
            var serviceProvider = scope.ServiceProvider;
            var context = serviceProvider.GetRequiredService<MigrationContext>();
            context.Database.Migrate();
        });
    }

    public async Task<T?> ReadFromJson<T>(HttpResponseMessage? response)
    {
        if (response == null)
        {
            return default;
        }
        
        var successApiResponse = await response.Content.ReadFromJsonAsync<SuccessApiResponse>();
        T? createdFile = default!;
        
        if (successApiResponse is { Response: JsonElement jsonElement })
        {
            createdFile = jsonElement.Deserialize<T>(JsonResponseSerializerOptions);
        }
        
        return createdFile;
    }
    
    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbconnection);
    }

    public async Task InitializeAsync()
    {
        await _mySqlContainer.StartAsync();
        _dbconnection = new MySqlConnection(_mySqlContainer.GetConnectionString());
        
        HttpClient = CreateClient();
        HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", @"1FR2TsR3kXu2zor7fModuf%2F3nBJRPI4I7LG5x3ODzTVVgFmUd3NguHEmVqDNMJkNlM%2BUnOAKsud4mvCJIyIU5eIT71fPrIfOjJ%2FVc38vVRAvfHoEG%2FC5dUI4%2FWFMG32YD56SXl24jvcSD12x69JEhQ%3D%3D");
        
        await _dbconnection.OpenAsync();
        var command = _dbconnection.CreateCommand();
        command.CommandText = """
                              INSERT INTO `login_events` (`id`, `login`, `active`, `ip`, `browser`, `platform`, `date`, `tenant_id`, `user_id`, `page`, `action`, `description`) VALUES
                              (3, NULL, 1, '127.0.0.1', 'Firefox 132', 'Windows 10', '2024-11-27 12:54:09', 1, '66faa6e4-f133-11ea-b126-00ffeec8b4ef', 'http://localhost:8092/login', 1010, NULL);
                              REPLACE INTO `core_user` (`id`, `tenant`, `username`, `firstname`, `lastname`, `sex`, `bithdate`, `status`, `activation_status`, `email`, `workfromdate`, `terminateddate`, `title`, `culture`, `contacts`, `phone`, `phone_activation`, `location`, `notes`, `sid`, `sso_name_id`, `sso_session_id`, `removed`, `create_on`, `last_modified`, `created_by`, `spam`) VALUES
                              	('66faa6e4-f133-11ea-b126-00ffeec8b4ef', 1, 'administrator', 'Administrator', '', NULL, NULL, 1, 0, 'test@test.com', '2021-03-09 09:52:55', NULL, NULL, NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL, 0, '2022-07-07 21:00:00', '2024-11-27 12:03:32', NULL, 1);
                              REPLACE INTO `core_usersecurity` (`userid`, `tenant`, `pwdhash`, `LastModified`) VALUES
                              	('66faa6e4-f133-11ea-b126-00ffeec8b4ef', 1, 'NRxLGsrQtboXTdER018rStAEEqGMbgEZWTX9jaFWW9RNUAY/xACsO24LMWKxqKmCkL/kTHkldx6NSTnKcO8Pcg==', '2024-11-27 09:03:32');
                              INSERT INTO `files_bunch_objects` (`tenant_id`, `right_node`, `left_node`) VALUES
                              	(1, 'files/virtualrooms/', '1'),
                              	(1, 'files/my/66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2'),
                              	(1, 'files/share/', '3'),
                              	(1, 'files/archive/', '4'),
                              	(1, 'files/trash/66faa6e4-f133-11ea-b126-00ffeec8b4ef', '5');
                              INSERT INTO `files_file` (`id`, `version`, `tenant_id`, `version_group`, `current_version`, `folder_id`, `title`, `content_length`, `file_status`, `category`, `create_by`, `create_on`, `modified_by`, `modified_on`, `converted_type`, `comment`, `changes`, `encrypted`, `forcesave`, `thumb`) VALUES
                              	(1, 1, 1, 1, 1, 2, 'ONLYOFFICE Sample Document.docx', 58193, 0, 3, '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:34', '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:34', NULL, 'Created', NULL, 0, 0, 0),
                              	(2, 1, 1, 1, 1, 2, 'ONLYOFFICE Sample Form.pdf', 1320840, 0, 23, '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:35', '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:35', NULL, 'Created', NULL, 0, 0, 0),
                              	(3, 1, 1, 1, 1, 2, 'ONLYOFFICE Sample Presentation.pptx', 1132381, 0, 4, '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:35', '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:35', NULL, 'Created', NULL, 0, 0, 0),
                              	(4, 1, 1, 1, 1, 2, 'ONLYOFFICE Sample Spreadsheets.xlsx', 144165, 0, 5, '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:35', '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:35', NULL, 'Created', NULL, 0, 0, 0);
                              INSERT INTO `files_folder` (`id`, `parent_id`, `title`, `folder_type`, `create_by`, `create_on`, `modified_by`, `modified_on`, `tenant_id`, `foldersCount`, `filesCount`, `counter`) VALUES
                              	(1, 0, 'virtualrooms', 14, '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:34', '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:34', 1, 0, 0, 0),
                              	(2, 0, 'my', 5, '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:34', '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:35', 1, 0, 4, 2655579),
                              	(3, 0, 'share', 6, '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:34', '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:34', 1, 0, 0, 0),
                              	(4, 0, 'archive', 20, '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:35', '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:35', 1, 0, 0, 0),
                              	(5, 0, 'trash', 3, '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:35', '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '2024-11-27 12:03:35', 1, 0, 0, 0);
                              INSERT INTO `files_folder_tree` (`folder_id`, `parent_id`, `level`) VALUES
                              	(1, 1, 0),
                              	(2, 2, 0),
                              	(3, 3, 0),
                              	(4, 4, 0),
                              	(5, 5, 0);
                              INSERT INTO `tenants_quotarow` (`tenant`, `path`, `user_id`, `counter`, `tag`, `last_modified`) VALUES
                              	(1, '/files/', '00000000-0000-0000-0000-000000000000', 2655579, 'e67be73d-f9ae-4ce1-8fec-1880cb518cb4', '2024-11-27 09:03:35'),
                              	(1, '/files/', '66faa6e4-f133-11ea-b126-00ffeec8b4ef', 2655579, 'e67be73d-f9ae-4ce1-8fec-1880cb518cb4', '2024-11-27 09:03:35');
                              INSERT INTO `webstudio_settings` (`TenantID`, `ID`, `UserID`, `Data`) VALUES
                              	(1, '03b382bd-3c20-4f03-8ab9-5a33f016316e', '66faa6e4-f133-11ea-b126-00ffeec8b4ef', '{"EnableThirdpartySettings":true,"FastDelete":false,"StoreOriginalFiles":true,"KeepNewFileName":false,"DisplayFileExtension":false,"ConvertNotify":true,"DefaultSortedBy":0,"DefaultSortedAsc":false,"HideConfirmConvertSave":false,"HideConfirmConvertOpen":false,"Forcesave":true,"StoreForcesave":false,"HideRecent":false,"HideFavorites":false,"HideTemplates":false,"DownloadZip":false,"ShareLink":false,"ShareLinkSocialMedia":false,"AutomaticallyCleanUp":{"IsAutoCleanUp":true,"Gap":4},"DefaultSharingAccessRights":null,"OpenEditorInSameTab":false}'),
                              	(1, '27504162-16ff-405f-8530-1537b0f2b89d', '00000000-0000-0000-0000-000000000000', '{"Domains":null}'),
                              	(1, 'ee139f6c-8821-4011-8444-fd87882cd5f5', '00000000-0000-0000-0000-000000000000', '{"IsFirst":true}');
                              DELETE FROM `webstudio_settings` WHERE ID = "9a925891-1f92-4ed7-b277-d6f649739f06";
                              """;
        await command.ExecuteNonQueryAsync();
        
        _respawner = await Respawner.CreateAsync(_dbconnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.MySql
        });
    }

    public async Task DisposeAsync()
    {
        await _mySqlContainer.StopAsync();
    }
}