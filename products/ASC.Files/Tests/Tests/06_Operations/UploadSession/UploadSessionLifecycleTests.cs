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

namespace ASC.Files.Tests.Tests._06_Operations.UploadSession;

/// <summary>
/// The life of an upload session: <c>POST /files/{folderId}/upload/create_session</c> (legacy envelope),
/// <c>POST /files/{folderId}/session</c>, <c>PUT .../finalize</c> and <c>DELETE .../session/{sessionId}</c>.
/// Sending the parts is in <see cref="UploadSessionChunkTests"/>, access control in
/// <see cref="UploadSessionIsolationTests"/>, open defects in <see cref="UploadSessionDefectTests"/>.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class UploadSessionLifecycleTests(
    AspireAppFixture fixture)
    : UploadSessionTestBase(fixture)
{
    [Fact]
    public async Task CreateUploadSession_LegacyEnvelope_UploadsAndFinalizesToByteIdenticalFile()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(2048, 1);
        const string fileName = "Autotest Upload Legacy.bin";

        // Act
        var (status, created) = await CreateLegacySession(folderId, fileName, payload.Length, createNewIfExist: true);

        // Assert
        status.Should().Be(200);
        created.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = created.GetProperty("data");
        var sessionId = data.GetProperty("id").GetString()!;
        sessionId.Should().MatchRegex("^[0-9a-f]{32}$");
        data.GetProperty("bytes_total").GetInt64().Should().Be(payload.Length);

        (await FilesIn(folderId, fileName)).Should().BeEmpty("nothing is stored before any part arrives");

        await SendChunk(folderId, sessionId, 1, payload);

        var result = await Finalize(folderId, sessionId);

        result.Uploaded.Should().BeTrue();
        result.Title.Should().Be(fileName);
        result.File.PureContentLength.Should().Be(payload.Length);
        await ExpectContent(result.File.Id, payload);
    }

    [Fact]
    public async Task CreateUploadSession_LegacyInNonExistentFolder_ReturnsNotFound()
    {
        // Act
        var (status, _) = await CreateLegacySession(99999999, "Autotest Upload Legacy Missing.bin", 10);

        // Assert
        status.Should().Be(404);
    }

    [Fact]
    public async Task CreateUploadSessionInFolder_NonExistentFolder_ReturnsNotFound()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CreateUploadSessionInFolderAsync(
                99999999,
                new SessionRequest("Autotest Upload Missing Folder.bin", 10),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateUploadSessionInFolder_TwoSessionsForOneName_StayIndependent()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        const string fileName = "Autotest Upload Two Sessions.bin";

        var first = await OpenSession(folderId, fileName, 100);
        var second = await OpenSession(folderId, fileName, 100);

        first.Should().NotBe(second);
        (await FilesIn(folderId, fileName)).Should().BeEmpty("opening a session stores no file");

        var bytesA = MakeBytes(100, 11);
        var bytesB = MakeBytes(100, 12);
        await SendChunk(folderId, first, 1, bytesA);
        await SendChunk(folderId, second, 1, bytesB);

        // Act
        var a = await Finalize(folderId, first);
        var b = await Finalize(folderId, second);

        // Assert: createNewIfExist=true, so the second upload lands beside the first one.
        a.File.Id.Should().NotBe(b.File.Id);
        await ExpectContent(a.File.Id, bytesA);
        await ExpectContent(b.File.Id, bytesB);
    }

    /// <remarks>
    /// On a disk storage the abort answers 500 with "The directory is not empty" for the session's chunks folder:
    /// the folder is removed while the stored chunk is still in it. No bug number yet.
    /// </remarks>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task AbortUploadSession_LargeSessionAfterOneChunk_LeavesNoFileClosesSessionAndSparesNeighbour()
    {
        // Arrange
        var chunkSize = await GetChunkSize();
        var folderId = await GetUserFolderIdAsync(Owner);
        var total = chunkSize + 100;
        var doomed = MakeBytes(total, 21);
        var survivor = MakeBytes(total, 22);

        var doomedId = await OpenSession(folderId, "Autotest Upload Doomed.bin", total);
        var survivorId = await OpenSession(folderId, "Autotest Upload Survivor.bin", total);

        await SendChunk(folderId, doomedId, 1, doomed[..chunkSize]);
        await SendChunk(folderId, survivorId, 1, survivor[..chunkSize]);

        // Act
        await _filesOperationsApi.AbortUploadSessionAsync(doomedId, folderId, TestContext.Current.CancellationToken);

        // Assert: the aborted session is gone for every method
        (await ChunkStatus(folderId, doomedId, 2, doomed[chunkSize..])).Should().Be(404);
        (await FinalizeStatus(folderId, doomedId)).Should().Be(404);
        (await AbortStatus(folderId, doomedId)).Should().Be(404);
        (await FilesIn(folderId, "Autotest Upload Doomed")).Should().BeEmpty();

        // and the neighbouring session still completes byte-for-byte
        await SendChunk(folderId, survivorId, 2, survivor[chunkSize..]);
        var done = await Finalize(folderId, survivorId);

        await ExpectContent(done.File.Id, survivor);
    }

    [Fact]
    public async Task AbortUploadSession_BeforeAnyChunk_LeavesNoFileAndClosesSession()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        var sessionId = await OpenSession(folderId, "Autotest Upload Abort Fresh.bin", 64);

        // Act
        await _filesOperationsApi.AbortUploadSessionAsync(sessionId, folderId, TestContext.Current.CancellationToken);

        // Assert
        (await FilesIn(folderId, "Autotest Upload Abort Fresh")).Should().BeEmpty();
        (await ChunkStatus(folderId, sessionId, 1, MakeBytes(64, 1))).Should().Be(404);
    }

    [Fact]
    public async Task UploadSession_NonExistentSessionId_ReturnsNotFoundOnEveryMethod()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        var missing = new string('0', 32);
        var part = MakeBytes(32, 1);

        // Act & Assert
        (await FinalizeStatus(folderId, missing)).Should().Be(404);
        (await ChunkStatus(folderId, missing, 1, part)).Should().Be(404);
        (await SequentialChunkStatus(folderId, missing, part)).Should().Be(404);
        (await AbortStatus(folderId, missing)).Should().Be(404);

        // a value that is not even shaped like an id is answered the same way
        (await FinalizeStatus(folderId, "not-a-session")).Should().Be(404);
    }
}
