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

namespace ASC.Files.Tests.Tests._01_Files.FormFilling;

/// <summary>
/// Files and forms of a Form filling room by role. A Form Filler only fills: creating, uploading or copying a
/// form in, renaming, locking, versioning, deleting and moving a started form are all refused with 403. A
/// Content creator edits a started form only when they created it or started its collection.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "FormFilling")]
public class FormFillingRoomFileRolesTests(
    AspireAppFixture fixture)
    : FormFillingTestBase(fixture)
{
    [Fact]
    public async Task FormFiller_CreateFileFolderOrUploadInRoom_Returns403()
    {
        // Arrange
        var (room, _, filler) = await CreateRoomWithStartedFormAndFiller("Autotest FFR Filler Create");
        await _filesClient.Authenticate(filler);

        // Act
        var createFile = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CreateFileAsync(room.Id, new CreateFileRequest("Autotest Filler Form.pdf"), TestContext.Current.CancellationToken));
        var createFolder = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateFolderAsync(room.Id, new CreateFolderRequest("Autotest Filler Folder"), TestContext.Current.CancellationToken));
        var upload = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CreateUploadSessionInFolderAsync(
                room.Id,
                new SessionRequest("Autotest Filler Upload.pdf", 1024),
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        createFile.ErrorCode.Should().Be(403);
        createFolder.ErrorCode.Should().Be(403);
        upload.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var content = (await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        content.Files.Should().ContainSingle();
        content.Folders.Should().NotContain(f => f.Title == "Autotest Filler Folder");
    }

    [Fact]
    public async Task FormFiller_RenameLockOrMarkVersionOfStartedForm_Returns403()
    {
        // Arrange
        var (_, form, filler) = await CreateRoomWithStartedFormAndFiller("Autotest FFR Filler Edit");
        await _filesClient.Authenticate(filler);

        // Act
        var rename = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.UpdateFileAsync(form.Id, new UpdateFileRequest("Autotest Renamed by Filler.pdf"), TestContext.Current.CancellationToken));
        var lockFile = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.LockFileAsync(form.Id, new LockFileRequest(true), TestContext.Current.CancellationToken));
        var version = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.ChangeVersionHistoryAsync(form.Id, new ChangeHistoryRequest(1, false), TestContext.Current.CancellationToken));

        // Assert
        rename.ErrorCode.Should().Be(403);
        lockFile.ErrorCode.Should().Be(403);
        version.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var info = (await _filesApi.GetFileInfoAsync(form.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be(form.Title);
        info.Locked.Should().NotBe(true);
    }

    [Fact]
    public async Task FormFiller_DeleteOrMoveStartedFormOut_Returns403AndFormStays()
    {
        // Arrange
        var (room, form, filler) = await CreateRoomWithStartedFormAndFiller("Autotest FFR Filler Delete");
        var fillerMy = await GetUserFolderIdAsync(filler);
        await _filesClient.Authenticate(filler);

        // Act
        var delete = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.DeleteFileAsync(form.Id, new DeleteFileRequest { Immediately = true }, true, TestContext.Current.CancellationToken));
        var move = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
            {
                ReturnSingleOperation = true,
                DestFolderId = new(fillerMy),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(form.Id)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        // Assert
        delete.ErrorCode.Should().Be(403);
        move.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var info = (await _filesApi.GetFileInfoAsync(form.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.FolderId.Should().Be(room.Id);
    }

    [Fact]
    public async Task FormFiller_CopyOwnFormIntoRoom_Returns403()
    {
        // Arrange
        var (room, _, filler) = await CreateRoomWithStartedFormAndFiller("Autotest FFR Filler Copy In");
        await _filesClient.Authenticate(filler);
        var ownForm = await CreateFileInMy("Autotest Filler Own Form.pdf", filler);

        // Act
        var copy = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(new BatchRequestDto
            {
                ReturnSingleOperation = true,
                DestFolderId = new(room.Id),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(ownForm.Id)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        // Assert
        copy.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var content = (await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        content.Files.Should().ContainSingle();
    }

    /// <summary>
    /// The edit right on a started form, read from <c>security.Edit</c>: a Content creator does not get it on
    /// the owner's form the owner started, and does get it on a form they created and started and on the
    /// owner's form whose collection they started themselves.
    /// </summary>
    [Fact]
    public async Task ContentCreator_EditRightOnStartedForm_OnlyOnOwnOrSelfStartedForm()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest FFR Creator Edit " + Guid.NewGuid().ToString()[..8]);
        var ownersStartedByOwner = await CreateFormInRoom(room.Id, "Autotest Owner Started.pdf");
        var ownersStartedByCreator = await CreateFormInRoom(room.Id, "Autotest Creator Started.pdf");
        await StartFormFilling(ownersStartedByOwner.Id);

        var creator = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, creator, FileShare.ContentCreator);

        await _filesClient.Authenticate(creator);
        var created = await CreateFormInRoom(room.Id, "Autotest Creator Own.pdf");
        await StartFormFilling(created.Id);
        await StartFormFilling(ownersStartedByCreator.Id);

        // Act
        var onOwners = await GetFileInfo(ownersStartedByOwner.Id);
        var onOwn = await GetFileInfo(created.Id);
        var onSelfStarted = await GetFileInfo(ownersStartedByCreator.Id);

        // Assert
        onOwners.StartFilling.Should().BeTrue();
        onOwners.Security.Edit.Should().BeFalse("the owner uploaded and started this form");
        onOwn.Security.Edit.Should().BeTrue("the Content creator created and started this form");
        onSelfStarted.Security.Edit.Should().BeTrue("the Content creator started the collection of this form");
    }

    private async Task<FileDto> GetFileInfo(int fileId)
    {
        return (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    private async Task<(FolderDto Room, FileDto Form, User Filler)> CreateRoomWithStartedFormAndFiller(string title)
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom(title + " " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);
        await StartFormFilling(form.Id);

        var filler = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, filler, FileShare.FillForms);

        return (room, form, filler);
    }
}
