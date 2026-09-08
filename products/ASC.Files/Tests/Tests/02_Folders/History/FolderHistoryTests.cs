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

namespace ASC.Files.Tests.Tests._02_Folders.History;

/// <summary>
/// <c>GET /api/2.0/files/folder/{folderId}/log</c> - two room-history events filed against the same
/// symptom: an action that visibly changes the room is never written to <c>FolderHistory</c>.
/// Bug 81623 (upload) is green as of 2026-09-08; bug 81640 (index export) is still open, so that
/// test is red.
/// </summary>
[Trait("Category", "Folders")]
[Trait("Feature", "History")]
public class FolderHistoryTests(
    AspireAppFixture fixture)
    : FolderHistoryTestBase(fixture)
{
    [Fact]
    [Trait("Bug", "81623")]
    public async Task GetFolderHistory_ContainsFileUploaded_AfterUploadEndpoint()
    {
        // Arrange
        var ownerDisplayName = await GetDisplayNameAsync();

        var room = await CreateCustomRoom("Autotest Folder History FileUploaded Direct");

        var before = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        before.Should().NotContain(e => e.Action != null && e.Action.Id == MessageAction.FileUploaded);

        var file = new FileParameter("Uploaded File.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", new MemoryStream("test content"u8.ToArray()));

        // Act
        await _foldersApi.UploadFileAsync(room.Id, createNewIfExist: true, file: file, cancellationToken: TestContext.Current.CancellationToken);

        var entry = await PollHistoryEntryAsync(room.Id, MessageAction.FileUploaded, TimeSpan.FromSeconds(10));

        // Assert
        entry.Should().NotBeNull("uploading a file through POST /files/{folderId}/upload must be recorded in the room history");
        entry!.Initiator.DisplayName.Should().Be(ownerDisplayName);
    }

    [Fact]
    [Trait("Bug", "81640")]
    public async Task GetFolderHistory_ContainsRoomIndexExportSaved_AfterIndexExportCompletes()
    {
        // Arrange - see RoomIndexExportTests for why My Documents has to be provisioned first.
        await GetUserFolderIdAsync(Owner);

        var ownerDisplayName = await GetDisplayNameAsync();

        var room = await CreateVirtualRoom("Autotest Folder History RoomIndexExportSaved");

        var before = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        before.Should().NotContain(e => e.Action != null && e.Action.Id == MessageAction.RoomIndexExportSaved);

        await _roomsApi.StartRoomIndexExportAsync(room.Id, TestContext.Current.CancellationToken);

        var exportDeadline = DateTime.UtcNow.AddSeconds(30);
        DocumentBuilderTaskDto export;

        while (true)
        {
            export = (await _roomsApi.GetRoomIndexExportAsync(TestContext.Current.CancellationToken)).Response;

            if (export.IsCompleted || DateTime.UtcNow >= exportDeadline)
            {
                break;
            }

            await Task.Delay(2_000, TestContext.Current.CancellationToken);
        }

        export.IsCompleted.Should().BeTrue("the room index export must complete within 30 seconds");

        // Act
        var entry = await PollHistoryEntryAsync(room.Id, MessageAction.RoomIndexExportSaved, TimeSpan.FromSeconds(60));

        // Assert: this is the open bug. The export completes (asserted above) but the
        // RoomIndexExportSaved event is never written to the room history, so `entry` stays null.
        // The product should record it, the same way FolderIndexReordered is recorded for reordering.
        entry.Should().NotBeNull("completing a room index export must be recorded in the room history");
        entry!.Initiator.DisplayName.Should().Be(ownerDisplayName);
    }
}
