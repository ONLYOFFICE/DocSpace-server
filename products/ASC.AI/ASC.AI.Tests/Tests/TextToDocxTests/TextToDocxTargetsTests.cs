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

namespace ASC.AI.Tests.Tests.TextToDocxTests;

/// <summary>
/// Where an export may land: a room and its subfolder take it, an archived room and Trash refuse it. Ported from
/// messages.spec.ts, "AI Messages - text-to-docx export".
/// </summary>
[Trait("Category", "API")]
[Trait("Feature", "AI/TextToDocx")]
public class TextToDocxTargetsTests(AspireAppFixture fixture) : TextToDocxPortTestBase(fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Export_IntoCustomRoom_LandsInTheRoomOrItsSubfolder(bool intoSubfolder)
    {
        var roomId = await CreateRoomOfTypeAsync(RoomType.CustomRoom, "Autotest TextToDocx Room");
        var folderId = intoSubfolder
            ? await CreateFolderAsync(roomId, "Autotest TextToDocx Subfolder")
            : roomId;
        var label = intoSubfolder ? "subfolder" : "room";
        var title = $"Exported {label} {RandomSuffix()}";

        await ExportAcceptedAsync(title, $"The assistant said hello in the {label}.", folderId);

        (await WaitForExportedFileAsync(folderId, $"{title}.docx")).Should().NotBeNull($"no export landed in the {label}");
    }

    [Fact]
    public async Task Export_IntoArchivedRoom_IsForbidden()
    {
        var roomId = await CreateRoomOfTypeAsync(RoomType.CustomRoom, "Autotest TextToDocx Archived Room");

        // A control file, so that "the export created nothing" is a folder still holding exactly this one file
        // rather than an empty list.
        var controlId = (await FilesApi.CreateFileAsync(roomId, new CreateFileRequest("Autotest Control"), TestContext.Current.CancellationToken)).Response.Id;

        await RoomsApi.ArchiveRoomAsync(roomId, new ArchiveRoomRequest(deleteAfter: false), TestContext.Current.CancellationToken);
        await WaitForFileOperationsAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _aiExport.AiExportTextToDocxAsync(BuildExport($"Exported {RandomSuffix()}", "hello", roomId), TestContext.Current.CancellationToken));

        // An archived room is read-only for everyone, its owner included.
        exception.ErrorCode.Should().Be(403);
        ErrorOf(exception.ErrorContent?.ToString()).Should().Be("Forbidden");

        // Refused there and then, so nothing was queued that could still land: one listing is the whole check.
        (await ListFolderFilesAsync(roomId)).Select(file => file.Id).Should().Equal(controlId);
    }

    [Fact]
    public async Task Export_IntoTrash_IsForbidden()
    {
        await FilesHttpClient.Authenticate(Owner);

        var trashId = (await FoldersApi.GetTrashFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;

        // Something has to be in Trash already, or "nothing was created" cannot be told from an empty listing.
        var controlId = (await FilesApi.CreateFileInMyDocumentsAsync(new CreateFileRequest("Autotest Control"), TestContext.Current.CancellationToken)).Response.Id;
        await FilesApi.DeleteFileAsync(controlId, new DeleteFileRequest(deleteAfter: false, immediately: false), cancellationToken: TestContext.Current.CancellationToken);
        await WaitForFileOperationsAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _aiExport.AiExportTextToDocxAsync(BuildExport($"Exported {RandomSuffix()}", "hello", trashId), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        ErrorOf(exception.ErrorContent?.ToString()).Should().Be("Forbidden");

        // Refused there and then, so nothing was queued that could still land: one listing is the whole check.
        (await ListFolderFilesAsync(trashId)).Select(file => file.Id).Should().Equal(controlId);
    }
}
