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

namespace ASC.Files.Tests.FilesController;

[Collection("Test Collection")]
public class FileHistoryTest(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    WebApplicationFactory<PeopleProgram> peopleFactory,
    WebApplicationFactory<FilesServiceProgram> filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task GetFileVersionInfo_ReturnsVersionHistory()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a file
        var file = await CreateFile("file_with_versions.docx", FolderType.USER, Initializer.Owner);
        
        // Add multiple versions by updating the file content
        await UpdateFileContent(file.Id, "Updated content 1");
        await UpdateFileContent(file.Id, "Updated content 2");
        
        // Act
        var versions = (await _filesFilesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        versions.Should().NotBeNull();
        versions.Should().HaveCountGreaterThanOrEqualTo(3); // Original + 2 updates
        versions.Should().BeInDescendingOrder(v => v.VarVersion);
        
        // Check that each version has the correct properties
        foreach (var version in versions)
        {
            version.Id.Should().Be(file.Id);
            version.Title.Should().Be(file.Title);
            version.FileExst.Should().Be(".docx");
        }
    }
    
    [Fact]
    public async Task ChangeHistory_UpdatesVersionGroup_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a file with multiple versions
        var file = await CreateFile("version_history.docx", FolderType.USER, Initializer.Owner);
        await UpdateFileContent(file.Id, "Updated content 1");
        await UpdateFileContent(file.Id, "Updated content 2");
        
        // Get the latest version
        var versions = (await _filesFilesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response;
        var latestVersion = versions.First().VarVersion;
        
        // Act
        var changeHistoryParams = new ChangeHistory(latestVersion, true);
        var result = (await _filesFilesApi.ChangeHistoryAsync(file.Id, changeHistoryParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        
        // Check that version groups have been updated
        var updatedVersions = (await _filesFilesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response;
        var updatedLatestVersion = updatedVersions.First();
        
        updatedLatestVersion.VarVersion.Should().Be(latestVersion);
        updatedLatestVersion.VersionGroup.Should().Be(latestVersion); // New version group starts at 1
    }
    
    [Fact]
    public async Task GetFileInfo_WithVersion_ReturnsSpecificVersion()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a file with multiple versions
        var file = await CreateFile("file_versions.docx", FolderType.USER, Initializer.Owner);
        await UpdateFileContent(file.Id, "Updated content 1");
        
        // Get the versions to identify the first version number
        var versions = (await _filesFilesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response;
        var firstVersion = versions.Last().VarVersion;
        
        // Act
        var specificVersion = (await _filesFilesApi.GetFileInfoAsync(file.Id, version: firstVersion, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        specificVersion.Should().NotBeNull();
        specificVersion.Id.Should().Be(file.Id);
        specificVersion.VarVersion.Should().Be(firstVersion);
    }
    
    private async Task UpdateFileContent(int fileId, string content)
    {
        // Method to update file content to create new versions
        // This is a simplified implementation, in a real test you would use
        // the actual API to update the file content
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        
        // Initialize parameters for file update
        var contentType = "application/octet-stream";
        var fileName = "updated_file.docx";
        
        var fileData = new FileParameter(fileName, contentType, stream);
        await _filesFilesApi.SaveEditingFromFormAsync(fileId, file: new FileParameter(fileName, contentType, stream), cancellationToken: TestContext.Current.CancellationToken);
    }
}
