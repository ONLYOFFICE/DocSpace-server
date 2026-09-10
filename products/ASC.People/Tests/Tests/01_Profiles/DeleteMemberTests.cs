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

namespace ASC.People.Tests.Tests._01_Profiles;

/// <summary>
/// <c>DELETE /people/:userIds</c> — permanently removing a single, already-deactivated member.
/// The endpoint refuses a target that is still active, and refuses a deleter without the
/// privilege to manage the target's type.
/// </summary>
public class DeleteMemberTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    public static readonly TheoryData<EmployeeType?, EmployeeType> DeleterAndTargetType = new()
    {
        { null, EmployeeType.User },
        { null, EmployeeType.DocSpaceAdmin },
        { null, EmployeeType.RoomAdmin },
        { EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin },
        { EmployeeType.DocSpaceAdmin, EmployeeType.User },
    };

    [Theory]
    [MemberData(nameof(DeleterAndTargetType))]
    public async Task DeleteMember_ByOwnerOrAdmin_ShouldDeleteDeactivatedMember(EmployeeType? deleterType, EmployeeType targetType)
    {
        var target = await InviteContact(targetType);
        await TerminateUser(target);

        await _peopleClient.Authenticate(deleterType is null ? Owner : await InviteContact(deleterType.Value));

        var deleted = await _profilesApi.DeleteMemberAsync(target.Id.ToString(), TestContext.Current.CancellationToken);

        deleted.Links.Single().Action.Should().Be("DELETE");
        deleted.Response.Id.Should().Be(target.Id);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "80753")]
    public async Task DeleteMember_OwnerDeletesDeactivatedGuest_ShouldSucceed()
    {
        var guest = await InviteGuest();
        await TerminateUser(guest);

        var deleted = await _profilesApi.DeleteMemberAsync(guest.Id.ToString(), TestContext.Current.CancellationToken);

        deleted.Links.Single().Action.Should().Be("DELETE");
        deleted.Response.Id.Should().Be(guest.Id);
    }

    [Fact]
    public async Task DeleteMember_OwnerDeletesNonDeactivatedGuest_ShouldThrowNotSuspended()
    {
        var guest = await InviteGuest();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.DeleteMemberAsync(guest.Id.ToString(), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("not suspended");
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79560")]
    public async Task DeleteMember_DocSpaceAdminDeletesNonDeactivatedUser_ShouldThrowNotSuspended()
    {
        var user = await InviteContact(EmployeeType.User);
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(docSpaceAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.DeleteMemberAsync(user.Id.ToString(), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("not suspended");
    }

    [Fact]
    public async Task DeleteMember_DocSpaceAdminDeletesDeactivatedDocSpaceAdmin_ShouldThrowAccessDenied()
    {
        var target = await InviteContact(EmployeeType.DocSpaceAdmin);
        await TerminateUser(target);

        var deleter = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(deleter);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.DeleteMemberAsync(target.Id.ToString(), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task DeleteMember_NonExistentUser_ShouldThrowNotFound()
    {
        var target = await InviteContact(EmployeeType.RoomAdmin);
        await TerminateUser(target);
        await _peopleClient.Authenticate(Owner);
        await _profilesApi.DeleteMemberAsync(target.Id.ToString(), TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.DeleteMemberAsync(target.Id.ToString(), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().Contain("The user could not be found");
    }
}
