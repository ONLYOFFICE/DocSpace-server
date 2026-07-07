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

using System.Reflection;

namespace ASC.Files.Tests.Tests._06_Operations;

[Collection("Test Collection")]
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class FileUploadTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UploadFile_ReturnsValidFile()
    {
        await _filesClient.Authenticate(Initializer.Owner);

        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response;
        var myFolder = await GetUserFolderIdAsync(Initializer.Owner);
        var fileName = "new.docx";
        
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream($"ASC.Files.Tests.Data.{fileName}")!;
        var contentLength = stream.Length;
        
        var createdSession = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(myFolder, new SessionRequest( fileName, contentLength), cancellationToken: TestContext.Current.CancellationToken)).Response;
        createdSession.Should().NotBeNull();
        createdSession.Id.Should().NotBeEmpty();
        
        var chunkSize = (int)settings.ChunkUploadSize;
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
        resultFile.File.PureContentLength.Should().Be(contentLength);
        
        var configuration = (await _filesApi.GetFileInfoAsync(resultFile.File.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var fileStream = await _filesClient.GetStreamAsync(configuration.ViewUrl, TestContext.Current.CancellationToken);
        
        await using var fileTempStream = new MemoryStream();
        await fileStream.CopyToAsync(fileTempStream, TestContext.Current.CancellationToken);

        fileTempStream.Position = 0;
        stream.Position = 0;

        AreStreamsEqual(fileTempStream, stream).Should().BeTrue();

    }
    
    private static bool AreStreamsEqual(Stream stream1, Stream stream2)
    {
        stream1.Position = 0;
        stream2.Position = 0;

        using var sha256 = SHA256.Create();
        var hash1 = sha256.ComputeHash(stream1);
    
        stream2.Position = 0;
        var hash2 = sha256.ComputeHash(stream2);

        return hash1.SequenceEqual(hash2);
    }

}