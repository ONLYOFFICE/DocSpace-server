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

namespace ASC.Files.Tests.Tests._03_Rooms;

/// <summary>
/// Verifies that the used space of form filling rooms is attributed to the dedicated "Forms"
/// root folder instead of the "Rooms" (Virtual Rooms) root folder.
///
/// Form filling rooms physically live under the VirtualRooms tree, but every counter change
/// crossing the root boundary is redirected to the Forms root folder, so the "Rooms" section
/// statistics no longer include their content while the "Forms" section reflects it.
/// </summary>
[Collection("Test Collection")]
[Trait("Category", "Rooms")]
public class FormFillingRoomUsedSpaceTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UploadFileToFillingFormsRoom_IncreasesFormsUsedSpace_NotRoomsUsedSpace()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var formRoom = await CreateFillingFormsRoom("Form Room " + Guid.NewGuid().ToString()[..8]);

        var before = await GetUsedSpaceAsync();

        // Act
        var uploadedBytes = await UploadFileAsync(formRoom.Id);

        // Assert
        uploadedBytes.Should().BePositive();

        var after = await GetUsedSpaceAsync();
        Space(after.FormsUsedSpace).Should().Be(Space(before.FormsUsedSpace) + uploadedBytes,
            "content of form filling rooms must be counted in the Forms section");
        Space(after.RoomsUsedSpace).Should().Be(Space(before.RoomsUsedSpace),
            "content of form filling rooms must not be counted in the Rooms section");
    }

    [Fact]
    public async Task UploadFileToCustomRoom_IncreasesRoomsUsedSpace_NotFormsUsedSpace()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = await CreateCustomRoom("Custom Room " + Guid.NewGuid().ToString()[..8]);

        var before = await GetUsedSpaceAsync();

        // Act
        var uploadedBytes = await UploadFileAsync(customRoom.Id);

        // Assert
        uploadedBytes.Should().BePositive();

        var after = await GetUsedSpaceAsync();
        Space(after.RoomsUsedSpace).Should().Be(Space(before.RoomsUsedSpace) + uploadedBytes,
            "content of regular rooms must be counted in the Rooms section");
        Space(after.FormsUsedSpace).Should().Be(Space(before.FormsUsedSpace),
            "content of regular rooms must not be counted in the Forms section");
    }

    [Fact]
    public async Task ArchiveAndUnarchiveFillingFormsRoom_MovesUsedSpaceBetweenFormsAndArchive()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var formRoom = await CreateFillingFormsRoom("Form Room " + Guid.NewGuid().ToString()[..8]);

        var before = await GetUsedSpaceAsync();
        var uploadedBytes = await UploadFileAsync(formRoom.Id);

        // Act - move the room to the archive
        var archiveOperation = (await _roomsApi.ArchiveRoomAsync(
            formRoom.Id,
            new ArchiveRoomRequest(deleteAfter: false),
            TestContext.Current.CancellationToken)).Response;
        await WaitLongOperation(archiveOperation.Id);

        // Assert - the used space moved from Forms to Archive
        var archived = await GetUsedSpaceAsync();
        Space(archived.FormsUsedSpace).Should().Be(Space(before.FormsUsedSpace),
            "archiving a form filling room must release the used space of the Forms section");
        Space(archived.ArchiveUsedSpace).Should().Be(Space(before.ArchiveUsedSpace) + uploadedBytes,
            "archiving a form filling room must add its used space to the Archive section");
        Space(archived.RoomsUsedSpace).Should().Be(Space(before.RoomsUsedSpace),
            "the Rooms section must not be affected by archiving a form filling room");

        // Act - restore the room from the archive
        var unarchiveOperation = (await _roomsApi.UnarchiveRoomAsync(
            formRoom.Id,
            new ArchiveRoomRequest(deleteAfter: false),
            TestContext.Current.CancellationToken)).Response;
        await WaitLongOperation(unarchiveOperation.Id);

        // Assert - the used space moved back from Archive to Forms
        var unarchived = await GetUsedSpaceAsync();
        Space(unarchived.FormsUsedSpace).Should().Be(Space(before.FormsUsedSpace) + uploadedBytes,
            "restoring a form filling room must return its used space to the Forms section");
        Space(unarchived.ArchiveUsedSpace).Should().Be(Space(before.ArchiveUsedSpace),
            "restoring a form filling room must release the used space of the Archive section");
        Space(unarchived.RoomsUsedSpace).Should().Be(Space(before.RoomsUsedSpace),
            "the Rooms section must not be affected by restoring a form filling room");
    }

    [Fact]
    public async Task DeleteFillingFormsRoom_DecreasesFormsUsedSpace()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var formRoom = await CreateFillingFormsRoom("Form Room " + Guid.NewGuid().ToString()[..8]);

        var before = await GetUsedSpaceAsync();
        await UploadFileAsync(formRoom.Id);

        // Act
        var deleteOperation = (await _roomsApi.DeleteRoomAsync(
            formRoom.Id,
            new DeleteRoomRequest(deleteAfter: false),
            TestContext.Current.CancellationToken)).Response;
        await WaitLongOperation(deleteOperation.Id);

        // Assert
        var after = await GetUsedSpaceAsync();
        Space(after.FormsUsedSpace).Should().Be(Space(before.FormsUsedSpace),
            "deleting a form filling room must release the used space of the Forms section");
        Space(after.RoomsUsedSpace).Should().Be(Space(before.RoomsUsedSpace),
            "the Rooms section must not be affected by deleting a form filling room");
    }

    private async Task<FilesStatisticsResultDto> GetUsedSpaceAsync()
    {
        return (await _foldersApi.GetFilesUsedSpaceAsync(TestContext.Current.CancellationToken)).Response;
    }

    private static long Space(FilesStatisticsFolder? folder)
    {
        return folder?.UsedSpace ?? 0;
    }

    private async Task<long> UploadFileAsync(int folderId)
    {
        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken)).Response;

        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream("ASC.Files.Tests.Data.new.pdf")!;
        var contentLength = stream.Length;

        var createdSession = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest("new.pdf", contentLength),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        var chunkSize = (int)settings.ChunkUploadSize;
        var buffer = new byte[chunkSize];
        var chunkNumber = 1;
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, chunkSize), TestContext.Current.CancellationToken)) > 0)
        {
            await using var chunkStream = new MemoryStream(buffer, 0, bytesRead);
            var fileParameter = new FileParameter(chunkStream);

            await _filesOperationsApi.UploadAsyncSessionAsync(
                folderId,
                createdSession.Id,
                chunkNumber,
                fileParameter,
                TestContext.Current.CancellationToken);

            chunkNumber++;
        }

        var resultFile = (await _filesOperationsApi.FinalizeSessionAsync(
            folderId,
            createdSession.Id,
            TestContext.Current.CancellationToken)).Response;

        resultFile.Uploaded.Should().BeTrue();

        return contentLength;
    }
}
