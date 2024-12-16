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

namespace ASC.Files.Tests1.FilesController;

[Collection("Test Collection")]
public class CreateFileControllerTest(FilesApiFactory filesFactory, WebApplicationFactory<WebApiProgram> apiFactory, WebApplicationFactory<PeopleProgram> peopleFactory) : IAsyncLifetime
{
    private readonly HttpClient _filesClient = filesFactory.HttpClient;
    private readonly Func<Task> _resetDatabase = filesFactory.ResetDatabaseAsync;
    public static TheoryData<string> Data =>
    [
        "test.docx",
        "test.pptx",
        "test.xlsx",
        "test.pdf"
    ];
    
    public static IEnumerable<object[]> FolderTypesData =>
        new List<object[]>
        {
            new object[] { FolderType.Archive },
            new object[] { FolderType.TRASH },
            new object[] { FolderType.VirtualRooms }
        };

    [Theory]
    [MemberData(nameof(Data))]
    public async Task CreateFile_FolderMy_Owner_ReturnsOk(string? fileName)
    {
        await CreateFile(fileName, FolderType.USER, Initializer.Owner);
    }
    
    [Theory]
    [MemberData(nameof(Data))]
    public async Task CreateFile_FolderMy_RoomAdmin_ReturnsOk(string? fileName)
    {
        var roomAdmin = await Initializer.InviteContact(filesFactory, EmployeeType.RoomAdmin);
        
        await CreateFile(fileName, FolderType.USER, roomAdmin);
    }
    
    [Theory]
    [MemberData(nameof(Data))]
    public async Task CreateFile_FolderMy_User_ReturnsOk(string? fileName)
    {
        var user = await Initializer.InviteContact(filesFactory, EmployeeType.User);
        
        await CreateFile(fileName, FolderType.USER, user);
    }
    
    [Fact]
    public async Task Create_FolderDoesNotExist_ReturnsFail()
    {
        await Initializer.Authenticate(filesFactory, _filesClient, Initializer.Owner.Email, Initializer.Owner.Password);
        
        //Arrange
        var file = new CreateFile<JsonElement> { Title = "test.docx" };
        
        //Act
        var response = await _filesClient.PostAsJsonAsync($"{Random.Shared.Next(10000, 20000)}/file", file, filesFactory.JsonRequestSerializerOptions);
        
        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
    
    [Theory]
    [MemberData(nameof(FolderTypesData))]
    public async Task CreateFile_SystemFolder_Owner_ReturnsOk(FolderType folderType)
    {
        var createdFile = await CreateFile("test.docx", folderType, Initializer.Owner);

        createdFile.RootFolderType.Should().Be(FolderType.DEFAULT);
    }
    
    [Fact]
    public async Task CreateFile_FolderRecent_Owner_ReturnsOk()
    {
        await Initializer.Authenticate(filesFactory, _filesClient, Initializer.Owner.Email, Initializer.Owner.Password);
        
        var response = await _filesClient.GetAsync("recent");
        var recentFolder = await HttpClientHelper.ReadFromJson<FolderContentDto>(response);
        var createdFile = await CreateFile("test.docx", recentFolder.Current.Id);

        createdFile.RootFolderType.Should().Be(FolderType.DEFAULT);
    }
    
    public async Task InitializeAsync()
    {
        await Initializer.InitializeAsync(filesFactory, apiFactory, peopleFactory);
    }

    public async Task DisposeAsync()
    {
        await _resetDatabase();
    }
    
    private async Task<FileDto<int>> CreateFile(string? fileName, FolderType folderType, User user)
    {
        await Initializer.Authenticate(filesFactory, _filesClient, user.Email, user.Password);
        
        var response = await _filesClient.GetAsync("@root");
        var rootFolder = await HttpClientHelper.ReadFromJson<IEnumerable<FolderContentDto>>(response);
        var folderId = rootFolder.FirstOrDefault(r => r.Current.RootFolderType == folderType).Current.Id;
        
        return await CreateFile(fileName, folderId);
    }
    
    private async Task<FileDto<int>> CreateFile(string? fileName, int folderId)
    {
        //Arrange
        var file = new CreateFile<JsonElement> { Title = fileName };
        
        //Act
        var response = await _filesClient.PostAsJsonAsync($"{folderId}/file", file, filesFactory.JsonRequestSerializerOptions);
        var createdFile = await HttpClientHelper.ReadFromJson<FileDto<int>>(response);
        
        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        createdFile.Should().NotBeNull();
        createdFile.Title.Should().Be(fileName);
        
        return createdFile;
    }
}

