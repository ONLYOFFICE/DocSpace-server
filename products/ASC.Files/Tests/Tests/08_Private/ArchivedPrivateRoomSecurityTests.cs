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

namespace ASC.Files.Tests.Tests._08_Privacy;

/// <summary>
/// An archived private room keeps the protection it has while active. The Archive branch of
/// <c>FileSecurity</c> used to allow Copy to everybody and to hand a DocSpace administrator who was
/// never invited Read and Copy on the content and Move and Delete on the room. Membership is what
/// counts now: an invited administrator keeps the rights of their share.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class ArchivedPrivateRoomSecurityTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private async Task<(User Creator, FolderDto Room, FolderDto Subfolder, FileDto File)> CreateArchivedPrivateRoom(Func<int, Task>? beforeArchive = null)
    {
        var creator = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(creator);

        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
        var subfolder = (await _foldersApi.CreateFolderAsync(room.Id, new CreateFolder("Subfolder"), TestContext.Current.CancellationToken)).Response;
        var file = await UploadEncryptedFileInfo(room.Id, "Encrypted.docx");

        if (beforeArchive != null)
        {
            await beforeArchive(room.Id);
        }

        await ArchiveRoom(room.Id);

        return (creator, room, subfolder, file);
    }

    [Fact]
    public async Task NonMemberAdmin_CannotReadTheContent()
    {
        var (_, room, subfolder, file) = await CreateArchivedPrivateRoom();

        await _filesClient.Authenticate(await InviteContact(EmployeeType.DocSpaceAdmin));

        (await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)))
            .ErrorCode.Should().Be(403);
        (await Assert.ThrowsAsync<ApiException>(async () => await _foldersApi.GetFolderInfoAsync(subfolder.Id, TestContext.Current.CancellationToken)))
            .ErrorCode.Should().Be(403);
        (await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)))
            .ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task PortalOwnerNotAMember_CannotReadTheContent()
    {
        var (_, _, _, file) = await CreateArchivedPrivateRoom();

        await _filesClient.Authenticate(Owner);

        (await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)))
            .ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task NonMemberAdmin_CannotDeleteTheRoom()
    {
        var (creator, room, _, _) = await CreateArchivedPrivateRoom();

        await _filesClient.Authenticate(await InviteContact(EmployeeType.DocSpaceAdmin));

        (await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken)))
            .ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(creator);
        (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response.Id.Should().Be(room.Id);
    }

    [Fact]
    public async Task NonMemberAdmin_CannotUnarchiveTheRoom()
    {
        var (_, room, _, _) = await CreateArchivedPrivateRoom();

        await _filesClient.Authenticate(await InviteContact(EmployeeType.DocSpaceAdmin));

        (await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken)))
            .ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Creator_KeepsTheRoomButCannotCopyOutOfIt()
    {
        var (_, room, subfolder, file) = await CreateArchivedPrivateRoom();

        var fileInfo = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var folderInfo = (await _foldersApi.GetFolderInfoAsync(subfolder.Id, TestContext.Current.CancellationToken)).Response;
        var roomInfo = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        fileInfo.Security.Copy.Should().BeFalse();
        fileInfo.Security.Read.Should().BeTrue();
        folderInfo.Security.Copy.Should().BeFalse();
        roomInfo.Security.Copy.Should().BeFalse();
        roomInfo.Security.Delete.Should().BeTrue();
    }

    [Fact]
    public async Task MemberAdmin_KeepsReadButNotCopy()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        await SetFakeKeys(publicKeyPrefix: "admin");

        var (_, _, _, file) = await CreateArchivedPrivateRoom(roomId => InviteToRoom(roomId, admin, FileShare.Read));

        await _filesClient.Authenticate(admin);
        var fileInfo = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        fileInfo.Security.Read.Should().BeTrue();
        fileInfo.Security.Copy.Should().BeFalse();
    }

    [Fact]
    public async Task Member_CannotCopyAFileOutOfTheRoom()
    {
        var member = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(member);
        await SetFakeKeys(publicKeyPrefix: "member");

        var (_, _, _, file) = await CreateArchivedPrivateRoom(roomId => InviteToRoom(roomId, member, FileShare.ContentCreator));

        await _filesClient.Authenticate(member);
        var target = await CreateCustomRoom("Autotest target room");

        // Refused either up front or by the queued operation; what counts is that nothing arrives.
        await _filesOperationsApi.CopyBatchItemsAsync(new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(target.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new BatchRequestDtoAllOfFileIds(file.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        }, TestContext.Current.CancellationToken).ContinueWith(_ => { }, TestContext.Current.CancellationToken);
        await WaitLongOperation();

        var content = (await _foldersApi.GetFolderByFolderIdAsync(target.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        content.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task OrdinaryArchivedRoom_AdminBypassIsUnchanged()
    {
        var creator = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(creator);

        var room = await CreateCustomRoom("Autotest archived room");
        var file = await CreateFile("Plain.docx", room.Id);
        await ArchiveRoom(room.Id);

        await _filesClient.Authenticate(await InviteContact(EmployeeType.DocSpaceAdmin));

        var fileInfo = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var roomInfo = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        fileInfo.Security.Read.Should().BeTrue();
        fileInfo.Security.Copy.Should().BeTrue();
        roomInfo.Security.Delete.Should().BeTrue();
    }
}
