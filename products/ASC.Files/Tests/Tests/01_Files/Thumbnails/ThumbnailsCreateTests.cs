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

namespace ASC.Files.Tests.Tests._01_Files.Thumbnails;

/// <summary>
/// POST /files/thumbnails — queues thumbnail generation for a batch of files and/or folders. The
/// endpoint only reports which file IDs were accepted into the queue; it does not wait for
/// generation to finish, so these tests assert the accepted-ids response shape rather than any
/// generated image. Thumbnail generation itself is a background job that needs the document
/// server, which the integration-test host does not run, so it is out of scope here.
/// </summary>
[Trait("Category", "Files")]
public class ThumbnailsCreateTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CreateThumbnails_SingleFile_ReturnsThatFileId()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Thumbnails Single File Room");
        var file = await CreateFile("Autotest Thumbnails Single File", room.Id);

        // Act
        var result = (await _filesApi.CreateThumbnailsWithHttpInfoAsync(
            new BaseBatchRequestDto(fileIds: [new BaseBatchRequestDtoAllOfFileIds(file.Id)]),
            TestContext.Current.CancellationToken)).Data;

        // Assert
        Ids(result).Should().ContainSingle().Which.Should().Be(file.Id);
    }

    [Fact]
    public async Task CreateThumbnails_MultipleFiles_ReturnsAllFileIds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Thumbnails Multi File Room");
        var file1 = await CreateFile("Autotest Thumbnails Multi File 1", room.Id);
        var file2 = await CreateFile("Autotest Thumbnails Multi File 2", room.Id);

        // Act
        var result = (await _filesApi.CreateThumbnailsWithHttpInfoAsync(
            new BaseBatchRequestDto(fileIds:
            [
                new BaseBatchRequestDtoAllOfFileIds(file1.Id),
                new BaseBatchRequestDtoAllOfFileIds(file2.Id)
            ]),
            TestContext.Current.CancellationToken)).Data;

        // Assert
        Ids(result).Should().BeEquivalentTo([file1.Id, file2.Id]);
    }

    /// <summary>
    /// A request that names only folders must be accepted and answer with an empty list: the endpoint
    /// only ever queues files. It currently fails instead - <c>FilesController.CreateThumbnails</c>
    /// calls <c>inDto.FileIds.ToList()</c> with no null guard, so an absent <c>fileIds</c> surfaces as
    /// <c>ArgumentNullException</c> and a 400 carrying a stack trace. No tracker number yet.
    /// </summary>
    [Trait("Bug", "unfiled-thumbnails-null-fileids")]
    [Fact]
    public async Task CreateThumbnails_EmptyFolder_ReturnsEmptyResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Thumbnails Folder Room");
        var folder = await CreateFolder("Autotest Thumbnails Folder", room.Id);

        // Act
        var result = (await _filesApi.CreateThumbnailsWithHttpInfoAsync(
            new BaseBatchRequestDto(folderIds: [new BaseBatchRequestDtoAllOfFolderIds(folder.Id)]),
            TestContext.Current.CancellationToken)).Data;

        // Assert
        Ids(result).Should().BeEmpty();
    }

    /// <summary>
    /// The endpoint only ever queues thumbnails for files: a <c>folderIds</c> entry is accepted in
    /// the request but never turns into a file ID in the response, even when the request also asks
    /// for files.
    /// </summary>
    [Fact]
    public async Task CreateThumbnails_FilesAndFolders_ReturnsOnlyFileIds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Thumbnails Mix Room");
        var file = await CreateFile("Autotest Thumbnails Mix File", room.Id);
        var folder = await CreateFolder("Autotest Thumbnails Mix Folder", room.Id);

        // Act
        var result = (await _filesApi.CreateThumbnailsWithHttpInfoAsync(
            new BaseBatchRequestDto(
                fileIds: [new BaseBatchRequestDtoAllOfFileIds(file.Id)],
                folderIds: [new BaseBatchRequestDtoAllOfFolderIds(folder.Id)]),
            TestContext.Current.CancellationToken)).Data;

        // Assert
        Ids(result).Should().ContainSingle().Which.Should().Be(file.Id);
    }

    /// <summary>
    /// An empty body must be accepted and answer with an empty list. Sent raw, because the generated
    /// client drops the <c>Content-Type</c> header together with the body and ASP.NET then refuses the
    /// call with 415 before it reaches the controller. It currently fails for the same reason as
    /// <see cref="CreateThumbnails_EmptyFolder_ReturnsEmptyResponse"/>: <c>inDto.FileIds</c> is null
    /// and dereferenced without a guard. No tracker number yet.
    /// </summary>
    [Trait("Bug", "unfiled-thumbnails-null-fileids")]
    [Fact]
    public async Task CreateThumbnails_EmptyRequestBody_Returns200WithEmptyResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await _filesClient.PostAsync("api/2.0/files/thumbnails", content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// The endpoint does not validate that a file ID actually exists: a non-existent ID is queued
    /// (and echoed back) exactly like a real one, rather than being rejected or silently dropped.
    /// </summary>
    [Fact]
    public async Task CreateThumbnails_NonExistentFileId_IsEchoedBackAnyway()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var result = (await _filesApi.CreateThumbnailsWithHttpInfoAsync(
            new BaseBatchRequestDto(fileIds: [new BaseBatchRequestDtoAllOfFileIds(999999999)]),
            TestContext.Current.CancellationToken)).Data;

        // Assert
        Ids(result).Should().Contain(999999999L);
    }

    /// <summary>
    /// <see cref="ObjectArrayWrapper.Response" /> is untyped (<c>List&lt;object&gt;</c>); the file
    /// IDs in it deserialize as boxed <see cref="long" />, not <see cref="int" />, so callers must
    /// normalize before comparing against an <see cref="int" /> file ID.
    /// </summary>
    private static List<long> Ids(ObjectArrayWrapper result) =>
        result.Response?.Select(Convert.ToInt64).ToList() ?? [];
}
