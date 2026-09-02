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
/// <c>GET /api/2.0/files/file/{id}/share</c> (<c>GetFileSecurityInfo</c>) - the single-file sharing
/// lookup: entry shape and access levels for the owner, shared users, guests and groups. Paging,
/// 404 and lifecycle cases live in <see cref="FileSecurityInfoLifecycleTests"/>, access control in
/// <see cref="FileSecurityInfoPermissionsTests"/>. The batch equivalent
/// (<c>POST /api/2.0/files/share</c>) is covered by <see cref="GetSecurityInfoTests"/>.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class FileSecurityInfoReadTests(
    AspireAppFixture fixture)
    : FileSecurityInfoTestBase(fixture)
{
    [Fact]
    public async Task GetFileSecurityInfo_Owner_ReturnsArray()
    {
        var file = await CreateFileInMy("Autotest Security Info File.docx", Owner);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        entries.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFileSecurityInfo_NoShares_HasExactlyOneOwnerEntry()
    {
        var file = await CreateFileInMy("Autotest Security Info No Shares.docx", Owner);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        entries.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFileSecurityInfo_OwnerEntry_IsOwnerTrue()
    {
        var file = await CreateFileInMy("Autotest Security Info Owner Entry.docx", Owner);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var ownerEntry = FindUserEntry(entries, Owner.Id);
        ownerEntry.Should().NotBeNull();
        ownerEntry!.IsOwner.Should().BeTrue();
    }

    /// <summary>
    /// The TS test's title says "canEditAccess=true" but its body asserts <c>false</c> - the owner's
    /// own entry cannot have its access edited (there is no one to grant/revoke ownership to), unlike
    /// a shared user's entry. Ported to the assertion the body actually makes.
    /// </summary>
    [Fact]
    public async Task GetFileSecurityInfo_OwnerEntry_CanEditAccessFalse()
    {
        var file = await CreateFileInMy("Autotest Security Info CanEditAccess.docx", Owner);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var ownerEntry = FindUserEntry(entries, Owner.Id);
        ownerEntry.Should().NotBeNull();
        ownerEntry!.CanEditAccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetFileSecurityInfo_OwnerEntry_HasSubjectTypeUser()
    {
        var file = await CreateFileInMy("Autotest Security Info SubjectType Owner.docx", Owner);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var ownerEntry = FindUserEntry(entries, Owner.Id);
        ownerEntry.Should().NotBeNull();
        ownerEntry!.SubjectType.Should().Be(SubjectType.User);
    }

    [Theory]
    [InlineData(FileShare.Read)]
    [InlineData(FileShare.Comment)]
    [InlineData(FileShare.ReadWrite)]
    public async Task GetFileSecurityInfo_SharedUserEntry_HasCorrectAccessLevel(FileShare access)
    {
        var file = await CreateFileInMy("Autotest Security Info Shared Access.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, access);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var userEntry = FindUserEntry(entries, user.Id);
        userEntry.Should().NotBeNull();
        userEntry!.Access.Should().Be(access);
    }

    [Fact]
    public async Task GetFileSecurityInfo_SharedUserEntry_IsOwnerFalse()
    {
        var file = await CreateFileInMy("Autotest Security Info IsOwner False.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var userEntry = FindUserEntry(entries, user.Id);
        userEntry.Should().NotBeNull();
        userEntry!.IsOwner.Should().BeFalse();
    }

    [Fact]
    public async Task GetFileSecurityInfo_SharedUserEntry_HasSubjectTypeUser()
    {
        var file = await CreateFileInMy("Autotest Security Info SubjectType User.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var userEntry = FindUserEntry(entries, user.Id);
        userEntry.Should().NotBeNull();
        userEntry!.SubjectType.Should().Be(SubjectType.User);
    }

    [Fact]
    public async Task GetFileSecurityInfo_MultipleSharedUsers_AllEntriesAppear()
    {
        var file = await CreateFileInMy("Autotest Security Info Multiple Users.docx", Owner);
        var user1 = await InviteContact(EmployeeType.User);
        var user2 = await InviteContact(EmployeeType.User);

        await _sharingApi.SetFileSecurityInfoAsync(file.Id, new SecurityInfoSimpleRequestDto
        {
            Share = [new() { ShareTo = user1.Id, Access = FileShare.Read }, new() { ShareTo = user2.Id, Access = FileShare.Comment }],
            Notify = false
        }, TestContext.Current.CancellationToken);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var userIds = entries.Where(e => e.SharedToUser != null).Select(e => e.SharedToUser.Id).ToList();
        userIds.Should().Contain(user1.Id);
        userIds.Should().Contain(user2.Id);
    }

    [Fact]
    public async Task GetFileSecurityInfo_AfterAccessChange_ReflectsNewLevel()
    {
        var file = await CreateFileInMy("Autotest Security Info Access Update.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);
        await ShareFile(file.Id, user.Id, FileShare.Comment);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var userEntry = FindUserEntry(entries, user.Id);
        userEntry.Should().NotBeNull();
        userEntry!.Access.Should().Be(FileShare.Comment);
    }

    [Fact]
    public async Task GetFileSecurityInfo_RevokedAccess_EntryDisappears()
    {
        var file = await CreateFileInMy("Autotest Security Info Revoke Disappear.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);
        await ShareFile(file.Id, user.Id, FileShare.None);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        FindUserEntry(entries, user.Id).Should().BeNull();
    }

    /// <summary>
    /// The TS test's title says "canRevoke=true" but its body asserts <c>false</c> for a plain file
    /// share (as opposed to a room membership, where the room manager can revoke it). Ported to the
    /// assertion the body actually makes.
    /// </summary>
    [Fact]
    public async Task GetFileSecurityInfo_SharedUserEntry_CanRevokeFalse()
    {
        var file = await CreateFileInMy("Autotest Security Info CanRevoke.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var userEntry = FindUserEntry(entries, user.Id);
        userEntry.Should().NotBeNull();
        userEntry!.CanRevoke.Should().BeFalse();
    }

    [Fact]
    public async Task GetFileSecurityInfo_SharedToUser_ContainsUserId()
    {
        var file = await CreateFileInMy("Autotest Security Info SharedToUser Id.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var userEntry = FindUserEntry(entries, user.Id);
        userEntry.Should().NotBeNull();
        userEntry!.SharedToUser.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetFileSecurityInfo_GuestEntry_AppearsWithCorrectAccess()
    {
        var file = await CreateFileInMy("Autotest Security Info Guest Entry.docx", Owner);
        var guest = await InviteGuest();

        await ShareFile(file.Id, guest.Id, FileShare.Read);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var guestEntry = FindUserEntry(entries, guest.Id);
        guestEntry.Should().NotBeNull();
        guestEntry!.Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task GetFileSecurityInfo_FileInRoom_RoomMemberAppears()
    {
        var room = await CreateCollaborationRoom("Autotest Security Info Room Member");
        var user = await InviteContact(EmployeeType.User);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = user.Id, Access = FileShare.Editing }]
        }, TestContext.Current.CancellationToken);

        var file = await CreateFile("Autotest Security Info Room File.docx", room.Id);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var userEntry = FindUserEntry(entries, user.Id);
        userEntry.Should().NotBeNull();
        userEntry!.Access.Should().Be(FileShare.Editing);
    }

    [Fact]
    public async Task GetFileSecurityInfo_SharedWithGroup_GroupEntryHasSubjectTypeGroup()
    {
        var file = await CreateFileInMy("Autotest Security Info Group Entry.docx", Owner);
        var user = await InviteContact(EmployeeType.User);
        var group = (await _groupApi.AddGroupAsync(new GroupRequestDto([Owner.Id, user.Id], Owner.Id, "Autotest Group"), TestContext.Current.CancellationToken)).Response;

        await ShareFile(file.Id, group.Id, FileShare.Read);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var groupEntry = entries.FirstOrDefault(e => e.SharedToGroup != null && e.SharedToGroup.Id == group.Id);
        groupEntry.Should().NotBeNull();
        groupEntry!.SubjectType.Should().Be(SubjectType.Group);
        groupEntry.Access.Should().Be(FileShare.Read);
    }
}
