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

namespace ASC.Files.Tests.Tests._08_Privacy.ExternalLinks;

/// <summary>
/// "Deleting an item deletes the key material of its links": a deleted file, folder or private room takes the key
/// material of its links with it. A link id that still held key material would be refused for a new link, so a new
/// link that takes the id of a deleted item's link shows the material is gone.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkCleanupTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    /// <summary>
    /// The deletion runs as a background operation, so the new link is asked for until it is stored or the deadline
    /// passes; a refused attempt stores nothing.
    /// </summary>
    private async Task AssertTheIdIsFree(int fileId, Guid linkId)
    {
        var status = await PollUntil(async () =>
        {
            using var response = await PutFileLink(fileId, new LinkBody { LinkId = linkId });

            return (int)response.StatusCode;
        }, code => code == 200, TimeSpan.FromSeconds(30));

        status.Should().Be(200);
        (await FileLinkCount(fileId)).Should().Be(1);
    }

    [Fact]
    public async Task DeleteFile_DeletesTheKeyMaterialOfItsLinks()
    {
        var room = await CreateRoomAsOwner();
        var deletedFileId = await UploadEncryptedFile(room.Id, "deleted.docx");
        var otherFileId = await UploadEncryptedFile(room.Id, "other.docx");
        var linkId = await StoreFileLink(deletedFileId);

        await _filesApi.DeleteFileAsync(deletedFileId, new DeleteFileRequest(deleteAfter: true, immediately: true), cancellationToken: TestContext.Current.CancellationToken);

        await AssertTheIdIsFree(otherFileId, linkId);
    }

    [Fact]
    public async Task DeleteFolder_DeletesTheKeyMaterialOfItsLinksAndOfItsFilesLinks()
    {
        var room = await CreateRoomAsOwner();
        var folderId = await CreateFolderIn(room.Id);
        var innerFileId = await UploadEncryptedFile(folderId, "inner.docx");
        var otherFileId = await UploadEncryptedFile(room.Id, "other.docx");
        var anotherFileId = await UploadEncryptedFile(room.Id, "another.docx");
        var folderLinkId = await StoreFolderLink(folderId);
        var fileLinkId = await StoreFileLink(innerFileId);

        await _foldersApi.DeleteFolderAsync(folderId, new DeleteFolderRequest(deleteAfter: true, immediately: true), TestContext.Current.CancellationToken);

        await AssertTheIdIsFree(otherFileId, folderLinkId);
        await AssertTheIdIsFree(anotherFileId, fileLinkId);
    }

    [Fact]
    public async Task DeleteRoom_DeletesTheKeyMaterialOfItsLinks()
    {
        var deletedRoom = await CreateRoomAsOwner();
        var roomFileId = await UploadEncryptedFile(deletedRoom.Id, "inside.docx");
        var fileLinkId = await StoreFileLink(roomFileId);
        Guid roomLinkId;

        using (var stored = await PutRoomLink(deletedRoom.Id, new LinkBody()))
        {
            ((int)stored.StatusCode).Should().Be(200);
            roomLinkId = Guid.Parse((await SharedLinkOf(stored)).GetProperty("id").GetString()!);
        }

        var room = await CreateRoomAsOwner();
        var otherFileId = await UploadEncryptedFile(room.Id, "other.docx");
        var anotherFileId = await UploadEncryptedFile(room.Id, "another.docx");

        await _roomsApi.DeleteRoomAsync(deletedRoom.Id, new DeleteRoomRequest(deleteAfter: true), TestContext.Current.CancellationToken);

        await AssertTheIdIsFree(otherFileId, roomLinkId);
        await AssertTheIdIsFree(anotherFileId, fileLinkId);
    }
}
