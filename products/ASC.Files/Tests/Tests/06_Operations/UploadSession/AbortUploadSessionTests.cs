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
/// <c>DELETE /api/2.0/files/{folderId}/session/{sessionId}</c> (<c>abortUploadSession</c>) —
/// functional coverage: aborting an active session, one that never received any bytes, one that
/// was already finalized into a file, and one created inside a room. Access control lives in
/// <see cref="AbortUploadSessionPermissionsTests"/>; the two bugs open against this endpoint are in
/// <see cref="UploadSessionAbortBugTests"/>.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class AbortUploadSessionTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task AbortUploadSession_ActiveSession_Succeeds()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest("Autotest AbortSession.docx", 1024, createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        // Act & Assert
        await _filesOperationsApi.AbortUploadSessionAsync(session.Id, folderId, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AbortUploadSession_WithoutUpload_DoesNotCreateFile()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        const string fileName = "Autotest AbortSession NoFile.docx";

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest(fileName, 1024, createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        // Act
        await _filesOperationsApi.AbortUploadSessionAsync(session.Id, folderId, TestContext.Current.CancellationToken);

        // Assert
        var folderContent = (await _foldersApi.GetFolderByFolderIdAsync(
            folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        folderContent.Files.Should().NotContain(f => f.Title == fileName);
    }

    [Fact]
    public async Task AbortUploadSession_AlreadyFinalized_Succeeds()
    {
        // Arrange
        var folderId = await GetUserFolderIdAsync(Owner);
        const string fileName = "Autotest AbortSession Finalized.docx";

        await using var stream = new MemoryStream("test content"u8.ToArray());

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest(fileName, stream.Length, createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        await _filesOperationsApi.UploadAsyncSessionAsync(
            folderId,
            session.Id,
            1,
            new FileParameter(fileName, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", stream),
            TestContext.Current.CancellationToken);
        await _filesOperationsApi.FinalizeSessionAsync(folderId, session.Id, TestContext.Current.CancellationToken);

        // Act & Assert
        await _filesOperationsApi.AbortUploadSessionAsync(session.Id, folderId, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AbortUploadSession_InCustomRoom_Succeeds()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest AbortSession Room");

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            room.Id,
            new SessionRequest("Autotest AbortSession InRoom.docx", 1024, createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        // Act & Assert
        await _filesOperationsApi.AbortUploadSessionAsync(session.Id, room.Id, TestContext.Current.CancellationToken);
    }
}
