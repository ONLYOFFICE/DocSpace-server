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
/// Two smaller sharing endpoints that don't warrant their own class:
/// <c>GET /api/2.0/files/file/{fileId}/group/{groupId}/share</c> (<c>GetGroupsMembersWithFileSecurity</c>)
/// and <c>POST /api/2.0/files/owner</c> (<c>ChangeFileOwner</c>).
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class GroupAndOwnerSharingTests(
    AspireAppFixture fixture)
    : SharingTestBase(fixture)
{
    [Fact]
    [Trait("Bug", "81023")]
    public async Task GetGroupsMembersWithFileSecurity_GuestSharedOnly_Returns403()
    {
        var file = await CreateFileInMy("Autotest Sharing File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(Owner);
        var group = (await _groupApi.AddGroupAsync(new GroupRequestDto([Owner.Id, user.Id], Owner.Id, "Autotest Group"), TestContext.Current.CancellationToken)).Response;

        await ShareFile(file.Id, guest.Id, FileShare.Read);

        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetGroupsMembersWithFileSecurityAsync(file.Id, group.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    [Trait("Bug", "81023")]
    public async Task GetGroupsMembersWithFileSecurity_GuestSharedWithFileAndGroup_Returns403()
    {
        var file = await CreateFileInMy("Autotest Sharing File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(Owner);
        var group = (await _groupApi.AddGroupAsync(new GroupRequestDto([Owner.Id, user.Id], Owner.Id, "Autotest Group"), TestContext.Current.CancellationToken)).Response;

        await ShareFile(file.Id, guest.Id, FileShare.Read);
        await ShareFile(file.Id, group.Id, FileShare.Read);

        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetGroupsMembersWithFileSecurityAsync(file.Id, group.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    [Trait("Bug", "66897")]
    public async Task ChangeFileOwner_NewOwnerIsDisabled_Returns403()
    {
        var file = await CreateFileInMy("Autotest Change Owner File.docx", Owner);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await TerminateUser(roomAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.ChangeFileOwnerAsync(new ChangeOwnerRequestDto(fileIds: [new(file.Id)], userId: roomAdmin.Id), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("You cannot select this user");
    }
}
