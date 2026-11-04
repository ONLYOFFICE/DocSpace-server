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
/// Access control and isolation of upload sessions: anonymous callers, a reader of a room, a user without
/// access to the folder and two users uploading side by side. The two open abort-access bugs
/// (82276) are in <see cref="UploadSessionAbortBugTests"/>; this class covers the cases the folder check
/// itself answers.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class UploadSessionIsolationTests(
    AspireAppFixture fixture)
    : UploadSessionTestBase(fixture)
{
    [Fact]
    public async Task UploadSession_Anonymous_ReturnsUnauthorizedAndLeavesOwnersSessionIntact()
    {
        // Arrange
        var chunkSize = await GetChunkSize();
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(chunkSize + 100, 61);
        var sessionId = await OpenSession(folderId, "Autotest Upload Anon.bin", payload.Length);
        await SendChunk(folderId, sessionId, 1, payload[..chunkSize]);
        var part = payload[chunkSize..];

        await _filesClient.Authenticate(null);

        // Act
        var create = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CreateUploadSessionInFolderAsync(
                folderId, new SessionRequest("Autotest Upload Anon New.bin", 10), TestContext.Current.CancellationToken));
        var chunk = await ChunkStatus(folderId, sessionId, 2, part);
        var sequential = await SequentialChunkStatus(folderId, sessionId, part);
        var finalize = await FinalizeStatus(folderId, sessionId);
        var abort = await AbortStatus(folderId, sessionId);

        // Assert
        create.ErrorCode.Should().Be(401);
        chunk.Should().Be(401);
        sequential.Should().Be(401);
        finalize.Should().Be(401);
        abort.Should().Be(401);

        await _filesClient.Authenticate(Owner);
        (await FilesIn(folderId, "Autotest Upload Anon")).Should().BeEmpty();

        // the owner's session is untouched and completes byte-for-byte
        await SendChunk(folderId, sessionId, 2, part);
        var done = await Finalize(folderId, sessionId);

        await ExpectContent(done.File.Id, payload);
    }

    [Fact]
    public async Task CreateUploadSession_UserWithReadAccessToRoom_ReturnsForbiddenForBothMethodsAndCreatesNothing()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Upload Read Only Room");
        var reader = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, reader, FileShare.Read);

        await _filesClient.Authenticate(reader);

        // premise: the user really is a member and can read the room
        (await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken))
            .Response.Current.Id.Should().Be(room.Id);

        // Act
        var current = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CreateUploadSessionInFolderAsync(
                room.Id, new SessionRequest("Autotest Upload Reader.bin", 64), TestContext.Current.CancellationToken));
        var (legacyStatus, _) = await CreateLegacySession(room.Id, "Autotest Upload Reader Legacy.bin", 64);

        // Assert
        current.ErrorCode.Should().Be(403);
        legacyStatus.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        (await FilesIn(room.Id, "Autotest Upload Reader")).Should().BeEmpty();
    }

    /// <remarks>
    /// The folder segment of the path is the thing the abort call checks access on (documented), unlike the
    /// chunk and finalize calls, which find the session by its id alone. Not the same as BUG 82276, where the
    /// caller does have access to the folder but not to the session.
    /// </remarks>
    [Fact]
    public async Task AbortUploadSession_UserWithoutAccessToFolder_ReturnsForbiddenAndUploadStillCompletes()
    {
        // Arrange
        var chunkSize = await GetChunkSize();
        var folderId = await GetUserFolderIdAsync(Owner);
        var payload = MakeBytes(chunkSize + 100, 62);
        var sessionId = await OpenSession(folderId, "Autotest Upload Foreign Abort.bin", payload.Length);
        await SendChunk(folderId, sessionId, 1, payload[..chunkSize]);

        var intruder = await InviteMember(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(intruder);

        // premise: the intruder has no access to the owner's My Documents
        var sanity = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken));
        sanity.ErrorCode.Should().Be(403);

        // Act
        var abort = await AbortStatus(folderId, sessionId);

        // Assert
        abort.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        await SendChunk(folderId, sessionId, 2, payload[chunkSize..]);
        var done = await Finalize(folderId, sessionId);

        await ExpectContent(done.File.Id, payload);
    }

    [Fact]
    public async Task CreateUploadSessionInFolder_UserWithoutAccessToFolder_ReturnsForbiddenAndCreatesNothing()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        var intruder = await InviteMember(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(intruder);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CreateUploadSessionInFolderAsync(
                folderId, new SessionRequest("Autotest Upload Intruder Create.bin", 64), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        (await FilesIn(folderId, "Autotest Upload Intruder")).Should().BeEmpty();
    }

    /// <remarks>
    /// The TypeScript test sends the two users' parts concurrently. One <c>HttpClient</c> carries one identity
    /// here, so the parts are interleaved instead; the parallel-session case is covered by
    /// <see cref="UploadSessionChunkTests"/>.
    /// </remarks>
    [Fact]
    public async Task UploadSession_TwoUsersInterleaved_EachFileLandsInOwnFolderByteForByte()
    {
        // Arrange
        var chunkSize = await GetChunkSize();
        var ownerFolder = await GetUserFolderIdAsync(Owner);
        var member = await InviteMember(EmployeeType.RoomAdmin);
        var memberFolder = await GetUserFolderIdAsync(member);
        memberFolder.Should().NotBe(ownerFolder);

        var total = chunkSize + 100;
        var ownerPayload = MakeBytes(total, 71);
        var memberPayload = MakeBytes(total, 72);

        await _filesClient.Authenticate(Owner);
        var ownerSession = await OpenSession(ownerFolder, "Autotest Upload Isolation Owner.bin", total);
        await _filesClient.Authenticate(member);
        var memberSession = await OpenSession(memberFolder, "Autotest Upload Isolation Member.bin", total);

        // Act
        await SendChunk(memberFolder, memberSession, 1, memberPayload[..chunkSize]);
        await _filesClient.Authenticate(Owner);
        await SendChunk(ownerFolder, ownerSession, 1, ownerPayload[..chunkSize]);
        await _filesClient.Authenticate(member);
        await SendChunk(memberFolder, memberSession, 2, memberPayload[chunkSize..]);
        await _filesClient.Authenticate(Owner);
        await SendChunk(ownerFolder, ownerSession, 2, ownerPayload[chunkSize..]);

        // the member's attempt on the owner's session changes nothing for the owner
        await _filesClient.Authenticate(member);
        (await AbortStatus(ownerFolder, ownerSession)).Should().Be(403);
        var memberDone = await Finalize(memberFolder, memberSession);
        var memberStored = await FilesIn(memberFolder, "Autotest Upload Isolation");
        await ExpectContent(memberDone.File.Id, memberPayload);

        await _filesClient.Authenticate(Owner);
        var ownerDone = await Finalize(ownerFolder, ownerSession);

        // Assert
        (await FilesIn(ownerFolder, "Autotest Upload Isolation")).Should().HaveCount(1);
        memberStored.Should().HaveCount(1);
        await ExpectContent(ownerDone.File.Id, ownerPayload);
    }

    /// <remarks>
    /// SECURITY DIAGNOSTIC, not a regression test (ported as such from the TypeScript suite). The contract
    /// documents that chunk and finalize calls find the session "by the id alone", so whoever holds the id can
    /// write into the owner's folder, while abort does check access. The test does not pin that as wanted: it
    /// records what happens in the test output and fails only if its own premises break.
    /// </remarks>
    [Fact]
    public async Task UploadSession_UserWithoutFolderAccessPresentsOwnersSessionId_RecordsObservation()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var folder = await CreateFolder("Autotest Upload Security Diag Folder", myDocs);
        var ownerBytes = MakeBytes(300, 81);
        var foreignBytes = MakeBytes(300, 82);
        var sessionId = await OpenSession(folder.Id, "Autotest Upload Diag Owner.bin", ownerBytes.Length);

        var intruder = await InviteMember(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(intruder);

        var sanity = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderByFolderIdAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken));
        sanity.ErrorCode.Should().Be(403);

        // Act
        var chunk = await ChunkStatus(folder.Id, sessionId, 1, foreignBytes);
        var finalize = await FinalizeStatus(folder.Id, sessionId);

        await _filesClient.Authenticate(Owner);
        var landed = await FilesIn(folder.Id, "Autotest Upload Diag");
        bool? landedIsIntruderBytes = landed.Count > 0
            ? Sha256(await ReadContent(landed[0].Id)) == Sha256(foreignBytes)
            : null;

        // Assert: observation only
        TestContext.Current.SendDiagnosticMessage(
            "foreign-session-observation: " + JsonSerializer.Serialize(new
            {
                intruderChunkStatus = chunk,
                intruderFinalizeStatus = finalize,
                fileLandedInOwnersFolder = landed.Count > 0,
                landedFileTitle = landed.Count > 0 ? landed[0].Title : null,
                landedContentIsIntruderBytes = landedIsIntruderBytes
            }));
    }
}
