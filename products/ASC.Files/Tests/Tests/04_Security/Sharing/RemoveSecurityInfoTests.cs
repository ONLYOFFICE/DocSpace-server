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
/// <c>DELETE /api/2.0/files/share</c> (<c>RemoveSecurityInfo</c>): revoking sharing rights in
/// batch. Access control for the same endpoint lives in <see cref="RemoveSecurityInfoPermissionsTests"/>.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class RemoveSecurityInfoTests(
    AspireAppFixture fixture)
    : SharingTestBase(fixture)
{
    /// <summary>
    /// BUG 83259: RemoveSecurityInfo answered 200/true but left the sharing entry in place. Fixed by
    /// making <c>FileStorageService.RemoveAceAsync</c> actually revoke user/group shares via
    /// <c>FileSecurity.ShareAsync(None)</c> when the caller can set access.
    /// </summary>
    [Fact]
    [Trait("Bug", "83259")]
    public async Task RemoveSecurityInfo_SingleFile_RemovesSharingEntry()
    {
        var file = await CreateFileInMy("Autotest Remove Security File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        var result = (await _sharingApi.RemoveSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;
        result.Should().BeTrue();

        var securityInfos = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        securityInfos.Should().NotContain(e => e.SharedToUser != null && e.SharedToUser.Id == user.Id);
    }

    /// <summary>
    /// BUG 83259: the no-op also affected folders — the invited member kept the room share. Fixed by
    /// the <c>FileStorageService.RemoveAceAsync</c> rewrite revoking shares via
    /// <c>FileSecurity.ShareAsync(None)</c>.
    /// </summary>
    [Fact]
    [Trait("Bug", "83259")]
    public async Task RemoveSecurityInfo_Folder_RemovesSharingEntry()
    {
        var room = await CreateCollaborationRoom("Autotest Remove Security Room");
        var user = await InviteContact(EmployeeType.User);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = user.Id, Access = FileShare.Editing }]
        }, TestContext.Current.CancellationToken);

        var result = (await _sharingApi.RemoveSecurityInfoAsync(
            new BaseBatchRequestDto { FolderIds = [new(room.Id)] }, TestContext.Current.CancellationToken)).Response;
        result.Should().BeTrue();

        var securityInfos = (await _sharingApi.GetFolderSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        securityInfos.Should().NotContain(e => e.SharedToUser != null && e.SharedToUser.Id == user.Id);
    }

    /// <summary>
    /// BUG 83259: a batch call over several files removed nothing. Fixed by the
    /// <c>FileStorageService.RemoveAceAsync</c> rewrite revoking each share via
    /// <c>FileSecurity.ShareAsync(None)</c>.
    /// </summary>
    [Fact]
    [Trait("Bug", "83259")]
    public async Task RemoveSecurityInfo_MultipleFiles_RemovesAllSharingEntries()
    {
        var file1 = await CreateFileInMy("Autotest Remove Security File 1.docx", Owner);
        var file2 = await CreateFileInMy("Autotest Remove Security File 2.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file1.Id, user.Id, FileShare.Read);
        await ShareFile(file2.Id, user.Id, FileShare.Read);

        var result = (await _sharingApi.RemoveSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file1.Id), new(file2.Id)] }, TestContext.Current.CancellationToken)).Response;
        result.Should().BeTrue();

        var securityInfos1 = (await _sharingApi.GetFileSecurityInfoAsync(file1.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        securityInfos1.Should().NotContain(e => e.SharedToUser != null && e.SharedToUser.Id == user.Id);

        var securityInfos2 = (await _sharingApi.GetFileSecurityInfoAsync(file2.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        securityInfos2.Should().NotContain(e => e.SharedToUser != null && e.SharedToUser.Id == user.Id);
    }

    /// <summary>
    /// BUG 83259: a mixed file-and-folder batch removed nothing. Fixed by the
    /// <c>FileStorageService.RemoveAceAsync</c> rewrite revoking each share via
    /// <c>FileSecurity.ShareAsync(None)</c>.
    /// </summary>
    [Fact]
    [Trait("Bug", "83259")]
    public async Task RemoveSecurityInfo_FilesAndFoldersCombined_RemovesAllSharingEntries()
    {
        var file = await CreateFileInMy("Autotest Remove Security File.docx", Owner);
        var room = await CreateCollaborationRoom("Autotest Remove Security Room");
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = user.Id, Access = FileShare.Editing }]
        }, TestContext.Current.CancellationToken);

        var result = (await _sharingApi.RemoveSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)], FolderIds = [new(room.Id)] }, TestContext.Current.CancellationToken)).Response;
        result.Should().BeTrue();

        var fileSecurityInfos = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        fileSecurityInfos.Should().NotContain(e => e.SharedToUser != null && e.SharedToUser.Id == user.Id);

        var folderSecurityInfos = (await _sharingApi.GetFolderSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        folderSecurityInfos.Should().NotContain(e => e.SharedToUser != null && e.SharedToUser.Id == user.Id);
    }

    [Fact]
    public async Task RemoveSecurityInfo_UnsharedFile_ReturnsTrue()
    {
        var file = await CreateFileInMy("Autotest Remove Security Unshared File.docx", Owner);

        var result = (await _sharingApi.RemoveSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        result.Should().BeTrue();
    }

    /// <summary>
    /// BUG 83259: after the no-op removal the user still appeared in the batch security info. Fixed
    /// by the <c>FileStorageService.RemoveAceAsync</c> rewrite revoking shares via
    /// <c>FileSecurity.ShareAsync(None)</c>.
    /// </summary>
    [Fact]
    [Trait("Bug", "83259")]
    public async Task RemoveSecurityInfo_AfterRemoval_SharedUserEntryDisappearsFromBatchSecurityInfo()
    {
        var file = await CreateFileInMy("Autotest Remove Security File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);
        await _sharingApi.RemoveSecurityInfoAsync(new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().NotContain(e => e.SharedToUser != null && e.SharedToUser.Id == user.Id);
    }

    /// <summary>
    /// BUG 83259: because the removal was a no-op, the formerly shared user kept access to the file.
    /// Fixed by the <c>FileStorageService.RemoveAceAsync</c> rewrite revoking shares via
    /// <c>FileSecurity.ShareAsync(None)</c>.
    /// </summary>
    [Fact]
    [Trait("Bug", "83259")]
    public async Task RemoveSecurityInfo_FormerlySharedUser_LosesAccessAfterRemoval()
    {
        var file = await CreateFileInMy("Autotest Remove Security File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);
        await _sharingApi.RemoveSecurityInfoAsync(new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task RemoveSecurityInfo_EmptyRequestBody_Returns200()
    {
        // Sent raw: the generated client drops the Content-Type header together with the body, so a
        // bodyless typed call is refused by ASP.NET with 415 before the controller runs.
        using var request = new HttpRequestMessage(HttpMethod.Delete, "api/2.0/files/share")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        using var response = await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveSecurityInfo_NonExistentFileId_Returns200()
    {
        var result = (await _sharingApi.RemoveSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(999999999)] }, TestContext.Current.CancellationToken)).Response;

        result.Should().BeTrue();
    }
}
