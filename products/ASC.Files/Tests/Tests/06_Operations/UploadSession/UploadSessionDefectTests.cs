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
/// Defects found in the upload-session endpoints while probing them; each test asserts what the contract
/// says and is red while the defect is open. The defects have no bug number yet, so they carry the
/// <c>pending</c> trait value - replace it with the number once the bug is filed.
/// A root cause behind several of them: a small session (payload under <c>chunkUploadSize</c>) STORES the
/// file on the first <c>uploadAsyncSession</c> call, before finalize, although the contract says that call
/// "never completes the upload".
/// </summary>
[Trait("Category", "Bug")]
[Trait("Feature", "Files")]
public class UploadSessionDefectTests(
    AspireAppFixture fixture)
    : UploadSessionTestBase(fixture)
{
    /// <summary>A missing multipart file part answers 500 (a NullReferenceException) instead of a client error.</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task UploadAsyncSession_WithoutFilePart_ReturnsClientError()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        var sessionId = await OpenSession(folderId, "Autotest Upload No File Part.bin", 500);

        // Act
        var status = await StatusOf(() => _filesOperationsApi.UploadAsyncSessionWithHttpInfoAsync(
            folderId, sessionId, 1, null, TestContext.Current.CancellationToken));

        // Assert
        status.Should().BeInRange(400, 499);
    }

    /// <summary>Finalize without any chunk answers 201 with <c>uploaded=true</c> and file id 0, and stores nothing.</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task FinalizeSession_WithoutAnyChunk_DoesNotReportAFileThatDoesNotExist()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        const string fileName = "Autotest Upload Finalize Empty.bin";
        var sessionId = await OpenSession(folderId, fileName, 500);
        (await FilesIn(folderId, fileName)).Should().BeEmpty();

        // Act
        var (status, result) = await TryFinalize(folderId, sessionId);

        // Assert
        await AssertFinalizeIsHonest(folderId, fileName, status, result);
    }

    /// <summary>A small session lists a truncated file in the folder after the first chunk, before finalize.</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task UploadAsyncSession_SmallSessionFirstChunk_DoesNotExposeTruncatedFile()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        const string fileName = "Autotest Upload Truncated Visible.bin";
        var sessionId = await OpenSession(folderId, fileName, 1000);
        (await FilesIn(folderId, fileName)).Should().BeEmpty();

        // Act: 400 of the declared 1000 bytes
        await SendChunk(folderId, sessionId, 1, MakeBytes(400, 52));

        // Assert
        (await FilesIn(folderId, fileName)).Should().BeEmpty("the file must not be listed short of its declared size");
    }

    /// <summary>A small session stores the file at once; the contract says the chunk call never completes the upload.</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task UploadAsyncSession_SmallSessionFullPayload_StoresNoFileUntilFinalize()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(1000, 52);
        const string fileName = "Autotest Upload Small Early Store.bin";
        var sessionId = await OpenSession(folderId, fileName, payload.Length);
        (await FilesIn(folderId, fileName)).Should().BeEmpty();

        // Act
        await SendChunk(folderId, sessionId, 1, payload);

        // Assert
        (await FilesIn(folderId, fileName)).Should().BeEmpty();
    }

    /// <summary>300 bytes into a 100-byte session: finalize answers 201 with file id 0.</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task FinalizeSession_SmallSessionWithMoreBytesThanDeclared_DoesNotReportAFileThatDoesNotExist()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        const string fileName = "Autotest Upload Finalize Over Declared.bin";
        var sessionId = await OpenSession(folderId, fileName, 100);
        await SendChunk(folderId, sessionId, 1, MakeBytes(300, 53));
        (await FilesIn(folderId, fileName)).Should().BeEmpty();

        // Act
        var (status, result) = await TryFinalize(folderId, sessionId);

        // Assert
        await AssertFinalizeIsHonest(folderId, fileName, status, result);
    }

    /// <summary>Chunk numbering starts at 1, but 0 and -1 answer 500 on a large session.</summary>
    [Theory]
    [Trait("Category", "Bug")]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UploadAsyncSession_NonPositiveChunkNumberOnLargeSession_ReturnsClientError(int chunkNumber)
    {
        // Arrange
        var chunkSize = await GetChunkSize();
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(chunkSize + 100, 54);
        var sessionId = await OpenSession(folderId, "Autotest Upload Bad Chunk Number.bin", payload.Length);

        // Act
        var status = await ChunkStatus(folderId, sessionId, chunkNumber, payload[..chunkSize]);

        // Assert
        status.Should().BeInRange(400, 499);
    }

    /// <summary>The contract says finalize ends the session and cannot be repeated; a small session answers 201 again.</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task FinalizeSession_SmallSessionFinalizedTwice_RefusesTheSecondCall()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(1000, 55);
        const string fileName = "Autotest Upload Small Refinalize.bin";
        var sessionId = await OpenSession(folderId, fileName, payload.Length);
        await SendChunk(folderId, sessionId, 1, payload);
        (await Finalize(folderId, sessionId)).File.Id.Should().BePositive();

        // Act
        var second = await FinalizeStatus(folderId, sessionId);

        // Assert
        second.Should().BeGreaterThanOrEqualTo(400);
        (await FilesIn(folderId, fileName)).Should().HaveCount(1);
    }

    /// <summary>A chunk sent after finalize is accepted by a small session.</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task UploadAsyncSession_SmallSessionChunkAfterFinalize_IsRefusedAndFileIsNotRewritten()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(1000, 56);
        var sessionId = await OpenSession(folderId, "Autotest Upload Small Late Chunk.bin", payload.Length);
        await SendChunk(folderId, sessionId, 1, payload);
        var fileId = (await Finalize(folderId, sessionId)).File.Id;

        // Act
        var late = await ChunkStatus(folderId, sessionId, 1, MakeBytes(1000, 57));

        // Assert
        late.Should().BeGreaterThanOrEqualTo(400);
        await ExpectContent(fileId, payload);
    }

    /// <summary>The contract says there is nothing left to abort once the file exists; a small session answers 200.</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task AbortUploadSession_SmallSessionAfterFinalize_IsRefused()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(1000, 58);
        var sessionId = await OpenSession(folderId, "Autotest Upload Small Late Abort.bin", payload.Length);
        await SendChunk(folderId, sessionId, 1, payload);
        await Finalize(folderId, sessionId);

        // Act
        var abort = await AbortStatus(folderId, sessionId);

        // Assert
        abort.Should().BeGreaterThanOrEqualTo(400);
    }

    /// <summary>A brand-new file is version 1; probing showed one extra version per repeated chunk call.</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task FinalizeSession_RepeatedChunksIntoOneSmallSession_ProduceANewFileAtVersionOne()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(1000, 59);
        var sessionId = await OpenSession(folderId, "Autotest Upload Extra Versions.bin", payload.Length);

        for (var i = 0; i < 3; i++)
        {
            await SendChunk(folderId, sessionId, 1, payload);
        }

        // Act
        var result = await Finalize(folderId, sessionId);

        // Assert
        result.Version.Should().Be(1);
        (await VersionsOf(result.File.Id)).Should().Equal(1);
    }

    /// <summary>A fileSize of -1 opens a session with bytes_total -1; finalize fails later with 400.</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task CreateUploadSessionInFolder_NegativeFileSize_IsRejectedWhenTheSessionOpens()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CreateUploadSessionInFolderAsync(
                folderId,
                new SessionRequest("Autotest Upload Negative Size.bin", -1, createNewIfExist: true),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().BeInRange(400, 499);
    }

    /// <summary>A short intermediate chunk makes finalize answer 500 ("smaller than the minimum allowed size").</summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task FinalizeSession_ShortIntermediateChunk_FailsAsClientErrorAtTheLatest()
    {
        // Arrange
        var chunkSize = await GetChunkSize();
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(chunkSize + 100, 60);
        const string fileName = "Autotest Upload Short Middle Chunk.bin";
        var sessionId = await OpenSession(folderId, fileName, payload.Length);

        // Chunk 1 is 100 bytes instead of chunkUploadSize and chunk 2 carries the rest; the answers to the
        // parts are left unasserted, refusing already there would be fine too.
        await ChunkStatus(folderId, sessionId, 1, payload[..100]);
        await ChunkStatus(folderId, sessionId, 2, payload[100..(100 + chunkSize)]);

        // Act
        var status = await FinalizeStatus(folderId, sessionId);

        // Assert
        (await FilesIn(folderId, fileName)).Should().BeEmpty();
        status.Should().BeInRange(400, 499);
    }

    private async Task<(int Status, UploadSessionResponseDto? Result)> TryFinalize(int folderId, string sessionId)
    {
        try
        {
            var response = await _filesOperationsApi.FinalizeSessionWithHttpInfoAsync(
                folderId, sessionId, TestContext.Current.CancellationToken);

            return ((int)response.StatusCode, response.Data.Response);
        }
        catch (ApiException exception)
        {
            return (exception.ErrorCode, null);
        }
    }

    /// <summary>A success answer is only honest if the file is really there; otherwise the answer must be a client error.</summary>
    private async Task AssertFinalizeIsHonest(int folderId, string fileName, int status, UploadSessionResponseDto? result)
    {
        var stored = await FilesIn(folderId, fileName);

        if (status < 300)
        {
            stored.Should().HaveCount(1);
            result!.File.Id.Should().BePositive();
        }
        else
        {
            status.Should().BeInRange(400, 499);
            stored.Should().BeEmpty();
        }
    }
}
