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
extern alias ASCPeople;
extern alias ASCWebApi;

using Docspace.Client;

namespace ASC.Files.Tests.FilesController;

[Collection("Test Collection")]
public class CacheTest(
    FilesApiFactory filesFactory,
    WebApplicationFactory<WebApiProgram> apiFactory,
    WebApplicationFactory<PeopleProgram> peopleFactory,
    WebApplicationFactory<FilesServiceProgram> filesServiceProgram)
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task Cache_root()
    {
        await _filesClient.Authenticate(Initializer.Owner);

        //send for get If-Modified-Since
        var response = await _filesFoldersApi.GetRootFoldersWithHttpInfoAsync(cancellationToken: TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        SetIfModifiedSince(response);

        //send for check 304
        var responseCached = await _filesFoldersApi.GetRootFoldersWithHttpInfoAsync(cancellationToken: TestContext.Current.CancellationToken);
        responseCached.StatusCode.Should().Be(HttpStatusCode.NotModified);

        //send for clear cache
        ClearIfModifiedSince();
        var createdFile = await CreateFile("test.docx", FolderType.USER, Initializer.Owner);
        SetIfModifiedSince(response);

        //send for check 200
        var responseAfterChanged = await _filesFoldersApi.GetRootFoldersWithHttpInfoAsync(cancellationToken: TestContext.Current.CancellationToken);
        responseAfterChanged.StatusCode.Should().Be(HttpStatusCode.OK);
        ClearIfModifiedSince();
    }

    [Fact]
    public async Task Cache_get_folder()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        //send for get folderId
        var folder = await GetUserFolderIdAsync(Initializer.Owner);

        //send for get If-Modified-Since
        var response = await _filesFoldersApi.GetFolderByFolderIdWithHttpInfoAsync(folder, cancellationToken: TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        SetIfModifiedSince(response);

        //send for check 304
        var responseCached = await _filesFoldersApi.GetFolderByFolderIdWithHttpInfoAsync(folder, cancellationToken: TestContext.Current.CancellationToken);
        responseCached.StatusCode.Should().Be(HttpStatusCode.NotModified);

        //send for clear cache
        await CreateFile("test.docx", FolderType.USER, Initializer.Owner);

        //send for check 200
        var responseAfterChanged = await _filesFoldersApi.GetFolderByFolderIdWithHttpInfoAsync(folder, cancellationToken: TestContext.Current.CancellationToken);
        responseAfterChanged.StatusCode.Should().Be(HttpStatusCode.OK);
        ClearIfModifiedSince();
    }

    [Fact]
    public async Task Cache_settings()
    {
        await _filesClient.Authenticate(Initializer.Owner);

        //send for get If-Modified-Since
        var response = await _filesSettingsApi.GetFilesSettingsWithHttpInfoAsync(TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        SetIfModifiedSince(response);

        //send for check 304
        var responseCached = await _filesSettingsApi.GetFilesSettingsWithHttpInfoAsync(TestContext.Current.CancellationToken);
        responseCached.StatusCode.Should().Be(HttpStatusCode.NotModified);
        ClearIfModifiedSince();
    }

    private void SetIfModifiedSince<T>(ApiResponse<T> response)
    {
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation("If-Modified-Since", response.Headers["Last-Modified"]);
    }

    private void ClearIfModifiedSince()
    {
        _filesClient.DefaultRequestHeaders.Remove("If-Modified-Since");
    }
}
