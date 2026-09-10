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

namespace ASC.Files.Tests.Tests._04_Security.Sharing;

/// <summary>
/// Access control for <c>POST /api/2.0/files/file/{fileId}/sendeditornotify</c>: who may notify the
/// mentioned users of a file. Functional coverage lives in <see cref="SendEditorNotifyTests"/>.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class SendEditorNotifyPermissionTests(
    AspireAppFixture fixture)
    : SharingTestBase(fixture)
{
    private static MentionMessageWrapper BuildRequest(string email)
    {
        return new MentionMessageWrapper(
            actionLink: new ActionLinkConfig(new ActionConfig("test-action", "comment")),
            emails: [email],
            message: "test");
    }

    [Fact]
    public async Task SendEditorNotify_Owner_Succeeds()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Perm Owner Room");
        var file = await CreateFile("Autotest Notify File.docx", room.Id);
        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Editing);

        await _sharingApi.SendEditorNotifyAsync(file.Id, BuildRequest(user.Email), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendEditorNotify_RoomAdminWithEditingAccess_Succeeds()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Perm RoomAdmin Room");
        var file = await CreateFile("Autotest Notify File.docx", room.Id);

        var admin = await InviteContact(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, admin, FileShare.RoomManager);

        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Editing);

        await _filesClient.Authenticate(admin);
        await _sharingApi.SendEditorNotifyAsync(file.Id, BuildRequest(user.Email), TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
    }

    [Fact]
    public async Task SendEditorNotify_UserWithEditingAccess_Succeeds()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Perm User Editing Room");
        var file = await CreateFile("Autotest Notify File.docx", room.Id);

        var sender = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, sender, FileShare.Editing);

        await _filesClient.Authenticate(sender);
        await _sharingApi.SendEditorNotifyAsync(file.Id, BuildRequest(Owner.Email), TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
    }

    /// <remarks>
    /// The TypeScript source expects 403 for a commenter, but its setup never takes effect: it invites into an
    /// editing room, where <c>FileSecurity.AvailableRoomAccesses</c> does not offer <c>Comment</c>, so the
    /// invitation is refused ("The role is not available for this user type") and the TS client silently ignores
    /// that status — the 403 it then observes is the no-access case already covered by
    /// <see cref="SendEditorNotify_UserWithoutRoomAccess_Returns403"/>. Invited with <c>Comment</c> into a custom
    /// room, where the role is legal, the member IS allowed to send an editor notification.
    /// </remarks>
    [Fact]
    public async Task SendEditorNotify_UserWithCommentAccess_Allowed()
    {
        var room = await CreateCustomRoom("Autotest Notify Perm User Comment Room");
        var file = await CreateFile("Autotest Notify File.docx", room.Id);

        var sender = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, sender, FileShare.Comment);

        await _filesClient.Authenticate(sender);
        await _sharingApi.SendEditorNotifyAsync(file.Id, BuildRequest(Owner.Email), TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
    }

    [Fact]
    public async Task SendEditorNotify_UserWithoutRoomAccess_Returns403()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Perm User No Access Room");
        var file = await CreateFile("Autotest Notify File.docx", room.Id);

        var sender = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(sender);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.SendEditorNotifyAsync(file.Id, BuildRequest(Owner.Email), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
    }

    [Fact]
    public async Task SendEditorNotify_Guest_Returns403()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Perm Guest Room");
        var file = await CreateFile("Autotest Notify File.docx", room.Id);

        var guest = await InviteGuest();
        await InviteToRoom(room.Id, guest, FileShare.Read);

        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.SendEditorNotifyAsync(file.Id, BuildRequest(Owner.Email), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
    }

    [Fact]
    public async Task SendEditorNotify_Anonymous_Returns401()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Perm Anon Room");
        var file = await CreateFile("Autotest Notify File.docx", room.Id);
        var target = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.SendEditorNotifyAsync(file.Id, BuildRequest(target.Email), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);

        await _filesClient.Authenticate(Owner);
    }
}
