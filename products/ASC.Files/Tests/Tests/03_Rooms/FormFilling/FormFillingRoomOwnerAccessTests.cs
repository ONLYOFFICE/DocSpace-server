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

namespace ASC.Files.Tests.Tests._03_Rooms.FormFilling;

/// <summary>
/// <c>GET /files/file/{fileId}</c> on a PDF form of a form filling room whose filling has not been started. Such a
/// form is hidden from the room's form fillers, but the room owner keeps every right on it. The owner holds no share
/// record of their own on the room, so the record that decides their access can be a form-filling one that is not
/// theirs: the room's external link carried by the request, or a group they belong to. The form used to be hidden
/// from the owner in that case, with every action of the context menu, Delete included (bug 84849); the check that
/// hides unstarted forms from form fillers now skips the room owner.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Bug", "84849")]
public class FormFillingRoomOwnerAccessTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <summary>
    /// The portal owner creates the room and opens its external link in the same browser, still signed in: every
    /// request then carries the link token, and the form must still be fully theirs.
    /// </summary>
    [Fact]
    public async Task GetFileInfo_RoomOwnerViaRoomLink_UnstartedFormCanBeDeleted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest Owner Link Form Room");
        var formId = await UploadFormAsync(room.Id);
        var token = await GetRoomLinkTokenAsync(room.Id);

        // Act
        var form = await GetFileInfoViaLinkAsync(formId, token);

        // Assert
        form.Security.Delete.Should().BeTrue("the room owner keeps every right on a form, whatever link the request carries");
    }

    /// <summary>
    /// The room owner belongs to a group invited into the room as form fillers: the group record must not take the
    /// owner's rights away.
    /// </summary>
    [Fact]
    public async Task GetFileInfo_RoomOwnerInFormFillersGroup_UnstartedFormCanBeDeleted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest Owner Group Form Room");
        var formId = await UploadFormAsync(room.Id);

        await _peopleClient.Authenticate(Owner);
        var group = (await _groupApi.AddGroupAsync(
            new GroupRequestDto(members: [Owner.Id], groupManager: Owner.Id, groupName: Guid.NewGuid().ToString("N")[..10]),
            TestContext.Current.CancellationToken)).Response;
        await InviteGroupToRoomAsync(room.Id, group.Id, FileShare.FillForms);

        // Act
        var form = (await _filesApi.GetFileInfoAsync(formId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        form.Security.Delete.Should().BeTrue("the room owner keeps every right on a form, whatever group they are in");
    }

    /// <summary>
    /// The owner is a room administrator, not a DocSpace administrator: the owner's rights come from owning the
    /// room, not from the portal role.
    /// </summary>
    [Fact]
    public async Task GetFileInfo_RoomAdminOwnerViaRoomLink_UnstartedFormCanBeDeleted()
    {
        // Arrange
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);
        var room = await CreateFillingFormsRoom("Autotest Room Admin Link Form Room");
        var formId = await UploadFormAsync(room.Id);
        var token = await GetRoomLinkTokenAsync(room.Id);

        // Act
        var form = await GetFileInfoViaLinkAsync(formId, token);

        // Assert
        form.Security.Delete.Should().BeTrue("the room owner keeps every right on a form, whatever link the request carries");
    }

    /// <summary>
    /// A member invited as a form filler must still not reach a form whose filling has not been started.
    /// </summary>
    [Fact]
    public async Task GetFileInfo_FormFillerMember_UnstartedFormIsHidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest Filler Form Room");
        var formId = await UploadFormAsync(room.Id);

        var filler = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, filler, FileShare.FillForms);
        await _filesClient.Authenticate(filler);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileInfoAsync(formId, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// Uploads the shared <c>new.pdf</c> asset - a real ONLYOFFICE PDF form - into the room without starting its
    /// filling.
    /// </summary>
    private async Task<int> UploadFormAsync(int roomId)
    {
        await using var stream = typeof(FormFillingRoomOwnerAccessTests).Assembly.GetManifestResourceStream("ASC.Files.Tests.Data.new.pdf")!;

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            roomId,
            new SessionRequest("Autotest Owner Access.pdf", stream.Length),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        await _filesOperationsApi.UploadAsyncSessionAsync(roomId, session.Id, 1, new FileParameter(stream), TestContext.Current.CancellationToken);

        return (await _filesOperationsApi.FinalizeSessionAsync(roomId, session.Id, TestContext.Current.CancellationToken)).Response.File.Id;
    }

    private async Task<string> GetRoomLinkTokenAsync(int roomId)
    {
        return (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response.SharedLink.RequestToken;
    }

    /// <summary>
    /// Follows the browser's path for a signed-in user: the link is opened first, then the file is read with the
    /// link token on the request.
    /// </summary>
    private async Task<FileDto> GetFileInfoViaLinkAsync(int fileId, string token)
    {
        await _sharingApi.GetExternalShareDataAsync(token, cancellationToken: TestContext.Current.CancellationToken);

        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, token);

        try
        {
            return (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        }
        finally
        {
            _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        }
    }

    private async Task InviteGroupToRoomAsync(int roomId, Guid groupId, FileShare access)
    {
        await _roomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest
            {
                Invitations = [new RoomInvitation { Id = groupId, Access = access }],
                Notify = false
            },
            cancellationToken: TestContext.Current.CancellationToken);
    }
}
