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

extern alias ASCWebApi;
using ASCWebApi::ASC.Web.Api.ApiModel.RequestsDto;
using ASCWebApi::ASC.Web.Api.ApiModel.ResponseDto;

using WebApiProgram = ASCWebApi::Program;

namespace ASC.Files.Tests1.FilesController;

[Collection("Test Collection")]
public class CreateFileControllerTest(FilesApiFactory filesFactory, WebApplicationFactory<WebApiProgram> factory) : IAsyncLifetime
{
    private readonly HttpClient _filesClient = filesFactory.HttpClient;
    private readonly Func<Task> _resetDatabase = filesFactory.ResetDatabaseAsync;
    private FolderContentDto? _myFolder;
    
    [Fact]
    public async Task Create_ReturnsOk()
    {
        //Arrange
        var file = new CreateFile<JsonElement> { Title = "test.docx" };
        
        //Act
        var response = await _filesClient.PostAsJsonAsync($"api/2.0/files/{_myFolder.Current.Id}/file", file, filesFactory.JsonRequestSerializerOptions);
        var createdFile = await HttpClientHelper.ReadFromJson<FileDto<int>>(response);
        
        //Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(createdFile);
        Assert.Equal("test.docx", createdFile.Title);
    }
    
    public async Task InitializeAsync()  
    {
        var passwordHasher = filesFactory.Services.GetRequiredService<PasswordHasher>();
        
        var apiClient = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("log:dir", Path.Combine("..", "..", "..", "Logs", "Test"));
            builder.UseSetting("ConnectionStrings:default:connectionString", filesFactory.ConnectionString);
        }).CreateClient();
        
        var response = await apiClient.GetAsync("api/2.0/settings");
        var settings = await HttpClientHelper.ReadFromJson<SettingsDto>(response);
        
        if (!string.IsNullOrEmpty(settings?.WizardToken))
        {
            apiClient.DefaultRequestHeaders.TryAddWithoutValidation("confirm", settings.WizardToken);

            response = await apiClient.PutAsJsonAsync("api/2.0/settings/wizard/complete", new WizardRequestsDto
            {
                Email = "test@example.com",
                PasswordHash = passwordHasher.GetClientPassword("11111111")
            });
            
            _ = await HttpClientHelper.ReadFromJson<WizardSettings>(response);
        }
        
        response = await apiClient.PostAsJsonAsync("api/2.0/authentication", new AuthRequestsDto
        { 
            UserName = "test@example.com",
            PasswordHash = passwordHasher.GetClientPassword("11111111")
        }, filesFactory.JsonRequestSerializerOptions);
        
        var authenticationTokenDto = await HttpClientHelper.ReadFromJson<AuthenticationTokenDto>(response);
        if (authenticationTokenDto != null)
        {
            _filesClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authenticationTokenDto.Token);
        }
        
        response = await _filesClient.GetAsync("api/2.0/files/@my");
        _myFolder = await HttpClientHelper.ReadFromJson<FolderContentDto>(response);
    }

    public async Task DisposeAsync()
    {
        await _resetDatabase();
    }
}

