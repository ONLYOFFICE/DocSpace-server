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

namespace ASC.Files.Tests.Tests._01_Files.Recent;

/// <summary>
/// A room file can be added to Recent by anyone with at least <see cref="FileShare.Read"/> access to
/// it, whatever role granted that access - the room owner, an invited member with a room role, or an
/// invited guest. Access levels below come from <c>FileSecurity.AvailableRoomAccesses</c> for
/// <see cref="RoomType.CustomRoom"/>.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Recent")]
public class RecentRoomFileAccessTests(
    AspireAppFixture fixture)
    : RecentTestBase(fixture)
{
    [Fact]
    public async Task AddFileToRecent_RoomOwnerWithoutInvitation_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Recent Owner Room Role");
        var file = await CreateFile("Autotest Recent Owner Room File.docx", room.Id);

        // Act
        var wrapper = await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        wrapper.Response.Id.Should().Be(file.Id);
        wrapper.Response.Title.Should().Be("Autotest Recent Owner Room File.docx");
        wrapper.Response.FileExst.Should().Be(".docx");
        wrapper.Response.FolderId.Should().Be(room.Id);

        var recent = await PollRecentUntil(r => r.Files.Any(f => f.Title == file.Title));
        recent.Files.Should().Contain(f => f.Title == file.Title);
    }

    [Fact]
    public async Task AddFileToRecent_DocSpaceAdminWithRoomManagerAccess_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Recent DocSpaceAdmin RoomManager");

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await InviteToRoom(room.Id, admin, FileShare.RoomManager);

        var file = await CreateFile("Autotest Recent DocSpaceAdmin File.docx", room.Id);

        // Act
        await _filesClient.Authenticate(admin);
        var wrapper = await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        wrapper.Response.Id.Should().Be(file.Id);
        wrapper.Response.Title.Should().Be("Autotest Recent DocSpaceAdmin File.docx");
        wrapper.Response.FileExst.Should().Be(".docx");
        wrapper.Response.FolderId.Should().Be(room.Id);

        var recent = await PollRecentUntil(r => r.Files.Any(f => f.Title == file.Title));
        recent.Files.Should().Contain(f => f.Title == file.Title);
    }

    [Fact]
    public async Task AddFileToRecent_RoomAdminWithEditingAccess_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Recent RoomAdmin Editing");

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.Editing);

        var file = await CreateFile("Autotest Recent RoomAdmin File.docx", room.Id);

        // Act
        await _filesClient.Authenticate(roomAdmin);
        var wrapper = await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        wrapper.Response.Id.Should().Be(file.Id);
        wrapper.Response.Title.Should().Be("Autotest Recent RoomAdmin File.docx");
        wrapper.Response.FileExst.Should().Be(".docx");
        wrapper.Response.FolderId.Should().Be(room.Id);

        var recent = await PollRecentUntil(r => r.Files.Any(f => f.Title == file.Title));
        recent.Files.Should().Contain(f => f.Title == file.Title);
    }

    [Fact]
    public async Task AddFileToRecent_UserWithCommentAccess_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Recent User Comment");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Comment);

        var file = await CreateFile("Autotest Recent User Comment File.docx", room.Id);

        // Act
        await _filesClient.Authenticate(user);
        var wrapper = await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        wrapper.Response.Id.Should().Be(file.Id);
        wrapper.Response.Title.Should().Be("Autotest Recent User Comment File.docx");
        wrapper.Response.FileExst.Should().Be(".docx");
        wrapper.Response.FolderId.Should().Be(room.Id);

        var recent = await PollRecentUntil(r => r.Files.Any(f => f.Title == file.Title));
        recent.Files.Should().Contain(f => f.Title == file.Title);
    }

    [Fact]
    public async Task AddFileToRecent_GuestWithReadAccess_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var guest = await InviteMember(EmployeeType.Guest);

        var room = await CreateCustomRoom("Autotest Recent Guest Read");
        await InviteToRoom(room.Id, guest, FileShare.Read);

        var file = await CreateFile("Autotest Recent Guest File.docx", room.Id);

        // Act
        await _filesClient.Authenticate(guest);
        var wrapper = await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        wrapper.Response.Id.Should().Be(file.Id);
        wrapper.Response.Title.Should().Be("Autotest Recent Guest File.docx");
        wrapper.Response.FileExst.Should().Be(".docx");
        wrapper.Response.FolderId.Should().Be(room.Id);

        var recent = await PollRecentUntil(r => r.Files.Any(f => f.Title == file.Title));
        recent.Files.Should().Contain(f => f.Title == file.Title);
    }
}
