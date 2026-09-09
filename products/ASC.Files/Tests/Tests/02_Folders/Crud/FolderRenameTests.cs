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

namespace ASC.Files.Tests.Tests._02_Folders.Crud;

/// <summary>
/// <see cref="FolderUpdateTests"/> already covers the basic "rename in My Documents" and
/// "title too long" cases; this class covers the remaining scenarios from the TypeScript rename
/// suite so the two do not overlap.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Folders")]
public class FolderRenameTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task RenameFolder_InsideRoom_ReturnsUpdatedFolder()
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreateCustomRoom("Autotest Room For Folder Rename");
        var folder = await CreateFolder("Autotest Room Folder Before Rename", room.Id);

        var renamed = (await _foldersApi.RenameFolderAsync(
            folder.Id, new CreateFolder("Autotest Room Folder After Rename"), TestContext.Current.CancellationToken)).Response;

        renamed.Title.Should().Be("Autotest Room Folder After Rename");
        renamed.Id.Should().Be(folder.Id);
        renamed.ParentId.Should().Be(room.Id);
    }

    [Fact]
    public async Task RenameFolder_ByUser_OwnMyDocumentsSubfolder_ReturnsOk()
    {
        var user = await InviteContact(EmployeeType.User);
        var folder = await CreateFolder("Autotest User Folder Before Rename", FolderType.USER, user);

        var renamed = (await _foldersApi.RenameFolderAsync(
            folder.Id, new CreateFolder("Autotest User Folder After Rename"), TestContext.Current.CancellationToken)).Response;

        renamed.Title.Should().Be("Autotest User Folder After Rename");
        renamed.Id.Should().Be(folder.Id);
        renamed.ParentId.Should().Be(folder.ParentId);
    }

    [Fact]
    public async Task RenameFolder_PreservesIdParentIdAndMetadata()
    {
        await _filesClient.Authenticate(Owner);

        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var folder = await CreateFolder("Autotest Folder Fields Before Rename", myDocsFolderId);

        var renamed = (await _foldersApi.RenameFolderAsync(
            folder.Id, new CreateFolder("Autotest Folder Fields After Rename"), TestContext.Current.CancellationToken)).Response;

        renamed.Id.Should().Be(folder.Id);
        renamed.ParentId.Should().Be(myDocsFolderId);
        renamed.ParentId.Should().Be(folder.ParentId);
        renamed.FilesCount.Should().Be(folder.FilesCount);
        renamed.FoldersCount.Should().Be(folder.FoldersCount);
        renamed.Type.Should().Be(folder.Type);
        renamed.RootFolderType.Should().Be(folder.RootFolderType);
    }

    [Fact]
    public async Task RenameFolder_WithCyrillicAndSpecialChars_TitlePreserved()
    {
        await _filesClient.Authenticate(Owner);

        var folder = await CreateFolder("Autotest Folder Special Chars", FolderType.USER, Owner);
        const string specialTitle = "Тест Папка & Folder (2024)";

        var renamed = (await _foldersApi.RenameFolderAsync(
            folder.Id, new CreateFolder(specialTitle), TestContext.Current.CancellationToken)).Response;

        renamed.Title.Should().Be(specialTitle);
    }

    [Fact]
    public async Task RenameFolder_ToSameTitle_ReturnsOk()
    {
        await _filesClient.Authenticate(Owner);

        const string sameTitle = "Autotest Folder Same Title Rename";
        var folder = await CreateFolder(sameTitle, FolderType.USER, Owner);

        var renamed = (await _foldersApi.RenameFolderAsync(
            folder.Id, new CreateFolder(sameTitle), TestContext.Current.CancellationToken)).Response;

        renamed.Title.Should().Be(sameTitle);
    }

    [Fact]
    public async Task RenameFolder_TitlePersistsAfterGet()
    {
        await _filesClient.Authenticate(Owner);

        const string oldTitle = "Autotest Folder Before Persist Check";
        const string newTitle = "Autotest Folder After Persist Check";
        var folder = await CreateFolder(oldTitle, FolderType.USER, Owner);

        await _foldersApi.RenameFolderAsync(folder.Id, new CreateFolder(newTitle), TestContext.Current.CancellationToken);

        var reread = (await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        reread.Title.Should().Be(newTitle);
        reread.Id.Should().Be(folder.Id);
    }

    [Fact]
    public async Task RenameFolder_AppearsInParentListingWithNewTitle_OldTitleAbsent()
    {
        await _filesClient.Authenticate(Owner);

        const string oldTitle = "Autotest Folder Before Listing Check";
        const string newTitle = "Autotest Folder After Listing Check";
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var folder = await CreateFolder(oldTitle, myDocsFolderId);

        await _foldersApi.RenameFolderAsync(folder.Id, new CreateFolder(newTitle), TestContext.Current.CancellationToken);

        var content = (await _foldersApi.GetFolderByFolderIdAsync(myDocsFolderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var titles = content.Folders.Select(f => f.Title).ToList();

        titles.Should().Contain(newTitle);
        titles.Should().NotContain(oldTitle);
    }

    /// <summary>
    /// TS: "BUG 81508: PUT /api/2.0/files/folder/:folderId - non-existent folderId returns 404".
    /// </summary>
    [Trait("Bug", "81508")]
    [Fact]
    public async Task RenameFolder_NonExistentFolderId_Returns404()
    {
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.RenameFolderAsync(999999999, new CreateFolder("Autotest Rename Non-Existent Folder"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    /// <summary>
    /// BUG 81507: renaming a folder to an empty title was accepted with 200 (the TS suite carried it
    /// as a <c>test.fail</c>). Fixed by implementing <c>IValidatableObject</c> on the
    /// <c>CreateFolder</c> DTO to reject blank titles with 400.
    /// </summary>
    [Trait("Bug", "81507")]
    [Fact]
    public async Task RenameFolder_EmptyTitle_Returns400()
    {
        await _filesClient.Authenticate(Owner);

        var folder = await CreateFolder("Autotest Folder Empty Title Check", FolderType.USER, Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.RenameFolderAsync(folder.Id, new CreateFolder(""), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    /// <summary>
    /// TS: "BUG 81508: PUT /api/2.0/files/folder/:folderId - folderId 0 returns 404 or 400". Folder
    /// id 0 resolves to nothing, so per the id-resolution contract this is 404, not 400.
    /// </summary>
    [Trait("Bug", "81508")]
    [Fact]
    public async Task RenameFolder_FolderIdZero_Returns404()
    {
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.RenameFolderAsync(0, new CreateFolder("Autotest Rename FolderId Zero"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }
}
