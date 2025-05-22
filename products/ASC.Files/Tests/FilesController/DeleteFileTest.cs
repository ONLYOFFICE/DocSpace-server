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
public class DeleteFileTest(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    WebApplicationFactory<PeopleProgram> peopleFactory,
    WebApplicationFactory<FilesServiceProgram> filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task DeleteFile_FolderMy_Owner_ReturnsOk()
    {
        var createdFile = await CreateFile("test.docx", FolderType.USER, Initializer.Owner);
        
        var fileToDelete = (await _filesFilesApi.DeleteFileAsync(createdFile.Id, new Delete { Immediately = true }, TestContext.Current.CancellationToken)).Response;
        
        if (fileToDelete.Any(r => !r.Finished))
        {
            fileToDelete = await WaitLongOperation();
        }
        
        fileToDelete.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
        
        // Verify file no longer exists or has been moved to trash
        await Assert.ThrowsAsync<Docspace.Client.ApiException>(async () => 
            await _filesFilesApi.GetFileInfoAsync(createdFile.Id, cancellationToken: TestContext.Current.CancellationToken));
    }
    
    // [Fact]
    // public async Task DeleteFile_NonExistingFile_ReturnsError()
    // {
    //     // Arrange
    //     await _filesClient.Authenticate(Initializer.Owner);
    //     var nonExistingFileId = 99999; // Non-existing file ID
    //     
    //     // Act & Assert
    //     var exception = await Assert.ThrowsAsync<Docspace.Client.ApiException>(
    //         async () => await _filesFilesApi.DeleteFileAsync(
    //             nonExistingFileId, 
    //             new Delete(false, true),
    //             TestContext.Current.CancellationToken));
    //     
    //     exception.ErrorCode.Should().Be(404);
    // }
    //
    // [Fact]
    // public async Task DeleteFile_WithoutPermission_ReturnsAccessDenied()
    // {
    //     // Arrange
    //     await _filesClient.Authenticate(Initializer.Owner);
    //     
    //     // Create a file by owner
    //     var file = await CreateFile("file_no_permissions.docx", FolderType.USER, Initializer.Owner);
    //     
    //     // Switch to another user who doesn't have permission
    //     var user = await Initializer.InviteContact(EmployeeType.User);
    //     await _filesClient.Authenticate(user);
    //     
    //     // Act & Assert
    //     var exception = await Assert.ThrowsAsync<Docspace.Client.ApiException>(
    //         async () => await _filesFilesApi.DeleteFileAsync(
    //             file.Id, 
    //             new Delete(false, true), 
    //             TestContext.Current.CancellationToken));
    //     
    //     exception.ErrorCode.Should().Be(403);
    // }
}