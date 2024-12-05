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
extern alias ASCPeople;
using FluentAssertions;

namespace ASC.Files.Tests1.FilesController;

[Collection("Test Collection")]
public class CreateFileControllerTest(FilesApiFactory filesFactory, WebApplicationFactory<WebApiProgram> apiFactory, WebApplicationFactory<PeopleProgram> peopleFactory) : IAsyncLifetime
{
    private readonly HttpClient _filesClient = filesFactory.HttpClient;
    private readonly Func<Task> _resetDatabase = filesFactory.ResetDatabaseAsync;
    public static IEnumerable<object[]> Data =>
        new List<object[]>
        {
            new object[] { "test.docx" },
            new object[] { "test.pptx" },
            new object[] { "test.xlsx" },
            new object[] { "test.pdf" },
        };

    [Theory]
    [MemberData(nameof(Data))]
    public async Task CreateFile_Owner_ReturnsOk(string? fileName)
    {
        await Initializer.AuthenticateOwner(filesFactory, _filesClient);
        
        //Arrange
        var file = new CreateFile<JsonElement> { Title = fileName };
        var response = await _filesClient.GetAsync("@my");
        var myFolder = await HttpClientHelper.ReadFromJson<FolderContentDto>(response);
        
        //Act
        response = await _filesClient.PostAsJsonAsync($"{myFolder.Current.Id}/file", file, filesFactory.JsonRequestSerializerOptions);
        var createdFile = await HttpClientHelper.ReadFromJson<FileDto<int>>(response);
        
        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        createdFile.Should().NotBeNull();
        createdFile.Title.Should().Be(fileName);
    }
    
    [Theory]
    [MemberData(nameof(Data))]
    public async Task CreateFile_RoomAdmin_ReturnsOk(string? fileName)
    {
        var roomAdmin = await Initializer.InviteUsers(filesFactory, EmployeeType.RoomAdmin);
        await Initializer.Authenticate(filesFactory, _filesClient, roomAdmin.Email, roomAdmin.Password);
        
        //Arrange
        var file = new CreateFile<JsonElement> { Title = fileName };
        var response = await _filesClient.GetAsync("@my");
        var myFolder = await HttpClientHelper.ReadFromJson<FolderContentDto>(response);
        
        //Act
        response = await _filesClient.PostAsJsonAsync($"{myFolder.Current.Id}/file", file, filesFactory.JsonRequestSerializerOptions);
        var createdFile = await HttpClientHelper.ReadFromJson<FileDto<int>>(response);
        
        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        createdFile.Should().NotBeNull();
        createdFile.Title.Should().Be(fileName);
    }
    
    [Fact]
    public async Task Create_FolderDoesNotExist_ReturnsFail()
    {
        await Initializer.AuthenticateOwner(filesFactory, _filesClient);
        
        //Arrange
        var file = new CreateFile<JsonElement> { Title = "test.docx" };
        
        //Act
        var response = await _filesClient.PostAsJsonAsync($"{Random.Shared.Next(10000, 20000)}/file", file, filesFactory.JsonRequestSerializerOptions);
        
        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
    
    public async Task InitializeAsync()
    {
        await Initializer.InitializeAsync(filesFactory, apiFactory, peopleFactory);
    }

    public async Task DisposeAsync()
    {
        await _resetDatabase();
    }
}

