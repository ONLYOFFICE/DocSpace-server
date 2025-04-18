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

extern alias ASCWebApi;
extern alias ASCPeople;

namespace ASC.Files.Tests.FilesController;

[Collection("Test Collection")]
public class CreateFileTest(
    FilesApiFactory filesFactory, 
    WebApplicationFactory<WebApiProgram> apiFactory, 
    WebApplicationFactory<PeopleProgram> peopleFactory,
    WebApplicationFactory<FilesServiceProgram> filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{

    public static TheoryData<string> Data =>
    [
        "test.docx",
        "test.pptx",
        "test.xlsx",
        "test.pdf"
    ];

    public static TheoryData<FolderType> FolderTypesData =>
    [
        FolderType.Archive,
        FolderType.TRASH,
        FolderType.VirtualRooms
    ];

    [Theory]
    [MemberData(nameof(Data))]
    public async Task CreateFile_FolderMy_Owner_ReturnsOk(string fileName)
    {
        var createdFile = await CreateFile(fileName, FolderType.USER, Initializer.Owner);
        
        createdFile.Should().NotBeNull();
        createdFile.Title.Should().Be(fileName);
        
        // Verify the file was created
        var file = await GetFile(createdFile.Id);
        file.Should().NotBeNull();
        file.Title.Should().Be(fileName);
    }
    
    [Theory]
    [MemberData(nameof(Data))]
    public async Task CreateFile_FolderMy_RoomAdmin_ReturnsOk(string fileName)
    {
        var roomAdmin = await Initializer.InviteContact(EmployeeType.RoomAdmin);
        
        var createdFile = await CreateFile(fileName, FolderType.USER, roomAdmin);
        
        createdFile.Should().NotBeNull();
        createdFile.Title.Should().Be(fileName);
        
        // Verify the file was created
        var file = await GetFile(createdFile.Id);
        file.Should().NotBeNull();
        file.Title.Should().Be(fileName);
    }
    
    [Theory]
    [MemberData(nameof(Data))]
    public async Task CreateFile_FolderMy_User_ReturnsOk(string fileName)
    {
        var user = await Initializer.InviteContact(EmployeeType.User);
        
        var createdFile = await CreateFile(fileName, FolderType.USER, user);

        createdFile.Should().NotBeNull();
        createdFile.Title.Should().Be(fileName);
        
        // Verify the file was created
        var file = await GetFile(createdFile.Id);
        file.Should().NotBeNull();
        file.Title.Should().Be(fileName);
    }
    
    [Fact]
    public async Task CreateFile_FolderDoesNotExist_ReturnsFail()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        
        //Arrange
        var file = new CreateFileJsonElement("test.docx");
        
        await Assert.ThrowsAsync<Docspace.Client.ApiException>(async () => await _filesFilesApi.CreateFileAsync(Random.Shared.Next(10000, 20000), file, cancellationToken: TestContext.Current.CancellationToken));
    }
    
    [Theory]
    [MemberData(nameof(FolderTypesData))]
    public async Task CreateFile_SystemFolder_Owner_ReturnsOk(FolderType folderType)
    {
        var createdFile = await CreateFile("test.docx", folderType, Initializer.Owner);

        createdFile.RootFolderType.Should().NotBe(folderType);
    }
    
    [Fact]
    public async Task CreateFile_NameLongerThan165Chars_Returns400()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Arrange
        var longFileName = new string('a', 166) + ".docx"; // 166 characters + 5 for extension = 171 characters
        var file = new CreateFileJsonElement(longFileName);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<Docspace.Client.ApiException>(
            async () => await _filesFilesApi.CreateFileAsync(
                await GetFolderIdAsync(FolderType.USER, Initializer.Owner), 
                file, 
                cancellationToken: TestContext.Current.CancellationToken));
        
        exception.ErrorCode.Should().Be(400);
    }
    
    [Fact]
    public async Task CreateTextFile_ValidContent_ReturnsNewTextFile()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var userFolderId = await GetUserFolderIdAsync(Initializer.Owner);
        var fileName = "new_text_file";
        var content = "This is the content of my text file.";
        
        // Act
        var createParams = new CreateTextOrHtmlFile(
            title: fileName,
            content: content,
            createNewIfExist: true
        );
        
        var result = (await _filesFilesApi.CreateTextFileAsync(userFolderId, createParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(fileName + ".txt");
        result.FolderId.Should().Be(userFolderId);
        result.FileExst.Should().Be(".txt");
        
        // We cannot directly verify content in this test without downloading the file
    }
    
    [Fact]
    public async Task CreateHtmlFile_ValidContent_ReturnsNewHtmlFile()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var userFolderId = await GetUserFolderIdAsync(Initializer.Owner);
        var fileName = "new_html_file";
        var content = "<html><body><h1>Test HTML</h1><p>This is a test HTML file.</p></body></html>";
        
        // Act
        var createParams = new CreateTextOrHtmlFile(
            title: fileName,
            content: content,
            createNewIfExist: true
        );
        
        var result = (await _filesFilesApi.CreateHtmlFileAsync(userFolderId, createParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(fileName + ".html");
        result.FolderId.Should().Be(userFolderId);
        result.FileExst.Should().Be(".html");
    }
}
