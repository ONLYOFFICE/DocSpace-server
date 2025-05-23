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

using ASC.Files.Tests.Factory;

namespace ASC.Files.Tests.FilesController;

[Collection("Test Collection")]
public class FileCustomizationTest(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task SetCustomFilterTag_InMy_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var file = await CreateFile("file_for_custom_filter.xlsx", FolderType.USER, Initializer.Owner);
        
        // Act
        var customFilterParams = new CustomFilterParameters(enabled: true);
        await Assert.ThrowsAsync<ApiException>(
            async () => await _filesFilesApi.SetCustomFilterTagAsync(file.Id, customFilterParams, TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task SetCustomFilterTag_EnableCustomFilter_ReturnsUpdatedFile()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var createdRoom = await CreateVirtualRoom("room_for_custom_filter", Initializer.Owner); 
        var file = await CreateFile("file_for_custom_filter.xlsx", createdRoom.Id);
        
        // Act
        var customFilterParams = new CustomFilterParameters(enabled: true);
        var result = (await _filesFilesApi.SetCustomFilterTagAsync(file.Id, customFilterParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(file.Id);
        result.CustomFilterEnabled.Should().BeTrue();
        //result.CustomFilterEnabledBy.Should().NotBeNullOrEmpty(); // Should contain user ID who enabled it
    }
    
    [Fact]
    public async Task SetCustomFilterTag_DisableCustomFilter_ReturnsUpdatedFile()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var createdRoom = await CreateVirtualRoom("room_for_custom_filter", Initializer.Owner); 
        var file = await CreateFile("file_for_custom_filter_disable.xlsx", createdRoom.Id);
        
        // First enable custom filter
        var enableParams = new CustomFilterParameters(enabled: true);
        await _filesFilesApi.SetCustomFilterTagAsync(file.Id, enableParams, TestContext.Current.CancellationToken);
        
        // Then disable it
        var disableParams = new CustomFilterParameters(enabled: false);
        var result = (await _filesFilesApi.SetCustomFilterTagAsync(file.Id, disableParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(file.Id);
        result.CustomFilterEnabled.Should().BeNull();
    }
    
    // [Fact]
    // public async Task CreateThumbnails_ValidFile_ReturnsSuccess()
    // {
    //     // Arrange
    //     await _filesClient.Authenticate(Initializer.Owner);
    //     
    //     var file = await CreateFile("file_for_thumbnail.docx", FolderType.USER, Initializer.Owner);
    //     
    //     // Act
    //     var thumbnailRequest = new BaseBatchRequestDto(fileIds: [new(file.Id)]);
    //     var result = (await _filesFilesApi.CreateThumbnailsAsync(thumbnailRequest, TestContext.Current.CancellationToken)).Response;
    //     
    //     // Assert
    //     result.Should().NotBeNull();
    //     result.Should().Contain(id => id == file.Id);
    //     
    //     // Get the file to check thumbnail status
    //     var updatedFile = await GetFile(file.Id);
    //     
    //     // The thumbnail might not be immediately created but the process should have started
    //     // Adjust this assertion based on actual behavior
    // }
}
