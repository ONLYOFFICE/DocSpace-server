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
/// <c>POST /api/2.0/files/share</c> (<c>GetSecurityInfo</c>) - the batch sharing-rights lookup for
/// a mix of files and folders. Access control for the same endpoint lives in
/// <see cref="GetSecurityInfoPermissionsTests"/>.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class GetSecurityInfoTests(
    AspireAppFixture fixture)
    : SharingTestBase(fixture)
{
    /// <summary>
    /// BUG 80956: a guest reading security info saw other users' group memberships in
    /// <c>sharedToUser</c>. Fixed by suppressing other users' groups for Guests as well as Users in
    /// <c>EmployeeFullDto.FillGroupsAsync</c> (common/ASC.Api.Core).
    /// </summary>
    [Fact]
    [Trait("Bug", "80956")]
    public async Task GetSecurityInfo_GuestOnRoomFile_DoesNotIncludeGroupsInSharedToUser()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(Owner);
        await _groupApi.AddGroupAsync(new GroupRequestDto([Owner.Id, user.Id], Owner.Id, "Autotest Sharing Group"), TestContext.Current.CancellationToken);

        var room = await CreateCollaborationRoom("Autotest Sharing Room");
        var file = await CreateFile("Autotest Sharing File.docx", room.Id);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = guest.Id, Access = FileShare.Read }]
        }, TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(guest);
        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        // Assert
        securityInfos.Should().NotBeNull();
        var entriesWithUser = securityInfos.Where(e => e.SharedToUser != null).ToList();
        entriesWithUser.Should().NotBeEmpty();
        entriesWithUser.Should().AllSatisfy(e => e.SharedToUser.Groups.Should().BeNullOrEmpty());
    }

    [Fact]
    public async Task GetSecurityInfo_SingleFileId_ReturnsNonEmptyResponse()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSecurityInfo_SingleFolderId_ReturnsNonEmptyResponse()
    {
        var room = await CreateCollaborationRoom("Autotest Security Info Room");

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FolderIds = [new(room.Id)] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSecurityInfo_MultipleFileIds_ReturnsResponse()
    {
        var file1 = await CreateFileInMy("Autotest Security Info File 1.docx", Owner);
        var file2 = await CreateFileInMy("Autotest Security Info File 2.docx", Owner);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file1.Id), new(file2.Id)] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSecurityInfo_FileIdsAndFolderIdsCombined_ReturnsResponse()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);
        var room = await CreateCollaborationRoom("Autotest Security Info Room");

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)], FolderIds = [new(room.Id)] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSecurityInfo_OwnerEntry_HasIsOwnerTrue()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().Contain(e => e.IsOwner);
    }

    [Fact]
    public async Task GetSecurityInfo_SharedUserEntry_HasCorrectAccessLevel()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        var userEntry = securityInfos.FirstOrDefault(e => e.SharedToUser != null && e.SharedToUser.Id == user.Id);
        userEntry.Should().NotBeNull();
        userEntry!.Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task GetSecurityInfo_SharedGroupEntry_AppearsInResponse()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);
        var group = (await _groupApi.AddGroupAsync(new GroupRequestDto([user.Id], user.Id, "Autotest Group"), TestContext.Current.CancellationToken)).Response;

        await ShareFile(file.Id, group.Id, FileShare.Read);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().Contain(e => e.SharedToGroup != null && e.SharedToGroup.Id == group.Id);
    }

    [Fact]
    public async Task GetSecurityInfo_Entries_HaveBooleanControlFields()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().NotBeEmpty();
        // IsLocked, IsOwner, CanEditAccess, CanEditInternal, CanEditDenyDownload, CanEditExpirationDate and
        // CanRevoke are all required, non-nullable bool properties on FileShareDto - the owner entry
        // reaching this point at all is proof they deserialized; canRevoke=false for the owner is the one
        // value that must hold for every response.
        var ownerEntry = securityInfos.FirstOrDefault(e => e.IsOwner);
        ownerEntry.Should().NotBeNull();
        ownerEntry!.CanRevoke.Should().BeFalse();
    }

    [Fact]
    public async Task GetSecurityInfo_Entries_HaveValidSubjectType()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().NotBeEmpty();
        securityInfos.Should().AllSatisfy(e => e.SubjectType.Should().BeOneOf(
            SubjectType.User, SubjectType.ExternalLink, SubjectType.Group, SubjectType.InvitationLink, SubjectType.PrimaryExternalLink));
    }

    [Fact]
    public async Task GetSecurityInfo_UserEntry_HasSubjectTypeUser()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        var userEntry = securityInfos.FirstOrDefault(e => e.SharedToUser != null && e.SharedToUser.Id == user.Id);
        userEntry.Should().NotBeNull();
        userEntry!.SubjectType.Should().Be(SubjectType.User);
    }

    [Fact]
    public async Task GetSecurityInfo_GroupEntry_HasSubjectTypeGroup()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);
        var group = (await _groupApi.AddGroupAsync(new GroupRequestDto([user.Id], user.Id, "Autotest Group"), TestContext.Current.CancellationToken)).Response;

        await ShareFile(file.Id, group.Id, FileShare.Read);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        var groupEntry = securityInfos.FirstOrDefault(e => e.SharedToGroup != null && e.SharedToGroup.Id == group.Id);
        groupEntry.Should().NotBeNull();
        groupEntry!.SubjectType.Should().Be(SubjectType.Group);
    }

    [Fact]
    public async Task GetSecurityInfo_SharedUserEntry_CanEditAccessTrue()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        var userEntry = securityInfos.FirstOrDefault(e => e.SharedToUser != null && e.SharedToUser.Id == user.Id);
        userEntry.Should().NotBeNull();
        userEntry!.CanEditAccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetSecurityInfo_OwnerEntry_CanRevokeFalse()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        var ownerEntry = securityInfos.FirstOrDefault(e => e.IsOwner);
        ownerEntry.Should().NotBeNull();
        ownerEntry!.CanRevoke.Should().BeFalse();
    }

    [Fact]
    public async Task GetSecurityInfo_SharedToUser_ContainsIdAndDisplayName()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        var userEntry = securityInfos.FirstOrDefault(e => e.SharedToUser != null && e.SharedToUser.Id == user.Id);
        userEntry.Should().NotBeNull();
        userEntry!.SharedToUser.DisplayName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSecurityInfo_SharedToGroup_ContainsGroupId()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);
        var group = (await _groupApi.AddGroupAsync(new GroupRequestDto([user.Id], user.Id, "Autotest Group"), TestContext.Current.CancellationToken)).Response;

        await ShareFile(file.Id, group.Id, FileShare.Read);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        var groupEntry = securityInfos.FirstOrDefault(e => e.SharedToGroup != null && e.SharedToGroup.Id == group.Id);
        groupEntry.Should().NotBeNull();
        groupEntry!.SharedToGroup.Id.Should().Be(group.Id);
    }

    [Fact]
    public async Task GetSecurityInfo_AfterAccessChange_ReflectsNewLevel()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);
        await ShareFile(file.Id, user.Id, FileShare.Comment);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        var userEntry = securityInfos.FirstOrDefault(e => e.SharedToUser != null && e.SharedToUser.Id == user.Id);
        userEntry.Should().NotBeNull();
        userEntry!.Access.Should().Be(FileShare.Comment);
    }

    [Fact]
    public async Task GetSecurityInfo_MultipleSharedUsers_AllAppear()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);
        var user1 = await InviteContact(EmployeeType.User);
        var user2 = await InviteContact(EmployeeType.User);

        await _sharingApi.SetFileSecurityInfoAsync(file.Id, new SecurityInfoSimpleRequestDto
        {
            Share = [new() { ShareTo = user1.Id, Access = FileShare.Read }, new() { ShareTo = user2.Id, Access = FileShare.Comment }],
            Notify = false
        }, TestContext.Current.CancellationToken);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        var sharedUserIds = securityInfos.Where(e => e.SharedToUser != null).Select(e => e.SharedToUser.Id).ToList();
        sharedUserIds.Should().Contain(user1.Id);
        sharedUserIds.Should().Contain(user2.Id);
    }

    [Fact]
    public async Task GetSecurityInfo_RevokedAccess_NoLongerAppears()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);
        await ShareFile(file.Id, user.Id, FileShare.None);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().NotContain(e => e.SharedToUser != null && e.SharedToUser.Id == user.Id);
    }

    [Fact]
    public async Task GetSecurityInfo_NonExistentFileId_Returns200()
    {
        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(999999999)] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSecurityInfo_LockedFile_OwnerEntryIsLockedTrue()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);

        await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        var ownerEntry = securityInfos.FirstOrDefault(e => e.IsOwner);
        ownerEntry.Should().NotBeNull();
        ownerEntry!.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task GetSecurityInfo_SharedUserEntry_IsOwnerFalse()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken)).Response;

        var userEntry = securityInfos.FirstOrDefault(e => e.SharedToUser != null && e.SharedToUser.Id == user.Id);
        userEntry.Should().NotBeNull();
        userEntry!.IsOwner.Should().BeFalse();
    }

    [Fact]
    public async Task GetSecurityInfo_EmptyRequestBody_Returns200()
    {
        // Sent raw: the generated client drops the Content-Type header together with the body, so a
        // bodyless typed call is refused by ASP.NET with 415 before the controller runs.
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await _filesClient.PostAsync("api/2.0/files/share", content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSecurityInfo_EmptyFileIdsArray_ReturnsEmptyResponse()
    {
        var securityInfos = (await _sharingApi.GetSecurityInfoAsync(
            new BaseBatchRequestDto { FileIds = [] }, TestContext.Current.CancellationToken)).Response;

        securityInfos.Should().BeEmpty();
    }
}
