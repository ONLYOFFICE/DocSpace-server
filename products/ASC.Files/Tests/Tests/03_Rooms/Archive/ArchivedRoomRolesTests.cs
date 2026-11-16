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

namespace ASC.Files.Tests.Tests._03_Rooms.Archive;

/// <summary>
/// Roles in an archived Custom room: every role still reads the room info, the member list and the history; the
/// file version history stays closed to Reviewer, Commentator and Viewer; an Editor loses edit and comment on the
/// room's files and cannot rename or lock them.
/// </summary>
[Trait("Category", "Rooms")]
public class ArchivedRoomRolesTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Theory]
    [InlineData(FileShare.RoomManager)]
    [InlineData(FileShare.ContentCreator)]
    [InlineData(FileShare.Editing)]
    [InlineData(FileShare.Review)]
    [InlineData(FileShare.Comment)]
    [InlineData(FileShare.Read)]
    public async Task ArchivedRoom_EveryRole_ReadsInfoMembersAndHistory(FileShare access)
    {
        // Arrange
        var (room, member, _) = await CreateArchivedRoomWithMember(access);

        // Act
        await _filesClient.Authenticate(member);
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var members = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.RootFolderType.Should().Be(FolderType.Archive);
        members.Should().Contain(s => s.SharedToUser != null && s.SharedToUser.Id == member.Id);
        history.Should().NotBeNull();
    }

    [Theory]
    [InlineData(FileShare.Review)]
    [InlineData(FileShare.Comment)]
    [InlineData(FileShare.Read)]
    public async Task GetEditHistory_ArchivedRoomRoleBelowEditor_Returns403(FileShare access)
    {
        // Arrange
        var (_, member, file) = await CreateArchivedRoomWithMember(access);

        // Act
        await _filesClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetEditHistoryAsync(file.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ArchivedRoom_Editor_LosesEditAndCommentAndCannotRenameOrLock()
    {
        // Arrange
        var editor = await InviteMember(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Archived Editor");
        var file = await CreateFile("Autotest Archived Editor File.docx", room.Id);
        await InviteToRoom(room.Id, editor, FileShare.Editing);

        await _filesClient.Authenticate(editor);
        var before = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.Security;
        before.Edit.Should().BeTrue();
        before.Comment.Should().BeTrue();

        await _filesClient.Authenticate(Owner);
        await ArchiveRoom(room.Id);

        // Act
        await _filesClient.Authenticate(editor);
        var after = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.Security;

        var rename = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.UpdateFileAsync(file.Id, new UpdateFileRequest { Title = "Autotest Archived Editor Renamed" }, TestContext.Current.CancellationToken));
        var lockFile = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.LockFileAsync(file.Id, new LockFileRequest(true), TestContext.Current.CancellationToken));

        // Assert
        after.Read.Should().BeTrue();
        after.Edit.Should().BeFalse();
        after.Comment.Should().BeFalse();
        after.Review.Should().BeFalse();
        after.Rename.Should().BeFalse();
        after.Delete.Should().BeFalse();
        after.Lock.Should().BeFalse();
        rename.ErrorCode.Should().Be(403);
        lockFile.ErrorCode.Should().Be(403);
    }

    private async Task<(FolderDto Room, User Member, FileDto File)> CreateArchivedRoomWithMember(FileShare access)
    {
        var member = await InviteMember(access == FileShare.RoomManager ? EmployeeType.RoomAdmin : EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Archived Role {access}");
        var file = await CreateFile($"Autotest Archived Role {access} File.docx", room.Id);
        await InviteToRoom(room.Id, member, access);
        await ArchiveRoom(room.Id);

        return (room, member, file);
    }
}
