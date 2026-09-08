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
/// Functional coverage for <c>POST /api/2.0/files/file/{fileId}/sendeditornotify</c>: request-body
/// validation and the state a target file has to be in. Access-level coverage lives in
/// <see cref="SendEditorNotifyPermissionTests"/>.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class SendEditorNotifyTests(
    AspireAppFixture fixture)
    : SharingTestBase(fixture)
{
    private static MentionMessageWrapper BuildRequest(IEnumerable<string> emails, string message, string actionData = "test-action")
    {
        return new MentionMessageWrapper(
            actionLink: new ActionLinkConfig(new ActionConfig(actionData, "comment")),
            emails: emails.ToList(),
            message: message);
    }

    [Fact]
    public async Task SendEditorNotify_Owner_Succeeds()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Room Basic");
        var file = await CreateFile("Autotest Notify File.docx", room.Id);
        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Editing);

        var result = await _sharingApi.SendEditorNotifyAsync(
            file.Id, BuildRequest([user.Email], "Hello"), TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SendEditorNotify_EmptyEmailsArray_Returns200()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Room Empty Emails");
        var file = await CreateFile("Autotest Notify File.docx", room.Id);

        await _sharingApi.SendEditorNotifyAsync(
            file.Id, BuildRequest([], "test"), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendEditorNotify_ActionLinkDataAt256Characters_Returns200()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Room Max Data");
        var file = await CreateFile("Autotest Notify File.docx", room.Id);
        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Editing);

        await _sharingApi.SendEditorNotifyAsync(
            file.Id, BuildRequest([user.Email], "test", new string('a', 256)), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendEditorNotify_EmptyMessage_IsAccepted_Returns200()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Room Empty Message");
        var file = await CreateFile("Autotest Notify File.docx", room.Id);
        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Editing);

        await _sharingApi.SendEditorNotifyAsync(
            file.Id, BuildRequest([user.Email], ""), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendEditorNotify_MessageLongerThan255Characters_Returns400()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Room Long Message");
        var file = await CreateFile("Autotest Notify File.docx", room.Id);
        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Editing);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.SendEditorNotifyAsync(
                file.Id, BuildRequest([user.Email], new string('a', 256)), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SendEditorNotify_NonExistentFileId_Returns404()
    {
        var user = await InviteContact(EmployeeType.User);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.SendEditorNotifyAsync(
                999999999, BuildRequest([user.Email], "test"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task SendEditorNotify_FileInTrash_Returns403()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Room Trash");
        var file = await CreateFile("Autotest Notify File Trash.docx", room.Id);
        var user = await InviteContact(EmployeeType.User);

        await _filesApi.DeleteFileAsync(file.Id, new Delete { Immediately = false }, true, TestContext.Current.CancellationToken);
        await WaitLongOperation();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.SendEditorNotifyAsync(
                file.Id, BuildRequest([user.Email], "test"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SendEditorNotify_PermanentlyDeletedFile_Returns404()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Room Perm Delete");
        var file = await CreateFile("Autotest Notify File Perm.docx", room.Id);
        var user = await InviteContact(EmployeeType.User);

        await _filesApi.DeleteFileAsync(file.Id, new Delete { Immediately = true }, true, TestContext.Current.CancellationToken);
        await WaitLongOperation();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.SendEditorNotifyAsync(
                file.Id, BuildRequest([user.Email], "test"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    /// <summary>
    /// BUG 83430: <c>FileStorageService.SendEditorNotifyAsync</c> only returns
    /// <c>fileSharing.GetSharedInfoShortFileAsync</c> when a mentioned e-mail does not resolve to a
    /// registered user (or the file is encrypted); for an ordinary mentioned member it returns
    /// <c>null</c>, so the response is 200 with an empty body instead of carrying the mentioned
    /// user's name and permissions.
    /// </summary>
    [Fact]
    [Trait("Bug", "83430")]
    public async Task SendEditorNotify_MentionedUserWithRoomAccess_ResponseContainsUserAndPermissions()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Room Response Fields");
        var file = await CreateFile("Autotest Notify File.docx", room.Id);
        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Editing);

        var result = (await _sharingApi.SendEditorNotifyAsync(
            file.Id, BuildRequest([user.Email], "test"), TestContext.Current.CancellationToken));

        result.Response.Should().ContainSingle();
        var entry = result.Response[0];
        entry.User.Should().NotBeNullOrEmpty();
        entry.Permissions.Should().NotBeNullOrEmpty();
        entry.IsLink.Should().BeFalse();
    }

    /// <summary>
    /// BUG 83430: same missing-response defect - a user invited with <c>Editing</c> access should be
    /// reported as "Full Access" in the mention response, but the response is empty.
    /// </summary>
    [Fact]
    [Trait("Bug", "83430")]
    public async Task SendEditorNotify_UserWithEditingAccess_ResponseShowsFullAccess()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Room Editing");
        var file = await CreateFile("Autotest Notify File.docx", room.Id);
        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Editing);

        var result = await _sharingApi.SendEditorNotifyAsync(
            file.Id, BuildRequest([user.Email], "test"), TestContext.Current.CancellationToken);

        result.Response.Should().ContainSingle();
        result.Response[0].Permissions.Should().Be("Full Access");
    }

    /// <summary>
    /// BUG 83430: same missing-response defect - a mentioned user with no access to the room should be
    /// reported as "Deny Access", but the response is empty.
    /// </summary>
    [Fact]
    [Trait("Bug", "83430")]
    public async Task SendEditorNotify_UserWithoutRoomAccess_ResponseShowsDenyAccess()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Room Deny");
        var file = await CreateFile("Autotest Notify File.docx", room.Id);
        var outsider = await InviteContact(EmployeeType.User);

        var result = await _sharingApi.SendEditorNotifyAsync(
            file.Id, BuildRequest([outsider.Email], "test"), TestContext.Current.CancellationToken);

        result.Response.Should().ContainSingle();
        result.Response[0].Permissions.Should().Be("Deny Access");
    }

    /// <summary>
    /// BUG 83430: same missing-response defect - mentioning several e-mails should yield one response
    /// entry per mentioned user, but the response is empty.
    /// </summary>
    [Fact]
    [Trait("Bug", "83430")]
    public async Task SendEditorNotify_MultipleEmails_ReturnsEntryPerMentionedUser()
    {
        var room = await CreateCollaborationRoom("Autotest Notify Room Multi");
        var file = await CreateFile("Autotest Notify File.docx", room.Id);
        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Editing);

        var result = await _sharingApi.SendEditorNotifyAsync(
            file.Id, BuildRequest([Owner.Email, user.Email], "test"), TestContext.Current.CancellationToken);

        result.Response.Should().HaveCount(2);
        result.Count.Should().Be(2);
    }
}
