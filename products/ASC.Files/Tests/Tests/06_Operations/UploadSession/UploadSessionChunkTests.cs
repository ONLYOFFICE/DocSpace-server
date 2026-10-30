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
/// Sending the parts of a session and finalizing it: <c>POST /files/{folderId}/session/{sessionId}</c>
/// (sequential), <c>POST /files/{folderId}/session/{sessionId}/upload</c> (numbered) and
/// <c>PUT .../finalize</c>. A payload below <c>chunkUploadSize</c> is a "small" session that takes one part;
/// a payload above it is a "large" one, and the file exists only after the last part or after finalize.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class UploadSessionChunkTests(
    AspireAppFixture fixture)
    : UploadSessionTestBase(fixture)
{
    /// <summary>Three parts of one payload, sent in order, out of order and concurrently.</summary>
    public static TheoryData<string, int[], bool> ChunkOrders => new()
    {
        { "in order 1,2,3", [1, 2, 3], false },
        { "out of order 3,1,2", [3, 1, 2], false },
        { "in parallel", [3, 1, 2], true }
    };

    [Fact]
    public async Task UploadSession_SmallPayload_Returns201AndStoresFileWithoutFinalize()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(1000, 31);
        const string fileName = "Autotest Upload Sequential Small.bin";
        var sessionId = await OpenSession(folderId, fileName, payload.Length);

        // Act
        var response = await _filesOperationsApi.UploadSessionWithHttpInfoAsync(
            folderId, sessionId, Part(payload), TestContext.Current.CancellationToken);

        // Assert
        ((int)response.StatusCode).Should().Be(201);
        var result = response.Data.Response;
        result.Uploaded.Should().BeTrue();
        result.Version.Should().Be(1);
        result.Title.Should().Be(fileName);
        result.File.PureContentLength.Should().Be(payload.Length);
        await ExpectContent(result.File.Id, payload);
        (await FilesIn(folderId, fileName)).Should().HaveCount(1);
    }

    [Fact]
    public async Task UploadSession_LargePayload_Returns200ForFirstChunkAnd201ForLastWithoutFinalize()
    {
        // Arrange
        var chunkSize = await GetChunkSize();
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(chunkSize + 100, 32);
        const string fileName = "Autotest Upload Sequential Large.bin";
        var sessionId = await OpenSession(folderId, fileName, payload.Length);

        // Act
        var first = await _filesOperationsApi.UploadSessionWithHttpInfoAsync(
            folderId, sessionId, Part(payload[..chunkSize]), TestContext.Current.CancellationToken);

        // Assert
        ((int)first.StatusCode).Should().Be(200);
        first.Data.Response.Uploaded.Should().BeFalse();
        (await FilesIn(folderId, fileName)).Should().BeEmpty();

        var last = await _filesOperationsApi.UploadSessionWithHttpInfoAsync(
            folderId, sessionId, Part(payload[chunkSize..]), TestContext.Current.CancellationToken);

        ((int)last.StatusCode).Should().Be(201);
        var result = last.Data.Response;
        result.Uploaded.Should().BeTrue();
        result.Version.Should().Be(1);
        result.File.PureContentLength.Should().Be(payload.Length);
        await ExpectContent(result.File.Id, payload);

        // the completed session is closed
        (await FinalizeStatus(folderId, sessionId)).Should().Be(404);
        (await SequentialChunkStatus(folderId, sessionId, payload[chunkSize..])).Should().Be(404);
        (await FilesIn(folderId, fileName)).Should().HaveCount(1);
    }

    [Fact]
    public async Task UploadAsyncSession_TwoChunksInOrder_StoreNothingUntilFinalize()
    {
        // Arrange
        var chunkSize = await GetChunkSize();
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(chunkSize + 100, 33);
        const string fileName = "Autotest Upload Async In Order.bin";
        var sessionId = await OpenSession(folderId, fileName, payload.Length);

        // Act
        var one = await _filesOperationsApi.UploadAsyncSessionAsync(
            folderId, sessionId, 1, Part(payload[..chunkSize]), TestContext.Current.CancellationToken);
        await SendChunk(folderId, sessionId, 2, payload[chunkSize..]);

        // Assert
        one.Response.Id.Should().Be(sessionId);
        one.Response.BytesTotal.Should().Be(payload.Length);
        (await FilesIn(folderId, fileName)).Should().BeEmpty();

        var result = await Finalize(folderId, sessionId);

        result.Uploaded.Should().BeTrue();
        result.Version.Should().Be(1);
        result.FolderId.Should().Be(folderId);
        result.Title.Should().Be(fileName);
        result.File.PureContentLength.Should().Be(payload.Length);
        await ExpectContent(result.File.Id, payload);
    }

    [Theory]
    [MemberData(nameof(ChunkOrders))]
    public async Task UploadAsyncSession_ThreeChunks_AssembleToOriginalHash(string order, int[] sequence, bool parallel)
    {
        // Arrange
        var chunkSize = await GetChunkSize();
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes((chunkSize * 2) + 777, 34);
        var sessionId = await OpenSession(folderId, $"Autotest Upload Order {order}.bin", payload.Length);

        byte[] PartOf(int number) => number switch
        {
            1 => payload[..chunkSize],
            2 => payload[chunkSize..(chunkSize * 2)],
            _ => payload[(chunkSize * 2)..]
        };

        // Act
        if (parallel)
        {
            var statuses = await Task.WhenAll(sequence.Select(n => ChunkStatus(folderId, sessionId, n, PartOf(n))));
            statuses.Should().AllBeEquivalentTo(200);
        }
        else
        {
            foreach (var number in sequence)
            {
                await SendChunk(folderId, sessionId, number, PartOf(number));
            }
        }

        var result = await Finalize(folderId, sessionId);

        // Assert
        await ExpectContent(result.File.Id, payload);
    }

    [Fact]
    public async Task UploadAsyncSession_ResendingChunkNumber_LastCopyWins()
    {
        // Arrange
        var chunkSize = await GetChunkSize();
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(chunkSize + 100, 37);
        var sessionId = await OpenSession(folderId, "Autotest Upload Resend.bin", payload.Length);

        // Act: same length, different bytes - if the first copy survived, the hash differs
        await SendChunk(folderId, sessionId, 1, MakeBytes(chunkSize, 99));
        await SendChunk(folderId, sessionId, 1, payload[..chunkSize]);
        await SendChunk(folderId, sessionId, 2, payload[chunkSize..]);

        var result = await Finalize(folderId, sessionId);

        // Assert
        await ExpectContent(result.File.Id, payload);
    }

    [Fact]
    public async Task UploadAsyncSession_TwoLargeSessionsInParallel_NeverMixTheirBytes()
    {
        // Arrange
        var chunkSize = await GetChunkSize();
        var folderId = await GetUserFolderIdAsync(Owner);
        var total = chunkSize + 100;
        var payloadA = MakeBytes(total, 41);
        var payloadB = MakeBytes(total, 42);
        var idA = await OpenSession(folderId, "Autotest Upload Parallel A.bin", total);
        var idB = await OpenSession(folderId, "Autotest Upload Parallel B.bin", total);

        // Act
        var statuses = await Task.WhenAll(
            ChunkStatus(folderId, idA, 2, payloadA[chunkSize..]),
            ChunkStatus(folderId, idB, 1, payloadB[..chunkSize]),
            ChunkStatus(folderId, idA, 1, payloadA[..chunkSize]),
            ChunkStatus(folderId, idB, 2, payloadB[chunkSize..]));

        var doneA = await Finalize(folderId, idA);
        var doneB = await Finalize(folderId, idB);

        // Assert
        statuses.Should().AllBeEquivalentTo(200);
        doneA.File.Id.Should().NotBe(doneB.File.Id);
        await ExpectContent(doneA.File.Id, payloadA);
        await ExpectContent(doneB.File.Id, payloadB);
    }

    [Fact]
    public async Task FinalizeSession_FinishedLargeSession_IsClosedAndFileStaysAtVersionOne()
    {
        // Arrange
        var chunkSize = await GetChunkSize();
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(chunkSize + 100, 43);
        const string fileName = "Autotest Upload Closed Session.bin";
        var sessionId = await OpenSession(folderId, fileName, payload.Length);

        await SendChunk(folderId, sessionId, 1, payload[..chunkSize]);
        await SendChunk(folderId, sessionId, 2, payload[chunkSize..]);
        var fileId = (await Finalize(folderId, sessionId)).File.Id;

        // Act & Assert
        (await FinalizeStatus(folderId, sessionId)).Should().Be(404);
        (await ChunkStatus(folderId, sessionId, 1, payload[..chunkSize])).Should().Be(404);
        (await AbortStatus(folderId, sessionId)).Should().Be(404);

        var stored = await FilesIn(folderId, fileName);
        stored.Should().ContainSingle().Which.Id.Should().Be(fileId);
        (await VersionsOf(fileId)).Should().Equal(1);
        await ExpectContent(fileId, payload);
    }

    /// <remarks>
    /// The JSDoc of finalize says a session that is short of bytes "assembles whatever is there"; the portal
    /// answers 400 and stores nothing instead, which is the safer behaviour. This pins what happens so that a
    /// switch to partial assembly is noticed.
    /// </remarks>
    [Fact]
    public async Task FinalizeSession_IncompleteLargeSession_ReturnsBadRequestAndStoresNoFile()
    {
        // Arrange
        var chunkSize = await GetChunkSize();
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes((chunkSize * 2) + 777, 44);
        const string fileName = "Autotest Upload Incomplete Large.bin";
        var sessionId = await OpenSession(folderId, fileName, payload.Length);

        // chunk 2 is skipped on purpose
        await SendChunk(folderId, sessionId, 1, payload[..chunkSize]);
        await SendChunk(folderId, sessionId, 3, payload[(chunkSize * 2)..]);

        // Act
        var status = await FinalizeStatus(folderId, sessionId);

        // Assert
        status.Should().Be(400);
        (await FilesIn(folderId, fileName)).Should().BeEmpty();
    }

    [Fact]
    public async Task UploadAsyncSession_PartAboveChunkUploadSize_IsRefusedAndDoesNotPoisonSession()
    {
        // Arrange
        var chunkSize = await GetChunkSize();
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(chunkSize + 100, 45);
        var sessionId = await OpenSession(folderId, "Autotest Upload Oversize.bin", payload.Length);

        // Act: the exact status is not part of the contract (the portal answers 402), only that it is not a success
        var oversize = await ChunkStatus(folderId, sessionId, 1, MakeBytes(chunkSize + 1, 5));

        // Assert
        oversize.Should().BeGreaterThanOrEqualTo(400);

        await SendChunk(folderId, sessionId, 1, payload[..chunkSize]);
        await SendChunk(folderId, sessionId, 2, payload[chunkSize..]);
        var done = await Finalize(folderId, sessionId);

        await ExpectContent(done.File.Id, payload);
    }

    [Fact]
    public async Task UploadAsyncSession_EmptyPart_DoesNotCorruptSession()
    {
        // Arrange
        var chunkSize = await GetChunkSize();
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(chunkSize + 100, 46);
        var sessionId = await OpenSession(folderId, "Autotest Upload Empty Part.bin", payload.Length);

        // Act: the contract does not say how an empty part is answered, so only what the session turns into is asserted
        await ChunkStatus(folderId, sessionId, 1, []);

        await SendChunk(folderId, sessionId, 1, payload[..chunkSize]);
        await SendChunk(folderId, sessionId, 2, payload[chunkSize..]);
        var done = await Finalize(folderId, sessionId);

        // Assert
        await ExpectContent(done.File.Id, payload);
    }
}
