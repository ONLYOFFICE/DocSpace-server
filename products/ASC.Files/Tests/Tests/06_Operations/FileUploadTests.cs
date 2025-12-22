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

using System.Reflection;

using ASC.Files.Tests.ApiFactories;
using ASC.Web.Studio.Core;

namespace ASC.Files.Tests.Tests._06_Operations;

[Collection("Test Collection")]
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class FileUploadTests(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task UploadFile_ReturnsValidFile()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        
        var myFolder = await GetUserFolderIdAsync(Initializer.Owner);
        var fileName = "new.docx";
        
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream($"ASC.Files.Tests.Data.{fileName}")!;
        var contentLength = stream.Length;
        
        var createdSession = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(myFolder, fileName, contentLength, cancellationToken: TestContext.Current.CancellationToken)).Response;
        createdSession.Should().NotBeNull();
        createdSession.Id.Should().NotBeEmpty();
        
        const int chunkSize = 4 * 1024; // 4 KB
        var buffer = new byte[chunkSize];
        var chunkNumber = 1;
        int bytesRead;
        
        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, chunkSize), TestContext.Current.CancellationToken)) > 0)
        {
            var chunkStream = new MemoryStream(buffer, 0, bytesRead);
            var fileParameter = new FileParameter(chunkStream);
            
            await _filesOperationsApi.UploadAsyncSessionAsync(myFolder, createdSession.Id, chunkNumber, fileParameter, TestContext.Current.CancellationToken);
            chunkNumber++;
        }
        
        var resultFile = (await _filesOperationsApi.FinalizeSessionAsync(myFolder, createdSession.Id, TestContext.Current.CancellationToken)).Response;
        resultFile.Should().NotBeNull();
        resultFile.FolderId.Should().Be(myFolder);
        resultFile.Uploaded.Should().BeTrue();
        resultFile.Title.Should().Be(fileName);
        resultFile.File.Should().NotBeNull();
        resultFile.File.FolderId.Should().Be(myFolder);
        resultFile.File.ContentLength.Should().Be(FileSizeComment.FilesSizeToString(contentLength));
    }
}