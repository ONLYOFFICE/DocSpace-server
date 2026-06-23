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

namespace ASC.Files.Tests.Tests._05_Features;

[Collection("Test Collection")]
[Trait("Category", "Features")]
public class FileHistoryTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetFileVersionInfo_ReturnsVersionHistory()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a file
        var file = await CreateFileInMy("file_with_versions.docx", Initializer.Owner);
        
        // Add multiple versions by updating the file content
        await UpdateFileContent(file.Id, "Updated content 1");
        await UpdateFileContent(file.Id, "Updated content 2");
        
        // Act
        var versions = (await _filesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        versions.Should().NotBeNull();
        versions.Should().HaveCountGreaterThanOrEqualTo(3); // Original + 2 updates
        versions.Should().BeInDescendingOrder(v => v.Version);
        
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
        var file = await CreateFileInMy("version_history.docx",  Initializer.Owner);
        await UpdateFileContent(file.Id, "Updated content 1");
        await UpdateFileContent(file.Id, "Updated content 2");
        
        // Get the latest version
        var versions = (await _filesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response;
        var latestVersion = versions.First().Version;
        
        // Act
        var changeHistoryParams = new ChangeHistory(latestVersion, true);
        var result = (await _filesApi.ChangeVersionHistoryAsync(file.Id, changeHistoryParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        
        // Check that version groups have been updated
        var updatedVersions = (await _filesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response;
        var updatedLatestVersion = updatedVersions.First();
        
        updatedLatestVersion.Version.Should().Be(latestVersion);
        updatedLatestVersion.VersionGroup.Should().Be(latestVersion); // New version group starts at 1
    }
    
    [Fact]
    public async Task GetFileInfo_WithVersion_ReturnsSpecificVersion()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        // Create a file with multiple versions
        var file = await CreateFileInMy("file_versions.docx",Initializer.Owner);
        await UpdateFileContent(file.Id, "Updated content 1");
        
        // Get the versions to identify the first version number
        var versions = (await _filesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response;
        var firstVersion = versions.Last().Version;
        
        // Act
        var specificVersion = (await _filesApi.GetFileInfoAsync(file.Id, version: firstVersion, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        specificVersion.Should().NotBeNull();
        specificVersion.Id.Should().Be(file.Id);
        specificVersion.Version.Should().Be(firstVersion);
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
        await _filesApi.SaveEditingFileFromFormAsync(fileId, file: new FileParameter(fileName, contentType, stream), cancellationToken: TestContext.Current.CancellationToken);
    }
}
