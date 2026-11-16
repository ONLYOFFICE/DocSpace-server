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
/// <c>GET /files/folder/{folderId}/group/{groupId}/share</c> - the members of a group with the access each of them has
/// on a folder the group was shared with: the fields of a record, paging, and the empty answer for a group the folder
/// is not shared with.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class GroupMembersFolderSecurityTests(
    AspireAppFixture fixture)
    : SharingTestBase(fixture)
{
    [Fact]
    public async Task GetGroupsMembersWithFolderSecurity_SharedFolder_ReportsGroupAndPersonalAccess()
    {
        // Arrange
        var (folder, group, overridden, plain) = await FolderSharedWithGroup();
        await ShareFolder(folder.Id, overridden.Id, FileShare.Editing);

        // Act
        var answer = await _sharingApi.GetGroupsMembersWithFolderSecurityAsync(folder.Id, group, cancellationToken: TestContext.Current.CancellationToken);
        var members = answer.Response;

        // Assert
        members.Select(m => m.User.Id).Should().BeEquivalentTo([Owner.Id, overridden.Id, plain.Id]);
        members.Should().OnlyContain(m => m.GroupAccess == FileShare.Read);

        var owner = members.Single(m => m.User.Id == Owner.Id);
        owner.Owner.Should().BeTrue();
        owner.UserAccess.Should().Be(FileShare.RoomManager);
        owner.CanEditAccess.Should().BeFalse();

        var personal = members.Single(m => m.User.Id == overridden.Id);
        personal.UserAccess.Should().Be(FileShare.Editing);
        personal.Overridden.Should().BeTrue();
        personal.Owner.Should().BeFalse();
        personal.CanEditAccess.Should().BeTrue();

        var groupOnly = members.Single(m => m.User.Id == plain.Id);
        groupOnly.UserAccess.Should().BeNull();
        groupOnly.Overridden.Should().BeFalse();

        answer.Total.Should().Be(3);
    }

    [Fact]
    public async Task GetGroupsMembersWithFolderSecurity_CountAndStartIndex_ReturnOnePageWithTheFullTotal()
    {
        // Arrange
        var (folder, group, _, _) = await FolderSharedWithGroup();
        var all = (await _sharingApi.GetGroupsMembersWithFolderSecurityAsync(folder.Id, group, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Act
        var page = await _sharingApi.GetGroupsMembersWithFolderSecurityAsync(folder.Id, group, count: 1, startIndex: 1, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        page.Response.Should().ContainSingle();
        page.Response[0].User.Id.Should().Be(all[1].User.Id);
        page.Count.Should().Be(1);
        page.Total.Should().Be(3);
    }

    [Fact]
    public async Task GetGroupsMembersWithFolderSecurity_GroupNotSharedWithFolder_ReturnsEmptyList()
    {
        // Arrange
        var folder = await CreateFolderInMy("Autotest Group Members Unshared", Owner);
        var member = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(Owner);
        var group = (await _groupApi.AddGroupAsync(new GroupRequestDto([member.Id], Owner.Id, "Autotest Unshared Group"), TestContext.Current.CancellationToken)).Response;

        // Act
        var answer = await _sharingApi.GetGroupsMembersWithFolderSecurityAsync(folder.Id, group.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        answer.Response.Should().BeEmpty();
    }

    /// <summary>
    /// The list of a group the folder is not shared with is empty, but <c>total</c> (and <c>count</c>) report the
    /// group's size: the count query counts the group members without checking that the group has a share on the
    /// entry, while the list query stops when it has none. A paging client sees pages that never fill.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task GetGroupsMembersWithFolderSecurity_GroupNotSharedWithFolder_ReportsZeroTotal()
    {
        // Arrange
        var folder = await CreateFolderInMy("Autotest Group Members Unshared Total", Owner);
        var member = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(Owner);
        var group = (await _groupApi.AddGroupAsync(new GroupRequestDto([member.Id], Owner.Id, "Autotest Unshared Total Group"), TestContext.Current.CancellationToken)).Response;

        // Act
        var answer = await _sharingApi.GetGroupsMembersWithFolderSecurityAsync(folder.Id, group.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        answer.Response.Should().BeEmpty();
        answer.Total.Should().Be(0);
        answer.Count.Should().Be(0);
    }

    private async Task<(FolderDto Folder, Guid Group, User Overridden, User Plain)> FolderSharedWithGroup()
    {
        var folder = await CreateFolderInMy("Autotest Group Members Folder", Owner);
        var first = await InviteContact(EmployeeType.User);
        var second = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(Owner);
        var group = (await _groupApi.AddGroupAsync(
            new GroupRequestDto([Owner.Id, first.Id, second.Id], Owner.Id, "Autotest Folder Group"),
            TestContext.Current.CancellationToken)).Response;

        await ShareFolder(folder.Id, group.Id, FileShare.Read);

        return (folder, group.Id, first, second);
    }
}
