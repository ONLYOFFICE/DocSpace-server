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

namespace ASC.Files.Tests.Tests._01_Files.Create;

/// <summary>
/// Who can create a text file, both in their own My Documents section
/// (<c>POST /files/@my/text</c>) and in a room they have been invited to
/// (<c>POST /files/:folderId/text</c>).
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class TextFileCreatePermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CreateTextFileInMyDocuments_Owner_ReturnsOk()
    {
        await _filesClient.Authenticate(Owner);

        var result = (await _filesApi.CreateTextFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest Text My Docs Owner", "Owner content", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("Autotest Text My Docs Owner.txt");
    }

    [Fact]
    public async Task CreateTextFileInMyDocuments_DocSpaceAdmin_ReturnsOk()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        var result = (await _filesApi.CreateTextFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest Text My Docs Admin", "Admin content", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("Autotest Text My Docs Admin.txt");
    }

    [Fact]
    public async Task CreateTextFileInMyDocuments_RoomAdmin_ReturnsOk()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);

        var result = (await _filesApi.CreateTextFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest Text My Docs Room Admin", "Room admin content", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("Autotest Text My Docs Room Admin.txt");
    }

    [Fact]
    public async Task CreateTextFileInMyDocuments_User_ReturnsOk()
    {
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        var result = (await _filesApi.CreateTextFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest Text My Docs User", "User content", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("Autotest Text My Docs User.txt");
    }

    [Fact]
    public async Task CreateTextFileInMyDocuments_Guest_Returns404()
    {
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CreateTextFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest Text My Docs Guest", "Guest content", createNewIfExist: true),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateTextFileInMyDocuments_Unauthenticated_Returns401()
    {
        await _filesClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CreateTextFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest Text My Docs Anon", createNewIfExist: true),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    // Catches: an unauthenticated request must be rejected before the permission check is reached.
    [Fact]
    public async Task CreateTextFile_InRoom_Unauthenticated_Returns401()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Text File Perm Room " + Guid.NewGuid().ToString()[..8]);

        await _filesClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CreateTextFileAsync(
            room.Id, new CreateTextOrHtmlFile("Autotest Text Anon", "some text"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    // Catches: a user with ContentCreator access must be able to create files in the room.
    [Fact]
    public async Task CreateTextFile_InRoom_UserWithContentCreatorAccess_ReturnsOk()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Text File Perm Room " + Guid.NewGuid().ToString()[..8]);

        var user = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.ContentCreator }],
            Notify = false
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);

        var result = (await _filesApi.CreateTextFileAsync(
            room.Id, new CreateTextOrHtmlFile("Autotest Text ContentCreator User", "some text", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        result.Id.Should().BeGreaterThan(0);
    }

    // Catches: a user with read-only access must not be able to create files (should be forbidden).
    [Fact]
    public async Task CreateTextFile_InRoom_UserWithReadOnlyAccess_Returns403()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Text File Perm Room " + Guid.NewGuid().ToString()[..8]);

        var user = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Read }],
            Notify = false
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CreateTextFileAsync(
            room.Id, new CreateTextOrHtmlFile("Autotest Text Read User", "some text"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    // Catches: a user without any access to the room must not be able to create files in it.
    [Fact]
    public async Task CreateTextFile_InRoom_UserWithoutRoomAccess_Returns403()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Text File Perm Room " + Guid.NewGuid().ToString()[..8]);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CreateTextFileAsync(
            room.Id, new CreateTextOrHtmlFile("Autotest Text No Access", "some text"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
